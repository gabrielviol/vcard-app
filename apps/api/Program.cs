using System.Text;
using System.Threading.RateLimiting;
using Api.Data;
using Api.Endpoints;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings__Default not set");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET not set");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT claim types as issued ("sub", "email") instead of ASP.NET Core's default
        // remapping to long ClaimTypes.* URIs (e.g. "sub" -> NameIdentifier) — AuthService issues
        // JwtRegisteredClaimNames.Sub/Email and endpoint handlers read those exact claim types.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = jwtKey,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder();

builder.Services.AddScoped<AuthService>();

// Limitador fixed-window por IP (WR-03) -- generoso o suficiente para nao incomodar
// uso legitimo na janela de MVP, mas fecha a exposicao de brute-force/credential-stuffing
// sem estado/lockout em /auth/login.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

var webOrigin = builder.Configuration["Cors:WebOrigin"]
    ?? throw new InvalidOperationException("Cors__WebOrigin not set");

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy.WithOrigins(webOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ok", database = "up" })
        : Results.Json(new { status = "error", database = "down" }, statusCode: 503);
});

// PUB-01/T-02-02: rota publica top-level em `app`, nunca dentro do grupo `cards`
// abaixo -- `.RequireAuthorization()` e aplicado no nivel do grupo, entao qualquer
// rota mapeada nele herdaria a exigencia de Bearer token (02-RESEARCH.md Pitfall 1).
app.MapGet("/public/cards/{slug}", PublicCardEndpoints.GetBySlugHandler);

app.MapAuthEndpoints();

var cards = app.MapGroup("/cards").RequireAuthorization();
cards.MapCardEndpoints();
cards.MapSocialLinkEndpoints();

app.Run();

public partial class Program { }
