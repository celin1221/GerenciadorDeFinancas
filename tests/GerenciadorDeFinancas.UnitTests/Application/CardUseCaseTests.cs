using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class CreateCardUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ValidCard_PersistsAndReturnsId()
    {
        var ownerId = await CreatePersonAsync();
        var useCase = new CreateCardUseCase(_factory);

        var id = await useCase.ExecuteAsync("Nubank do Marcelo", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);

        Assert.NotEqual(Guid.Empty, id);
        using var unitOfWork = _factory.Create();
        var card = await unitOfWork.Cards.GetByIdAsync(id);
        Assert.NotNull(card);
        Assert.Equal("nubank", card!.BankId);
        Assert.Equal(ownerId, card.OwnerPersonId);
        Assert.Equal(15, card.ClosingDay);
        Assert.Equal(25, card.DueDay);
    }

    [Fact]
    public async Task ExecuteAsync_OwnerNotFound_Throws()
    {
        var useCase = new CreateCardUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync("Cartão", "nubank", null, Guid.NewGuid(), closingDay: 15, dueDay: 25));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidLast4_Throws()
    {
        var ownerId = await CreatePersonAsync();
        var useCase = new CreateCardUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync("Cartão", "nubank", "123", ownerId, closingDay: 15, dueDay: 25));
    }

    private async Task<Guid> CreatePersonAsync() =>
        await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
}

public class UpdateCardUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_UpdatesFields()
    {
        var (ownerId, cardId) = await CreateCardAsync();
        var useCase = new UpdateCardUseCase(_factory);

        await useCase.ExecuteAsync(cardId, "Novo nome", "inter", null, ownerId, closingDay: 5, dueDay: 10);

        using var unitOfWork = _factory.Create();
        var card = await unitOfWork.Cards.GetByIdAsync(cardId);
        Assert.Equal("Novo nome", card!.Name);
        Assert.Equal("inter", card.BankId);
        Assert.Null(card.Last4Digits);
        Assert.Equal(5, card.ClosingDay);
        Assert.Equal(10, card.DueDay);
    }

    [Fact]
    public async Task ExecuteAsync_NotFound_Throws()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var useCase = new UpdateCardUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), "X", "nubank", null, ownerId, closingDay: 15, dueDay: 25));
    }

    private async Task<(Guid OwnerId, Guid CardId)> CreateCardAsync()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var cardId = await new CreateCardUseCase(_factory)
            .ExecuteAsync("Nubank", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);
        return (ownerId, cardId);
    }
}

public class SetCardActiveUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_DeactivatesAndReactivates()
    {
        var cardId = await CreateCardAsync();
        var useCase = new SetCardActiveUseCase(_factory);

        await useCase.ExecuteAsync(cardId, isActive: false);

        using (var unitOfWork = _factory.Create())
        {
            var card = await unitOfWork.Cards.GetByIdAsync(cardId);
            Assert.False(card!.IsActive);
        }

        await useCase.ExecuteAsync(cardId, isActive: true);

        using (var unitOfWork = _factory.Create())
        {
            var card = await unitOfWork.Cards.GetByIdAsync(cardId);
            Assert.True(card!.IsActive);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NotFound_Throws()
    {
        var useCase = new SetCardActiveUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Guid.NewGuid(), isActive: false));
    }

    private async Task<Guid> CreateCardAsync()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        return await new CreateCardUseCase(_factory)
            .ExecuteAsync("Nubank", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);
    }
}

public class ListCardsUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ReturnsCardsWithOwnerAndBankDisplayName()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var cardId = await new CreateCardUseCase(_factory)
            .ExecuteAsync("Nubank do Marcelo", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);

        var useCase = new ListCardsUseCase(_factory);
        var items = await useCase.ExecuteAsync();

        var card = Assert.Single(items);
        Assert.Equal(cardId, card.Id);
        Assert.Equal("Nubank do Marcelo", card.Name);
        Assert.Equal("Nubank", card.BankDisplayName);
        Assert.Equal("Marcelo", card.OwnerName);
        Assert.Equal(ownerId, card.OwnerPersonId);
        Assert.True(card.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesInactiveCards()
    {
        var ownerId = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var cardId = await new CreateCardUseCase(_factory)
            .ExecuteAsync("Nubank", "nubank", "1234", ownerId, closingDay: 15, dueDay: 25);

        using (var unitOfWork = _factory.Create())
        {
            var storedCard = await unitOfWork.Cards.GetByIdAsync(cardId);
            storedCard!.Deactivate();
            unitOfWork.Cards.Update(storedCard);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new ListCardsUseCase(_factory);
        var items = await useCase.ExecuteAsync();

        var card = Assert.Single(items);
        Assert.False(card.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyDatabase_ReturnsEmpty()
    {
        var useCase = new ListCardsUseCase(_factory);

        Assert.Empty(await useCase.ExecuteAsync());
    }
}
