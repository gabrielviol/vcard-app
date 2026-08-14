using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

// Testes de integracao de photo_url (CARD-09/T-01-34): o servidor e a autoridade sobre
// o host permitido, independente do que o cliente enviar. Nome comeca com "PhotoUrlTests"
// (mesmo prefixo do arquivo) para casar com dotnet test --filter FullyQualifiedName~PhotoUrlTests
// (operador ~ do xUnit e Contains, nao prefixo -- mesma armadilha documentada em
// PixValidationTests.cs/01-05-SUMMARY.md).
public class PhotoUrlTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = "senha12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpClient client, string cardId)> CreateCardAsync(TestAppFactory factory, string email, string slug)
    {
        var client = factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await client.PostAsJsonAsync("/cards", new { slug, fullName = "Dono do Cartão" });
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        return (client, body.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task PutCard_HostPermitido_Returns200EPersisteColuna()
    {
        var (client, cardId) = await CreateCardAsync(factory, "photo-host-permitido@example.com", "photo-host-permitido");
        const string photoUrl = "https://abc123.public.blob.vercel-storage.com/foto-xyz.png";

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "photo-host-permitido",
            fullName = "Dono do Cartão",
            photoUrl,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var card = await db.Cards.FirstAsync(c => c.Slug == "photo-host-permitido");
        Assert.Equal(photoUrl, card.PhotoUrl);
    }

    [Fact]
    public async Task PutCard_HttpDoMesmoHost_Returns400PhotoUrlInvalid()
    {
        var (client, cardId) = await CreateCardAsync(factory, "photo-http@example.com", "photo-http");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "photo-http",
            fullName = "Dono do Cartão",
            photoUrl = "http://abc123.public.blob.vercel-storage.com/foto-xyz.png",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("photo_url_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutCard_HostExterno_Returns400PhotoUrlInvalid()
    {
        var (client, cardId) = await CreateCardAsync(factory, "photo-host-externo@example.com", "photo-host-externo");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "photo-host-externo",
            fullName = "Dono do Cartão",
            photoUrl = "https://exemplo.com/foto.png",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("photo_url_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutCard_EsquemaJavascript_Returns400PhotoUrlInvalid()
    {
        var (client, cardId) = await CreateCardAsync(factory, "photo-javascript@example.com", "photo-javascript");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "photo-javascript",
            fullName = "Dono do Cartão",
            photoUrl = "javascript:alert(1)",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("photo_url_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutCard_SemPhotoUrl_Returns200ComColunaNula()
    {
        var (client, cardId) = await CreateCardAsync(factory, "photo-sem-url@example.com", "photo-sem-url");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "photo-sem-url",
            fullName = "Dono do Cartão",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("photoUrl").ValueKind);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var card = await db.Cards.FirstAsync(c => c.Slug == "photo-sem-url");
        Assert.Null(card.PhotoUrl);
    }

    [Fact]
    public async Task PostCard_HostPermitido_Returns201EPersisteColuna()
    {
        var client = factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client, "photo-post-ok@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        const string photoUrl = "https://abc123.public.blob.vercel-storage.com/foto-post.png";

        var response = await client.PostAsJsonAsync("/cards", new
        {
            slug = "photo-post-ok",
            fullName = "Dono do Cartão",
            photoUrl,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(photoUrl, body.GetProperty("photoUrl").GetString());
    }
}
