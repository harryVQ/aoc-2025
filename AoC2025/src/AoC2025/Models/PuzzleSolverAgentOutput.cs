namespace AoC2025.Models;

using JetBrains.Annotations;

[PublicAPI]
public sealed class PuzzleSolverAgentOutput
{
    public string Summary { get; set; }

    public string SampleOutput { get; set; }

    public string Explanation { get; set; }

    public string Code { get; set; }
}
