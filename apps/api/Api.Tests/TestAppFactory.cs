using Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

// Program.cs reads ConnectionStrings__Default / JWT_SECRET / Jwt__* / Cors__WebOrigin eagerly
// (`?? throw`) BEFORE builder.Build() runs. WebApplicationFactory's ConfigureWebHost/
// ConfigureAppConfiguration overrides are only injected at Build() time, which is too late for
// those eager reads — so overriding via process environment variables (picked up by the default
// AddEnvironmentVariables() source at CreateBuilder time) is the reliable way to inject test
// config here, and it matches the interfaces contract (values are "lidas de variavel de ambiente").
public class TestAppFactory : WebApplicationFactory<Program>
{
    public const string TestJwtSecret = "test-only-fixed-secret-key-not-used-outside-integration-tests";
    public const string TestIssuer = "vcard-api";
    public const string TestAudience = "vcard-web";

    private static string TestConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
        ?? "Host=localhost;Port=5432;Database=vcard_test;Username=vcard;Password=vcard;SSL Mode=Disable";

    public TestAppFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestConnectionString);
        Environment.SetEnvironmentVariable("JWT_SECRET", TestJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestAudience);
        Environment.SetEnvironmentVariable("Jwt__ExpiresMinutes", "20");
        Environment.SetEnvironmentVariable("Cors__WebOrigin", "http://localhost:3000");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE card_views, social_links, cards, users RESTART IDENTITY CASCADE");
    }
}
