using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class GetDashboardSummaryUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public GetDashboardSummaryUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<DashboardSummary> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var persons = await unitOfWork.Persons.ListActiveAsync(cancellationToken);
        var pending = await unitOfWork.Purchases.ListPendingAsync(cancellationToken);
        var classified = await unitOfWork.Purchases.ListClassifiedAsync(cancellationToken);
        var ignoredCount = await unitOfWork.Purchases.CountIgnoredAsync(cancellationToken);

        var personTotals = persons
            .Select(person => new PersonTotal(
                person.Id,
                person.Name,
                person.Color,
                classified
                    .SelectMany(purchase => purchase.Shares)
                    .Where(share => share.PersonId == person.Id)
                    .Sum(share => share.AmountCents)))
            .OrderByDescending(total => total.TotalCents)
            .ToList();

        var cardTotals = classified
            .GroupBy(purchase => new { purchase.CardId, CardName = purchase.Card?.Name ?? "Cartão" })
            .Select(group => new CardTotal(
                group.Key.CardId,
                group.Key.CardName,
                group.Sum(purchase => purchase.AmountCents)))
            .OrderByDescending(total => total.TotalCents)
            .ToList();

        return new DashboardSummary(
            PendingCount: pending.Count,
            PendingCents: pending.Sum(purchase => purchase.AmountCents),
            ClassifiedCount: classified.Count,
            ClassifiedCents: classified.Sum(purchase => purchase.AmountCents),
            IgnoredCount: ignoredCount,
            Persons: personTotals,
            Cards: cardTotals);
    }
}
