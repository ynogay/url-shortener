namespace Ari.UrlShortener.Services;

public readonly record struct UrlValidationResult(bool IsValid, string? Error, string? NormalizedUrl)
{
    public static UrlValidationResult Ok(string normalizedUrl) => new(true, null, normalizedUrl);
    public static UrlValidationResult Fail(string error) => new(false, error, null);
}

public interface IUrlValidator
{
    UrlValidationResult Validate(string? url);
}
