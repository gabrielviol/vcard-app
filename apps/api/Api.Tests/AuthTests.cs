using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests;

file static class TestTokens
{
    public static string Create(
        string secret,
        string issuer,
        string audience,
        string subject,
        string email,
        DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Email, email),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class RegisterTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ValidData_Returns201WithAccessToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "joao@example.com",
            password = "senha12345",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal("joao@example.com", body.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_ValidData_HashesPasswordWithBCryptPrefix()
    {
        var client = factory.CreateClient();
        const string plainPassword = "senha12345";

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "conceicao@example.com",
            password = plainPassword,
        });
        response.EnsureSuccessStatusCode();

        var stored = await GetPasswordHashAsync("conceicao@example.com");

        Assert.NotNull(stored);
        Assert.NotEqual(plainPassword, stored);
        Assert.StartsWith("$2", stored);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = factory.CreateClient();
        var payload = new { email = "duplicado@example.com", password = "senha12345" };

        var first = await client.PostAsJsonAsync("/auth/register", payload);
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private async Task<string?> GetPasswordHashAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user?.PasswordHash;
    }
}

public class LoginTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsTokenWithMatchingClaims()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "login-ok@example.com",
            password = "senha12345",
        });
        register.EnsureSuccessStatusCode();
        var registerBody = await register.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registerBody.GetProperty("user").GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "login-ok@example.com",
            password = "senha12345",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(accessToken));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(userId, jwt.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("login-ok@example.com", jwt.Claims.First(c => c.Type == "email").Value);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericMessage()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "login-wrong@example.com",
            password = "senha12345",
        });
        register.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "login-wrong@example.com",
            password = "senhaErrada",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_credentials", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Login_NonexistentEmail_Returns401WithSameGenericMessage()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "nao-existe@example.com",
            password = "qualquer123",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_credentials", body.GetProperty("error").GetString());
    }
}

public class AuthGuardTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200WithCorrectEmail()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email = "guard-ok@example.com",
            password = "senha12345",
        });
        register.EnsureSuccessStatusCode();
        var registerBody = await register.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = registerBody.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("guard-ok@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_WithExpiredToken_Returns401()
    {
        var client = factory.CreateClient();
        var expiredToken = TestTokens.Create(
            TestAppFactory.TestJwtSecret,
            TestAppFactory.TestIssuer,
            TestAppFactory.TestAudience,
            Guid.NewGuid().ToString(),
            "expired@example.com",
            DateTime.UtcNow.AddMinutes(-10));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithWrongSignature_Returns401()
    {
        var client = factory.CreateClient();
        var wrongKeyToken = TestTokens.Create(
            "a-completely-different-signing-key-not-matching-the-app-secret",
            TestAppFactory.TestIssuer,
            TestAppFactory.TestAudience,
            Guid.NewGuid().ToString(),
            "wrongkey@example.com",
            DateTime.UtcNow.AddMinutes(20));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongKeyToken);
        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostCards_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/cards", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
