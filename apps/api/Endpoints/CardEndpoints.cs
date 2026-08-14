using System.Security.Claims;
using Api.Contracts;
using Api.Data;
using Api.Data.Entities;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Endpoints;

public static class CardEndpoints
{
    public static void MapCardEndpoints(this RouteGroupBuilder cards)
    {
        cards.MapGet("/slug-available", SlugAvailableHandler);
        cards.MapPost("/", CreateCardHandler);
        cards.MapGet("/me", GetMyCardHandler);
        cards.MapPut("/{id}", UpdateCardHandler);
    }

    private static async Task<IResult> SlugAvailableHandler(string? value, AppDbContext db)
    {
        var slug = SlugService.Normalize(value ?? string.Empty);

        if (!SlugService.IsValidFormat(slug))
            return Results.Ok(new { available = false, reason = "invalid" });

        if (SlugService.IsReserved(slug))
            return Results.Ok(new { available = false, reason = "reserved" });

        var taken = await db.Cards.AnyAsync(c => c.Slug == slug);
        if (taken)
            return Results.Ok(new { available = false, reason = "taken" });

        return Results.Ok(new { available = true, reason = (string?)null });
    }

    private static async Task<IResult> CreateCardHandler(CardWriteDto dto, ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        var alreadyHasCard = await db.Cards.AnyAsync(c => c.UserId == userId);
        if (alreadyHasCard)
            return Results.Conflict(new { error = "card_exists" });

        var slug = SlugService.Normalize(dto.Slug);
        if (!SlugService.IsValidFormat(slug))
            return Results.BadRequest(new { error = "slug_invalid" });
        if (SlugService.IsReserved(slug))
            return Results.Conflict(new { error = "slug_reserved" });

        var card = new Card
        {
            UserId = userId.Value,
            Slug = slug,
            FullName = dto.FullName,
            Role = dto.Role,
            Company = dto.Company,
            PhotoUrl = dto.PhotoUrl,
            Phone = dto.Phone,
            Email = dto.Email,
            WhatsappNumber = dto.WhatsappNumber,
            PixKey = dto.PixKey,
            PixKeyType = dto.PixKeyType,
            PixConsentConfirmed = false,
            IsBranded = true,
        };

        db.Cards.Add(card);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Results.Conflict(new { error = "slug_taken" });
        }

        return Results.Created($"/cards/{card.Id}", ToResponseDto(card));
    }

    private static async Task<IResult> GetMyCardHandler(ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        var card = await db.Cards
            .Include(c => c.SocialLinks.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (card is null)
            return Results.NotFound(new { error = "no_card" });

        return Results.Ok(ToResponseDto(card));
    }

    private static async Task<IResult> UpdateCardHandler(Guid id, CardWriteDto dto, ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        var card = await db.Cards
            .Include(c => c.SocialLinks.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (card is null)
            return Results.NotFound(new { error = "not_found" });

        // Checagem de posse obrigatoria (T-01-15/BOLA) -- .RequireAuthorization() sozinho
        // so prova que existe um token valido, nunca que o dono do token e o dono deste
        // cartao especifico.
        if (card.UserId != userId)
            return Results.Json(new { error = "not_owner" }, statusCode: StatusCodes.Status403Forbidden);

        var slug = SlugService.Normalize(dto.Slug);
        if (!SlugService.IsValidFormat(slug))
            return Results.BadRequest(new { error = "slug_invalid" });
        if (SlugService.IsReserved(slug))
            return Results.Conflict(new { error = "slug_reserved" });

        card.Slug = slug;
        card.FullName = dto.FullName;
        card.Role = dto.Role;
        card.Company = dto.Company;
        card.PhotoUrl = dto.PhotoUrl;
        card.Phone = dto.Phone;
        card.Email = dto.Email;
        card.WhatsappNumber = dto.WhatsappNumber;
        card.PixKey = dto.PixKey;
        card.PixKeyType = dto.PixKeyType;
        card.UpdatedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Results.Conflict(new { error = "slug_taken" });
        }

        return Results.Ok(ToResponseDto(card));
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static CardResponseDto ToResponseDto(Card card) => new(
        card.Id,
        card.Slug,
        card.FullName,
        card.Role,
        card.Company,
        card.PhotoUrl,
        card.Phone,
        card.Email,
        card.WhatsappNumber,
        card.PixKey,
        card.PixKeyType,
        card.PixConsentConfirmed,
        card.IsBranded,
        card.CreatedAt,
        card.UpdatedAt,
        card.SocialLinks
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new SocialLinkDto(l.Id, l.Platform, l.Url, l.DisplayOrder))
            .ToList());
}
