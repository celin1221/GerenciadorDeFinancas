using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class SplitPurchaseUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteEqualAsync_SplitsAmountEquallyAndClassifies()
    {
        var (people, _, purchaseId) = await ArrangeAsync();

        var useCase = new SplitPurchaseUseCase(_factory);
        await useCase.ExecuteEqualAsync(purchaseId, new[] { people[0], people[1] });

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(PurchaseStatus.Classified, purchase.Status);
        Assert.NotNull(purchase.ClassifiedAt);
        Assert.Equal(2, purchase.Shares.Count);
        Assert.Equal(30000, purchase.ClassifiedAmountCents);
        Assert.All(purchase.Shares, share => Assert.Equal(15000, share.AmountCents));
    }

    [Fact]
    public async Task ExecuteEqualAsync_ThreeWaySplitRoundsCents()
    {
        var (people, _, purchaseId) = await ArrangeAsync(amountCents: 100);

        var useCase = new SplitPurchaseUseCase(_factory);
        await useCase.ExecuteEqualAsync(purchaseId, people);

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(100, purchase.ClassifiedAmountCents);
        Assert.Equal(
            new[] { 34L, 33L, 33L },
            purchase.Shares.Select(share => share.AmountCents).OrderByDescending(value => value));
    }

    [Fact]
    public async Task ExecuteCustomAsync_AcceptsArbitrarySplit()
    {
        var (people, _, purchaseId) = await ArrangeAsync();

        var useCase = new SplitPurchaseUseCase(_factory);
        var shares = new[]
        {
            (people[0], 15000L),
            (people[1], 10000L),
            (people[2], 5000L),
        };
        await useCase.ExecuteCustomAsync(purchaseId, shares);

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(PurchaseStatus.Classified, purchase.Status);
        Assert.Equal(30000, purchase.ClassifiedAmountCents);
    }

    [Fact]
    public async Task ExecuteCustomAsync_RejectsSplitThatDoesNotMatchTotal()
    {
        var (people, _, purchaseId) = await ArrangeAsync();

        var useCase = new SplitPurchaseUseCase(_factory);
        var shares = new[]
        {
            (people[0], 10000L),
            (people[1], 10000L),
        };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteCustomAsync(purchaseId, shares));
    }

    [Fact]
    public async Task ExecuteCustomAsync_RejectsUnknownPerson()
    {
        var (_, _, purchaseId) = await ArrangeAsync();

        var useCase = new SplitPurchaseUseCase(_factory);
        var shares = new[] { (Guid.NewGuid(), 30000L) };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteCustomAsync(purchaseId, shares));
    }

    private async Task<(Guid[] People, Guid CardId, Guid PurchaseId)> ArrangeAsync(long amountCents = 30000)
    {
        Guid cardId;
        Guid purchaseId;
        Guid[] people;

        using (var unitOfWork = _factory.Create())
        {
            var marcelo = new Person("Marcelo");
            var joao = new Person("João");
            var maria = new Person("Maria");
            unitOfWork.Persons.Add(marcelo);
            unitOfWork.Persons.Add(joao);
            unitOfWork.Persons.Add(maria);
            var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: 15, dueDay: 25);
            unitOfWork.Cards.Add(card);
            await unitOfWork.SaveChangesAsync();
            cardId = card.Id;
            people = new[] { marcelo.Id, joao.Id, maria.Id };
        }

        using (var unitOfWork = _factory.Create())
        {
            var purchase = new Purchase(cardId, amountCents, DateTime.UtcNow, "Jantar");
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        return (people, cardId, purchaseId);
    }
}
