using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

file static class SocialLinkTestAuth
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

// Testes de integracao de links sociais (CARD-10/T-01-38..41): posse verificada em todo
// handler, display_order sempre resequenciado sem buracos, e URL/plataforma restritas.
// Nome comeca com "SocialLinkReorderTests" para casar com
// dotnet test --filter FullyQualifiedName~SocialLinkReorderTests.
public class SocialLinkReorderTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<Guid> CreateCardAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync("/cards", new { slug, fullName = "Dono dos Links" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<Guid> AddLinkAsync(HttpClient client, Guid cardId, string platform, string url = "https://example.com/perfil")
    {
        var response = await client.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform, url });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task AddThreeLinks_AssignsSequentialDisplayOrderStartingAtZero()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "tres-links@example.com"));
        var cardId = await CreateCardAsync(client, "tres-links");

        await AddLinkAsync(client, cardId, "instagram");
        await AddLinkAsync(client, cardId, "linkedin");
        await AddLinkAsync(client, cardId, "site");

        var me = await client.GetAsync("/cards/me");
        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        var links = body.GetProperty("socialLinks").EnumerateArray().ToArray();

        Assert.Equal(3, links.Length);
        for (var i = 0; i < 3; i++)
            Assert.Equal(i, links[i].GetProperty("displayOrder").GetInt32());
    }

    [Fact]
    public async Task ReorderLinks_InvertingOrder_PersistsInDbAndInGetMe()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "reordenar@example.com"));
        var cardId = await CreateCardAsync(client, "reordenar");

        var idA = await AddLinkAsync(client, cardId, "instagram");
        var idB = await AddLinkAsync(client, cardId, "linkedin");
        var idC = await AddLinkAsync(client, cardId, "site");

        var reversed = new[] { idC, idB, idA };
        var orderResponse = await client.PutAsJsonAsync($"/cards/{cardId}/social-links/order", new { orderedIds = reversed });
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbLinks = await db.SocialLinks
                .Where(l => l.CardId == cardId)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();

            Assert.Equal(reversed, dbLinks.Select(l => l.Id).ToArray());
        }

        var me = await client.GetAsync("/cards/me");
        var meBody = await me.Content.ReadFromJsonAsync<JsonElement>();
        var meIds = meBody.GetProperty("socialLinks").EnumerateArray()
            .Select(l => Guid.Parse(l.GetProperty("id").GetString()!))
            .ToArray();

        Assert.Equal(reversed, meIds);
    }

    [Fact]
    public async Task RemoveMiddleLink_ResequencesRemainingWithoutGap()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "remover-meio@example.com"));
        var cardId = await CreateCardAsync(client, "remover-meio");

        var idA = await AddLinkAsync(client, cardId, "instagram");
        var idB = await AddLinkAsync(client, cardId, "linkedin");
        var idC = await AddLinkAsync(client, cardId, "site");

        var deleteResponse = await client.DeleteAsync($"/cards/{cardId}/social-links/{idB}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remaining = await db.SocialLinks
            .Where(l => l.CardId == cardId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();

        Assert.Equal(2, remaining.Count);
        Assert.Equal(idA, remaining[0].Id);
        Assert.Equal(0, remaining[0].DisplayOrder);
        Assert.Equal(idC, remaining[1].Id);
        Assert.Equal(1, remaining[1].DisplayOrder);
    }

    [Fact]
    public async Task CreateLink_UnknownPlatform_Returns400PlatformInvalid()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "plataforma-invalida@example.com"));
        var cardId = await CreateCardAsync(client, "plataforma-invalida");

        var response = await client.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform = "facebook", url = "https://example.com/perfil" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("platform_invalid", body.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("http://example.com/perfil")]
    [InlineData("javascript:alert(1)")]
    public async Task CreateLink_InvalidUrl_Returns400UrlInvalid(string url)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, $"url-invalida-{suffix}@example.com"));
        var cardId = await CreateCardAsync(client, $"url-inv-{suffix}");

        var response = await client.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform = "instagram", url });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("url_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task OtherUser_PostDeleteAndReorder_AllReturn403NotOwner()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "dono-a@example.com"));
        var cardId = await CreateCardAsync(client, "cartao-do-dono-a");
        var linkId = await AddLinkAsync(client, cardId, "instagram");

        var invasorToken = await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "invasor-links@example.com");
        SocialLinkTestAuth.SetBearer(client, invasorToken);

        var postResponse = await client.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform = "linkedin", url = "https://example.com/outro" });
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        Assert.Equal("not_owner", (await postResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());

        var deleteResponse = await client.DeleteAsync($"/cards/{cardId}/social-links/{linkId}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        Assert.Equal("not_owner", (await deleteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());

        var orderResponse = await client.PutAsJsonAsync($"/cards/{cardId}/social-links/order", new { orderedIds = new[] { linkId } });
        Assert.Equal(HttpStatusCode.Forbidden, orderResponse.StatusCode);
        Assert.Equal("not_owner", (await orderResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());
    }

    [Fact]
    public async Task ReorderLinks_WithIdFromAnotherCard_Returns400OrderMismatch()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "cartao-alvo@example.com"));
        var cardId = await CreateCardAsync(client, "cartao-alvo");
        var ownLinkId = await AddLinkAsync(client, cardId, "instagram");

        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "cartao-estranho@example.com"));
        var otherCardId = await CreateCardAsync(client, "cartao-estranho");
        var foreignLinkId = await AddLinkAsync(client, otherCardId, "linkedin");

        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "cartao-alvo-de-novo@example.com"));

        // Reautentica como o dono original do cartao-alvo para testar o mismatch com o
        // dono correto (nao apenas 403 de posse).
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { email = "cartao-alvo@example.com", password = "senha12345" });
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        SocialLinkTestAuth.SetBearer(client, loginBody.GetProperty("accessToken").GetString()!);

        var response = await client.PutAsJsonAsync($"/cards/{cardId}/social-links/order", new { orderedIds = new[] { ownLinkId, foreignLinkId } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("order_mismatch", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SocialLinkEndpoints_WithoutToken_Return401()
    {
        var client = factory.CreateClient();
        SocialLinkTestAuth.SetBearer(client, await SocialLinkTestAuth.RegisterAndGetTokenAsync(client, "sem-token-links@example.com"));
        var cardId = await CreateCardAsync(client, "sem-token-links");
        var linkId = await AddLinkAsync(client, cardId, "instagram");

        client.DefaultRequestHeaders.Authorization = null;

        var postResponse = await client.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform = "linkedin", url = "https://example.com/outro" });
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/cards/{cardId}/social-links/{linkId}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);

        var orderResponse = await client.PutAsJsonAsync($"/cards/{cardId}/social-links/order", new { orderedIds = new[] { linkId } });
        Assert.Equal(HttpStatusCode.Unauthorized, orderResponse.StatusCode);
    }
}
