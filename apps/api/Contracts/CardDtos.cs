namespace Api.Contracts;

// DTO unico de escrita do cartao -- aceito por POST /cards e PUT /cards/{id}. Ja contempla
// os campos dos slices seguintes (04-07) para que aqueles planos so acrescentem
// validacao/normalizacao, nao reestruturem o handler. Obrigatorios na escrita: Slug e
// FullName (D-04); todo o resto pode ficar nulo indefinidamente.
public record CardWriteDto(
    string Slug,
    string FullName,
    string? Role,
    string? Company,
    string? PhotoUrl,
    string? Phone,
    string? Email,
    string? WhatsappNumber,
    string? PixKey,
    string? PixKeyType,
    bool PixConsentConfirmed);

public record SocialLinkDto(Guid Id, string Platform, string Url, int DisplayOrder);

// Nunca expoe User nem PasswordHash.
public record CardResponseDto(
    Guid Id,
    string Slug,
    string FullName,
    string? Role,
    string? Company,
    string? PhotoUrl,
    string? Phone,
    string? Email,
    string? WhatsappNumber,
    string? PixKey,
    string? PixKeyType,
    bool PixConsentConfirmed,
    bool IsBranded,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<SocialLinkDto> SocialLinks);
