using System.Globalization;
using System.Text.RegularExpressions;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

public static partial class BrCurrencyParser
{
    public static long? TryParseBrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = AmountRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var value = ParseNumber(match.Groups[1].Value);
        return value is null ? null : ToCents(value.Value);
    }

    public static long? TryParseBrlParcelAware(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parcelMatch = ParcelAmountRegex().Match(text);
        if (parcelMatch.Success)
        {
            var value = ParseNumber(parcelMatch.Groups[2].Value);
            if (value is not null)
            {
                return ToCents(value.Value);
            }
        }

        return TryParseBrl(text);
    }

    private static long ToCents(decimal value) =>
        (long)Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero);

    private static decimal? ParseNumber(string raw)
    {
        var hasComma = raw.Contains(',');
        var hasDot = raw.Contains('.');

        if (hasComma && hasDot)
        {
            return TryParse(raw.Replace(".", string.Empty).Replace(",", "."));
        }

        if (hasComma)
        {
            return TryParse(raw.Replace(',', '.'));
        }

        if (hasDot)
        {
            var parts = raw.Split('.');
            if (parts.Length == 2 && parts[1].Length == 2 && parts[1].All(char.IsDigit))
            {
                return TryParse(raw);
            }

            return TryParse(raw.Replace(".", string.Empty));
        }

        return TryParse(raw);
    }

    private static decimal? TryParse(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    [GeneratedRegex(@"R\$\s*([\d.,]+)", RegexOptions.IgnoreCase)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"\b(\d+)\s*x\s*de\s*R\$\s*([\d.,]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ParcelAmountRegex();
}
