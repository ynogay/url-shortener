using Ari.UrlShortener.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Ari.UrlShortener.Features.ShortLinks;

/// <summary>
/// Action filter that validates the API key from the request header.
/// Returns 401 Unauthorized if the key is missing or invalid.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireApiKeyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiKeyOptions>>().Value;

        if (!context.HttpContext.Request.Headers.TryGetValue(options.HeaderName, out var headerValue))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var providedKey = headerValue.ToString();
        if (string.IsNullOrEmpty(options.Key) || !providedKey.Equals(options.Key, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await Task.CompletedTask;
    }
}
