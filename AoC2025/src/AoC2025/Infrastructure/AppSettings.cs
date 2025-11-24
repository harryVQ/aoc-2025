namespace AoC2025.Infrastructure;

public class AppSettings
{
    public string ProjectRoot { get; }
    
    public string ProblemsDir { get; }
    
    public string SolutionsDir { get; }
    
    public string OllamaEndpoint => "http://localhost:11434";

    public string PuzzleSolverModelName => "gpt-oss:20b";
    
    public string SpecBuilderModelName => "gpt-oss:20b";
    
    public string CodeModelName => "gpt-oss:20b";

    public string AoCSessionToken => "<AOC_SESSOION_TOKEN>";

    public AppSettings()
    {
        var exeDir = AppContext.BaseDirectory;
        this.ProjectRoot = Path.GetFullPath(Path.Combine(exeDir, "..", "..", ".."));
        this.ProblemsDir = Path.Combine(this.ProjectRoot, "Problems");
        this.SolutionsDir = Path.Combine(this.ProjectRoot, "Solutions");
    }
}
