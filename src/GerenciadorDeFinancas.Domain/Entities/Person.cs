using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class Person
{
    public Guid Id { get; }

    public string Name { get; private set; } = null!;

    public string? Color { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; }

    private Person()
    {
    }

    public Person(string name, string? color = null)
    {
        Id = Guid.NewGuid();
        SetName(name);
        Color = color;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Nome da pessoa é obrigatório.");
        }

        Name = name.Trim();
    }

    public void SetColor(string? color) => Color = color;

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
