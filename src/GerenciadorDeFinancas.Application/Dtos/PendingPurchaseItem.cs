namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record PendingPurchaseItem(
    Guid Id,
    string Description,
    string? MerchantName,
    string CardName,
    long AmountCents,
    DateTime Date,
    Guid OwnerPersonId,
    int InstallmentNumber = 0,
    int TotalInstallments = 0)
{
    public string InstallmentText => TotalInstallments > 1 ? $"{InstallmentNumber}/{TotalInstallments}" : string.Empty;
}
