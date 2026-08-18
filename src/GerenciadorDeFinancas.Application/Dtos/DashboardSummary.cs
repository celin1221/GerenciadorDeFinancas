namespace GerenciadorDeFinancas.Application.Dtos;

public sealed record PersonTotal(Guid PersonId, string Name, string? Color, long TotalCents);

public sealed record CardTotal(Guid CardId, string Name, long TotalCents);

public sealed record DashboardSummary(
    int PendingCount,
    long PendingCents,
    int ClassifiedCount,
    long ClassifiedCents,
    int IgnoredCount,
    IReadOnlyList<PersonTotal> Persons,
    IReadOnlyList<CardTotal> Cards);
