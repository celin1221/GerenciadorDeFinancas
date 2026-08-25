using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class InstallmentShareReplicationTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task SplittingFirstInstallment_ReplicatesDivisionToPendingSiblings()
    {
        var (people, _, purchaseId) = await ArrangeAsync(amountCents: 30000, purchaseDate: new DateTime(2026, 3, 10));

        await new DeclareInstallmentsUseCase(_factory).ExecuteAsync(purchaseId, 3);

        var groupIds = await GetGroupIdsAsync(purchaseId);
        var splitUseCase = new SplitPurchaseUseCase(_factory);
        await splitUseCase.ExecuteEqualAsync(purchaseId, new[] { people[0], people[1] });

        using var unitOfWork = _factory.Create();
        var all = await unitOfWork.Purchases.ListByGroupAsync(groupIds[0]);
        Assert.Equal(3, all.Count);
        Assert.All(all, purchase =>
        {
            Assert.Equal(PurchaseStatus.Classified, purchase.Status);
            Assert.Equal(2, purchase.Shares.Count);
            Assert.Equal(purchase.AmountCents, purchase.ClassifiedAmountCents);
            Assert.All(purchase.Shares, share => Assert.Equal(purchase.AmountCents / 2, share.AmountCents));
        });
    }

    [Fact]
    public async Task Replication_FixesRoundingDriftSoSiblingTotalsMatch()
    {
        var (people, _, purchaseId) = await ArrangeAsync(amountCents: 100, purchaseDate: new DateTime(2026, 3, 10));

        await new DeclareInstallmentsUseCase(_factory).ExecuteAsync(purchaseId, 3);

        var groupIds = await GetGroupIdsAsync(purchaseId);
        using (var unitOfWork = _factory.Create())
        {
            var first = (await unitOfWork.Purchases.ListByGroupAsync(groupIds[0]))
                .Single(purchase => purchase.InstallmentNumber == 1);
            var shares = new[]
            {
                (people[0], first.AmountCents / 2),
                (people[1], first.AmountCents - first.AmountCents / 2),
            };
            var splitUseCase = new SplitPurchaseUseCase(_factory);
            await splitUseCase.ExecuteCustomAsync(first.Id, shares);
        }

        using var verify = _factory.Create();
        var purchases = await verify.Purchases.ListByGroupAsync(groupIds[0]);
        Assert.Equal(100, purchases.Sum(purchase => purchase.ClassifiedAmountCents));
        Assert.All(purchases, purchase => Assert.Equal(
            purchase.AmountCents,
            purchase.Shares.Sum(share => share.AmountCents)));
    }

    private async Task<List<Guid>> GetGroupIdsAsync(Guid anyGroupId)
    {
        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(anyGroupId);
        return [(Guid)purchase!.InstallmentGroupId!];
    }

    private async Task<(Guid[] People, Guid CardId, Guid PurchaseId)> ArrangeAsync(
        long amountCents,
        DateTime? purchaseDate = null)
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
            var purchase = new Purchase(cardId, amountCents, purchaseDate ?? DateTime.UtcNow, "Jantar");
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        return (people, cardId, purchaseId);
    }
}
