namespace AoC2025.Models;

using JetBrains.Annotations;

[PublicAPI]
public sealed class PuzzleSpecBuilderOutput
{
    public required string Title { get; set; }

    public required int Day { get; set; }

    public required int Part { get; set; }
    
    public required string Description { get; set; }
    
    public string PartOneDescription { get; set; }

    public string SampleInput { get; set; }

    public string ExpectedSampleOutput { get; set; }
}
