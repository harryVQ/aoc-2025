using AoC2025.Features.Agents;
using AoC2025.Features.Services;
using AoC2025.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using static AoC2025.Infrastructure.HelperMethods;

var services = new ServiceCollection();

services.AddSingleton<AppSettings>();
services.AddSingleton<ProblemLoader>();
services.AddSingleton<PuzzleSpecBuilderAgent>();
services.AddSingleton<PuzzleSolverAgent>();
services.AddSingleton<CodingAgent>();
services.AddSingleton<SolutionSaver>();

var serviceProvider = services.BuildServiceProvider();

var appSettings = serviceProvider.GetRequiredService<AppSettings>();
var problemLoader = serviceProvider.GetRequiredService<ProblemLoader>();
var puzzleSolverAgent = serviceProvider.GetRequiredService<PuzzleSolverAgent>();
var solutionSaver = serviceProvider.GetRequiredService<SolutionSaver>();

Console.WriteLine("Day:");
var day = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
Console.WriteLine("Part:");
var part = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());

var sessionToken = appSettings.AoCSessionToken;

var workDir = CreateWorkDirectory();

try
{
    var spec = await problemLoader.LoadOrFetch(day, part, sessionToken);
    if (spec is null)
    {
        throw new Exception("failed to fetch or parse spec");
    }

    DisplaySpecData(spec);

    var result = await puzzleSolverAgent.RunAsync(spec, workDir);

    DisplayResults(result);

    await solutionSaver.SaveSolution(spec, result);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"error: {ex.Message}");
    Console.ResetColor();
}
finally
{
    CleanupWorkDirectory(workDir);
}
