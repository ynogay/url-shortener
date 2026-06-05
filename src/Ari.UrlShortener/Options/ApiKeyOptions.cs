namespace Ari.UrlShortener.Options;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    /// <summary>The API key required to access protected endpoints.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>HTTP header name for the API key (default: X-API-Key).</summary>
    public string HeaderName { get; set; } = "X-API-Key";
}
