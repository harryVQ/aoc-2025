namespace AoC2025.Features.Agents;

using System.Text;
using System.Text.Json;
using AoC2025.Features.Services;
using AoC2025.Infrastructure;
using AoC2025.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

public class PuzzleSpecBuilderAgent(AppSettings settings)
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PuzzleSpecBuilderOutput> Build(int day, int part, string problemHtml, CancellationToken cancellationToken = default)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(20),
            BaseAddress = new Uri(settings.OllamaEndpoint)
        };

        IChatClient client = new OllamaApiClient(httpClient, settings.SpecBuilderModelName);

        var agent = BuildAgent(client);

        var responseFormat = ChatResponseFormat.ForJsonSchema<PuzzleSpecBuilderOutput>(this.jsonOptions);

        var prompt = BuildPrompt(day, part, problemHtml);

        ConsoleHelper.WriteLineGray($"\nBuilding Day:{day} Part:{part}...\n");

        var thread = agent.GetNewThread();

        var response = await agent.RunAsync(
            prompt,
            thread,
            new ChatClientAgentRunOptions
            {
                ChatOptions = new ChatOptions
                {
                    ToolMode = ChatToolMode.None,
                    ResponseFormat = responseFormat
                }
            },
            cancellationToken);

        var structured = response.Deserialize<PuzzleSpecBuilderOutput>(this.jsonOptions);

        if (structured is null)
        {
            throw new InvalidOperationException($"failed to deserialise structured output from spec building agent. Raw:\n{response.Text}");
        }

        if (structured.Day == 0 || structured.Day != day)
        {
            structured.Day = day;
        }

        if (structured.Part == 0 || structured.Part != part)
        {
            structured.Part = part;
        }

        return structured;
    }

    private static AIAgent BuildAgent(IChatClient client)
    {
        return client
            .CreateAIAgent(
                name: "PuzzleSpecBuilderAgent",
                instructions: GetAgentInstructions())
            .AsBuilder()
            .Build();
    }

    private static string BuildPrompt(int day, int part, string problemHtml)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Day: {day}");
        sb.AppendLine($"Part: {part}");
        sb.AppendLine();
        sb.AppendLine("Extract the problem specification for THIS part only from the HTML below.");
        sb.AppendLine("Remember to respond with a single JSON object as described in your instructions.");
        sb.AppendLine();
        sb.AppendLine("=== FULL HTML DOCUMENT START ===");
        sb.AppendLine(problemHtml);
        sb.AppendLine("=== FULL HTML DOCUMENT END ===");

        return sb.ToString();
    }

    private static string GetAgentInstructions()
    {
        return """
               You are an assistant that specializes in parsing Advent of Code HTML pages
               into a clean, structured problem specification.

               INPUT YOU RECEIVE (via the prompt):
               - Day, and part number.
               - The FULL HTML document of an Advent of Code problem page for that day.

               HTML STRUCTURE:
               - The main puzzle text lives in one or more <article class="day-desc"> elements.
               - Part 1 is always the FIRST <article class="day-desc"> block.
               - Part 2 is the SECOND <article class="day-desc"> block (when it exists).
               - Other HTML elements (header, nav, sidebar, sponsor text, share links, forms) must be ignored.

               YOUR TASK:
               - Build a JSON spec for the REQUESTED part (1 or 2).
               - Additionally, always provide the Part 1 description as context when it exists.

               FIELDS TO EXTRACT:

                 1. title
                    - From the <h2> of the REQUESTED part's <article class="day-desc">.
                    - Clean '--- Day 2: Dive! ---' to 'Day 2: Dive!'.

                 2. description
                    - For part = 1:
                        * This is the full description of Part 1 only.
                    - For part = 2:
                        * This is ONLY the additional narrative/rules introduced by Part 2
                          (i.e., the SECOND <article class="day-desc">).
                        * Do NOT repeat the full Part 1 text here.

                 3. partOneDescription
                    - Always describe the Part 1 rules from the FIRST <article class="day-desc">.
                    - For part = 1:
                        * This MUST be identical to description.
                    - For part = 2:
                        * This MUST be a clean description of Part 1 so that Part 2 can be
                          understood in context.
                    - Do not include the user's personal puzzle input or 'get your puzzle input' links.

                 4. sampleInput
                    - Use the main example puzzle input for the day, typically introduced in Part 1.
                    - You may look at all <article class="day-desc"> blocks to find it.
                    - Copy the <pre><code>...</code></pre> contents that clearly represent the
                      example INPUT (not diagrams or output visualizations).
                    - If Part 2 refers to “the above example”, reuse the same input from Part 1.
                    - If there is truly no example input, use null.

                 5. expectedSampleOutput
                    - From the REQUESTED part’s text only.
                    - Extract any explicit numeric result(s) the puzzle states for the example input,
                      such as "150" or "1924".
                    - If none is clearly given, use null.

               REQUIRED JSON FIELDS:
               Return a single JSON object with:
               - "title": string
               - "day": integer
               - "part": integer
               - "description": string
               - "partOneDescription": string or null
               - "sampleInput": string or null
               - "expectedSampleOutput": string or null

               CRITICAL RULES:
               - Do NOT include Markdown fences or extra text; ONLY the JSON object.
               - Do NOT invent examples; only use what is in the HTML.
               - Do NOT include the user's personal puzzle input anywhere in the JSON.
               """;
    }
}
