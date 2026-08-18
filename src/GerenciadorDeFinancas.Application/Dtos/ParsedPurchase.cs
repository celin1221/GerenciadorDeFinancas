namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record ParsedPurchase(
    string BankId,
    string MerchantName,
    long AmountCents,
    DateTime Date,
    string Description,
    string? BankRefId,
    string? CardLast4);
