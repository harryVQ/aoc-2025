namespace AoC2025.Infrastructure;

using AoC2025.Features.Agents;
using AoC2025.Models;

public static class HelperMethods
{
    public static string CreateWorkDirectory()
    {
        var tempPath = Path.GetTempPath();
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var workDir = Path.Combine(tempPath, "aoc-2025", $"work-{sessionId}");

        Directory.CreateDirectory(workDir);

        return workDir;
    }

    public static void CleanupWorkDirectory(string workDir)
    {
        if (Directory.Exists(workDir))
        {
            Directory.Delete(workDir, true);
        }
    }

    public static void DisplaySpecData(AocProblemSpec spec)
    {
        Console.ResetColor();
        Console.WriteLine($"AoC - Day {spec.Day} Part {spec.Part}");
        Console.WriteLine($" {spec.Title}");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void DisplayResults(CodeAgentRunResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.RealRunOutput))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nAnswer: {result.RealRunOutput}");
            Console.ResetColor();
        }
        else if (!string.IsNullOrWhiteSpace(result.RealRunError))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nExecution failed");
            Console.ResetColor();
        }
    
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  Generation Time: {result.CodeGenerationTime.TotalSeconds:F1}s | Code Runtime: {result.CodeRuntime.TotalMilliseconds:F2}ms | Tokens: Input..{result.Response.Usage?.InputTokenCount:N0} → Output..{result.Response.Usage?.OutputTokenCount:N0}");
        Console.ResetColor();
    }
}
