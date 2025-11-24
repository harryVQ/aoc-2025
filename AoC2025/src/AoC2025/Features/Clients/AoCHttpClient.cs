namespace AoC2025.Features.Clients;

using System;
using System.Net.Http;
using System.Threading.Tasks;

public class AoCHttpClient
{
    private readonly HttpClient httpClient;
    
    private static string Year => "2025";

    public AoCHttpClient(string sessionToken)
    {
        this.httpClient = new HttpClient();
        this.httpClient.BaseAddress = new Uri("https://adventofcode.com/");

        this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Cookie",
            $"session={sessionToken}"
        );
    }

    public async Task<string> GetProblemHtml(int d)
    {
        var url = $"{Year}/day/{d}";
        var response = await this.httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"failed: {response.StatusCode}");
        }
        
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetPuzzleInput(int d)
    {
        var url = $"{Year}/day/{d}/input";
        var response = await this.httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"failed: {response.StatusCode}");
        }
        
        var body = await response.Content.ReadAsStringAsync();
        return body.TrimEnd('\r', '\n');
    }
}
