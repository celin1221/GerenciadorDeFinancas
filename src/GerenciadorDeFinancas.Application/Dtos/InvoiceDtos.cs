namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record InvoiceItem(
    Guid StatementId,
    Guid CardId,
    string CardName,
    int YearMonth,
    long TotalCents,
    int PurchaseCount,
    int PendingCount,
    string Status);

public sealed record InvoicePurchaseItem(
    Guid Id,
    string Description,
    string? MerchantName,
    DateTime Date,
    long AmountCents,
    bool IsPending,
    bool IsIgnored,
    int InstallmentNumber,
    int TotalInstallments);

public sealed record InvoiceDetail(
    Guid StatementId,
    Guid CardId,
    string CardName,
    int YearMonth,
    DateOnly ClosingDate,
    DateOnly EstimatedDueDate,
    long TotalCents,
    IReadOnlyList<InvoicePurchaseItem> Purchases);
