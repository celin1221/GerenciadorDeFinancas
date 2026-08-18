namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class Merchant
{
    public Guid Id { get; }

    public string NormalizedName { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public Guid? CategoryId { get; private set; }

    public Category? Category { get; private set; }

    private Merchant()
    {
    }

    public Merchant(string displayName)
    {
        Id = Guid.NewGuid();
        SetDisplayName(displayName);
        CategoryId = null;
    }

    public void SetDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new Exceptions.DomainException("Nome do estabelecimento é obrigatório.");
        }

        DisplayName = displayName.Trim();
        NormalizedName = Normalize(DisplayName);
    }

    public void SetCategory(Guid? categoryId) => CategoryId = categoryId;

    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(name.Length);
        foreach (var character in name.ToLowerInvariant().Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString().Trim();
    }
}
