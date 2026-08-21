using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class GetPersonDetailUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ReturnsNullWhenPersonDoesNotExist()
    {
        var useCase = new GetPersonDetailUseCase(_factory);

        var detail = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Null(detail);
    }

    [Fact]
    public async Task ExecuteAsync_SingleOwnerPurchaseIsNotSplit()
    {
        var personId = await SeedPersonAsync("Dona");
        var cardId = await SeedCardAsync(personId, "Nubank");
        await SeedClassifiedPurchaseAsync(cardId, amountCents: 10000, shares: new[] { (personId, 10000L) });

        var useCase = new GetPersonDetailUseCase(_factory);
        var detail = await useCase.ExecuteAsync(personId);

        Assert.NotNull(detail);
        Assert.Equal(10000, detail.TotalCents);
        var purchase = Assert.Single(detail.Purchases);
        Assert.False(purchase.IsSplit);
        Assert.Equal(1, purchase.ShareCount);
        Assert.Equal(10000, purchase.PersonShareCents);
        Assert.Equal(10000, purchase.PurchaseTotalCents);
        var card = Assert.Single(detail.Cards);
        Assert.Equal(cardId, card.CardId);
        Assert.Equal(10000, card.TotalCents);
    }

    [Fact]
    public async Task ExecuteAsync_SplitPurchaseShowsTotalAndShare()
    {
        var (personA, personB, cardId) = await SeedTwoPeopleWithCardAsync();
        await SeedClassifiedPurchaseAsync(
            cardId,
            amountCents: 30000,
            shares: new[] { (personA, 10000L), (personB, 20000L) });

        var useCase = new GetPersonDetailUseCase(_factory);
        var detail = await useCase.ExecuteAsync(personA);

        Assert.NotNull(detail);
        Assert.Equal(10000, detail.TotalCents);
        var purchase = Assert.Single(detail.Purchases);
        Assert.True(purchase.IsSplit);
        Assert.Equal(2, purchase.ShareCount);
        Assert.Equal(30000, purchase.PurchaseTotalCents);
        Assert.Equal(10000, purchase.PersonShareCents);
    }

    [Fact]
    public async Task ExecuteAsync_TotalsAreGroupedByCard()
    {
        var personId = await SeedPersonAsync("Dona");
        var nubankId = await SeedCardAsync(personId, "Nubank");
        var interId = await SeedCardAsync(personId, "Inter");
        await SeedClassifiedPurchaseAsync(nubankId, 5000, new[] { (personId, 5000L) });
        await SeedClassifiedPurchaseAsync(nubankId, 7000, new[] { (personId, 7000L) });
        await SeedClassifiedPurchaseAsync(interId, 3000, new[] { (personId, 3000L) });

        var useCase = new GetPersonDetailUseCase(_factory);
        var detail = await useCase.ExecuteAsync(personId);

        Assert.NotNull(detail);
        Assert.Equal(15000, detail.TotalCents);
        Assert.Equal(2, detail.Cards.Count);
        var nubank = detail.Cards.Single(card => card.CardName == "Nubank");
        var inter = detail.Cards.Single(card => card.CardName == "Inter");
        Assert.Equal(12000, nubank.TotalCents);
        Assert.Equal(3000, inter.TotalCents);
        Assert.Equal(3, detail.Purchases.Count);
    }

    [Fact]
    public async Task ExecuteAsync_PersonWithoutPurchasesHasEmptyLists()
    {
        var personId = await SeedPersonAsync("Sem compras");

        var useCase = new GetPersonDetailUseCase(_factory);
        var detail = await useCase.ExecuteAsync(personId);

        Assert.NotNull(detail);
        Assert.Equal(0, detail.TotalCents);
        Assert.Empty(detail.Cards);
        Assert.Empty(detail.Purchases);
    }

    [Fact]
    public async Task ExecuteAsync_PendingPurchasesAreExcluded()
    {
        var personId = await SeedPersonAsync("Dona");
        var cardId = await SeedCardAsync(personId, "Nubank");
        await SeedClassifiedPurchaseAsync(cardId, 5000, new[] { (personId, 5000L) });
        await SeedPendingPurchaseAsync(cardId, 9000);

        var useCase = new GetPersonDetailUseCase(_factory);
        var detail = await useCase.ExecuteAsync(personId);

        Assert.NotNull(detail);
        Assert.Equal(5000, detail.TotalCents);
        Assert.Single(detail.Purchases);
    }

    [Fact]
    public async Task ExecuteAsync_TitlePrefersMerchantDisplayName()
    {
        var personId = await SeedPersonAsync("Dona");
        var cardId = await SeedCardAsync(personId, "Nubank");

        using (var unitOfWork = _factory.Create())
        {
            var merchant = new Merchant("Padaria Central");
            unitOfWork.Merchants.Add(merchant);
            var purchase = new Purchase(cardId, 2000, DateTime.UtcNow, "Compra de R$ 20,00");
            purchase.SetMerchant(merchant.Id);
            purchase.AssignToSingle(personId);
            unitOfWork.Purchases.Add(purchase);
            foreach (var share in purchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetPersonDetailUseCase(_factory);
        var detail = await useCase.ExecuteAsync(personId);

        Assert.NotNull(detail);
        Assert.Equal("Padaria Central", Assert.Single(detail.Purchases).Title);
    }

    private async Task<Guid> SeedPersonAsync(string name)
    {
        using var unitOfWork = _factory.Create();
        var person = new Person(name);
        unitOfWork.Persons.Add(person);
        await unitOfWork.SaveChangesAsync();
        return person.Id;
    }

    private async Task<Guid> SeedCardAsync(Guid ownerId, string name)
    {
        using var unitOfWork = _factory.Create();
        var card = new Card(name, "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync();
        return card.Id;
    }

    private async Task<(Guid PersonA, Guid PersonB, Guid CardId)> SeedTwoPeopleWithCardAsync()
    {
        using var unitOfWork = _factory.Create();
        var personA = new Person("Alice");
        var personB = new Person("Bruno");
        unitOfWork.Persons.Add(personA);
        unitOfWork.Persons.Add(personB);
        var card = new Card("Nubank", "nubank", "1234", personA.Id, closingDay: 15, dueDay: 25);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync();
        return (personA.Id, personB.Id, card.Id);
    }

    private async Task SeedClassifiedPurchaseAsync(
        Guid cardId,
        long amountCents,
        IReadOnlyList<(Guid PersonId, long Amount)> shares)
    {
        using var unitOfWork = _factory.Create();
        var purchase = new Purchase(cardId, amountCents, DateTime.UtcNow, "Compra teste");
        purchase.SetShares(shares.Select(share => (share.PersonId, share.Amount)).ToList());
        purchase.MarkClassified();
        unitOfWork.Purchases.Add(purchase);
        foreach (var share in purchase.Shares)
        {
            unitOfWork.Purchases.AddShare(share);
        }
        await unitOfWork.SaveChangesAsync();
    }

    private async Task SeedPendingPurchaseAsync(Guid cardId, long amountCents)
    {
        using var unitOfWork = _factory.Create();
        var purchase = new Purchase(cardId, amountCents, DateTime.UtcNow, "Compra pendente");
        unitOfWork.Purchases.Add(purchase);
        await unitOfWork.SaveChangesAsync();
    }
}
