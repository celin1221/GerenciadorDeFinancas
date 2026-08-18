namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; }

    public string Name { get; private set; } = null!;

    public string? Icon { get; private set; }

    public string? Color { get; private set; }

    public Guid? ParentId { get; private set; }

    public Category? Parent { get; private set; }

    public bool IsSystem { get; private set; }

    private Category()
    {
    }

    public Category(string name, string? icon = null, string? color = null, Guid? parentId = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exceptions.DomainException("Nome da categoria é obrigatório.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Icon = icon;
        Color = color;
        ParentId = parentId;
        IsSystem = isSystem;
    }
}
