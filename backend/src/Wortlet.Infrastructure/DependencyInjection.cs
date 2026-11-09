using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wortlet.Application.Auth;
using Wortlet.Application.Common;
using Wortlet.Application.Onboarding;
using Wortlet.Application.Recognition;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Recognition;
using Wortlet.Infrastructure.Identity;
using Wortlet.Infrastructure.OpenRouter;
using Wortlet.Infrastructure.Persistence;
using Wortlet.Infrastructure.Persistence.Repositories;
using Wortlet.Infrastructure.Seed;
using Wortlet.Infrastructure.Storage;
using Wortlet.Infrastructure.Time;

namespace Wortlet.Infrastructure;

public static class DependencyInjection
{
    public const string PostgresProvider = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Datenordner (SQLite-Datei, erzeugter JWT-Schlüssel)
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IHostEnvironment>();
            var dir = config["Storage:DataDirectory"];
            if (string.IsNullOrWhiteSpace(dir)) dir = "data";
            return new DataDirectory(Path.IsPathRooted(dir) ? dir : Path.Combine(env.ContentRootPath, dir));
        });

        // Datenbank: SQLite (Standard) oder PostgreSQL, gewählt über Database:Provider
        services.AddDbContext<SqliteAppDbContext>((sp, o) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var cs = config.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(cs))
                cs = $"Data Source={Path.Combine(sp.GetRequiredService<DataDirectory>().Path, "wortlet.db")}";
            o.UseSqlite(cs);
        });
        services.AddDbContext<PostgresAppDbContext>((sp, o) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            o.UseNpgsql(config.GetConnectionString("Default"));
        });
        services.AddScoped<AppDbContext>(sp => IsPostgres(sp.GetRequiredService<IConfiguration>())
            ? sp.GetRequiredService<PostgresAppDbContext>()
            : sp.GetRequiredService<SqliteAppDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Automatische Sicherungen der SQLite-Datei
        services.AddOptions<BackupOptions>().BindConfiguration(BackupOptions.SectionName);
        services.AddSingleton<SqliteBackupService>();
        services.AddHostedService<DatabaseBackupWorker>();

        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();
        services.AddScoped<IRecognitionUsageRepository, RecognitionUsageRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IStarterContentProvider, EmbeddedStarterContentProvider>();

        // Benutzerkonten
        services
            .AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 1;
                o.Lockout.MaxFailedAccessAttempts = 10;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<GermanIdentityErrorDescriber>();
        services.AddScoped<IIdentityService, IdentityService>();

        // JWT
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName);
        services.AddSingleton<JwtSigningKeyProvider>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSigningKeyProvider, IOptions<JwtOptions>>((bearer, keys, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keys.Key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "email",
                };
            });
        services.AddAuthorization();

        // Bilderkennung über OpenRouter
        services.AddOptions<OpenRouterOptions>().BindConfiguration(OpenRouterOptions.SectionName);
        services.AddOptions<RecognitionOptions>().BindConfiguration(RecognitionOptions.SectionName);
        services.AddHttpClient<IVisionModelClient, OpenRouterVisionClient>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10); // Pro Modell gilt OpenRouter:TimeoutSeconds
        });

        return services;
    }

    internal static bool IsPostgres(IConfiguration config) =>
        string.Equals(config["Database:Provider"], PostgresProvider, StringComparison.OrdinalIgnoreCase);
}
