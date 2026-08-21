namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record PersonCardTotal(Guid CardId, string CardName, long TotalCents);

public sealed record PersonPurchaseItem(
    Guid PurchaseId,
    string Title,
    DateTime Date,
    long PurchaseTotalCents,
    long PersonShareCents,
    bool IsSplit,
    int ShareCount);

public sealed record PersonDetail(
    Guid PersonId,
    string Name,
    string? Color,
    long TotalCents,
    IReadOnlyList<PersonCardTotal> Cards,
    IReadOnlyList<PersonPurchaseItem> Purchases);
