using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class ListPendingPurchasesUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyPendingWithDetails()
    {
        var (ownerId, cardId) = await CreateCardAsync();

        Guid pendingId;
        using (var unitOfWork = _factory.Create())
        {
            var pending = new Purchase(cardId, 18000, DateTime.UtcNow, "Supermercado");
            unitOfWork.Purchases.Add(pending);
            var classified = new Purchase(cardId, 5000, DateTime.UtcNow, "Padaria");
            classified.SetShares(new[] { (ownerId, 5000L) });
            classified.MarkClassified();
            unitOfWork.Purchases.Add(classified);
            await unitOfWork.SaveChangesAsync();
            pendingId = pending.Id;
        }

        var useCase = new ListPendingPurchasesUseCase(_factory);
        var items = await useCase.ExecuteAsync();

        var item = Assert.Single(items);
        Assert.Equal(pendingId, item.Id);
        Assert.Equal(18000, item.AmountCents);
        Assert.Equal(ownerId, item.OwnerPersonId);
        Assert.Equal("Cartão", item.CardName);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyDatabase_ReturnsEmpty()
    {
        var useCase = new ListPendingPurchasesUseCase(_factory);

        Assert.Empty(await useCase.ExecuteAsync());
    }

    private async Task<(Guid OwnerId, Guid CardId)> CreateCardAsync()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var cardId = await new CreateCardUseCase(_factory)
            .ExecuteAsync("Cartão", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);
        return (ownerId, cardId);
    }
}

public class GetPendingPurchaseUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ReturnsPurchaseWithOwner()
    {
        var (ownerId, cardId) = await CreateCardAsync();
        Guid purchaseId;
        using (var unitOfWork = _factory.Create())
        {
            var purchase = new Purchase(cardId, 18000, DateTime.UtcNow, "Supermercado");
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        var useCase = new GetPendingPurchaseUseCase(_factory);
        var item = await useCase.ExecuteAsync(purchaseId);

        Assert.Equal(purchaseId, item.Id);
        Assert.Equal(18000, item.AmountCents);
        Assert.Equal(ownerId, item.OwnerPersonId);
    }

    [Fact]
    public async Task ExecuteAsync_NotFound_Throws()
    {
        var useCase = new GetPendingPurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    private async Task<(Guid OwnerId, Guid CardId)> CreateCardAsync()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var cardId = await new CreateCardUseCase(_factory)
            .ExecuteAsync("Cartão", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);
        return (ownerId, cardId);
    }
}
