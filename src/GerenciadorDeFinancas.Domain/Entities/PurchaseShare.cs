namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class PurchaseShare
{
    public Guid Id { get; }

    public Guid PurchaseId { get; }

    public Purchase? Purchase { get; private set; }

    public Guid PersonId { get; }

    public Person? Person { get; private set; }

    public long AmountCents { get; private set; }

    public DateTime CreatedAt { get; }

    internal PurchaseShare(Purchase purchase, Guid personId, long amountCents)
    {
        Id = Guid.NewGuid();
        PurchaseId = purchase.Id;
        Purchase = purchase;
        PersonId = personId;
        AmountCents = amountCents;
        CreatedAt = DateTime.UtcNow;
    }

    private PurchaseShare()
    {
    }
}
