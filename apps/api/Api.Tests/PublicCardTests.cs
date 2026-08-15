using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

// Helper de autenticacao proprio deste arquivo -- nome distinto de OwnershipTestAuth
// (CardOwnershipTests.cs) para nao colidir, ja que ambos sao `file static class` no
// mesmo namespace Api.Tests.
file static class PublicCardTestAuth
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

public class PublicCardTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetBySlug_ExistingSlug_Returns200_WithoutAuthHeader()
    {
        // PUB-01: qualquer pessoa, sem token, consegue ler o cartao publico por slug.
        var setupClient = factory.CreateClient();
        var (token, _) = await PublicCardTestAuth.RegisterAndGetTokenAsync(setupClient, "dono-publico@example.com");
        PublicCardTestAuth.SetBearer(setupClient, token);

        var created = await setupClient.PostAsJsonAsync("/cards", new
        {
            slug = "cartao-publico",
            fullName = "João Conceição",
            role = "Fotógrafo",
            company = "Estúdio Conceição",
            phone = "11999999999",
            email = "joao@example.com",
            whatsappNumber = "11999999999",
            pixKey = "joao@example.com",
            pixKeyType = "email",
        });
        created.EnsureSuccessStatusCode();

        // T-02-02 (rota publica fora do grupo autorizado): cliente NOVO, sem
        // SetBearer, exercitando a leitura publica -- guarda de regressao contra
        // o Pitfall 1 (RequireAuthorization herdado do grupo cards).
        var publicClient = factory.CreateClient();

        var response = await publicClient.GetAsync("/public/cards/cartao-publico");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_ExistingSlug_DoesNotLeakPrivateFields()
    {
        // PUB-05 / T-02-01 (nao vazar campos privados): a resposta publica so pode
        // conter identidade + links sociais, nunca campos de contato/pagamento/auditoria.
        var setupClient = factory.CreateClient();
        var (token, _) = await PublicCardTestAuth.RegisterAndGetTokenAsync(setupClient, "sem-vazamento@example.com");
        PublicCardTestAuth.SetBearer(setupClient, token);

        var created = await setupClient.PostAsJsonAsync("/cards", new
        {
            slug = "cartao-sem-vazamento",
            fullName = "Maria Conceição",
            role = "Consultora",
            company = "MC Consultoria",
            phone = "11988888888",
            email = "maria@example.com",
            whatsappNumber = "11988888888",
            pixKey = "maria@example.com",
            pixKeyType = "email",
        });
        created.EnsureSuccessStatusCode();

        var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/public/cards/cartao-sem-vazamento");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var forbiddenKey in new[]
        {
            "id", "userId", "phone", "email", "whatsappNumber", "pixKey", "pixKeyType",
            "pixConsentConfirmed", "isBranded", "createdAt", "updatedAt",
        })
        {
            Assert.False(body.TryGetProperty(forbiddenKey, out _), $"chave privada '{forbiddenKey}' nao deveria estar presente");
        }

        Assert.True(body.TryGetProperty("slug", out var slugProp));
        Assert.Equal("cartao-sem-vazamento", slugProp.GetString());
        Assert.True(body.TryGetProperty("fullName", out var fullNameProp));
        Assert.Equal("Maria Conceição", fullNameProp.GetString());
        Assert.True(body.TryGetProperty("role", out var roleProp));
        Assert.Equal("Consultora", roleProp.GetString());
        Assert.True(body.TryGetProperty("company", out var companyProp));
        Assert.Equal("MC Consultoria", companyProp.GetString());
        Assert.True(body.TryGetProperty("photoUrl", out _));
        Assert.True(body.TryGetProperty("socialLinks", out _));
    }

    [Fact]
    public async Task GetBySlug_UppercaseSlug_Returns200()
    {
        // Normalizacao via SlugService.Normalize deve valer tambem na leitura publica.
        var setupClient = factory.CreateClient();
        var (token, _) = await PublicCardTestAuth.RegisterAndGetTokenAsync(setupClient, "caixa-alta@example.com");
        PublicCardTestAuth.SetBearer(setupClient, token);

        var created = await setupClient.PostAsJsonAsync("/cards", new { slug = "cartao-caixa-baixa", fullName = "Pedro" });
        created.EnsureSuccessStatusCode();

        var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/public/cards/CARTAO-CAIXA-BAIXA");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_NonexistentSlug_Returns404()
    {
        // PUB-06: slug inexistente retorna 404 propria, sem exigir autenticacao.
        var publicClient = factory.CreateClient();

        var response = await publicClient.GetAsync("/public/cards/nao-existe-mesmo");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_ReturnsSocialLinksOrderedByDisplayOrder()
    {
        var setupClient = factory.CreateClient();
        var (token, _) = await PublicCardTestAuth.RegisterAndGetTokenAsync(setupClient, "com-links@example.com");
        PublicCardTestAuth.SetBearer(setupClient, token);

        var created = await setupClient.PostAsJsonAsync("/cards", new { slug = "cartao-com-links", fullName = "Ana" });
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = createdBody.GetProperty("id").GetString();

        var first = await setupClient.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform = "instagram", url = "https://instagram.com/ana" });
        first.EnsureSuccessStatusCode();
        var second = await setupClient.PostAsJsonAsync($"/cards/{cardId}/social-links", new { platform = "linkedin", url = "https://linkedin.com/in/ana" });
        second.EnsureSuccessStatusCode();

        var publicClient = factory.CreateClient();
        var response = await publicClient.GetAsync("/public/cards/cartao-com-links");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var socialLinks = body.GetProperty("socialLinks").EnumerateArray().ToList();
        Assert.Equal(2, socialLinks.Count);
        Assert.Equal(0, socialLinks[0].GetProperty("displayOrder").GetInt32());
        Assert.Equal("instagram", socialLinks[0].GetProperty("platform").GetString());
        Assert.Equal(1, socialLinks[1].GetProperty("displayOrder").GetInt32());
        Assert.Equal("linkedin", socialLinks[1].GetProperty("platform").GetString());

        foreach (var link in socialLinks)
            Assert.False(link.TryGetProperty("id", out _), "socialLinks publico nao deve expor id");
    }
}
