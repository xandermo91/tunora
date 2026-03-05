using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Tunora.Infrastructure.Options;

namespace Tunora.Infrastructure.Services;

public record JamendoTrack(
    string Id,
    string Title,
    string ArtistName,
    string AudioUrl,
    string? AlbumImageUrl
);

public class JamendoClient(HttpClient http, IOptions<JamendoOptions> options)
{
    private readonly JamendoOptions _opts = options.Value;

    public async Task<JamendoTrack?> GetTrackByTagAsync(string tag, CancellationToken ct = default)
    {
        var offset = Random.Shared.Next(0, 50);
        var url = $"tracks/?client_id={_opts.ClientId}&format=json&limit=1" +
                  $"&tags={Uri.EscapeDataString(tag)}&audioformat=mp32&offset={offset}&imagesize=500";

        var response = await http.GetFromJsonAsync<JamendoResponse>(url, ct);
        var track = response?.Results?.FirstOrDefault();
        if (track is null) return null;

        return new JamendoTrack(
            track.Id,
            track.Name,
            track.ArtistName,
            track.Audio,
            string.IsNullOrEmpty(track.AlbumImage) ? null : track.AlbumImage
        );
    }

    // ── Deserialization models ────────────────────────────────────────────────

    private sealed class JamendoResponse
    {
        [JsonPropertyName("results")]
        public List<JamendoTrackRaw>? Results { get; set; }
    }

    private sealed class JamendoTrackRaw
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("artist_name")]
        public string ArtistName { get; set; } = string.Empty;

        [JsonPropertyName("audio")]
        public string Audio { get; set; } = string.Empty;

        [JsonPropertyName("album_image")]
        public string? AlbumImage { get; set; }
    }
}
