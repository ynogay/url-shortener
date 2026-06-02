using Ari.UrlShortener.Data;
using Ari.UrlShortener.Domain;
using Ari.UrlShortener.Options;
using Ari.UrlShortener.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ari.UrlShortener.Features.ShortLinks;

public sealed class ShortLinkService : IShortLinkService
{
    // SQL Server error numbers for unique constraint / unique index violations.
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    private readonly AppDbContext _db;
    private readonly ICodeGenerator _codeGenerator;
    private readonly ShortLinkOptions _options;
    private readonly ILogger<ShortLinkService> _logger;

    public ShortLinkService(
        AppDbContext db,
        ICodeGenerator codeGenerator,
        IOptions<ShortLinkOptions> options,
        ILogger<ShortLinkService> logger)
    {
        _db = db;
        _codeGenerator = codeGenerator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ShortLink> CreateAsync(string longUrl, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        for (var attempt = 1; attempt <= _options.MaxGenerationAttempts; attempt++)
        {
            var link = new ShortLink
            {
                Code = _codeGenerator.Generate(),
                LongUrl = longUrl,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMonths(_options.ExpiryMonths),
                IsActive = true,
                ClickCount = 0,
            };

            _db.ShortLinks.Add(link);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return link;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Code collided with an existing one — detach and try a fresh code.
                _db.Entry(link).State = EntityState.Detached;
                _logger.LogWarning("Short code collision on attempt {Attempt}; retrying.", attempt);
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a unique short code after {_options.MaxGenerationAttempts} attempts.");
    }

    public async Task<string?> ResolveAndCountClickAsync(string code, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var match = await _db.ShortLinks
            .Where(x => x.Code == code && x.IsActive && x.ExpiresAtUtc > now)
            .Select(x => new { x.Id, x.LongUrl })
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return null;
        }

        // Increment in the database so concurrent redirects don't lose counts.
        await _db.ShortLinks
            .Where(x => x.Id == match.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ClickCount, x => x.ClickCount + 1),
                cancellationToken);

        return match.LongUrl;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql &&
        sql.Number is UniqueConstraintViolation or UniqueIndexViolation;
}
