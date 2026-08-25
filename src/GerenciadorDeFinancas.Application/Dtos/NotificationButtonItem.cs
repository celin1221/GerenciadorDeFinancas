namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record NotificationButtonItem(
    Guid Id,
    string Label,
    string PersonNames,
    string ActionDescription);
