namespace Ari.UrlShortener.Domain;

/// <summary>
/// A single shortened link mapping a generated <see cref="Code"/> to a <see cref="LongUrl"/>.
/// </summary>
public sealed class ShortLink
{
    public long Id { get; set; }

    /// <summary>The unique base62 code that appears in the short URL.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The original (long) destination URL.</summary>
    public string LongUrl { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>False once the link is expired/disabled; such links never redirect.</summary>
    public bool IsActive { get; set; }

    /// <summary>Number of successful redirects served for this link.</summary>
    public long ClickCount { get; set; }
}
