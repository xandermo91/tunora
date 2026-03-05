namespace Tunora.Infrastructure.Options;

public class JamendoOptions
{
    public string BaseUrl { get; set; } = "https://api.jamendo.com/v3.0/";
    public string ClientId { get; set; } = string.Empty;
}
