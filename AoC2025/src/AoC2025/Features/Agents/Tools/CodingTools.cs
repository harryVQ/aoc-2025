namespace AoC2025.Features.Agents.Tools;

using System.ComponentModel;
using System.Diagnostics;
using JetBrains.Annotations;

[PublicAPI]
public sealed class CodingTools
{
    private readonly CodingAgent codingAgent;
    private readonly string workDir;
    private readonly string dockerImage;

    public CodingTools(
        CodingAgent codingAgent,
        string workDir,
        string dockerImage = "csharp-runner:latest")
    {
        this.codingAgent = codingAgent;
        this.workDir = workDir;
        this.dockerImage = dockerImage;
        Directory.CreateDirectory(this.workDir);
    }

    // ------------------------------------------------------------------------
    // 1. Code generation (unchanged in spirit)
    // ------------------------------------------------------------------------
    [Description("""
                 Generate or repair a complete C# console application (`Program.cs`)
                 for an Advent of Code puzzle.

                 Use this whenever you need C# code. You may pass:
                 - The full puzzle description.
                 - Optional SAMPLE input and expected SAMPLE output.
                 - Optional previous C# code and compiler/runtime errors when fixing bugs.

                 The tool returns ONLY raw C# source code for Program.cs
                 (no markdown, no commentary).
                 """)]
    public async Task<string> GenerateCodeAsync(
        [Description("Full natural-language puzzle description, including all details needed to solve it.")]
        string puzzleDescription,

        [Description("Sample input text exactly as it will be fed to stdin when testing the program. May be empty.")]
        string sampleInput,

        [Description("Expected output for the sample input, if known. May be empty.")]
        string expectedSampleOutput,

        [Description("Previous version of the C# program to improve or fix. Pass empty when creating the first version.")]
        string previousCode,

        [Description("Compiler or runtime errors from the last build/run (dotnet) when fixing bugs. Pass empty for the first attempt.")]
        string compilerErrors,

        CancellationToken cancellationToken = default)
    {
        return await this.codingAgent.GenerateOrFix(
            puzzleDescription,
            sampleInput,
            expectedSampleOutput,
            previousCode,
            compilerErrors,
            cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 2. Result type
    // ------------------------------------------------------------------------
    public sealed record RunResult(
        bool Success,
        string Stdout,
        string Stderr,
        int ExitCode,
        double? InnerRuntimeSeconds);

    [Description("""
Compile and run C# `Program.cs` INSIDE a locked-down Docker container using .NET SDK.

Use this ONLY with SAMPLE INPUT.
""")]
    public async Task<RunResult> CompileAndRunToolAsync(
        [Description("Complete C# source code for Program.cs")]
        string code,
        [Description("Standard input to feed to the program (SAMPLE INPUT).")]
        string stdin,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = Path.Combine(this.workDir, "Program.cs");
        await File.WriteAllTextAsync(sourcePath, code, cancellationToken);

        return await this.RunInDockerAsync(stdin, cancellationToken);
    }
    
    [Description("""
Compile and run the existing `Program.cs` in the working directory INSIDE Docker.

""")]
    public async Task<RunResult> CompileAndRunAsync(
        [Description("Standard input to feed to the program (typically REAL puzzle input).")]
        string stdin,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = Path.Combine(this.workDir, "Program.cs");

        if (!File.Exists(sourcePath))
        {
            return new RunResult(
                Success: false,
                Stdout: string.Empty,
                Stderr: "Program.cs not found. No code has been written yet.",
                ExitCode: -1,
                InnerRuntimeSeconds: null);
        }

        return await this.RunInDockerAsync(stdin, cancellationToken);
    }
    
    private async Task<RunResult> RunInDockerAsync(
        string stdin,
        CancellationToken cancellationToken)
    {
        const string innerCommand = """
                                    if [ ! -d app ]; then
                                      dotnet new console -n app -o app -f net8.0 --force > /dev/null
                                    fi

                                    cp Program.cs app/Program.cs

                                    # Build once so the binary exists
                                    dotnet build app -c Release --nologo -v q > /dev/null

                                    # Time only the actual run (no build)
                                    /usr/bin/time -f "RUNTIME_SECONDS=%e" \
                                      dotnet run --project app -c Release --no-build --nologo -v q
                                    """;

        var dockerInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        dockerInfo.ArgumentList.Add("run");
        dockerInfo.ArgumentList.Add("--rm");
        dockerInfo.ArgumentList.Add("--network=none");
        dockerInfo.ArgumentList.Add("-i");

        dockerInfo.ArgumentList.Add("-v");
        dockerInfo.ArgumentList.Add($"{this.workDir}:/work");
        dockerInfo.ArgumentList.Add("-w");
        dockerInfo.ArgumentList.Add("/work");

        dockerInfo.ArgumentList.Add(this.dockerImage);
        dockerInfo.ArgumentList.Add("sh");
        dockerInfo.ArgumentList.Add("-c");
        dockerInfo.ArgumentList.Add(innerCommand);

        using var proc = Process.Start(dockerInfo)
                        ?? throw new InvalidOperationException("Failed to start docker process");

        if (!string.IsNullOrEmpty(stdin))
        {
            await proc.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken);
        }
        proc.StandardInput.Close();

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);

        await proc.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var success = proc.ExitCode == 0;

        double? innerSeconds = null;
        const string marker = "RUNTIME_SECONDS=";

        foreach (var line in stderr.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                var valuePart = trimmed[marker.Length..];
                if (double.TryParse(
                        valuePart,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var seconds))
                {
                    innerSeconds = seconds;
                }
            }
        }

        return new RunResult(
            Success: success,
            Stdout: stdout,
            Stderr: stderr,
            ExitCode: proc.ExitCode,
            InnerRuntimeSeconds: innerSeconds);
    }
}