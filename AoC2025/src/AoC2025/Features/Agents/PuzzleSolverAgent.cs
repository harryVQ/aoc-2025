namespace AoC2025.Features.Agents;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AoC2025.Features.Agents.Tools;
using AoC2025.Features.Services;
using AoC2025.Infrastructure;
using AoC2025.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

public class PuzzleSolverAgent(AppSettings settings)
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private AgentThread thread;

    public async Task<CodeAgentRunResult> RunAsync(AocProblemSpec spec, string workDir, CancellationToken cancellationToken = default)
    {
        var codingAgent = new CodingAgent(settings);
        var codingTools = new CodingTools(codingAgent, workDir);

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(20),
            BaseAddress = new Uri(settings.OllamaEndpoint)
        };
        IChatClient client = new OllamaApiClient(httpClient, settings.PuzzleSolverModelName);

        var agent = this.BuildAgent(client, codingTools);
        var prompt = BuildPrompt(spec);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("\nRunning Puzzle Solving agent...\n");
        Console.ResetColor();

        var sw = Stopwatch.StartNew();

        this.thread = agent.GetNewThread();

        var chatResponseFormatJson = ChatResponseFormat.ForJsonSchema<PuzzleSolverAgentOutput>(this.jsonOptions);

        var response = await agent.RunAsync(
            prompt,
            this.thread,
            new ChatClientAgentRunOptions
            {
                ChatOptions = new ChatOptions
                {
                    ToolMode = ChatToolMode.Auto,
                    ResponseFormat = chatResponseFormatJson
                }
            }, cancellationToken);

        sw.Stop();
        var swCodeGen = sw.Elapsed;

        var structured = response.Deserialize<PuzzleSolverAgentOutput>(this.jsonOptions);

        var realRun = await codingTools.CompileAndRunAsync(spec.PuzzleInput, cancellationToken);

        return new CodeAgentRunResult
        {
            Response = response,
            StructuredOutput = structured,

            RealRunOutput = realRun.Success
                ? ExtractAnswerFromStdout(realRun.Stdout)
                : null,

            RealRunError = !realRun.Success ? realRun.Stderr : null,
            CodeGenerationTime = swCodeGen,
            CodeRuntime = realRun.InnerRuntimeSeconds is { } s
                ? TimeSpan.FromSeconds(s)
                : TimeSpan.Zero,
            WorkDir = workDir
        };
    }

    private static string ExtractAnswerFromStdout(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return string.Empty;
        }

        var lines = stdout
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();

        return lines.Length == 0 ? string.Empty : lines[^1];
    }

    private AIAgent BuildAgent(IChatClient client, CodingTools codingTools)
    {
        return client
            .CreateAIAgent(
                name: "PuzzleSolverAgent",
                instructions: GetAgentInstructions(),
                tools:
                [
                    AIFunctionFactory.Create(codingTools.GenerateCodeAsync, "generate_code"),
                    AIFunctionFactory.Create(codingTools.CompileAndRunToolAsync, "compile_and_run")
                ])
            .AsBuilder()
            .Use(FunctionCallMiddleware)
            .Build();
    }

    private static string BuildPrompt(AocProblemSpec spec)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("=== Advent of Code Problem ===");
        prompt.AppendLine($"Title: {spec.Title}");
        prompt.AppendLine($"Day: {spec.Day}, Part: {spec.Part}");
        prompt.AppendLine();

        if (spec.Part == 2 && !string.IsNullOrWhiteSpace(spec.PartOneDescription))
        {
            prompt.AppendLine("=== Part 1 context (original rules) ===");
            prompt.AppendLine(spec.PartOneDescription.Trim());
            prompt.AppendLine();
            prompt.AppendLine("=== Part 2 - additional rules / changes ===");
        }

        prompt.AppendLine(spec.Description.Trim());

        prompt.AppendLine();

        if (!string.IsNullOrWhiteSpace(spec.SampleInput))
        {
            prompt.AppendLine("=== SAMPLE INPUT (use this to test your solution) ===");
            prompt.AppendLine(spec.SampleInput.TrimEnd('\r', '\n'));
            prompt.AppendLine("=== EXPECTED SAMPLE OUTPUT ===");
            prompt.AppendLine(spec.ExpectedSampleOutput?.TrimEnd('\r', '\n') ?? "");
        }

        return prompt.ToString();
    }

    private static string GetAgentInstructions()
    {
        return """
               You are an expert Advent of Code solver and orchestrator.

               Your job:
               - Read the puzzle description and SAMPLE input.
               - Use tools to obtain working C# code.
               - Verify the code against the SAMPLE input.
               - Return a JSON summary (no raw code written directly in your messages).

               TOOLS YOU CAN USE
               ------------------
               1. generate_code
                  - Purpose: create or repair a complete C# solution for this puzzle.
                  - Always pass:
                    - puzzleDescription: clear, concise description of the puzzle, input, and required output.
                    - sampleInput: SAMPLE INPUT exactly as it will be fed to stdin (may be empty).
                    - expectedSampleOutput: expected SAMPLE output as a string (may be empty).
                    - previousCode: last C# program used (empty on first attempt).
                    - compilerErrors: compiler/runtime errors or wrong-answer info from the last run (empty on first attempt). This should include the full stderr and exit code, plus the wrong output if applicable.
                  - Returns: ONLY the full C# source for Program.cs (no markdown).

               2. compile_and_run
                  - Purpose: compile and execute the C# program inside a .NET 8 environment.
                  - Always pass:
                    - code: the COMPLETE C# source for Program.cs that you want to test.
                    - stdin: the SAMPLE INPUT (or empty string if none).
                  - Returns: success flag, stdout, stderr, exitCode, innerRuntimeSeconds.

               REQUIRED WORKFLOW
               ------------------
               For each puzzle, follow this pattern:

               1. FIRST ATTEMPT
                  - Call generate_code with:
                    - previousCode = "" (empty)
                    - compilerErrors = "" (empty)
                  - Then call compile_and_run with:
                    - code  = the C# source returned by generate_code
                    - stdin = SAMPLE INPUT (if any)

               2. CHECK SAMPLE OUTPUT
                  - If compile_and_run.success is false, or stderr shows a runtime error:
                    - Treat this as a failed attempt.
                  - Otherwise:
                    - From stdout, take the LAST non-empty line as the program’s answer.
                    - Compare that string to expectedSampleOutput (if provided).
                    - If they differ, treat this as a failed attempt.

               3. REPAIR (IF NEEDED)
                  - On failure (compile error, runtime error, or wrong SAMPLE answer):
                    - Call generate_code again with:
                      - previousCode   = the last C# source you used.
                      - compilerErrors = a short summary including:
                          - exitCode
                          - stderr
                          - the wrong output (last non-empty stdout line), if any.
                    - Then call compile_and_run again with the new code and the same SAMPLE INPUT.
                  - Do this repair loop up to 2–3 times.
                  - If it still fails, stop and prepare a JSON response that clearly describes the failure
                    and includes the latest code.

               ABOUT THE CODE ITSELF
               ----------------------
               - Do NOT write C# directly in your chat responses.
               - Let generate_code decide the exact structure of Program.cs.
               - You only need to:
                 - Describe the puzzle clearly in puzzleDescription.
                 - Feed back compilerErrors and previousCode accurately during repairs.
                 - Judge correctness using the SAMPLE input/output.

               FINAL RESPONSE FORMAT
               ----------------------
               After you are done (either with a working solution or after giving up), your FINAL message
               back to the host MUST be a single JSON object with:

               - "summary": short natural-language summary of what the program does to solve the puzzle.
               - "sampleOutput": the exact output produced on the SAMPLE INPUT
                                 (the last non-empty line from stdout; trim trailing newlines).
               - "explanation": a brief explanation of the algorithm and why it should be correct.
               - "code": the complete C# source code of the final Program.cs (no markdown fences).

               Do NOT include markdown, code fences, or any extra text outside of this JSON object.
               """;
    }

    private static async ValueTask<object> FunctionCallMiddleware(
        AIAgent callingAgent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        var functionCallDetails = new StringBuilder();
        functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");

        if (context.Arguments.Count > 0)
        {
            functionCallDetails.Append(" (Args: ");
            functionCallDetails.Append(string.Join(", ",
                context.Arguments.Select(x => $"[{x.Key} = {x.Value}]")));
            functionCallDetails.Append(')');
        }

        ConsoleHelper.WriteLineGray(functionCallDetails.ToString());

        var result = await next(context, cancellationToken);

        if (result is not null)
        {
            var resultString = result.ToString() ?? string.Empty;
            var preview = resultString.Length > 300
                ? resultString[..300] + "..."
                : resultString;

            ConsoleHelper.WriteLineGray($"- Tool Result (preview): {preview}");
        }

        return result ?? new object();
    }
}

public class CodeAgentRunResult
{
    public required AgentRunResponse Response { get; init; }
    public PuzzleSolverAgentOutput StructuredOutput { get; init; }
    public string RealRunOutput { get; init; }
    public string RealRunError { get; init; }
    public TimeSpan CodeGenerationTime { get; init; }
    public TimeSpan CodeRuntime { get; set; }
    public required string WorkDir { get; init; }
}
