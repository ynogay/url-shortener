using Ari.UrlShortener.Domain;

namespace Ari.UrlShortener.Features.ShortLinks;

public interface IShortLinkService
{
    /// <summary>
    /// Creates and persists a short link for an already-validated long URL,
    /// generating a unique code (with collision retries).
    /// </summary>
    Task<ShortLink> CreateAsync(string longUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a code to its destination if the link exists, is active and not expired,
    /// atomically incrementing the click count. Returns null otherwise.
    /// </summary>
    Task<string?> ResolveAndCountClickAsync(string code, CancellationToken cancellationToken);
}
