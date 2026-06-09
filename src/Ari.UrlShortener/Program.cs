using Ari.UrlShortener.Background;
using Ari.UrlShortener.Data;
using Ari.UrlShortener.Features.ShortLinks;
using Ari.UrlShortener.Infrastructure;
using Ari.UrlShortener.Options;
using Ari.UrlShortener.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;

// Bootstrap NLog before anything else so startup failures are captured.
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("NLog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with NLog.
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Options
    builder.Services.Configure<ShortLinkOptions>(
        builder.Configuration.GetSection(ShortLinkOptions.SectionName));
    builder.Services.Configure<ApiKeyOptions>(
        builder.Configuration.GetSection(ApiKeyOptions.SectionName));

    // Data
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

    // Application services
    builder.Services.AddSingleton<ICodeGenerator, Base62CodeGenerator>();
    builder.Services.AddSingleton<IUrlValidator, UrlValidator>();
    builder.Services.AddScoped<IShortLinkService, ShortLinkService>();

    // Background jobs
    builder.Services.AddHostedService<ExpiredLinkCleanupService>();

    // Global exception handling
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    // Apply any pending migrations on startup (fine for an MVP/single-instance deployment).
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // Must be first in the pipeline so exceptions from all downstream middleware are caught.
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
