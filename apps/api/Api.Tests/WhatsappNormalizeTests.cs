using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

// Casos unitarios de WhatsappNormalizer -- mesmos casos da suite Vitest de
// apps/web/lib/whatsapp-normalize.test.ts (os dois arquivos sao espelhos).
public class WhatsappNormalizerUnitTests
{
    [Fact]
    public void Normalize_Ddd11Com8Digitos_AdicionaNonoDigito() =>
        Assert.Equal("5511987654321", WhatsappNormalizer.Normalize("1187654321"));

    [Fact]
    public void Normalize_Ddd11Com9Digitos_MantemInalterado() =>
        Assert.Equal("5511987654321", WhatsappNormalizer.Normalize("11987654321"));

    [Fact]
    public void Normalize_Ddd85Com8Digitos_NaoAdicionaNonoDigito() =>
        Assert.Equal("558587654321", WhatsappNormalizer.Normalize("8587654321"));

    [Fact]
    public void Normalize_Ddd27Com8Digitos_AdicionaNonoDigito() =>
        Assert.Equal("5527934567890", WhatsappNormalizer.Normalize("2734567890"));

    [Fact]
    public void Normalize_EntradaComDdiEMascaraCompleta_EhIdempotente() =>
        Assert.Equal("5511987654321", WhatsappNormalizer.Normalize("+55 (11) 98765-4321"));

    [Fact]
    public void Normalize_RemoveZeroDeTronco() =>
        Assert.Equal("5511987654321", WhatsappNormalizer.Normalize("011987654321"));

    [Fact]
    public void Normalize_RemoveMascara() =>
        Assert.Equal("5511987654321", WhatsappNormalizer.Normalize("(11) 98765-4321"));

    [Fact]
    public void Normalize_EntradaVazia_DevolveVazio() =>
        Assert.Equal(string.Empty, WhatsappNormalizer.Normalize(string.Empty));

    [Fact]
    public void Normalize_EntradaNula_DevolveVazio() =>
        Assert.Equal(string.Empty, WhatsappNormalizer.Normalize(null));

    [Fact]
    public void IsValid_RejeitaCincoDigitos() =>
        Assert.False(WhatsappNormalizer.IsValid("12345"));

    [Fact]
    public void IsValid_AceitaDezDigitosNacionais() =>
        Assert.True(WhatsappNormalizer.IsValid("8587654321"));

    [Fact]
    public void IsValid_AceitaOnzeDigitosNacionais() =>
        Assert.True(WhatsappNormalizer.IsValid("11987654321"));

    [Fact]
    public void IsValid_RejeitaVazio() =>
        Assert.False(WhatsappNormalizer.IsValid(""));
}

file static class WhatsappTestAuth
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

// Testes de integracao: provam que o servidor e a autoridade sobre o valor persistido
// em cards.whatsapp_number, independente do que o cliente enviar (T-01-22).
public class WhatsappNormalizeIntegrationTests(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PutCard_MaskedWhatsapp_PersistsAsPureDigitsWithDdi55()
    {
        var client = factory.CreateClient();
        var token = await WhatsappTestAuth.RegisterAndGetTokenAsync(client, "whatsapp-mascarado@example.com");
        WhatsappTestAuth.SetBearer(client, token);

        var created = await client.PostAsJsonAsync("/cards", new { slug = "whatsapp-mascarado", fullName = "Dono" });
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = createdBody.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "whatsapp-mascarado",
            fullName = "Dono",
            whatsappNumber = "(11) 98765-4321",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var card = await db.Cards.FirstAsync(c => c.Slug == "whatsapp-mascarado");
        Assert.Equal("5511987654321", card.WhatsappNumber);
    }

    [Fact]
    public async Task PutCard_Ddd85With8Digits_DoesNotReceiveSpuriousNinthDigit()
    {
        var client = factory.CreateClient();
        var token = await WhatsappTestAuth.RegisterAndGetTokenAsync(client, "ddd85@example.com");
        WhatsappTestAuth.SetBearer(client, token);

        var created = await client.PostAsJsonAsync("/cards", new { slug = "ddd85-teste", fullName = "Dono" });
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = createdBody.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "ddd85-teste",
            fullName = "Dono",
            whatsappNumber = "(85) 8765-4321",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var card = await db.Cards.FirstAsync(c => c.Slug == "ddd85-teste");
        Assert.Equal("558587654321", card.WhatsappNumber);
    }

    [Fact]
    public async Task PutCard_InvalidWhatsapp_Returns400WhatsappInvalid()
    {
        var client = factory.CreateClient();
        var token = await WhatsappTestAuth.RegisterAndGetTokenAsync(client, "whatsapp-invalido@example.com");
        WhatsappTestAuth.SetBearer(client, token);

        var created = await client.PostAsJsonAsync("/cards", new { slug = "whatsapp-invalido", fullName = "Dono" });
        created.EnsureSuccessStatusCode();
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = createdBody.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "whatsapp-invalido",
            fullName = "Dono",
            whatsappNumber = "123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("whatsapp_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostCard_EmptyWhatsapp_PersistsAsNull()
    {
        var client = factory.CreateClient();
        var token = await WhatsappTestAuth.RegisterAndGetTokenAsync(client, "whatsapp-vazio@example.com");
        WhatsappTestAuth.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/cards", new { slug = "whatsapp-vazio", fullName = "Dono" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("whatsappNumber").ValueKind);
    }
}
