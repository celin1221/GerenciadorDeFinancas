namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record NotificationRaw(
    string PackageName,
    string Title,
    string Text,
    string? NotificationKey,
    DateTimeOffset PostedAt);
