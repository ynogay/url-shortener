namespace Ari.UrlShortener.Services;

public interface ICodeGenerator
{
    /// <summary>Generates a random base62 code of the configured length.</summary>
    string Generate();
}
