using System.Text.RegularExpressions;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

public static partial class MerchantExtractor
{
    public static string Extract(string source, params string[] phrasesToRemove)
    {
        var result = source ?? string.Empty;

        foreach (var phrase in phrasesToRemove)
        {
            result = Regex.Replace(result, Regex.Escape(phrase.Trim()), " ", RegexOptions.IgnoreCase);
        }

        result = AmountsRegex().Replace(result, " ");
        result = CardRefRegex().Replace(result, " ");
        result = WhitespaceRegex().Replace(result, " ").Trim(' ', '-', '|', ':', ';', ',', '.', '/');
        result = LeadingPrepositionsRegex().Replace(result, string.Empty).Trim();

        for (var i = 0; i < 3; i++)
        {
            var cleaned = TrailingPrepositionsRegex().Replace(result, string.Empty).Trim();
            if (cleaned == result)
            {
                break;
            }

            result = cleaned;
        }

        return result.Length > 60 ? result[..60].Trim() : result;
    }

    public static string Fallback(string? title, string? text)
    {
        var source = !string.IsNullOrWhiteSpace(title)
            ? title
            : text?.Split('\n').FirstOrDefault() ?? string.Empty;
        var cleaned = WhitespaceRegex().Replace(source, " ").Trim();
        return cleaned.Length > 60 ? cleaned[..60].Trim() : cleaned;
    }

    [GeneratedRegex(@"(?:R\$\s*[\d.,]+|\d+\s*x\s*de\s*R\$\s*[\d.,]+)", RegexOptions.IgnoreCase)]
    private static partial Regex AmountsRegex();

    [GeneratedRegex(@"\b(?:(?:(?:com\s+)?(?:o\s+)?(?:cart[ãa]o|card)|(?:cart[ãa]o|card)\s+com)\s+)?(?:final|terminando\s+em|últimos\s+dígitos)\s*\d{4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex CardRefRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^(?:(?:na|no|em|de|do|da|ao|a)\s+)+", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingPrepositionsRegex();

    [GeneratedRegex(@"\s+(?:para\s+[oa]|de\s+[oa]|em|no|na)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingPrepositionsRegex();
}
