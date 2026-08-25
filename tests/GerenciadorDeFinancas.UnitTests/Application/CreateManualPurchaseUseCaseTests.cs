using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class CreateManualPurchaseUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_CreatesPendingPurchaseInCorrectStatement()
    {
        var cardId = await ArrangeAsync(closingDay: 15);

        var useCase = new CreateManualPurchaseUseCase(_factory);
        var purchaseId = await useCase.ExecuteAsync(new ManualPurchaseInput(
            cardId,
            12345,
            new DateTime(2026, 3, 20),
            "Supermercado",
            null));

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(PurchaseStatus.Pending, purchase.Status);
        Assert.Equal(0, purchase.TotalInstallments);
        Assert.Null(purchase.InstallmentGroupId);
        Assert.Null(purchase.DedupHash);
        var statement = await unitOfWork.Statements.GetByIdAsync(purchase.StatementId!.Value);
        Assert.Equal(202604, statement!.YearMonth);
    }

    [Fact]
    public async Task ExecuteAsync_PastDateIsAllowed()
    {
        var cardId = await ArrangeAsync(closingDay: 15);

        var useCase = new CreateManualPurchaseUseCase(_factory);
        var purchaseId = await useCase.ExecuteAsync(new ManualPurchaseInput(
            cardId,
            5000,
            new DateTime(2024, 1, 5),
            "Compra antiga",
            null));

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.NotNull(purchase);
        Assert.Equal(new DateTime(2024, 1, 5), purchase.Date);
        var statement = await unitOfWork.Statements.GetByIdAsync(purchase.StatementId!.Value);
        Assert.Equal(202401, statement!.YearMonth);
    }

    [Fact]
    public async Task ExecuteAsync_ReusesExistingMerchant()
    {
        var cardId = await ArrangeAsync(closingDay: 15);

        var useCase = new CreateManualPurchaseUseCase(_factory);
        await useCase.ExecuteAsync(new ManualPurchaseInput(cardId, 1000, new DateTime(2026, 3, 10), "Lanche", "Padaria X"));
        await useCase.ExecuteAsync(new ManualPurchaseInput(cardId, 2000, new DateTime(2026, 3, 11), "Lanche", "padaria x"));

        using var unitOfWork = _factory.Create();
        var merchant = await unitOfWork.Merchants.GetByNormalizedNameAsync(Merchant.Normalize("Padaria X"));
        Assert.NotNull(merchant);
        Assert.Equal("Padaria X", merchant!.DisplayName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task ExecuteAsync_RejectsNonPositiveAmount(long amountCents)
    {
        var cardId = await ArrangeAsync(closingDay: 15);

        var useCase = new CreateManualPurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(
            new ManualPurchaseInput(cardId, amountCents, DateTime.UtcNow, "Teste", null)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyDescription()
    {
        var cardId = await ArrangeAsync(closingDay: 15);

        var useCase = new CreateManualPurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(
            new ManualPurchaseInput(cardId, 1000, DateTime.UtcNow, "  ", null)));
    }

    private async Task<Guid> ArrangeAsync(int closingDay)
    {
        using var unitOfWork = _factory.Create();
        var marcelo = new Person("Marcelo");
        unitOfWork.Persons.Add(marcelo);
        var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: closingDay, dueDay: 25);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync();
        return card.Id;
    }
}
