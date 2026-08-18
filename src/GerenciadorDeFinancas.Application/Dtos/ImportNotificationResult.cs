namespace GerenciadorDeFinancas.Application.Dtos;

public enum ImportOutcome
{
    Created,
    Duplicate,
    Unsupported,
    CardNotMatched,
    ParseFailed,
}

public sealed record ImportNotificationResult(ImportOutcome Outcome, Guid? PurchaseId = null)
{
    public static ImportNotificationResult Created(Guid purchaseId) =>
        new(ImportOutcome.Created, purchaseId);

    public static ImportNotificationResult Duplicate() => new(ImportOutcome.Duplicate);

    public static ImportNotificationResult Unsupported() => new(ImportOutcome.Unsupported);

    public static ImportNotificationResult CardNotMatched() => new(ImportOutcome.CardNotMatched);

    public static ImportNotificationResult ParseFailed() => new(ImportOutcome.ParseFailed);
}
