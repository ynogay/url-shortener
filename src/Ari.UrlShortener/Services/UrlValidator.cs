using System.Net;
using System.Net.Sockets;
using Ari.UrlShortener.Options;
using Microsoft.Extensions.Options;

namespace Ari.UrlShortener.Services;

/// <summary>
/// Validates input URLs for the shortener:
/// http/https only, length-capped, and rejecting localhost / private / reserved hosts.
/// Performs literal checks only — hostnames are not DNS-resolved (internal-service assumption).
/// </summary>
public sealed class UrlValidator : IUrlValidator
{
    private readonly int _maxUrlLength;

    public UrlValidator(IOptions<ShortLinkOptions> options)
    {
        _maxUrlLength = options.Value.MaxUrlLength;
    }

    public UrlValidationResult Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlValidationResult.Fail("URL is required.");
        }

        url = url.Trim();

        if (url.Length > _maxUrlLength)
        {
            return UrlValidationResult.Fail($"URL exceeds the maximum length of {_maxUrlLength} characters.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return UrlValidationResult.Fail("URL is not a valid absolute URI.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlValidationResult.Fail("Only http and https URLs are allowed.");
        }

        var host = uri.DnsSafeHost;

        if (string.IsNullOrEmpty(host))
        {
            return UrlValidationResult.Fail("URL must contain a host.");
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return UrlValidationResult.Fail("localhost URLs are not allowed.");
        }

        if (IPAddress.TryParse(host, out var ip) && IsPrivateOrReserved(ip))
        {
            return UrlValidationResult.Fail("URLs pointing at private or reserved IP addresses are not allowed.");
        }

        return UrlValidationResult.Ok(uri.AbsoluteUri);
    }

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        // Unwrap IPv4-mapped IPv6 addresses (e.g. ::ffff:10.0.0.1) before range checks.
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPrivateOrReservedIPv4(address),
            AddressFamily.InterNetworkV6 => address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || IsIPv6UniqueLocal(address),
            _ => true, // Unknown families are rejected to be safe.
        };
    }

    private static bool IsPrivateOrReservedIPv4(IPAddress address)
    {
        var b = address.GetAddressBytes();

        // 0.0.0.0/8 "this network"
        if (b[0] == 0) return true;
        // 10.0.0.0/8
        if (b[0] == 10) return true;
        // 127.0.0.0/8 loopback (also covered by IsLoopback, kept for clarity)
        if (b[0] == 127) return true;
        // 169.254.0.0/16 link-local
        if (b[0] == 169 && b[1] == 254) return true;
        // 172.16.0.0/12
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
        // 192.168.0.0/16
        if (b[0] == 192 && b[1] == 168) return true;

        return false;
    }

    // fc00::/7 unique local addresses (not exposed as a property by IPAddress).
    private static bool IsIPv6UniqueLocal(IPAddress address)
    {
        var first = address.GetAddressBytes()[0];
        return (first & 0xFE) == 0xFC;
    }
}
