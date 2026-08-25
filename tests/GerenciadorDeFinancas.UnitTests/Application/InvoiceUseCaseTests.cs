using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class InvoiceUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ListInvoicesAsync_ReturnsStatementsWithTotalsForCard()
    {
        var (cardId, otherCardId) = await ArrangeTwoCardsAsync();

        await new CreateManualPurchaseUseCase(_factory).ExecuteAsync(
            new ManualPurchaseInput(cardId, 10000, new DateTime(2026, 3, 20), "A", null));
        await new CreateManualPurchaseUseCase(_factory).ExecuteAsync(
            new ManualPurchaseInput(cardId, 5000, new DateTime(2026, 3, 10), "B", null));
        await new CreateManualPurchaseUseCase(_factory).ExecuteAsync(
            new ManualPurchaseInput(otherCardId, 999, new DateTime(2026, 3, 10), "C", null));

        var useCase = new ListInvoicesUseCase(_factory);
        var invoices = await useCase.ExecuteAsync(cardId);

        Assert.Equal(2, invoices.Count);
        Assert.All(invoices, invoice => Assert.Equal(cardId, invoice.CardId));
        var april = invoices.Single(invoice => invoice.YearMonth == 202604);
        Assert.Equal(10000, april.TotalCents);
        Assert.Equal(1, april.PurchaseCount);
        Assert.Equal(1, april.PendingCount);
        Assert.Equal("Aberta", april.Status);
        var march = invoices.Single(invoice => invoice.YearMonth == 202603);
        Assert.Equal(5000, march.TotalCents);
    }

    [Fact]
    public async Task GetInvoiceDetail_ReturnsPurchasesAndEstimatedDueDate()
    {
        var cardId = await ArrangeSingleCardAsync(closingDay: 15, dueDay: 25);

        await new CreateManualPurchaseUseCase(_factory).ExecuteAsync(
            new ManualPurchaseInput(cardId, 30000, new DateTime(2026, 3, 20), "Notebook", "Loja Y"));

        var invoices = await new ListInvoicesUseCase(_factory).ExecuteAsync(cardId);
        var invoice = invoices.Single(item => item.YearMonth == 202604);

        var detail = await new GetInvoiceDetailUseCase(_factory).ExecuteAsync(invoice.StatementId);

        Assert.NotNull(detail);
        Assert.Equal("Nubank", detail!.CardName);
        Assert.Equal(new DateOnly(2026, 4, 15), detail.ClosingDate);
        Assert.Equal(new DateOnly(2026, 4, 25), detail.EstimatedDueDate);

        // O lançamento manual cria uma única compra pendente com o valor total.
        Assert.Equal(30000, detail.TotalCents);
        var purchase = Assert.Single(detail.Purchases);
        Assert.True(purchase.IsPending);
        Assert.False(purchase.IsIgnored);
        Assert.Equal(0, purchase.InstallmentNumber);
        Assert.Equal(0, purchase.TotalInstallments);
    }

    private async Task<(Guid CardId, Guid OtherCardId)> ArrangeTwoCardsAsync()
    {
        using var unitOfWork = _factory.Create();
        var marcelo = new Person("Marcelo");
        unitOfWork.Persons.Add(marcelo);
        var nubank = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: 15, dueDay: 25);
        var inter = new Card("Inter", "inter", "5678", marcelo.Id, closingDay: 20, dueDay: 5);
        unitOfWork.Cards.Add(nubank);
        unitOfWork.Cards.Add(inter);
        await unitOfWork.SaveChangesAsync();
        return (nubank.Id, inter.Id);
    }

    private async Task<Guid> ArrangeSingleCardAsync(int closingDay, int dueDay)
    {
        using var unitOfWork = _factory.Create();
        var marcelo = new Person("Marcelo");
        unitOfWork.Persons.Add(marcelo);
        var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: closingDay, dueDay: dueDay);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync();
        return card.Id;
    }
}
