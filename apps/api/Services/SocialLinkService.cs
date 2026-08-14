using Api.Data.Entities;

namespace Api.Services;

// Regras de link social (CARD-10): plataforma restrita a uma allow-list de 6 valores
// (T-01-40), URL restrita a https absoluto (T-01-39 -- protege a pagina publica da Fase 2,
// onde este valor vira href), e display_order sempre resequenciado como indice
// sequencial a partir de 0, sem buracos, apos remocao ou reordenacao.
//
// Classe estatica, seguindo o mesmo padrao ja estabelecido por SlugService/
// WhatsappNormalizer/PixValidationService (utilitario sem estado, sem dependencia de DI).
public static class SocialLinkService
{
    public static readonly HashSet<string> AllowedPlatforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "instagram", "linkedin", "twitter", "tiktok", "youtube", "site",
    };

    public static bool IsValidPlatform(string? platform) =>
        platform is not null && AllowedPlatforms.Contains(platform);

    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 500)
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps;
    }

    // Reatribui DisplayOrder como indice sequencial a partir de 0, na ordem em que os
    // links forem enumerados pelo chamador -- nunca deixa buracos.
    public static void Resequence(IEnumerable<SocialLink> links)
    {
        var index = 0;
        foreach (var link in links)
        {
            link.DisplayOrder = index;
            index++;
        }
    }
}
