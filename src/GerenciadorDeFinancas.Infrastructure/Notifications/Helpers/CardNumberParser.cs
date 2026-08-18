using System.Text.RegularExpressions;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

public static partial class CardNumberParser
{
    public static string? ExtractLast4(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (Match match in Last4Regex().Matches(text))
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    [GeneratedRegex(@"(?:final|terminando\s+em|últimos\s+dígitos)[^\d]*(\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex Last4Regex();
}
