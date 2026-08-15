using Api.Contracts;
using Api.Data;
using Api.Data.Entities;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

// Leitura publica, sem autenticacao (PUB-01/PUB-05). Handler somente-leitura,
// AsNoTracking(), nenhum SaveChangesAsync neste arquivo (T-02-03) -- caminho
// deliberadamente isolado de CreateCardHandler/UpdateCardHandler.
public static class PublicCardEndpoints
{
    public static async Task<IResult> GetBySlugHandler(string slug, AppDbContext db)
    {
        var normalized = SlugService.Normalize(slug);

        var card = await db.Cards
            .AsNoTracking()
            .Include(c => c.SocialLinks.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Slug == normalized);

        if (card is null)
            return Results.NotFound();

        return Results.Ok(ToPublicDto(card));
    }

    private static PublicCardDto ToPublicDto(Card card) => new(
        card.Slug,
        card.FullName,
        card.Role,
        card.Company,
        card.PhotoUrl,
        card.SocialLinks
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new PublicSocialLinkDto(l.Platform, l.Url, l.DisplayOrder))
            .ToList());
}
