using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

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
    }

    [Fact]
    public async Task ExecuteAsync_AggregatesTotalsPerPersonAndCard()
    {
        var (people, cardId) = await ArrangeAsync();

        var useCase = new GetDashboardSummaryUseCase(_factory);
        var summary = await useCase.ExecuteAsync();

        Assert.Equal(1, summary.ClassifiedCount);
        Assert.Equal(30000, summary.ClassifiedCents);
        Assert.Equal(0, summary.PendingCount);
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
    public async Task ExecuteAsync_IncludesPendingAndIgnoredCounts()
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
            var purchase = new Purchase(cardId, 30000, DateTime.UtcNow, "Jantar");
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
}
