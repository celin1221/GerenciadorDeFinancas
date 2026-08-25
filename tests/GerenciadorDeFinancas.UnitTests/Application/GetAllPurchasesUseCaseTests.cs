using GerenciadorDeFinancas.Application.Services;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class GetAllPurchasesUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var useCase = new GetAllPurchasesUseCase(_factory);

        var result = await useCase.ExecuteAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_RecentClassifiedPurchases_ReturnsAll()
    {
        var (people, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var purchase = new Purchase(cardId, 5000, DateTime.UtcNow, "Café");
            purchase.SetShares(new[] { (people[0], 5000L) });
            purchase.MarkClassified();
            unitOfWork.Purchases.Add(purchase);
            foreach (var share in purchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetAllPurchasesUseCase(_factory);
        var result = await useCase.ExecuteAsync();

        Assert.Single(result);
        Assert.Equal("Café", result[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_OldPurchases_ExcludesOlderThanThreeMonths()
    {
        var (people, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var oldPurchase = new Purchase(cardId, 10000, DateTime.UtcNow.AddMonths(-4), "Compra antiga");
            oldPurchase.SetShares(new[] { (people[0], 10000L) });
            oldPurchase.MarkClassified();
            unitOfWork.Purchases.Add(oldPurchase);
            foreach (var share in oldPurchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }

            var recentPurchase = new Purchase(cardId, 20000, DateTime.UtcNow, "Compra recente");
            recentPurchase.SetShares(new[] { (people[1], 20000L) });
            recentPurchase.MarkClassified();
            unitOfWork.Purchases.Add(recentPurchase);
            foreach (var share in recentPurchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetAllPurchasesUseCase(_factory);
        var result = await useCase.ExecuteAsync();

        Assert.Single(result);
        Assert.Equal("Compra recente", result[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_PendingPurchases_Excluded()
    {
        var (_, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var pending = new Purchase(cardId, 8000, DateTime.UtcNow, "Pendente");
            unitOfWork.Purchases.Add(pending);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetAllPurchasesUseCase(_factory);
        var result = await useCase.ExecuteAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_InstallmentInfo_ReturnedCorrectly()
    {
        var (people, cardId) = await ArrangeAsync();

        using (var unitOfWork = _factory.Create())
        {
            var groupId = Guid.NewGuid();
            var purchase = new Purchase(cardId, 30000, DateTime.UtcNow, "Parcelado",
                installmentGroupId: groupId, installmentNumber: 2, totalInstallments: 3);
            purchase.SetShares(new[] { (people[0], 30000L) });
            purchase.MarkClassified();
            unitOfWork.Purchases.Add(purchase);
            foreach (var share in purchase.Shares)
            {
                unitOfWork.Purchases.AddShare(share);
            }
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new GetAllPurchasesUseCase(_factory);
        var result = await useCase.ExecuteAsync();

        var item = Assert.Single(result);
        Assert.Equal(2, item.InstallmentNumber);
        Assert.Equal(3, item.TotalInstallments);
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

        return (people, cardId);
    }
}
