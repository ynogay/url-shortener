using Ari.UrlShortener.Background;
using Ari.UrlShortener.Data;
using Ari.UrlShortener.Features.ShortLinks;
using Ari.UrlShortener.Options;
using Ari.UrlShortener.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Apply any pending migrations on startup (fine for an MVP/single-instance deployment).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
