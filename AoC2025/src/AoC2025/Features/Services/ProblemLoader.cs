namespace AoC2025.Features.Services;

using System.Text.Json;
using AoC2025.Infrastructure;
using AoC2025.Models;
using AoC2025.Features.Agents;
using AoC2025.Features.Clients;

public class ProblemLoader(AppSettings settings, PuzzleSpecBuilderAgent specBuilder)
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public async Task<AocProblemSpec> LoadOrFetch(int day, int part, string sessionToken, CancellationToken cancellationToken = default)
    {
        var filePath = this.GetProblemPath(day, part);

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);

            var spec = JsonSerializer.Deserialize<AocProblemSpec>(json, this.jsonOptions) ?? throw new InvalidOperationException("deserialised problem spec is null?");

            return spec;
        }
        
        var aocHttpClient = new AoCHttpClient(sessionToken);
        
        var html = await aocHttpClient.GetProblemHtml(day);
        var puzzleInput = await aocHttpClient.GetPuzzleInput(day);

        var parsed = await specBuilder.Build(day, part, html, cancellationToken);

        var specToSave = new AocProblemSpec
        {
            Title = parsed.Title,
            Day = parsed.Day,
            Part = parsed.Part,
            Description = parsed.Description,
            PartOneDescription = parsed.PartOneDescription,
            SampleInput = parsed.SampleInput,
            ExpectedSampleOutput = parsed.ExpectedSampleOutput,
            PuzzleInput = puzzleInput
        };

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        
        var outJson = JsonSerializer.Serialize(specToSave, this.jsonOptions);
        
        await File.WriteAllTextAsync(filePath!, outJson, cancellationToken);
        
        return specToSave;
    }

    private string GetProblemPath(int day, int part)
    {
        var dayDir = Path.Combine(settings.ProblemsDir, $"Day{day.ToString()}");
        var fileName = $"part{part}.json";
        return Path.Combine(dayDir, fileName);
    }
}
