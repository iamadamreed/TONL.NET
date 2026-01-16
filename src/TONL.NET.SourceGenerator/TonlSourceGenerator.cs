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
/// for types marked with [TonlSerializable].
/// </summary>
[Generator]
public class TonlSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all types marked with [TonlSerializable]
        // Using ForAttributeWithMetadataName is ~99% more efficient than CreateSyntaxProvider
        var serializableTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "TONL.NET.TonlSerializableAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, ct) => ExtractTypeInfo(ctx, ct))
            .Where(static info => info is not null);

        // Register output - generate code for each marked type
        context.RegisterSourceOutput(
            serializableTypes,
            static (ctx, typeInfo) => GenerateSerializerCode(ctx, typeInfo!));
    }

    private static SerializableTypeInfo? ExtractTypeInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // Extract attribute configuration
        var generateSerializer = true;
        var generateDeserializer = true;

        foreach (var attr in context.Attributes)
        {
            foreach (var namedArg in attr.NamedArguments)
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
            .Select(p => new PropertyInfo(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                p.Type.NullableAnnotation == NullableAnnotation.Annotated,
                p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public,
                GetPropertyCategory(p.Type)))
            .ToImmutableArray();

        var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return new SerializableTypeInfo(
            TypeName: typeSymbol.Name,
            FullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Namespace: namespaceName,
            Properties: properties,
            IsRecord: typeSymbol.IsRecord,
            IsValueType: typeSymbol.IsValueType,
            GenerateSerializer: generateSerializer,
            GenerateDeserializer: generateDeserializer);
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

    private static void GenerateSerializerCode(
        SourceProductionContext context,
        SerializableTypeInfo typeInfo)
    {
        var code = CodeGenerator.GenerateSerializer(typeInfo);
        var hintName = $"{typeInfo.TypeName}.Tonl.g.cs";

        context.AddSource(hintName, SourceText.From(code, Encoding.UTF8));
    }
}

/// <summary>
/// Extracted information about a serializable type.
/// Using record for structural equality (required for incremental generator caching).
/// </summary>
internal sealed record SerializableTypeInfo(
    string TypeName,
    string FullyQualifiedName,
    string? Namespace,
    ImmutableArray<PropertyInfo> Properties,
    bool IsRecord,
    bool IsValueType,
    bool GenerateSerializer,
    bool GenerateDeserializer);

/// <summary>
/// Information about a property to serialize.
/// </summary>
internal sealed record PropertyInfo(
    string Name,
    string TypeName,
    bool IsNullable,
    bool HasPublicSetter,
    PropertyCategory Category);

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
