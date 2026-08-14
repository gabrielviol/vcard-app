using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

// Casos unitarios de PixValidationService -- mesmos casos da suite Vitest de
// apps/web/lib/pix-validation.test.ts (os dois arquivos sao espelhos).
//
// Nome da classe comeca com "PixValidationTests" (mesmo prefixo do arquivo) de proposito:
// `dotnet test --filter FullyQualifiedName~PixValidationTests` usa contains, nao prefixo de
// palavra -- "PixValidationServiceUnitTests" NAO seria capturado pelo filtro porque "Service"
// quebra a sequencia contigua "PixValidationTests" exigida pelo operador `~`.
public class PixValidationTests_Unit
{
    [Theory]
    [InlineData("295.379.955-93")]
    [InlineData("29537995593")]
    public void IsValid_Cpf_AceitaCpfValido(string cpf) =>
        Assert.True(PixValidationService.IsValid("cpf", cpf));

    [Fact]
    public void IsValid_Cpf_RejeitaDigitoVerificadorErrado() =>
        Assert.False(PixValidationService.IsValid("cpf", "29537995594"));

    [Theory]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    public void IsValid_Cpf_RejeitaSequenciaRepetida(string cpf) =>
        Assert.False(PixValidationService.IsValid("cpf", cpf));

    [Fact]
    public void IsValid_Cnpj_AceitaNumericoValido() =>
        Assert.True(PixValidationService.IsValid("cnpj", "54.550.752/0001-55"));

    [Fact]
    public void IsValid_Cnpj_RejeitaNumericoInvalido() =>
        Assert.False(PixValidationService.IsValid("cnpj", "54.550.752/0001-56"));

    [Theory]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12ABC34501DE35")]
    public void IsValid_Cnpj_AceitaAlfanumericoDaRfb(string cnpj) =>
        // Vetor oficial (exemplo canonico da RFB, pergunta 14 do PDF) -- mesmo caso
        // coberto em pix-validation.test.ts (Open Question 1 do 01-RESEARCH.md).
        Assert.True(PixValidationService.IsValid("cnpj", cnpj));

    [Fact]
    public void IsValid_Email_RejeitaInvalido() =>
        Assert.False(PixValidationService.IsValid("email", "nao-e-email"));

    [Fact]
    public void IsValid_Email_AceitaValido() =>
        Assert.True(PixValidationService.IsValid("email", "dono@example.com"));

    [Fact]
    public void IsValid_Telefone_AceitaComEsemDdi()
    {
        Assert.True(PixValidationService.IsValid("telefone", "+55 11 98765-4321"));
        Assert.True(PixValidationService.IsValid("telefone", "11987654321"));
    }

    [Fact]
    public void IsValid_Aleatoria_AceitaUuidV4Valido() =>
        Assert.True(PixValidationService.IsValid("aleatoria", "9c858901-8a57-4791-81fe-4c455b099bc9"));

    [Fact]
    public void IsValid_Aleatoria_RejeitaUuidV1() =>
        Assert.False(PixValidationService.IsValid("aleatoria", "9c858901-8a57-1791-81fe-4c455b099bc9"));

    [Fact]
    public void IsValid_Aleatoria_RejeitaStringQualquer() =>
        Assert.False(PixValidationService.IsValid("aleatoria", "nao-e-um-uuid"));

    [Fact]
    public void IsKnownType_RejeitaTipoDesconhecido() =>
        Assert.False(PixValidationService.IsKnownType("bitcoin"));

    [Theory]
    [InlineData("cpf")]
    [InlineData("cnpj")]
    [InlineData("email")]
    [InlineData("telefone")]
    [InlineData("aleatoria")]
    public void IsKnownType_AceitaOsCincoTipos(string type) =>
        Assert.True(PixValidationService.IsKnownType(type));
}

file static class PixTestAuth
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

// Testes de integracao: provam que o servidor e a autoridade sobre validacao de formato
// e sobre o consentimento de CPF, independente do que o cliente enviar (T-01-27/T-01-28).
// Nome comeca com "PixValidationTests" pelo mesmo motivo documentado acima.
public class PixValidationTests_Integration(TestAppFactory factory) : IClassFixture<TestAppFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<(HttpClient client, string cardId)> CreateCardAsync(TestAppFactory factory, string email, string slug)
    {
        var client = factory.CreateClient();
        var token = await PixTestAuth.RegisterAndGetTokenAsync(client, email);
        PixTestAuth.SetBearer(client, token);

        var created = await client.PostAsJsonAsync("/cards", new { slug, fullName = "Dono do Cartão" });
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        return (client, body.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task PutCard_CpfValidoSemConsentimento_Returns400PixConsentRequired()
    {
        var (client, cardId) = await CreateCardAsync(factory, "cpf-sem-consentimento@example.com", "cpf-sem-consentimento");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "cpf-sem-consentimento",
            fullName = "Dono do Cartão",
            pixKey = "295.379.955-93",
            pixKeyType = "cpf",
            pixConsentConfirmed = false,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pix_consent_required", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutCard_CpfValidoComConsentimento_Returns200EPersisteConsentimento()
    {
        var (client, cardId) = await CreateCardAsync(factory, "cpf-com-consentimento@example.com", "cpf-com-consentimento");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "cpf-com-consentimento",
            fullName = "Dono do Cartão",
            pixKey = "295.379.955-93",
            pixKeyType = "cpf",
            pixConsentConfirmed = true,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var card = await db.Cards.FirstAsync(c => c.Slug == "cpf-com-consentimento");
        Assert.True(card.PixConsentConfirmed);
        Assert.Equal("cpf", card.PixKeyType);
    }

    [Fact]
    public async Task PutCard_CpfInvalidoComConsentimento_Returns400PixKeyInvalid()
    {
        var (client, cardId) = await CreateCardAsync(factory, "cpf-invalido@example.com", "cpf-invalido");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "cpf-invalido",
            fullName = "Dono do Cartão",
            pixKey = "11111111111",
            pixKeyType = "cpf",
            pixConsentConfirmed = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pix_key_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutCard_ChaveAleatoriaComUuidInvalido_Returns400PixKeyInvalid()
    {
        var (client, cardId) = await CreateCardAsync(factory, "aleatoria-invalida@example.com", "aleatoria-invalida");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "aleatoria-invalida",
            fullName = "Dono do Cartão",
            pixKey = "nao-e-um-uuid",
            pixKeyType = "aleatoria",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pix_key_invalid", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PutCard_TrocaDeCpfParaEmail_ZeraPixConsentConfirmedNoBanco()
    {
        var (client, cardId) = await CreateCardAsync(factory, "troca-tipo@example.com", "troca-tipo");

        var withCpf = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "troca-tipo",
            fullName = "Dono do Cartão",
            pixKey = "295.379.955-93",
            pixKeyType = "cpf",
            pixConsentConfirmed = true,
        });
        Assert.Equal(HttpStatusCode.OK, withCpf.StatusCode);

        var withEmail = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "troca-tipo",
            fullName = "Dono do Cartão",
            pixKey = "dono@example.com",
            pixKeyType = "email",
            pixConsentConfirmed = true, // valor "grudado" do form -- servidor deve ignorar (T-01-31)
        });
        Assert.Equal(HttpStatusCode.OK, withEmail.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Api.Data.AppDbContext>();
        var card = await db.Cards.FirstAsync(c => c.Slug == "troca-tipo");
        Assert.False(card.PixConsentConfirmed);
        Assert.Equal("email", card.PixKeyType);
    }

    [Fact]
    public async Task PostCard_SemNenhumCampoPix_Returns201()
    {
        var client = factory.CreateClient();
        var token = await PixTestAuth.RegisterAndGetTokenAsync(client, "sem-pix@example.com");
        PixTestAuth.SetBearer(client, token);

        var response = await client.PostAsJsonAsync("/cards", new { slug = "sem-pix", fullName = "Dono do Cartão" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("pixKey").ValueKind);
        Assert.False(body.GetProperty("pixConsentConfirmed").GetBoolean());
    }

    [Fact]
    public async Task PutCard_TipoPixDesconhecido_Returns400PixTypeInvalid()
    {
        var (client, cardId) = await CreateCardAsync(factory, "tipo-desconhecido@example.com", "tipo-desconhecido");

        var response = await client.PutAsJsonAsync($"/cards/{cardId}", new
        {
            slug = "tipo-desconhecido",
            fullName = "Dono do Cartão",
            pixKeyType = "bitcoin",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pix_type_invalid", body.GetProperty("error").GetString());
    }
}
