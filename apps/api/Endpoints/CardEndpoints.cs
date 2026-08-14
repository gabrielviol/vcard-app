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

        // Nunca confiar no valor ja "normalizado" que o cliente enviou (T-01-22) -- o
        // servidor e a autoridade para o valor persistido em whatsapp_number.
        if (!string.IsNullOrWhiteSpace(dto.WhatsappNumber) && !WhatsappNormalizer.IsValid(dto.WhatsappNumber))
            return Results.BadRequest(new { error = "whatsapp_invalid" });
        var normalizedWhatsapp = WhatsappNormalizer.Normalize(dto.WhatsappNumber);

        var pixValidation = ValidatePix(dto);
        if (pixValidation is not null)
            return pixValidation;

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
            WhatsappNumber = string.IsNullOrEmpty(normalizedWhatsapp) ? null : normalizedWhatsapp,
            PixKey = dto.PixKey,
            PixKeyType = dto.PixKeyType,
            // So persiste true quando o tipo e "cpf" -- trocar/enviar qualquer outro tipo
            // sempre zera o consentimento (T-01-31), mesmo que o cliente mande true por engano.
            PixConsentConfirmed = dto.PixKeyType == "cpf" && dto.PixConsentConfirmed,
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

        // Nunca confiar no valor ja "normalizado" que o cliente enviou (T-01-22) -- o
        // servidor e a autoridade para o valor persistido em whatsapp_number.
        if (!string.IsNullOrWhiteSpace(dto.WhatsappNumber) && !WhatsappNormalizer.IsValid(dto.WhatsappNumber))
            return Results.BadRequest(new { error = "whatsapp_invalid" });
        var normalizedWhatsapp = WhatsappNormalizer.Normalize(dto.WhatsappNumber);

        var pixValidation = ValidatePix(dto);
        if (pixValidation is not null)
            return pixValidation;

        card.Slug = slug;
        card.FullName = dto.FullName;
        card.Role = dto.Role;
        card.Company = dto.Company;
        card.PhotoUrl = dto.PhotoUrl;
        card.Phone = dto.Phone;
        card.Email = dto.Email;
        card.WhatsappNumber = string.IsNullOrEmpty(normalizedWhatsapp) ? null : normalizedWhatsapp;
        card.PixKey = dto.PixKey;
        card.PixKeyType = dto.PixKeyType;
        // So persiste true quando o tipo e "cpf" -- trocar para qualquer outro tipo sempre
        // zera o consentimento (T-01-31), mesmo que o cliente mande true por engano.
        card.PixConsentConfirmed = dto.PixKeyType == "cpf" && dto.PixConsentConfirmed;
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

    // Validacao Pix (CARD-06/CARD-07) compartilhada entre POST e PUT -- nunca confiar no
    // formato/consentimento reportado pelo cliente (T-01-27/T-01-28). Devolve null quando
    // o payload esta ok para persistir; devolve o IResult de erro caso contrario.
    private static IResult? ValidatePix(CardWriteDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.PixKeyType) && !PixValidationService.IsKnownType(dto.PixKeyType))
            return Results.BadRequest(new { error = "pix_type_invalid" });

        if (!string.IsNullOrWhiteSpace(dto.PixKey))
        {
            if (!PixValidationService.IsValid(dto.PixKeyType, dto.PixKey))
                return Results.BadRequest(new { error = "pix_key_invalid" });

            // CARD-07 (D-09): CPF exposto publicamente exige consentimento explicito,
            // verificado aqui a partir do valor persistido -- nao do estado efemero do
            // formulario (T-01-28).
            if (dto.PixKeyType == "cpf" && !dto.PixConsentConfirmed)
                return Results.BadRequest(new { error = "pix_consent_required" });
        }

        return null;
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
