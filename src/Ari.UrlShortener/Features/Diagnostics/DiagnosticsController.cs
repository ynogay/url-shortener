using Microsoft.AspNetCore.Mvc;

namespace Ari.UrlShortener.Features.Diagnostics;

/// <summary>
/// Temporary diagnostic endpoints for verifying global exception handling.
/// Remove before going to production.
/// </summary>
[ApiController]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpGet("/api/test/throw")]
    public IActionResult ThrowUnhandled()
    {
        throw new InvalidOperationException("Test exception from diagnostics endpoint.");
    }
}
