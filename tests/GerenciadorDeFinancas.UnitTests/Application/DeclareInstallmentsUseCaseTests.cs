using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class DeclareInstallmentsUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_CreatesMonthlySiblingsAndConvertsOriginal()
    {
        var (cardId, purchaseId) = await ArrangeAsync(amountCents: 30000, purchaseDate: new DateTime(2026, 3, 10));

        var useCase = new DeclareInstallmentsUseCase(_factory);
        var ids = await useCase.ExecuteAsync(purchaseId, 3);

        Assert.Equal(3, ids.Count);

        using var unitOfWork = _factory.Create();
        var purchases = new List<Purchase>();
        foreach (var id in ids)
        {
            var purchase = await unitOfWork.Purchases.GetByIdAsync(id);
            Assert.NotNull(purchase);
            purchases.Add(purchase!);
        }

        var groupId = purchases[0].InstallmentGroupId;
        Assert.NotNull(groupId);
        Assert.All(purchases, purchase => Assert.Equal(groupId, purchase.InstallmentGroupId));
        Assert.All(purchases, purchase => Assert.Equal(3, purchase.TotalInstallments));

        Assert.Equal(
            new[] { 1, 2, 3 },
            purchases.OrderBy(purchase => purchase.InstallmentNumber).Select(purchase => purchase.InstallmentNumber));
        Assert.Equal(30000, purchases.Sum(purchase => purchase.AmountCents));
        Assert.All(purchases, purchase => Assert.Equal(PurchaseStatus.Pending, purchase.Status));
    }

    [Fact]
    public async Task ExecuteAsync_AssignsEachSiblingToItsOwnStatement()
    {
        var (cardId, purchaseId) = await ArrangeAsync(amountCents: 30000, purchaseDate: new DateTime(2026, 12, 20));

        var useCase = new DeclareInstallmentsUseCase(_factory);
        var ids = await useCase.ExecuteAsync(purchaseId, 4);

        using var unitOfWork = _factory.Create();
        var yearMonths = new List<int>();
        foreach (var id in ids)
        {
            var purchase = await unitOfWork.Purchases.GetByIdAsync(id);
            Assert.NotNull(purchase!.StatementId);
            var statement = await unitOfWork.Statements.GetByIdAsync(purchase.StatementId!.Value);
            Assert.NotNull(statement);
            Assert.Equal(cardId, statement!.CardId);
            yearMonths.Add(statement.YearMonth);
        }

        // 20/dez cai na fatura de jan/2027 (fechamento dia 15); irmãos: fev, mar e abr/2027.
        Assert.Equal(new[] { 202701, 202702, 202703, 202704 }, yearMonths.OrderBy(month => month));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsAlreadyClassifiedPurchase()
    {
        var (_, purchaseId) = await ArrangeAsync(amountCents: 30000);
        Guid personId;

        using (var unitOfWork = _factory.Create())
        {
            personId = (await unitOfWork.Persons.ListActiveAsync())[0].Id;
        }

        using (var unitOfWork = _factory.Create())
        {
            var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
            purchase!.AssignToSingle(personId);
            foreach (var share in purchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new DeclareInstallmentsUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(purchaseId, 3));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(49)]
    public async Task ExecuteAsync_RejectsInvalidInstallmentCount(int totalInstallments)
    {
        var (_, purchaseId) = await ArrangeAsync();

        var useCase = new DeclareInstallmentsUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(purchaseId, totalInstallments));
    }

    private async Task<(Guid CardId, Guid PurchaseId)> ArrangeAsync(
        long amountCents = 30000,
        DateTime? purchaseDate = null)
    {
        Guid cardId;
        Guid purchaseId;

        using (var unitOfWork = _factory.Create())
        {
            var marcelo = new Person("Marcelo");
            unitOfWork.Persons.Add(marcelo);
            var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: 15, dueDay: 25);
            unitOfWork.Cards.Add(card);
            await unitOfWork.SaveChangesAsync();
            cardId = card.Id;
        }

        using (var unitOfWork = _factory.Create())
        {
            var purchase = new Purchase(cardId, amountCents, purchaseDate ?? DateTime.UtcNow, "Jantar");
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        return (cardId, purchaseId);
    }
}
