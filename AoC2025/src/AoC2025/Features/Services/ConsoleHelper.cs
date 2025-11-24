namespace AoC2025.Features.Services;

public static class ConsoleHelper
{
    public static void WriteLineGray(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }
}
