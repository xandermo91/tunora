using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tunora.Infrastructure.Options;

namespace Tunora.Infrastructure.Services;

public class ClaudeAdvisorService(HttpClient httpClient, IOptions<ClaudeOptions> options) : IClaudeAdvisorService
{
    private readonly ClaudeOptions _options = options.Value;

    public async Task<AdvisorResponse> GetMusicAdviceAsync(string businessType, string? description, CancellationToken ct = default)
    {
        EnsureApiKeyConfigured();

        // Wrap user-supplied values in XML delimiters to prevent prompt injection
        var prompt = "You are a music advisor for in-store background music. A business needs music recommendations.\n\n" +
            $"<business_type>{businessType}</business_type>\n" +
            (description is not null ? $"<additional_context>{description}</additional_context>\n\n" : "\n") +
            "Respond with a JSON object (no markdown, raw JSON only) with this exact structure:\n" +
            "{\"insight\": \"A 2-3 sentence overview of what music style suits this business and why.\", \"suggestions\": [\"Suggestion 1\", \"Suggestion 2\", \"Suggestion 3\"]}\n\n" +
            "Keep suggestions concise and actionable (under 15 words each). Focus on genre, tempo, and atmosphere.";

        return await CallClaudeAsync(prompt, ct);
    }

    public async Task<AdvisorResponse> GetAnalyticsInsightAsync(OverviewStats stats, CancellationToken ct = default)
    {
        EnsureApiKeyConfigured();

        var prompt = "You are an analytics advisor for Tunora, an in-store music SaaS.\n\n" +
            $"Current company stats:\n" +
            $"- Total locations: {stats.TotalLocations}\n" +
            $"- Active locations (online): {stats.ActiveLocations}\n" +
            $"- Currently playing: {stats.PlayingNow}\n" +
            $"- Scheduled events this week: {stats.SchedulesThisWeek}\n\n" +
            "Respond with a JSON object (no markdown, raw JSON only) with this exact structure:\n" +
            "{\"insight\": \"A 2-3 sentence observation about the data and what it means for the business.\", \"suggestions\": [\"Suggestion 1\", \"Suggestion 2\", \"Suggestion 3\"]}\n\n" +
            "Keep suggestions concise and actionable (under 20 words each). Focus on operational improvements.";

        return await CallClaudeAsync(prompt, ct);
    }

    private async Task<AdvisorResponse> CallClaudeAsync(string userMessage, CancellationToken ct)
    {
        var requestBody = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("messages", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? throw new InvalidOperationException("Claude returned an empty response.");

            // Parse the JSON returned in Claude's text response
            using var advisorDoc = JsonDocument.Parse(text);
            var insight = advisorDoc.RootElement.GetProperty("insight").GetString() ?? "";
            var suggestions = advisorDoc.RootElement.GetProperty("suggestions")
                .EnumerateArray()
                .Select(s => s.GetString() ?? "")
                .ToArray();

            return new AdvisorResponse(insight, suggestions);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Claude API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Claude returned an unexpected response format: {ex.Message}", ex);
        }
    }

    private void EnsureApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is not configured.");
    }
}
