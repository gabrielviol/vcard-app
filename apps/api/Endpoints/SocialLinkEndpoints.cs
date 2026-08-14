using System.Security.Claims;
using Api.Contracts;
using Api.Data;
using Api.Data.Entities;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class SocialLinkEndpoints
{
    public static void MapSocialLinkEndpoints(this RouteGroupBuilder cards)
    {
        cards.MapPost("/{cardId}/social-links", CreateSocialLinkHandler);
        cards.MapDelete("/{cardId}/social-links/{linkId}", DeleteSocialLinkHandler);
        cards.MapPut("/{cardId}/social-links/order", ReorderSocialLinksHandler);
    }

    private static async Task<IResult> CreateSocialLinkHandler(Guid cardId, SocialLinkWriteDto dto, ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        // Trava a linha do cartao (SELECT ... FOR UPDATE) para serializar criacoes
        // concorrentes de social link do MESMO cartao (WR-04) -- sem isso, duas
        // requisicoes concorrentes podem ler o mesmo snapshot de SocialLinks.Count e
        // persistir o mesmo display_order. A segunda transacao so le o cartao (e a
        // contagem atual de links) depois que a primeira libera a trava ao commitar.
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM cards WHERE id = {cardId} FOR UPDATE");

        var card = await db.Cards
            .Include(c => c.SocialLinks)
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card is null)
            return Results.NotFound(new { error = "not_found" });

        // Checagem de posse obrigatoria (T-01-38/BOLA) -- o linkId/cardId sozinhos nao
        // provam dono; a claim "sub" e a autoridade, sempre comparada com card.UserId.
        if (card.UserId != userId)
            return Results.Json(new { error = "not_owner" }, statusCode: StatusCodes.Status403Forbidden);

        if (!SocialLinkService.IsValidPlatform(dto.Platform))
            return Results.BadRequest(new { error = "platform_invalid" });

        if (!SocialLinkService.IsValidUrl(dto.Url))
            return Results.BadRequest(new { error = "url_invalid" });

        var link = new SocialLink
        {
            CardId = card.Id,
            Platform = dto.Platform.ToLowerInvariant(),
            Url = dto.Url,
            DisplayOrder = card.SocialLinks.Count,
        };

        db.SocialLinks.Add(link);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.Created($"/cards/{cardId}/social-links/{link.Id}", ToResponseDto(link));
    }

    private static async Task<IResult> DeleteSocialLinkHandler(Guid cardId, Guid linkId, ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        var card = await db.Cards
            .Include(c => c.SocialLinks.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card is null)
            return Results.NotFound(new { error = "not_found" });

        if (card.UserId != userId)
            return Results.Json(new { error = "not_owner" }, statusCode: StatusCodes.Status403Forbidden);

        // Confere que o link pertence a ESTE cardId, nao apenas que existe em algum lugar
        // (T-01-38) -- um linkId valido de outro cartao nao pode ser removido por aqui.
        var link = card.SocialLinks.FirstOrDefault(l => l.Id == linkId);
        if (link is null)
            return Results.NotFound(new { error = "not_found" });

        db.SocialLinks.Remove(link);

        var remaining = card.SocialLinks.Where(l => l.Id != linkId).OrderBy(l => l.DisplayOrder);
        SocialLinkService.Resequence(remaining);

        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> ReorderSocialLinksHandler(Guid cardId, SocialLinkOrderDto dto, ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        var card = await db.Cards
            .Include(c => c.SocialLinks)
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card is null)
            return Results.NotFound(new { error = "not_found" });

        if (card.UserId != userId)
            return Results.Json(new { error = "not_owner" }, statusCode: StatusCodes.Status403Forbidden);

        var orderedIds = dto.OrderedIds ?? [];
        var existingIds = card.SocialLinks.Select(l => l.Id).ToHashSet();

        // O conjunto recebido precisa corresponder EXATAMENTE ao conjunto de links do
        // cartao -- faltando, sobrando ou contendo id de outro cartao, tudo vira
        // order_mismatch (T-01-41), impedindo escrita cruzada disfarcada de reordenacao.
        if (orderedIds.Length != existingIds.Count || !orderedIds.ToHashSet().SetEquals(existingIds))
            return Results.BadRequest(new { error = "order_mismatch" });

        var linksById = card.SocialLinks.ToDictionary(l => l.Id);
        for (var i = 0; i < orderedIds.Length; i++)
            linksById[orderedIds[i]].DisplayOrder = i;

        await db.SaveChangesAsync();

        var ordered = card.SocialLinks
            .OrderBy(l => l.DisplayOrder)
            .Select(ToResponseDto)
            .ToList();

        return Results.Ok(ordered);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static SocialLinkResponseDto ToResponseDto(SocialLink link) =>
        new(link.Id, link.Platform, link.Url, link.DisplayOrder);
}
