using System.Text.Json;

namespace TONL.NET.Benchmarks.Reports;

/// <summary>
/// Cross-language comparison report generator.
/// Produces output compatible with official TONL TypeScript benchmarks.
/// </summary>
public static class CrossLanguageReport
{
    /// <summary>
    /// Generates and prints a size comparison report for all fixtures.
    /// Output format matches official TONL bench output for easy comparison.
    /// </summary>
    public static void GenerateSizeReport()
    {
        var results = SizeComparisonBenchmarks.GenerateSizeReport();

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    TONL.NET Size Comparison Report                                    ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║ Fixture                   │ JSON (B) │ TONL (B) │ Ratio  │ Savings │ JSON Tok│TONL Tok║");
        Console.WriteLine("╠═══════════════════════════╪══════════╪══════════╪════════╪═════════╪═════════╪════════╣");

        int totalJsonBytes = 0;
        int totalTonlBytes = 0;
        int totalJsonTokens = 0;
        int totalTonlTokens = 0;

        foreach (var r in results)
        {
            var fixtureName = r.Fixture.Length > 24 ? r.Fixture[..21] + "..." : r.Fixture;
            Console.WriteLine(
                $"║ {fixtureName,-25} │ {r.JsonBytes,8} │ {r.TonlBytes,8} │ {r.CompressionRatio,5:F2}x │ {r.SavingsPercent,6:F1}% │ {r.EstimatedJsonTokens,7} │ {r.EstimatedTonlTokens,6} ║"
            );

            totalJsonBytes += r.JsonBytes;
            totalTonlBytes += r.TonlBytes;
            totalJsonTokens += r.EstimatedJsonTokens;
            totalTonlTokens += r.EstimatedTonlTokens;
        }

        var totalRatio = (double)totalJsonBytes / totalTonlBytes;
        var totalSavings = (1.0 - (double)totalTonlBytes / totalJsonBytes) * 100;
        var tokenSavings = (1.0 - (double)totalTonlTokens / totalJsonTokens) * 100;

        Console.WriteLine("╠═══════════════════════════╪══════════╪══════════╪════════╪═════════╪═════════╪════════╣");
        Console.WriteLine(
            $"║ {"TOTAL",-25} │ {totalJsonBytes,8} │ {totalTonlBytes,8} │ {totalRatio,5:F2}x │ {totalSavings,6:F1}% │ {totalJsonTokens,7} │ {totalTonlTokens,6} ║"
        );
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════╝");

        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine($"  • Average compression ratio: {totalRatio:F2}x");
        Console.WriteLine($"  • Average byte savings: {totalSavings:F1}%");
        Console.WriteLine($"  • Estimated token savings: {tokenSavings:F1}%");

        // Cost analysis (GPT-4 pricing approximation: $0.03/1K tokens)
        var costPerThousandTokens = 0.03m;
        var jsonCost = totalJsonTokens / 1000.0m * costPerThousandTokens;
        var tonlCost = totalTonlTokens / 1000.0m * costPerThousandTokens;
        var costSavings = jsonCost - tonlCost;

        Console.WriteLine();
        Console.WriteLine("Cost Analysis (GPT-4 pricing @ $0.03/1K tokens):");
        Console.WriteLine($"  • JSON cost: ${jsonCost:F4}");
        Console.WriteLine($"  • TONL cost: ${tonlCost:F4}");
        Console.WriteLine($"  • Savings: ${costSavings:F4} ({(costSavings / jsonCost * 100):F1}%)");

        // Recommendations
        Console.WriteLine();
        Console.WriteLine("Recommendations:");
        if (totalSavings >= 20)
        {
            Console.WriteLine("  ✅ TONL provides significant size reduction (>20%)");
        }
        else if (totalSavings >= 10)
        {
            Console.WriteLine("  ⚠️ TONL provides moderate size reduction (10-20%)");
        }
        else
        {
            Console.WriteLine("  ❌ TONL provides minimal size reduction (<10%)");
        }

        // Export as JSON for comparison
        var exportPath = "size-comparison-results.json";
        var jsonExport = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(exportPath, jsonExport);
        Console.WriteLine();
        Console.WriteLine($"Results exported to: {exportPath}");
    }
}
