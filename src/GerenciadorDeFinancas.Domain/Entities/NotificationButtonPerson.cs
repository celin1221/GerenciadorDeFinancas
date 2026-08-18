namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class NotificationButtonPerson
{
    public Guid ButtonId { get; }

    public NotificationButton? Button { get; private set; }

    public Guid PersonId { get; }

    public Person? Person { get; private set; }

    internal NotificationButtonPerson(Guid buttonId, Guid personId)
    {
        ButtonId = buttonId;
        PersonId = personId;
    }

    private NotificationButtonPerson()
    {
    }
}
