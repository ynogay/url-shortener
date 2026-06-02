using Ari.UrlShortener.Options;
using Ari.UrlShortener.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ari.UrlShortener.Features.ShortLinks;

[ApiController]
public sealed class ShortLinksController : ControllerBase
{
    private readonly IShortLinkService _service;
    private readonly IUrlValidator _urlValidator;
    private readonly ShortLinkOptions _options;

    public ShortLinksController(
        IShortLinkService service,
        IUrlValidator urlValidator,
        IOptions<ShortLinkOptions> options)
    {
        _service = service;
        _urlValidator = urlValidator;
        _options = options.Value;
    }

    /// <summary>Creates a short link from a long URL.</summary>
    [HttpPost("/api/links")]
    [ProducesResponseType(typeof(CreateLinkResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLinkRequest request,
        CancellationToken cancellationToken)
    {
        var validation = _urlValidator.Validate(request.Url);
        if (!validation.IsValid)
        {
            ModelState.AddModelError(nameof(request.Url), validation.Error!);
            return ValidationProblem(ModelState);
        }

        var link = await _service.CreateAsync(validation.NormalizedUrl!, cancellationToken);

        var response = new CreateLinkResponse(
            link.Code,
            BuildShortUrl(link.Code),
            link.LongUrl,
            link.CreatedAtUtc,
            link.ExpiresAtUtc);

        return CreatedAtAction(nameof(RedirectToLong), new { code = link.Code }, response);
    }

    /// <summary>Redirects a short code to its destination, or 404 if invalid/expired/inactive.</summary>
    [HttpGet("/{code:regex(^[[A-Za-z0-9]]+$)}", Name = nameof(RedirectToLong))]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToLong(string code, CancellationToken cancellationToken)
    {
        var longUrl = await _service.ResolveAndCountClickAsync(code, cancellationToken);
        if (longUrl is null)
        {
            return NotFound();
        }

        return Redirect(longUrl);
    }

    private string BuildShortUrl(string code)
    {
        var baseAddress = string.IsNullOrWhiteSpace(_options.ShortUrlBaseAddress)
            ? $"{Request.Scheme}://{Request.Host}"
            : _options.ShortUrlBaseAddress.TrimEnd('/');

        return $"{baseAddress}/{code}";
    }
}
