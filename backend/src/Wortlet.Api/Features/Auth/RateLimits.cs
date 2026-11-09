using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Wortlet.Api.Features.Auth;

public static class RateLimits
{
    public const string Auth = "auth";

    /// <summary>Schützt Login/Registrierung vor Durchprobieren: N Versuche pro Minute und IP.</summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var perMinute = config.GetValue("RateLimiting:AuthPerMinute", 20);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Auth, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.OnRejected = async (context, ct) =>
            {
                const string message = "Zu viele Versuche. Bitte warte eine Minute.";
                var problem = new ProblemDetails { Status = 429, Title = message, Detail = message };
                problem.Extensions["code"] = "rate_limited";
                await context.HttpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", ct);
            };
        });
        return services;
    }
}
