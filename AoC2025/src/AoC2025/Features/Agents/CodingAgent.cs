namespace AoC2025.Features.Agents;

using System.Text;
using AoC2025.Infrastructure;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

public sealed class CodingAgent
{
    private readonly IChatClient client;

    public CodingAgent(AppSettings settings)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(20),
            BaseAddress = new Uri(settings.OllamaEndpoint)
        };
        this.client = new OllamaApiClient(httpClient, settings.CodeModelName);
    }

    public async Task<string> GenerateOrFix(
        string puzzleDescription,
        string sampleInput,
        string expectedSampleOutput,
        string previousCode,
        string compilerErrors,
        CancellationToken ct = default)
    {
        var agent = this.client
            .CreateAIAgent(
                name: "CodingAgent",
                instructions: GetCodingInstructions())
            .AsBuilder()
            .Build();

        var sb = new StringBuilder();
        sb.AppendLine("You must output ONLY the complete C# program, no comments or explanation.");
        sb.AppendLine();
        sb.AppendLine("=== PUZZLE DESCRIPTION ===");
        sb.AppendLine(puzzleDescription.Trim());
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(sampleInput))
        {
            sb.AppendLine("=== SAMPLE INPUT ===");
            sb.AppendLine(sampleInput.TrimEnd('\r', '\n'));
        }

        if (!string.IsNullOrWhiteSpace(expectedSampleOutput))
        {
            sb.AppendLine("=== EXPECTED SAMPLE OUTPUT ===");
            sb.AppendLine(expectedSampleOutput.TrimEnd('\r', '\n'));
        }

        if (!string.IsNullOrWhiteSpace(previousCode))
        {
            sb.AppendLine();
            sb.AppendLine("=== PREVIOUS C# CODE (BUGGY) ===");
            sb.AppendLine(previousCode);
        }

        if (!string.IsNullOrWhiteSpace(compilerErrors))
        {
            sb.AppendLine();
            sb.AppendLine("=== COMPILER ERRORS (dotnet) ===");
            sb.AppendLine(compilerErrors);
        }

        var response = await agent.RunAsync(
            sb.ToString(),
            agent.GetNewThread(),
            new ChatClientAgentRunOptions
            {
                ChatOptions = new ChatOptions
                {
                    ToolMode = ChatToolMode.None
                }
            },
            ct);

        return response.Text?.Trim();
    }

    private static string GetCodingInstructions()
    {
        return """
               You are a specialist C# code generator and bug fixer for Advent of Code puzzles.

               You are NOT the orchestrator; you ONLY:
               - Generate a complete C# program in a single file called Program.cs, OR
               - Repair an existing C# program based on compiler/runtime errors.

               INPUTS YOU RECEIVE:
               - puzzleDescription: full natural-language description of the puzzle,
                 including input format and required output.
               - sampleInput: example stdin to test with (may be empty).
               - expectedSampleOutput: expected output for sampleInput, if known (may be empty).
               - previousCode: previous Program.cs contents, when fixing code.
               - compilerErrors: compiler or runtime errors from the last attempt, when fixing code.

               YOUR JOB:
               1. If previousCode is empty:
                  - Design a correct algorithm for the puzzle.
                  - Implement it as a complete C# console program in Program.cs.
               2. If previousCode is NOT empty:
                  - Carefully read compilerErrors and previousCode.
                  - FIX the code while preserving the overall algorithm when possible.
                  - Pay special attention to:
                    - Correct use of loops and indices.
                    - Null / bounds checks.
                    - Correct parsing of the AoC input format.

               C# REQUIREMENTS:
               - Target .NET 8.
               - You MAY use top-level statements OR:
                   namespace AoC;
                   public static class Program
                   {
                       public static void Main(string[] args)
                       {
                           // ...
                       }
                   }
                 Do NOT use more than one entry point.
               - Read from standard input using Console.ReadLine() in a loop.
               - Write ONLY the required answer(s) to Console.WriteLine.
               - Do NOT reference external NuGet packages.
               - Prefer simple, clear code over cleverness.

               INPUT PARSING:
               - sampleInput is exactly what will be fed to stdin when testing.
               - For puzzles with multiple lines, read until Console.ReadLine() returns null.
               - For AoC "boards" or grids, parse them line by line as described.

               SAMPLE VALIDATION (MENTAL):
               - Use expectedSampleOutput as a check: structure your algorithm so that,
                 when run on sampleInput, it would produce that output.
               - However, you do NOT execute code yourself; only the host will run it.

               OUTPUT FORMAT:
               - You MUST return ONLY the raw C# source code for Program.cs.
               - Do NOT include markdown fences, comments about the code, JSON, or any extra text.
               """;
    }
}
