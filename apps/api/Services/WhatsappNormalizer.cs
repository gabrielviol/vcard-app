using System.Text.RegularExpressions;

namespace Api.Services;

// ESPELHO: este arquivo e apps/web/lib/whatsapp-normalize.ts implementam a MESMA logica
// nos dois lados (servidor e cliente) -- precisam mudar juntos. O servidor e a
// autoridade (Architectural Responsibility Map do 01-RESEARCH.md): o valor persistido
// em cards.whatsapp_number e SEMPRE o retorno de Normalize, nunca o valor que o cliente
// enviou (T-01-22 -- um cliente malicioso que poste um valor arbitrario tem o valor
// reescrito aqui).
//
// Regra do nono digito (ANATEL, 2012-2016): so os DDDs abaixo ganharam um "9" extra no
// inicio do numero local. Aplicar essa regra a qualquer outro DDD corromperia o numero
// (01-RESEARCH.md, Pitfall 2 / Assumptions Log A2 -- whitelist vem de fontes
// comunitarias, nao de documento oficial da ANATEL; recomendavel conferir com numeros
// reais antes do deploy da Fase 3).
public static class WhatsappNormalizer
{
    public static readonly HashSet<string> NinthDigitDdds = new()
    {
        "11", "12", "13", "14", "15", "16", "17", "18", "19", // Sao Paulo
        "21", "22", "24", // Rio de Janeiro
        "27", "28", // Espirito Santo
    };

    private static readonly Regex NonDigitRegex = new(@"\D", RegexOptions.Compiled);

    // Devolve apenas digitos, sempre prefixados por "55" (DDI Brasil).
    // Entrada nula/vazia devolve string vazia -- o campo e opcional (D-04).
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var digits = NonDigitRegex.Replace(raw, string.Empty);
        if (digits.Length == 0)
            return string.Empty;

        // Remove o prefixo DDI "55" apenas quando sobrarem digitos suficientes para um
        // numero nacional completo -- caso contrario um DDD "55" (Rio Grande do Sul)
        // seria confundido com o DDI e cortado por engano.
        var national = digits.StartsWith("55", StringComparison.Ordinal) && digits.Length > 11
            ? digits[2..]
            : digits;

        // Remove um unico "0" de tronco a esquerda, se presente.
        if (national.StartsWith('0'))
            national = national[1..];

        var ddd = national.Length >= 2 ? national[..2] : national;
        var localNumber = national.Length > 2 ? national[2..] : string.Empty;

        if (NinthDigitDdds.Contains(ddd) && localNumber.Length == 8)
            localNumber = $"9{localNumber}";

        return $"55{ddd}{localNumber}";
    }

    // DDD entre 11 e 99 e numero local com 8 ou 9 digitos apos a normalizacao.
    public static bool IsValid(string? raw)
    {
        var normalized = Normalize(raw);
        if (normalized.Length == 0)
            return false;

        var ddd = normalized.Length >= 4 ? normalized[2..4] : string.Empty;
        var localNumber = normalized.Length > 4 ? normalized[4..] : string.Empty;

        if (ddd.Length != 2 || !int.TryParse(ddd, out var dddNumber) || dddNumber < 11 || dddNumber > 99)
            return false;

        return localNumber.Length is 8 or 9;
    }
}
