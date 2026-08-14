using System.Text.RegularExpressions;

namespace Api.Services;

// ESPELHO: este arquivo e apps/web/lib/pix-validation.ts implementam a MESMA logica nos
// dois lados (servidor e cliente) -- precisam mudar juntos. O servidor e a autoridade
// (T-01-27/T-01-28): a validacao aqui roda antes de qualquer persistencia, nunca confiando
// no resultado que o cliente reportou.
//
// CPF/CNPJ: calculo de digito verificador feito a mao (nenhum pacote NuGet vetado/aprovado
// cobre isso para .NET -- unica excecao legitima ao "Don't Hand-Roll" desta fase, per
// 01-RESEARCH.md e action step 4 do plano 01-05). CNPJ suporta o novo formato alfanumerico
// da RFB (vigente jul/2026): caracteres alfanumericos entram no calculo do modulo 11
// convertidos para o valor ASCII menos 48 (ex.: 'A' = 65 - 48 = 17), os 2 digitos
// verificadores finais permanecem sempre numericos.
public static class PixValidationService
{
    private static readonly HashSet<string> KnownTypes = new(StringComparer.Ordinal)
    {
        "cpf", "cnpj", "email", "telefone", "aleatoria",
    };

    private static readonly Regex UuidV4Regex = new(
        @"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BrPhoneRegex = new(
        @"^(\+?55\s?)?\(?\d{2}\)?\s?9?\d{4}-?\d{4}$",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled);

    private static readonly Regex NonAlphanumericRegex = new(@"[^0-9A-Za-z]", RegexOptions.Compiled);

    public static bool IsKnownType(string? type) => type is not null && KnownTypes.Contains(type);

    public static bool IsValid(string? type, string? key)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(key))
            return false;

        return type switch
        {
            "cpf" => IsValidCpf(key),
            "cnpj" => IsValidCnpj(key),
            "email" => EmailRegex.IsMatch(key),
            "telefone" => BrPhoneRegex.IsMatch(key),
            "aleatoria" => UuidV4Regex.IsMatch(key),
            _ => false,
        };
    }

    // ─── CPF ────────────────────────────────────────────────────────
    private static bool IsValidCpf(string raw)
    {
        var digits = NonAlphanumericRegex.Replace(raw, string.Empty);
        if (digits.Length != 11)
            return false;

        // Blacklist de sequencias repetidas (000.000.000-00, 111.111.111-11, ...) -- o
        // ponto que uma soma ponderada ingenua deixaria passar, pois o modulo 11 tambem
        // "valida" essas sequencias por acidente aritmetico.
        if (digits.Distinct().Count() == 1)
            return false;

        var numbers = digits[..9];
        var dv1 = CpfCheckDigit(numbers);
        var dv2 = CpfCheckDigit(numbers + dv1);

        return digits[9..] == $"{dv1}{dv2}";
    }

    private static int CpfCheckDigit(string numbers)
    {
        var weight = numbers.Length + 1;
        var sum = 0;
        foreach (var ch in numbers)
        {
            sum += (ch - '0') * weight;
            weight--;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    // ─── CNPJ ───────────────────────────────────────────────────────
    private static bool IsValidCnpj(string raw)
    {
        var stripped = NonAlphanumericRegex.Replace(raw, string.Empty).ToUpperInvariant();
        if (stripped.Length != 14)
            return false;

        // Os 2 digitos verificadores finais sao sempre numericos, mesmo no formato
        // alfanumerico da RFB.
        if (!Regex.IsMatch(stripped[12..], @"^\d{2}$"))
            return false;

        if (stripped.Distinct().Count() == 1)
            return false;

        var numbers = stripped[..12];
        var dv1 = CnpjCheckDigit(numbers);
        var dv2 = CnpjCheckDigit(numbers + dv1);

        return stripped[12..] == $"{dv1}{dv2}";
    }

    private static int CnpjCheckDigit(string numbers)
    {
        // Pesos padrao do modulo 11 para CNPJ: 6,5,4,3,2,9,8,7,6,5,4,3,2 (da direita p/ esquerda,
        // ciclando 2-9), aplicados aqui da esquerda p/ direita com o array pre-computado.
        int[] weightsFor12 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weightsFor13 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var weights = numbers.Length == 12 ? weightsFor12 : weightsFor13;

        var sum = 0;
        for (var i = 0; i < numbers.Length; i++)
        {
            // Caractere alfanumerico convertido para valor ASCII menos 48 (nota tecnica da
            // RFB) -- digitos numericos (0-9, ASCII 48-57) resultam no proprio valor numerico
            // (ex.: '0' -> 0, '9' -> 9); letras (A-Z, ASCII 65-90) resultam em 17-42 (ex.: 'A' -> 17).
            var charValue = numbers[i] - 48;
            sum += charValue * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
