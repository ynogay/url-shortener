using System.Security.Cryptography;
using Ari.UrlShortener.Options;
using Microsoft.Extensions.Options;

namespace Ari.UrlShortener.Services;

/// <summary>
/// Generates random base62 codes using a cryptographically strong RNG with
/// unbiased character selection (<see cref="RandomNumberGenerator.GetInt32(int)"/>).
/// </summary>
public sealed class Base62CodeGenerator : ICodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private readonly int _length;

    public Base62CodeGenerator(IOptions<ShortLinkOptions> options)
    {
        _length = options.Value.CodeLength;
    }

    public string Generate()
    {
        var chars = new char[_length];
        for (var i = 0; i < _length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}
