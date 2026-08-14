namespace Api.Contracts;

// DTOs de escrita/leitura de links sociais (CARD-10) -- endpoints proprios em
// SocialLinkEndpoints.cs, distintos do DTO de escrita do cartao (CardWriteDto).
public record SocialLinkWriteDto(string Platform, string Url);

public record SocialLinkResponseDto(Guid Id, string Platform, string Url, int DisplayOrder);

public record SocialLinkOrderDto(Guid[] OrderedIds);
