namespace Ari.UrlShortener.Features.ShortLinks;

public sealed class CreateLinkRequest
{
    /// <summary>The long URL to shorten.</summary>
    public string? Url { get; set; }
}
