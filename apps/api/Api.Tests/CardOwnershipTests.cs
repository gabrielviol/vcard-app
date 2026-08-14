using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

file static class OwnershipTestAuth
{
    public static async Task<(string Token, string UserId)> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = "senha12345" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString()!;
        var userId = body.GetProperty("user").GetProperty("id").GetString()!;
        return (token, userId);
    }

    public static void SetBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

public class CardOwnershipTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Put_AnotherUsersCard_Returns403AndLeavesRecordUnchanged()
    {
        var client = factory.CreateClient();

        var (tokenA, _) = await OwnershipTestAuth.RegisterAndGetTokenAsync(client, "dono-original@example.com");
        OwnershipTestAuth.SetBearer(client, tokenA);
        var created = await client.PostAsJsonAsync("/cards", new { slug = "cartao-do-a", fullName = "Dono A" });
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = createdBody.GetProperty("id").GetString();

        var (tokenB, _) = await OwnershipTestAuth.RegisterAndGetTokenAsync(client, "invasor@example.com");
        OwnershipTestAuth.SetBearer(client, tokenB);

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "cartao-do-a",
            fullName = "Nome Alterado Pelo Invasor",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_owner", body.GetProperty("error").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var cardInDb = await db.Cards.FirstAsync(c => c.Slug == "cartao-do-a");
        Assert.Equal("Dono A", cardInDb.FullName);
    }

    [Fact]
    public async Task Put_NonexistentCard_Returns404()
    {
        var client = factory.CreateClient();
        var (token, _) = await OwnershipTestAuth.RegisterAndGetTokenAsync(client, "sem-cartao@example.com");
        OwnershipTestAuth.SetBearer(client, token);

        var response = await client.PutAsJsonAsync($"/cards/{Guid.NewGuid()}", new
        {
            slug = "nao-importa",
            fullName = "Nome Qualquer",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostCards_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/cards", new { slug = "sem-token", fullName = "Ninguém" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutCards_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/cards/{Guid.NewGuid()}", new { slug = "sem-token", fullName = "Ninguém" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostCards_SecondCardForSameUser_Returns409CardExists()
    {
        var client = factory.CreateClient();
        var (token, _) = await OwnershipTestAuth.RegisterAndGetTokenAsync(client, "dois-cartoes@example.com");
        OwnershipTestAuth.SetBearer(client, token);

        var first = await client.PostAsJsonAsync("/cards", new { slug = "primeiro-cartao", fullName = "Primeiro" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/cards", new { slug = "segundo-cartao", fullName = "Segundo" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("card_exists", body.GetProperty("error").GetString());
    }
}
