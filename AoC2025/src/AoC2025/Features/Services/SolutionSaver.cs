namespace AoC2025.Features.Services;

using AoC2025.Features.Agents;
using AoC2025.Infrastructure;
using AoC2025.Models;

public class SolutionSaver(AppSettings settings)
{
    public async Task SaveSolution(AocProblemSpec spec, CodeAgentRunResult runResult)
    {
        var day = spec.Day;
        var part = spec.Part;
        
        var solutionDir = Path.Combine(settings.SolutionsDir, $"Day{day}");
        Directory.CreateDirectory(solutionDir);

        await SaveSource(runResult, solutionDir, part);

        await SaveExplanation(spec, runResult, solutionDir, part);

        await UpdateReadme(spec, solutionDir);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Solution saved to: {solutionDir}");
        Console.ResetColor();
    }

    private static async Task SaveSource(CodeAgentRunResult runResult, string solutionDir, int part)
    {
        string code = null;
        
        if (!string.IsNullOrWhiteSpace(runResult.StructuredOutput?.Code))
        {
            code = runResult.StructuredOutput.Code;
        }
        else
        {
            var sourcePath = Path.Combine(runResult.WorkDir, "solution.cs");
            if (File.Exists(sourcePath))
            {
                code = await File.ReadAllTextAsync(sourcePath);
            }
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codePath = Path.Combine(solutionDir, $"part{part}.txt"); // txt so we don't get entrypoint errors in IDEs
            await File.WriteAllTextAsync(codePath, code);
        }
    }

    private static async Task SaveExplanation(AocProblemSpec spec, CodeAgentRunResult runResult, string solutionDir, int part)
    {
        var markdown = $"""
            # Day {spec.Day} Part {part}: {spec.Title}
            
            ## Solution Summary
            {runResult.StructuredOutput?.Summary ?? "N/A"}
            
            ## Algorithm Explanation
            {runResult.StructuredOutput?.Explanation ?? "N/A"}
            
            ## Sample Output
            ```
            {runResult.StructuredOutput?.SampleOutput ?? "N/A"}
            ```
            
            ## Real Answer
            ```
            {runResult.RealRunOutput ?? "Failed to compute"}
            ```
            
            ## Performance
            - Code Generation: {runResult.CodeGenerationTime.TotalSeconds:F2}s
            - Code Runtime: {runResult.CodeRuntime.TotalMilliseconds:F2}ms
            - Input Tokens: {runResult.Response.Usage?.InputTokenCount}
            - Output Tokens: {runResult.Response.Usage?.OutputTokenCount}
            """;

        var explanationPath = Path.Combine(solutionDir, $"part{part}_explanation.md");
        await File.WriteAllTextAsync(explanationPath, markdown);
    }

    private static async Task UpdateReadme(AocProblemSpec spec, string solutionDir)
    {
        var readmePath = Path.Combine(solutionDir, "README.md");
        if (!File.Exists(readmePath))
        {
            var readme = $"""
                # Advent of Code - Day {spec.Day}
                
                ## {spec.Title}
                
                ### Solutions
                - [Part 1 C#](part1.txt) - [Explanation](part1_explanation.md)
                - [Part 2 C#](part2.txt) - [Explanation](part2_explanation.md)
                """;
            
            await File.WriteAllTextAsync(readmePath, readme);
        }
    }
}