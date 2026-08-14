using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

file static class TestAuth
{
    public static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = "senha12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    public static void SetBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

public class SlugTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("login")]
    [InlineData("Dashboard")]
    [InlineData("API")]
    public async Task CreateCard_ReservedSlug_Returns409SlugReserved(string reservedSlug)
    {
        var client = factory.CreateClient();
        var token = await TestAuth.RegisterAndGetTokenAsync(client, $"reserved-{reservedSlug.ToLowerInvariant()}@example.com");
        TestAuth.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/cards", new
        {
            slug = reservedSlug,
            fullName = "Dono do Cartão",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("slug_reserved", body.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("com espaço")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("a--b")]
    [InlineData("umnomecomtrintaetrescaracteresxxx")]
    public async Task CreateCard_InvalidFormat_Returns400SlugInvalid(string invalidSlug)
    {
        var client = factory.CreateClient();
        var token = await TestAuth.RegisterAndGetTokenAsync(client, $"invalid-{Guid.NewGuid():N}@example.com");
        TestAuth.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/cards", new
        {
            slug = invalidSlug,
            fullName = "Dono do Cartão",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("slug_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateCard_FreeSlug_Returns201()
    {
        var client = factory.CreateClient();
        var token = await TestAuth.RegisterAndGetTokenAsync(client, "livre@example.com");
        TestAuth.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/cards", new
        {
            slug = "gabriel-livre",
            fullName = "Gabriel Oliveira",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateCard_SlugAlreadyTakenByAnotherUser_Returns409SlugTaken()
    {
        var client = factory.CreateClient();

        var tokenA = await TestAuth.RegisterAndGetTokenAsync(client, "dono-a@example.com");
        TestAuth.SetBearer(client, tokenA);
        var first = await client.PostAsJsonAsync("/cards", new { slug = "mesmo-slug", fullName = "Dono A" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var tokenB = await TestAuth.RegisterAndGetTokenAsync(client, "dono-b@example.com");
        TestAuth.SetBearer(client, tokenB);
        var second = await client.PostAsJsonAsync("/cards", new { slug = "mesmo-slug", fullName = "Dono B" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("slug_taken", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SlugAvailable_ReturnsCorrectReasonForEachCase()
    {
        var client = factory.CreateClient();
        var token = await TestAuth.RegisterAndGetTokenAsync(client, "checa-disponibilidade@example.com");
        TestAuth.SetBearer(client, token);

        var reserved = await client.GetFromJsonAsync<JsonElement>("/cards/slug-available?value=login");
        Assert.False(reserved.GetProperty("available").GetBoolean());
        Assert.Equal("reserved", reserved.GetProperty("reason").GetString());

        var invalid = await client.GetFromJsonAsync<JsonElement>("/cards/slug-available?value=ab");
        Assert.False(invalid.GetProperty("available").GetBoolean());
        Assert.Equal("invalid", invalid.GetProperty("reason").GetString());

        var created = await client.PostAsJsonAsync("/cards", new { slug = "ocupado-teste", fullName = "Alguém" });
        created.EnsureSuccessStatusCode();

        var taken = await client.GetFromJsonAsync<JsonElement>("/cards/slug-available?value=ocupado-teste");
        Assert.False(taken.GetProperty("available").GetBoolean());
        Assert.Equal("taken", taken.GetProperty("reason").GetString());

        var available = await client.GetFromJsonAsync<JsonElement>("/cards/slug-available?value=slug-livre-de-verdade");
        Assert.True(available.GetProperty("available").GetBoolean());
        Assert.Equal(JsonValueKind.Null, available.GetProperty("reason").ValueKind);
    }

    [Fact]
    public async Task CreateCard_OnlySlugAndFullName_Returns201WithNullOptionalFields()
    {
        var client = factory.CreateClient();
        var token = await TestAuth.RegisterAndGetTokenAsync(client, "so-obrigatorios@example.com");
        TestAuth.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/cards", new
        {
            slug = "so-obrigatorios",
            fullName = "Só Obrigatórios",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("role").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("company").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("pixKey").ValueKind);
    }
}
