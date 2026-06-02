# Ari.UrlShortener

A simple URL shortener MVP built on **ASP.NET Core 8**, **EF Core 8**, and **SQL Server**.

Create a short link from a long URL, then redirect through the generated 7-character
base62 code. Links expire after 6 months and clicks are counted.

## Requirements

- [.NET SDK 8+](https://dotnet.microsoft.com/download) (the project targets `net8.0`)
- SQL Server. The default connection string uses **SQL Server LocalDB**
  (`(localdb)\MSSQLLocalDB`), which ships with Visual Studio / the SQL Server tools.

## Getting started

```bash
# from the repo root
dotnet run --project src/Ari.UrlShortener
```

On startup the app applies EF Core migrations automatically (creates the
`AriUrlShortener` database if it does not exist). In Development, Swagger UI is
served at `/swagger`.

To point at a different database, edit `ConnectionStrings:Default` in
[`appsettings.json`](src/Ari.UrlShortener/appsettings.json).

## API

### Create a short link

```http
POST /api/links
Content-Type: application/json

{ "url": "https://www.example.com/some/very/long/path?q=1" }
```

`201 Created`:

```json
{
  "code": "mtUoMlH",
  "shortUrl": "http://localhost:5180/mtUoMlH",
  "longUrl": "https://www.example.com/some/very/long/path?q=1",
  "createdAtUtc": "2026-06-02T13:21:39.97Z",
  "expiresAtUtc": "2026-12-02T13:21:39.97Z"
}
```

Returns `400 Bad Request` when the URL fails validation:

- only `http`/`https` schemes are allowed
- `localhost` is rejected
- private/reserved IP literals are rejected (`10/8`, `172.16/12`, `192.168/16`,
  `127/8`, `169.254/16`, IPv6 loopback/link-local/unique-local)
- maximum URL length is 2048 characters

> Validation performs literal checks only — hostnames are **not** DNS-resolved
> (the service is assumed to run in a trusted internal environment).

### Redirect

```http
GET /{code}
```

- `302 Found` with a `Location` header when the code exists, is active, and is not
  expired (and `ClickCount` is incremented).
- `404 Not Found` when the code does not exist, is inactive, or is expired. The same
  response is used for all three so existence is not leaked.

## How it works

- **Codes** are 7-character base62 (`[0-9A-Za-z]`) generated with a cryptographically
  strong RNG. Creation retries on the (rare) unique-code collision.
- **Expiry** is `CreatedAtUtc + 6 months`. The redirect path checks expiry live, so
  correctness never depends on the cleanup job.
- **Click counting** uses an atomic SQL `UPDATE` (`ExecuteUpdateAsync`) so concurrent
  redirects don't lose counts.
- **Cleanup job** ([`ExpiredLinkCleanupService`](src/Ari.UrlShortener/Background/ExpiredLinkCleanupService.cs))
  runs once at startup and every 24 hours, bulk-marking expired links as inactive.

## Configuration

All knobs live under the `ShortLink` section of `appsettings.json` and bind to
[`ShortLinkOptions`](src/Ari.UrlShortener/Options/ShortLinkOptions.cs):

| Setting | Default | Description |
|---|---|---|
| `CodeLength` | `7` | Number of base62 characters in a code |
| `ExpiryMonths` | `6` | Link lifetime after creation |
| `MaxUrlLength` | `2048` | Maximum accepted input URL length |
| `MaxGenerationAttempts` | `5` | Code-collision retries before failing |
| `CleanupIntervalHours` | `24` | How often the cleanup job runs |
| `ShortUrlBaseAddress` | `""` | Base address for the returned short URL; falls back to the request host when empty |

## Project structure

```
src/Ari.UrlShortener/
├─ Program.cs                 # DI, middleware, migration-on-startup
├─ appsettings.json           # connection string + ShortLink options
├─ Domain/ShortLink.cs        # entity
├─ Data/                      # DbContext, EF configuration, migrations
├─ Features/ShortLinks/       # controller, DTOs, service
├─ Services/                  # base62 code generator, URL validator
├─ Options/                   # ShortLinkOptions
└─ Background/                # daily expired-link cleanup
```

## Database migrations

A migration is applied automatically on startup. To manage migrations manually:

```bash
dotnet ef migrations add <Name> --project src/Ari.UrlShortener --output-dir Data/Migrations
dotnet ef database update --project src/Ari.UrlShortener
```
