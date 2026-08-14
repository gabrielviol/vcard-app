using System.Text.RegularExpressions;

namespace Api.Services;

// Normalizacao, validacao de formato e checagem de slugs reservados (CARD-02).
//
// A checagem de disponibilidade que este servico expoe e apenas dica de UX/pre-filtro
// (T-01-19) -- a garantia real de unicidade contra corrida TOCTOU (T-01-20) e o indice
// unico do Postgres em cards.slug, traduzido pelo handler via catch de SqlState 23505.
public static class SlugService
{
    // Comparador case-insensitive (OrdinalIgnoreCase) fecha o bypass de reservado por
    // caixa (T-01-18) -- ex: "Dashboard", "DASHBOARD" e "dashboard" caem no mesmo bucket.
    // Normalize() roda ANTES de qualquer lookup neste HashSet.
    public static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        // Sistema / framework
        "login", "dashboard", "api", "_next", "admin", "favicon.ico", "robots.txt",
        "sitemap.xml", "manifest.json", "sw.js", ".well-known", "static", "public", "assets",

        // Rotas futuras (Fase 2/3)
        "qr", "vcard", "og", "share",

        // Paginas de produto/marketing
        "sobre", "about", "contato", "contact", "termos", "terms", "privacidade", "privacy",
        "preco", "precos", "pricing", "planos", "plans", "ajuda", "help", "suporte", "support",
        "blog", "home", "index",

        // Palavras genericas de conta/acao
        "cadastro", "cadastrar", "registro", "entrar", "sair", "logout", "signup", "perfil",
        "conta", "account", "configuracoes", "settings", "config", "editar", "edit", "novo",
        "new", "criar", "create", "delete", "excluir",

        // Auto-protecao de marca
        "www", "mail", "app", "apps",
    };

    // 3 a 32 caracteres, apenas [a-z0-9-], sem hifen no inicio/fim e sem hifen duplo.
    // A restricao a [a-z0-9-] tambem barra homoglifos Unicode (T-01-18).
    private static readonly Regex SlugFormatRegex = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public static string Normalize(string raw) => raw.Trim().ToLowerInvariant();

    public static bool IsValidFormat(string slug)
    {
        if (slug.Length is < 3 or > 32)
            return false;

        return SlugFormatRegex.IsMatch(slug);
    }

    public static bool IsReserved(string slug) => ReservedSlugs.Contains(slug);
}
