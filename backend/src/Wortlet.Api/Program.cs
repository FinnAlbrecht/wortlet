using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Api.Common;
using Wortlet.Api.Features.Auth;
using Wortlet.Application;
using Wortlet.Application.Common;
using Wortlet.Infrastructure;
using Wortlet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ValidationResponses.Create);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // Hinter dem Proxy von Render, Fly.io, Azure usw. die echte Client-IP verwenden.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Map("/api/{**rest}", () => Results.Problem(statusCode: 404, title: "Diese Adresse gibt es nicht."));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
