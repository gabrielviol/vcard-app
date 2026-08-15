namespace Api.Contracts;

// Espelha um item de SocialLink na leitura publica. Omite Id de proposito -- o
// SocialLinkDto autenticado (CardDtos.cs) tem Guid Id, mas visitantes anonimos nao
// precisam (nem devem) receber a chave primaria de um recurso que nao podem escrever.
public record PublicSocialLinkDto(string Platform, string Url, int DisplayOrder);

// DTO de leitura publica de um cartao (GET /public/cards/{slug}, sem autenticacao).
// Omite deliberadamente: Id, UserId, Phone, Email, WhatsappNumber, PixKey, PixKeyType,
// PixConsentConfirmed, IsBranded, CreatedAt, UpdatedAt -- nenhum desses campos e
// renderizado pela pagina publica da Fase 2 (D-19). Ver 02-RESEARCH.md Open Question 1:
// a Fase 3 (CONT-*/PAY-*) amplia este contrato junto com a UI que de fato consome
// WhatsApp/Pix, nao antes disso.
public record PublicCardDto(
    string Slug,
    string FullName,
    string? Role,
    string? Company,
    string? PhotoUrl,
    IReadOnlyList<PublicSocialLinkDto> SocialLinks);
