namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record ClassificationPrompt(
    Guid PurchaseId,
    long AmountCents,
    string MerchantName,
    IReadOnlyList<Guid> SuggestedPersonIds);
