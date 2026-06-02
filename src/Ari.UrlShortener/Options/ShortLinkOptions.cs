namespace Ari.UrlShortener.Options;

/// <summary>
/// Configurable knobs for the URL shortener. Bound from the "ShortLink" section of configuration.
/// </summary>
public sealed class ShortLinkOptions
{
    public const string SectionName = "ShortLink";

    /// <summary>Number of base62 characters in a generated code.</summary>
    public int CodeLength { get; set; } = 7;

    /// <summary>How long a link stays valid after creation.</summary>
    public int ExpiryMonths { get; set; } = 6;

    /// <summary>Maximum accepted length of an input URL.</summary>
    public int MaxUrlLength { get; set; } = 2048;

    /// <summary>How many times to retry code generation on a unique-collision before giving up.</summary>
    public int MaxGenerationAttempts { get; set; } = 5;

    /// <summary>How often the cleanup job runs, in hours.</summary>
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Optional absolute base address used to build the returned short URL
    /// (e.g. "https://sho.rt"). When empty, the incoming request's host is used.
    /// </summary>
    public string ShortUrlBaseAddress { get; set; } = string.Empty;
}
