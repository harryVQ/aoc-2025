namespace AoC2025.Models;

public sealed record AocProblemSpec
{
    public string Title { get; init; }
    
    public int Day { get; init; }
    
    public int Part { get; init; }

    public string Description { get; init; } = string.Empty;
    
    public string PartOneDescription { get; init; }

    public string SampleInput { get; init; }

    public string ExpectedSampleOutput { get; init; }
    
    public string PuzzleInput { get; init; } = string.Empty;
}
