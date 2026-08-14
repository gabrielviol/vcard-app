using System.Text;
using Api.Data;
using Api.Endpoints;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ok", database = "up" })
        : Results.Json(new { status = "error", database = "down" }, statusCode: 503);
});

app.MapAuthEndpoints();

var cards = app.MapGroup("/cards").RequireAuthorization();

// Placeholder so the group has a routable endpoint for RequireAuthorization to guard against
// (an empty group has no matched route, so unauthenticated requests would 404 before auth even
// runs) — anchors ACCT-05 now; the real handler lands in plan 01-03.
cards.MapPost("/", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

public partial class Program { }
