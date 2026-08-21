namespace GerenciadorDeFinancas.Application.Dtos;

public enum ImportOutcome
{
    Created,
    Duplicate,
    Unsupported,
    CardNotMatched,
    ParseFailed,
}

public sealed record ImportNotificationResult(
    ImportOutcome Outcome,
    Guid? PurchaseId = null,
    long AmountCents = 0,
    string? MerchantName = null,
    Guid? OwnerPersonId = null)
{
    public static ImportNotificationResult Created(Guid purchaseId, long amountCents, string merchantName, Guid ownerPersonId) =>
        new(ImportOutcome.Created, purchaseId, amountCents, merchantName, ownerPersonId);

    public static ImportNotificationResult Duplicate() => new(ImportOutcome.Duplicate);

    public static ImportNotificationResult Unsupported() => new(ImportOutcome.Unsupported);

    public static ImportNotificationResult CardNotMatched() => new(ImportOutcome.CardNotMatched);

    public static ImportNotificationResult ParseFailed() => new(ImportOutcome.ParseFailed);
}
