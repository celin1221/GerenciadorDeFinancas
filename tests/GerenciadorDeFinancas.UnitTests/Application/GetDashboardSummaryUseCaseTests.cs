using GerenciadorDeFinancas.Application.Services;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class GetDashboardSummaryUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_EmptyDatabase_ReturnsEmptySummary()
    {
        var useCase = new GetDashboardSummaryUseCase(_factory);

        var summary = await useCase.ExecuteAsync();

        Assert.Equal(0, summary.PendingCount);
        Assert.Equal(0, summary.ClassifiedCount);
        Assert.Equal(0, summary.IgnoredCount);
        Assert.Empty(summary.Persons);
        Assert.Empty(summary.Cards);
        Assert.Equal(0, summary.AllTimePurchaseCount);
    }

    [Fact]
    public async Task ExecuteAsync_AggregatesCurrentInvoiceTotalsPerPersonAndCard()
    {
        var (people, cardId) = await ArrangeAsync();

        var useCase = new GetDashboardSummaryUseCase(_factory);
        var summary = await useCase.ExecuteAsync();

        Assert.Equal(1, summary.ClassifiedCount);
        Assert.Equal(30000, summary.ClassifiedCents);
        Assert.Equal(1, summary.AllTimePurchaseCount);
        Assert.Equal(0, summary.PendingCents);
        Assert.Equal(0, summary.IgnoredCount);

        Assert.Equal(2, summary.Persons.Count);
        var marcelo = Assert.Single(summary.Persons, person => person.Name == "Marcelo");
        Assert.Equal(20000, marcelo.TotalCents);
        var joao = Assert.Single(summary.Persons, person => person.Name == "João");
        Assert.Equal(10000, joao.TotalCents);

        var card = Assert.Single(summary.Cards);
        Assert.Equal("Nubank", card.Name);
        Assert.Equal(30000, card.TotalCents);
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesPurchasesFromOtherStatements()
    {
        var (people, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var card = await unitOfWork.Cards.GetByIdAsync(cardId);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var currentPeriod = StatementPeriodCalculator.Calculate(card!.ClosingDay, today);
            var previousYearMonth = AddMonths(currentPeriod.YearMonth, -1);

            var pastPurchase = new Purchase(cardId, 70000, DateTime.UtcNow, "Compra de fatura passada");
            pastPurchase.SetShares(new[] { (people[0], 70000L) });
            pastPurchase.MarkClassified();
            unitOfWork.Purchases.Add(pastPurchase);
            foreach (var share in pastPurchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }

            var previousStatement = await unitOfWork.Statements.GetByCardAndYearMonthAsync(cardId, previousYearMonth);
            if (previousStatement is null)
            {
                var year = previousYearMonth / 100;
                var month = previousYearMonth % 100;
                previousStatement = new Statement(
                    cardId,
                    previousYearMonth,
                    new DateOnly(year, month, 1),
                    new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
                unitOfWork.Statements.Add(previousStatement);
            }

            pastPurchase.MoveToStatement(previousStatement.Id);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetDashboardSummaryUseCase(_factory);
        var summary = await useCase.ExecuteAsync();

        Assert.Equal(1, summary.ClassifiedCount);
        Assert.Equal(30000, summary.ClassifiedCents);
        Assert.Equal(30000, Assert.Single(summary.Cards).TotalCents);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesPendingAndIgnoredCountsGlobally()
    {
        var (people, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var pending = new Purchase(cardId, 5000, DateTime.UtcNow, "Restaurante");
            unitOfWork.Purchases.Add(pending);

            var ignored = new Purchase(cardId, 15000, DateTime.UtcNow, "Compra ignorada");
            ignored.MarkIgnored();
            unitOfWork.Purchases.Add(ignored);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetDashboardSummaryUseCase(_factory);
        var summary = await useCase.ExecuteAsync();

        Assert.Equal(1, summary.PendingCount);
        Assert.Equal(5000, summary.PendingCents);
        Assert.Equal(1, summary.IgnoredCount);
        Assert.Equal(30000, summary.ClassifiedCents);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesCardWithoutCurrentPurchases()
    {
        await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var marcelo = (await unitOfWork.Persons.ListActiveAsync()).Single(person => person.Name == "Marcelo");
            var inter = new Card("Inter", "inter", "5678", marcelo.Id, closingDay: 20, dueDay: 5);
            unitOfWork.Cards.Add(inter);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetDashboardSummaryUseCase(_factory);
        var summary = await useCase.ExecuteAsync();

        Assert.Equal(2, summary.Cards.Count);
        Assert.Contains(summary.Cards, card => card.Name == "Inter" && card.TotalCents == 0);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyIncludesActivePersons()
    {
        var (people, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var inactive = new Person("Inativa");
            inactive.Deactivate();
            unitOfWork.Persons.Add(inactive);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetDashboardSummaryUseCase(_factory);
        var summary = await useCase.ExecuteAsync();

        Assert.DoesNotContain(summary.Persons, person => person.Name == "Inativa");
    }

    private async Task<(Guid[] People, Guid CardId)> ArrangeAsync()
    {
        Guid cardId;
        Guid[] people;

        using (var unitOfWork = _factory.Create())
        {
            var marcelo = new Person("Marcelo");
            var joao = new Person("João");
            unitOfWork.Persons.Add(marcelo);
            unitOfWork.Persons.Add(joao);
            var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: 15, dueDay: 25);
            unitOfWork.Cards.Add(card);
            await unitOfWork.SaveChangesAsync();
            cardId = card.Id;
            people = new[] { marcelo.Id, joao.Id };
        }

        using (var unitOfWork = _factory.Create())
        {
            var card = await unitOfWork.Cards.GetByIdAsync(cardId);
            var statement = await StatementResolver.ResolveAsync(
                unitOfWork,
                card!,
                DateTime.Today,
                CancellationToken.None);

            var purchase = new Purchase(cardId, 30000, DateTime.UtcNow, "Jantar", statementId: statement.Id);
            purchase.SetShares(new[]
            {
                (people[0], 20000L),
                (people[1], 10000L),
            });
            purchase.MarkClassified();
            unitOfWork.Purchases.Add(purchase);
            foreach (var share in purchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }

            await unitOfWork.SaveChangesAsync();
        }

        return (people, cardId);
    }

    private static int AddMonths(int yearMonth, int months)
    {
        var year = yearMonth / 100;
        var month = yearMonth % 100;
        var index = year * 12 + (month - 1) + months;
        return (index / 12) * 100 + (index % 12) + 1;
    }
}
