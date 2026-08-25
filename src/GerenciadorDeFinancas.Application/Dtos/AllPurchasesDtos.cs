namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record AllPurchaseItem(
    Guid PurchaseId,
    string Title,
    string? MerchantName,
    string CardName,
    DateTime Date,
    long AmountCents,
    int ShareCount,
    int InstallmentNumber,
    int TotalInstallments);
