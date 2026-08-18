namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record CardItem(
    Guid Id,
    string Name,
    string BankId,
    string? BankDisplayName,
    string? Last4Digits,
    Guid OwnerPersonId,
    string OwnerName,
    int ClosingDay,
    int DueDay,
    bool IsActive);
