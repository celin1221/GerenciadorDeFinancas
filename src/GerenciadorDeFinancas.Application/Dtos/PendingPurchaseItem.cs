namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record PendingPurchaseItem(
    Guid Id,
    string Description,
    string? MerchantName,
    string CardName,
    long AmountCents,
    DateTime Date,
    Guid OwnerPersonId);
