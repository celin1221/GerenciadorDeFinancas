using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.ValueObjects;

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
        var cards = await unitOfWork.Cards.ListActiveAsync(cancellationToken);
        var pending = await unitOfWork.Purchases.ListPendingAsync(cancellationToken);
        var ignoredCount = await unitOfWork.Purchases.CountIgnoredAsync(cancellationToken);
        var classified = await unitOfWork.Purchases.ListClassifiedAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentInvoicePurchases = new List<Domain.Entities.Purchase>();
        var cardTotals = new List<CardTotal>();

        foreach (var card in cards)
        {
            var period = StatementPeriodCalculator.Calculate(card.ClosingDay, today);
            var statement = await unitOfWork.Statements.GetByCardAndYearMonthAsync(card.Id, period.YearMonth, cancellationToken);
            var invoicePurchases = statement?.Purchases
                .Where(purchase => purchase.Status == PurchaseStatus.Classified)
                .ToList() ?? [];

            currentInvoicePurchases.AddRange(invoicePurchases);
            cardTotals.Add(new CardTotal(
                card.Id,
                card.Name,
                invoicePurchases.Sum(purchase => purchase.AmountCents)));
        }

        var sharesByPerson = currentInvoicePurchases
            .SelectMany(purchase => purchase.Shares)
            .GroupBy(share => share.PersonId)
            .ToDictionary(group => group.Key, group => group.Sum(share => share.AmountCents));

        var personTotals = persons
            .Select(person => new PersonTotal(
                person.Id,
                person.Name,
                person.Color,
                sharesByPerson.GetValueOrDefault(person.Id)))
            .OrderByDescending(total => total.TotalCents)
            .ToList();

        var allTimeCutoff = DateTime.UtcNow.AddMonths(-3);
        var allTimePurchases = classified.Where(p => p.Date >= allTimeCutoff).ToList();

        return new DashboardSummary(
            PendingCount: pending.Count,
            PendingCents: pending.Sum(purchase => purchase.AmountCents),
            ClassifiedCount: currentInvoicePurchases.Count,
            ClassifiedCents: currentInvoicePurchases.Sum(purchase => purchase.AmountCents),
            IgnoredCount: ignoredCount,
            AllTimePurchaseCount: allTimePurchases.Count,
            Persons: personTotals,
            Cards: cardTotals);
    }
}
