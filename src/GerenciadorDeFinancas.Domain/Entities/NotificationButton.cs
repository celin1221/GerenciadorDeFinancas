using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class NotificationButton
{
    private readonly List<NotificationButtonPerson> _persons = new();

    public Guid Id { get; }

    public string Label { get; private set; } = null!;

    public int Order { get; private set; }

    public IReadOnlyCollection<NotificationButtonPerson> Persons => _persons;

    private NotificationButton()
    {
    }

    public NotificationButton(string label, int order)
    {
        Id = Guid.NewGuid();
        SetLabel(label);
        Order = order;
    }

    public void SetLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("Rótulo do botão é obrigatório.");
        }

        if (label.Length > 20)
        {
            throw new DomainException("Rótulo do botão deve ter no máximo 20 caracteres.");
        }

        Label = label.Trim();
    }

    public void SetOrder(int order) => Order = order;

    public void SetPersons(IReadOnlyList<Guid> personIds)
    {
        if (personIds.Count == 0)
        {
            throw new DomainException("Selecione ao menos uma pessoa para o botão.");
        }

        if (personIds.Count > 10)
        {
            throw new DomainException("Um botão pode ter no máximo 10 pessoas.");
        }

        _persons.Clear();
        foreach (var personId in personIds.Distinct())
        {
            _persons.Add(new NotificationButtonPerson(Id, personId));
        }
    }
}
