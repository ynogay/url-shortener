namespace Ari.UrlShortener.Features.ShortLinks;

public sealed record CreateLinkResponse(
    string Code,
    string ShortUrl,
    string LongUrl,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
