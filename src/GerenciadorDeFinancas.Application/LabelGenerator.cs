namespace GerenciadorDeFinancas.Application;

public static class LabelGenerator
{
    private const int MaxLabelLength = 20;

    public static string Generate(IReadOnlyList<string> personNames)
    {
        if (personNames.Count == 0)
        {
            return "?";
        }

        var tags = personNames
            .Select(ExtractTag)
            .ToList();

        var label = string.Join("/", tags);

        return label.Length > MaxLabelLength
            ? label[..MaxLabelLength]
            : label;
    }

    private static string ExtractTag(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length <= 2)
        {
            return trimmed.ToUpperInvariant();
        }

        return trimmed[..2].ToUpperInvariant();
    }
}
