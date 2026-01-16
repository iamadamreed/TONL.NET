using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TONL.NET.SourceGenerator;

/// <summary>
/// Roslyn source generator that generates high-performance serialization code
/// for types marked with [TonlSerializable] and context classes marked with [TonlSourceGenerationOptions].
/// </summary>
[Generator]
public class TonlSourceGenerator : IIncrementalGenerator
{
    private const string TonlSerializableAttribute = "TONL.NET.TonlSerializableAttribute";
    private const string TonlSourceGenerationOptionsAttribute = "TONL.NET.TonlSourceGenerationOptionsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all types marked with [TonlSerializable] directly (not on context classes)
        // Using ForAttributeWithMetadataName is ~99% more efficient than CreateSyntaxProvider
        var serializableTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: TonlSerializableAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, ct) => ExtractTypeInfo(ctx, ct))
            .Where(static info => info is not null);

        // Register output - generate code for each marked type
        context.RegisterSourceOutput(
            serializableTypes,
            static (ctx, typeInfo) => GenerateSerializerCode(ctx, typeInfo!));

        // Find all context classes marked with [TonlSourceGenerationOptions]
        var contextClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: TonlSourceGenerationOptionsAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractContextInfo(ctx, ct))
            .Where(static info => info is not null);

        // Register output - generate code for each context class
        context.RegisterSourceOutput(
            contextClasses,
            static (ctx, contextInfo) => GenerateContextCode(ctx, contextInfo!));
    }

    private static SerializableTypeInfo? ExtractTypeInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // Skip if this is a context class (has TonlSourceGenerationOptions)
        if (typeSymbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == TonlSourceGenerationOptionsAttribute))
            return null;

        // Skip if this [TonlSerializable] has a Type argument (used on context classes)
        var attr = context.Attributes.FirstOrDefault();
        if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is ITypeSymbol)
            return null;

        // Extract attribute configuration
        var generateSerializer = true;
        var generateDeserializer = true;

        foreach (var a in context.Attributes)
        {
            foreach (var namedArg in a.NamedArguments)
            {
                if (namedArg.Key == "GenerateSerializer" && namedArg.Value.Value is bool genSer)
                    generateSerializer = genSer;
                else if (namedArg.Key == "GenerateDeserializer" && namedArg.Value.Value is bool genDeser)
                    generateDeserializer = genDeser;
            }
        }

        // Extract properties
        // For records: preserve constructor parameter order for deserialization
        // For other types: use alphabetical order for consistent output
        var publicProperties = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                        p.GetMethod != null &&
                        !p.IsIndexer &&
                        !p.IsStatic)
            .ToList();

        IEnumerable<IPropertySymbol> orderedProperties;
        if (typeSymbol.IsRecord)
        {
            // For records, use primary constructor parameter order
            var constructor = typeSymbol.Constructors
                .FirstOrDefault(c => c.Parameters.Length == publicProperties.Count &&
                                     c.Parameters.All(p => publicProperties.Any(prop =>
                                         prop.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase))));

            if (constructor != null)
            {
                // Order properties by constructor parameter position
                var paramOrder = constructor.Parameters
                    .Select((p, i) => (Name: p.Name, Index: i))
                    .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

                orderedProperties = publicProperties
                    .OrderBy(p => paramOrder.TryGetValue(p.Name, out var idx) ? idx : int.MaxValue);
            }
            else
            {
                orderedProperties = publicProperties.OrderBy(p => p.Name, StringComparer.Ordinal);
            }
        }
        else
        {
            orderedProperties = publicProperties.OrderBy(p => p.Name, StringComparer.Ordinal);
        }

        var properties = orderedProperties
            .Select(p =>
            {
                var category = GetPropertyCategory(p.Type);
                var (elementTypeName, elementCategory, elementSafePropertyName, isDictionary, keyTypeName, elementGeneratedNamespace) =
                    category == PropertyCategory.Collection
                        ? GetCollectionElementInfo(p.Type)
                        : (null, PropertyCategory.Unknown, null, false, null, null);
                var objectSafePropertyName = category == PropertyCategory.Object
                    ? GetObjectSafePropertyName(p.Type)
                    : null;
                var objectGeneratedNamespace = category == PropertyCategory.Object
                    ? GetObjectGeneratedNamespace(p.Type)
                    : null;

                return new PropertyInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.Type.NullableAnnotation == NullableAnnotation.Annotated,
                    p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public,
                    IsInitOnlyProperty(p),
                    IsRequiredProperty(p),
                    category,
                    elementTypeName,
                    elementCategory,
                    elementSafePropertyName,
                    isDictionary,
                    keyTypeName,
                    objectSafePropertyName,
                    elementGeneratedNamespace,
                    objectGeneratedNamespace);
            })
            .ToImmutableArray();

        var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        var hasInitOnlyProps = properties.Any(p => p.IsInitOnly || p.IsRequired);
        var isPrimitive = IsPrimitiveType(typeSymbol) || IsFrameworkType(typeSymbol);
        var canInstantiate = CanInstantiateType(typeSymbol);

        return new SerializableTypeInfo(
            TypeName: typeSymbol.Name,
            SafePropertyName: GetSafePropertyName(typeSymbol),
            FullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Namespace: namespaceName,
            Properties: properties,
            IsRecord: typeSymbol.IsRecord,
            IsValueType: typeSymbol.IsValueType,
            GenerateSerializer: generateSerializer,
            GenerateDeserializer: generateDeserializer,
            CanInstantiate: canInstantiate,
            IsInterface: typeSymbol.TypeKind == TypeKind.Interface,
            IsAbstract: typeSymbol.IsAbstract,
            IsPrimitive: isPrimitive,
            HasInitOnlyProperties: hasInitOnlyProps);
    }

    private static PropertyCategory GetPropertyCategory(ITypeSymbol type)
    {
        // Use FullyQualifiedFormat for consistent type name matching
        var displayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Check for nullable value types
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = ((INamedTypeSymbol)type).TypeArguments[0];
            return GetPropertyCategory(underlyingType);
        }

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => PropertyCategory.Boolean,
            SpecialType.System_Char => PropertyCategory.String, // Treat char as string for serialization
            SpecialType.System_Byte => PropertyCategory.Integer,
            SpecialType.System_SByte => PropertyCategory.Integer,
            SpecialType.System_Int16 => PropertyCategory.Integer,
            SpecialType.System_UInt16 => PropertyCategory.Integer,
            SpecialType.System_Int32 => PropertyCategory.Integer,
            SpecialType.System_UInt32 => PropertyCategory.Integer,
            SpecialType.System_Int64 => PropertyCategory.Integer,
            SpecialType.System_UInt64 => PropertyCategory.Integer,
            SpecialType.System_Single => PropertyCategory.Float,
            SpecialType.System_Double => PropertyCategory.Float,
            SpecialType.System_Decimal => PropertyCategory.Decimal,
            SpecialType.System_String => PropertyCategory.String,
            SpecialType.System_DateTime => PropertyCategory.DateTime,
            _ when displayName == "global::System.DateTimeOffset" => PropertyCategory.DateTime,
            _ when displayName == "global::System.Guid" => PropertyCategory.Guid,
            _ when displayName == "global::System.TimeSpan" => PropertyCategory.TimeSpan,
            _ when type.TypeKind == TypeKind.Enum => PropertyCategory.Enum,
            _ when IsCollectionType(type) => PropertyCategory.Collection,
            _ when type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct => PropertyCategory.Object,
            _ => PropertyCategory.Unknown
        };
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
            return true;

        var displayName = type.ToDisplayString();
        return displayName.StartsWith("global::System.Collections.", StringComparison.Ordinal) ||
               displayName.Contains("List<") ||
               displayName.Contains("Dictionary<") ||
               displayName.Contains("IEnumerable<") ||
               displayName.Contains("ICollection<");
    }

    private static bool IsDictionaryType(ITypeSymbol type)
    {
        var displayName = type.ToDisplayString();
        return displayName.Contains("Dictionary<") ||
               displayName.Contains("IDictionary<") ||
               displayName.Contains("ConcurrentDictionary<") ||
               displayName.Contains("IReadOnlyDictionary<");
    }

    private static (string? ElementTypeName, PropertyCategory ElementCategory, string? ElementSafePropertyName, bool IsDictionary, string? KeyTypeName, string? ElementGeneratedNamespace) GetCollectionElementInfo(ITypeSymbol type)
    {
        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;
            var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var elementCategory = GetPropertyCategory(elementType);
            var elementSafePropertyName = elementType is INamedTypeSymbol namedElement
                ? GetSafePropertyName(namedElement)
                : elementType.Name;
            var elementNamespace = GetGeneratedNamespace(elementType);
            return (elementTypeName, elementCategory, elementSafePropertyName, false, null, elementNamespace);
        }

        // Handle generic collections
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeArgs = namedType.TypeArguments;

            // Dictionary types have 2 type arguments
            if (IsDictionaryType(type) && typeArgs.Length == 2)
            {
                var keyType = typeArgs[0];
                var valueType = typeArgs[1];
                var valueTypeName = valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var valueCategory = GetPropertyCategory(valueType);
                var valueSafePropertyName = valueType is INamedTypeSymbol namedValue
                    ? GetSafePropertyName(namedValue)
                    : valueType.Name;
                var keyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var valueNamespace = GetGeneratedNamespace(valueType);
                return (valueTypeName, valueCategory, valueSafePropertyName, true, keyTypeName, valueNamespace);
            }

            // Other collections (List<T>, IEnumerable<T>, etc.) have 1 type argument
            if (typeArgs.Length >= 1)
            {
                var elementType = typeArgs[0];
                var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var elementCategory = GetPropertyCategory(elementType);
                var elementSafePropertyName = elementType is INamedTypeSymbol namedElement
                    ? GetSafePropertyName(namedElement)
                    : elementType.Name;
                var elementNamespace = GetGeneratedNamespace(elementType);
                return (elementTypeName, elementCategory, elementSafePropertyName, false, null, elementNamespace);
            }
        }

        return (null, PropertyCategory.Unknown, null, false, null, null);
    }

    private static string? GetObjectSafePropertyName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
            return GetSafePropertyName(namedType);
        return type.Name;
    }

    private static string? GetGeneratedNamespace(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            var ns = namedType.ContainingNamespace;
            if (ns.IsGlobalNamespace)
                return "global::TONL.NET.Generated";
            return $"global::{ns.ToDisplayString()}.Generated";
        }
        return "global::TONL.NET.Generated";
    }

    private static string? GetObjectGeneratedNamespace(ITypeSymbol type)
    {
        return GetGeneratedNamespace(type);
    }

    /// <summary>
    /// Checks if the type is a built-in primitive that should be handled by core serialization.
    /// </summary>
    private static bool IsPrimitiveType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_Byte => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Decimal => true,
            SpecialType.System_String => true,
            SpecialType.System_DateTime => true,
            SpecialType.System_Object => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a type is a common framework type that should be handled by core serialization.
    /// </summary>
    private static bool IsFrameworkType(ITypeSymbol type)
    {
        var displayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return displayName is "global::System.DateTimeOffset"
            or "global::System.Guid"
            or "global::System.TimeSpan"
            or "global::System.Uri";
    }

    /// <summary>
    /// Checks if a type can be instantiated with a parameterless constructor.
    /// </summary>
    private static bool CanInstantiateType(INamedTypeSymbol typeSymbol)
    {
        // Cannot instantiate interfaces or abstract classes
        if (typeSymbol.TypeKind == TypeKind.Interface || typeSymbol.IsAbstract)
            return false;

        // Cannot instantiate primitives or framework types with new T()
        if (IsPrimitiveType(typeSymbol) || IsFrameworkType(typeSymbol))
            return false;

        // Structs always have a parameterless constructor
        if (typeSymbol.IsValueType)
            return true;

        // Check for a public parameterless constructor
        return typeSymbol.Constructors.Any(c =>
            c.DeclaredAccessibility == Accessibility.Public &&
            c.Parameters.Length == 0);
    }

    /// <summary>
    /// Checks if a property has an init-only setter.
    /// </summary>
    private static bool IsInitOnlyProperty(IPropertySymbol property)
    {
        if (property.SetMethod == null)
            return false;

        return property.SetMethod.IsInitOnly;
    }

    /// <summary>
    /// Checks if a property is required (C# 11+ required modifier).
    /// </summary>
    private static bool IsRequiredProperty(IPropertySymbol property)
    {
        return property.IsRequired;
    }

    /// <summary>
    /// Creates a valid C# identifier from a type symbol.
    /// Handles generic types by concatenating type arguments.
    /// Example: List&lt;User&gt; -> ListOfUser, Dictionary&lt;string, int&gt; -> DictionaryOfStringAndInt
    /// </summary>
    private static string GetSafePropertyName(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
            return typeSymbol.Name;

        var sb = new StringBuilder();
        sb.Append(typeSymbol.Name);
        sb.Append("Of");

        var args = typeSymbol.TypeArguments;
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0)
                sb.Append("And");

            if (args[i] is INamedTypeSymbol namedArg)
                sb.Append(GetSafePropertyName(namedArg));
            else if (args[i] is IArrayTypeSymbol arrayArg)
                sb.Append(GetSafeArrayName(arrayArg));
            else
                sb.Append(args[i].Name);
        }

        return sb.ToString();
    }

    private static string GetSafeArrayName(IArrayTypeSymbol arrayType)
    {
        if (arrayType.ElementType is INamedTypeSymbol namedElement)
            return GetSafePropertyName(namedElement) + "Array";
        return arrayType.ElementType.Name + "Array";
    }

    private static void GenerateSerializerCode(
        SourceProductionContext context,
        SerializableTypeInfo typeInfo)
    {
        var code = CodeGenerator.GenerateSerializer(typeInfo);
        var hintName = $"{typeInfo.SafePropertyName}.Tonl.g.cs";

        context.AddSource(hintName, SourceText.From(code, Encoding.UTF8));
    }

    private static ContextInfo? ExtractContextInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol contextSymbol)
            return null;

        // Get generation mode from [TonlSourceGenerationOptions]
        var optionsAttr = context.Attributes.FirstOrDefault();
        var generationMode = TonlSourceGenerationMode.Default;

        if (optionsAttr != null)
        {
            foreach (var namedArg in optionsAttr.NamedArguments)
            {
                if (namedArg.Key == "GenerationMode" && namedArg.Value.Value is int mode)
                    generationMode = (TonlSourceGenerationMode)mode;
            }
        }

        // Collect all [TonlSerializable(typeof(T))] attributes
        var types = new List<SerializableTypeInfo>();
        foreach (var attr in contextSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == TonlSerializableAttribute &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol targetType)
            {
                var typeInfo = ExtractTypeInfoFromSymbol(targetType, attr);
                types.Add(typeInfo);
            }
        }

        if (types.Count == 0)
            return null;

        var namespaceName = contextSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : contextSymbol.ContainingNamespace.ToDisplayString();

        return new ContextInfo(
            ContextName: contextSymbol.Name,
            FullyQualifiedName: contextSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Namespace: namespaceName,
            GenerationMode: generationMode,
            Types: types.ToImmutableArray());
    }

    private static SerializableTypeInfo ExtractTypeInfoFromSymbol(INamedTypeSymbol typeSymbol, AttributeData? attr)
    {
        var generateSerializer = true;
        var generateDeserializer = true;

        if (attr != null)
        {
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "GenerateSerializer" && namedArg.Value.Value is bool genSer)
                    generateSerializer = genSer;
                else if (namedArg.Key == "GenerateDeserializer" && namedArg.Value.Value is bool genDeser)
                    generateDeserializer = genDeser;
            }
        }

        var publicProperties = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                        p.GetMethod != null &&
                        !p.IsIndexer &&
                        !p.IsStatic)
            .ToList();

        IEnumerable<IPropertySymbol> orderedProperties;
        if (typeSymbol.IsRecord)
        {
            var constructor = typeSymbol.Constructors
                .FirstOrDefault(c => c.Parameters.Length == publicProperties.Count &&
                                     c.Parameters.All(p => publicProperties.Any(prop =>
                                         prop.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase))));

            if (constructor != null)
            {
                var paramOrder = constructor.Parameters
                    .Select((p, i) => (Name: p.Name, Index: i))
                    .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

                orderedProperties = publicProperties
                    .OrderBy(p => paramOrder.TryGetValue(p.Name, out var idx) ? idx : int.MaxValue);
            }
            else
            {
                orderedProperties = publicProperties.OrderBy(p => p.Name, StringComparer.Ordinal);
            }
        }
        else
        {
            orderedProperties = publicProperties.OrderBy(p => p.Name, StringComparer.Ordinal);
        }

        var properties = orderedProperties
            .Select(p =>
            {
                var category = GetPropertyCategory(p.Type);
                var (elementTypeName, elementCategory, elementSafePropertyName, isDictionary, keyTypeName, elementGeneratedNamespace) =
                    category == PropertyCategory.Collection
                        ? GetCollectionElementInfo(p.Type)
                        : (null, PropertyCategory.Unknown, null, false, null, null);
                var objectSafePropertyName = category == PropertyCategory.Object
                    ? GetObjectSafePropertyName(p.Type)
                    : null;
                var objectGeneratedNamespace = category == PropertyCategory.Object
                    ? GetObjectGeneratedNamespace(p.Type)
                    : null;

                return new PropertyInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.Type.NullableAnnotation == NullableAnnotation.Annotated,
                    p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public,
                    IsInitOnlyProperty(p),
                    IsRequiredProperty(p),
                    category,
                    elementTypeName,
                    elementCategory,
                    elementSafePropertyName,
                    isDictionary,
                    keyTypeName,
                    objectSafePropertyName,
                    elementGeneratedNamespace,
                    objectGeneratedNamespace);
            })
            .ToImmutableArray();

        var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        var hasInitOnlyProps = properties.Any(p => p.IsInitOnly || p.IsRequired);
        var isPrimitive = IsPrimitiveType(typeSymbol) || IsFrameworkType(typeSymbol);
        var canInstantiate = CanInstantiateType(typeSymbol);

        return new SerializableTypeInfo(
            TypeName: typeSymbol.Name,
            SafePropertyName: GetSafePropertyName(typeSymbol),
            FullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Namespace: namespaceName,
            Properties: properties,
            IsRecord: typeSymbol.IsRecord,
            IsValueType: typeSymbol.IsValueType,
            GenerateSerializer: generateSerializer,
            GenerateDeserializer: generateDeserializer,
            CanInstantiate: canInstantiate,
            IsInterface: typeSymbol.TypeKind == TypeKind.Interface,
            IsAbstract: typeSymbol.IsAbstract,
            IsPrimitive: isPrimitive,
            HasInitOnlyProperties: hasInitOnlyProps);
    }

    private static void GenerateContextCode(
        SourceProductionContext context,
        ContextInfo contextInfo)
    {
        // Generate individual serializer files for each registered type
        // This is required because the context code references {Type}TonlSerializer classes
        // Generate for all concrete types (not primitives, interfaces, or abstract classes)
        // Even types without parameterless constructors need serializers for PropertyNames, WriteRow, etc.
        foreach (var typeInfo in contextInfo.Types)
        {
            if (!typeInfo.IsPrimitive && !typeInfo.IsInterface && !typeInfo.IsAbstract)
            {
                var serializerCode = CodeGenerator.GenerateSerializer(typeInfo);
                var serializerHintName = $"{typeInfo.SafePropertyName}.Tonl.g.cs";
                context.AddSource(serializerHintName, SourceText.From(serializerCode, Encoding.UTF8));
            }
        }

        // Generate the context partial class
        var code = CodeGenerator.GenerateContext(contextInfo);
        var hintName = $"{contextInfo.ContextName}.Tonl.g.cs";

        context.AddSource(hintName, SourceText.From(code, Encoding.UTF8));
    }
}

/// <summary>
/// Extracted information about a serializable type.
/// Using record for structural equality (required for incremental generator caching).
/// </summary>
internal sealed record SerializableTypeInfo(
    string TypeName,
    string SafePropertyName,
    string FullyQualifiedName,
    string? Namespace,
    ImmutableArray<PropertyInfo> Properties,
    bool IsRecord,
    bool IsValueType,
    bool GenerateSerializer,
    bool GenerateDeserializer,
    bool CanInstantiate,
    bool IsInterface,
    bool IsAbstract,
    bool IsPrimitive,
    bool HasInitOnlyProperties);

/// <summary>
/// Information about a property to serialize.
/// </summary>
internal sealed record PropertyInfo(
    string Name,
    string TypeName,
    bool IsNullable,
    bool HasPublicSetter,
    bool IsInitOnly,
    bool IsRequired,
    PropertyCategory Category,
    // Collection/Object type information
    string? ElementTypeName = null,           // Element type for collections (e.g., "int" for List<int>)
    PropertyCategory ElementCategory = PropertyCategory.Unknown,  // Category of element type
    string? ElementSafePropertyName = null,   // Safe property name for context lookup (e.g., "OrderItem")
    bool IsDictionary = false,                // True for Dictionary<K,V> types
    string? KeyTypeName = null,               // Key type for dictionaries
    string? ObjectSafePropertyName = null,    // Safe property name for nested object context lookup
    string? ElementGeneratedNamespace = null, // Generated namespace for element type serializer
    string? ObjectGeneratedNamespace = null); // Generated namespace for nested object serializer

/// <summary>
/// Categories of property types for serialization dispatch.
/// </summary>
internal enum PropertyCategory
{
    Unknown,
    Boolean,
    Integer,
    Float,
    Decimal,
    String,
    DateTime,
    Guid,
    TimeSpan,
    Enum,
    Collection,
    Object
}

/// <summary>
/// Information about a serializer context class.
/// </summary>
internal sealed record ContextInfo(
    string ContextName,
    string FullyQualifiedName,
    string? Namespace,
    TonlSourceGenerationMode GenerationMode,
    ImmutableArray<SerializableTypeInfo> Types);

/// <summary>
/// Mirrors TONL.NET.TonlSourceGenerationMode for source generator.
/// </summary>
internal enum TonlSourceGenerationMode
{
    Default = 0,
    Metadata = 1,
    Serialization = 2
}
