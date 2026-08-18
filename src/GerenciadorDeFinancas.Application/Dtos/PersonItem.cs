namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record PersonItem(
    Guid Id,
    string Name,
    string? Color,
    bool IsActive);
