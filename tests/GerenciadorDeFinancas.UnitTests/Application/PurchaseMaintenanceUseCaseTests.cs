using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class PurchaseMaintenanceUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task AssignMissingStatements_CreatesStatementsForOrphanPurchases()
    {
        var cardId = await ArrangeCardAsync(closingDay: 15);
        var purchaseId = await ArrangeOrphanPurchaseAsync(cardId, new DateTime(2026, 3, 20));

        var useCase = new AssignMissingStatementsUseCase(_factory);
        var assigned = await useCase.ExecuteAsync();

        Assert.Equal(1, assigned);

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.NotNull(purchase!.StatementId);
        var statement = await unitOfWork.Statements.GetByIdAsync(purchase.StatementId!.Value);
        Assert.NotNull(statement!);
        Assert.Equal(cardId, statement.CardId);
        Assert.Equal(202604, statement.YearMonth);
    }

    [Fact]
    public async Task AssignMissingStatements_IsIdempotent()
    {
        var cardId = await ArrangeCardAsync(closingDay: 15);
        await ArrangeOrphanPurchaseAsync(cardId, new DateTime(2026, 3, 20));

        var useCase = new AssignMissingStatementsUseCase(_factory);
        await useCase.ExecuteAsync();
        var secondRun = await useCase.ExecuteAsync();

        Assert.Equal(0, secondRun);
    }

    [Fact]
    public async Task UpdatePurchase_ChangesDescriptionAmountAndDate()
    {
        var cardId = await ArrangeCardAsync(closingDay: 15);
        var purchaseId = await ArrangeClassifiedPurchaseAsync(cardId, new DateTime(2026, 3, 20), 30000);

        var useCase = new UpdatePurchaseUseCase(_factory);
        await useCase.ExecuteAsync(new UpdatePurchaseInput(
            purchaseId,
            Description: "Jantar especial",
            Date: new DateTime(2026, 3, 10)));

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.Equal("Jantar especial", purchase!.Description);
        Assert.Equal(30000, purchase.AmountCents);
        Assert.Equal(new DateTime(2026, 3, 10), purchase.Date);
    }

    [Fact]
    public async Task UpdatePurchase_DateChangeMovesToCorrectStatement()
    {
        var cardId = await ArrangeCardAsync(closingDay: 15);
        var purchaseId = await ArrangeOrphanPurchaseAsync(cardId, new DateTime(2026, 3, 20));
        await new AssignMissingStatementsUseCase(_factory).ExecuteAsync();

        await new UpdatePurchaseUseCase(_factory).ExecuteAsync(new UpdatePurchaseInput(
            purchaseId,
            Date: new DateTime(2026, 2, 5)));

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        var statement = await unitOfWork.Statements.GetByIdAsync(purchase!.StatementId!.Value);
        Assert.Equal(202602, statement!.YearMonth);
    }

    [Fact]
    public async Task UpdatePurchase_AmountOnClassifiedPurchaseThrows()
    {
        var cardId = await ArrangeCardAsync(closingDay: 15);
        var purchaseId = await ArrangeClassifiedPurchaseAsync(cardId, DateTime.Today, 30000);

        var useCase = new UpdatePurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(
            new UpdatePurchaseInput(purchaseId, AmountCents: 999)));
    }

    [Fact]
    public async Task UpdatePurchase_IgnoredPurchaseThrows()
    {
        var cardId = await ArrangeCardAsync(closingDay: 15);
        Guid purchaseId;
        using (var unitOfWork = _factory.Create())
        {
            var purchase = new Purchase(cardId, 10000, DateTime.UtcNow, "Compra ignorada");
            purchase.MarkIgnored();
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        var useCase = new UpdatePurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(
            new UpdatePurchaseInput(purchaseId, Description: "Novo nome")));
    }

    [Fact]
    public async Task UpdatePurchase_UnknownPurchaseThrows()
    {
        var useCase = new UpdatePurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(
            new UpdatePurchaseInput(Guid.NewGuid(), Description: "X")));
    }

    [Fact]
    public async Task DeletePurchase_RemovesSinglePurchaseAndShares()
    {
        var (cardId, people) = await ArrangeWithTwoPeopleAsync(closingDay: 15);
        var purchaseId = await ArrangeClassifiedPurchaseAsync(cardId, DateTime.Today, 30000, people);

        using (var unitOfWork = _factory.Create())
        {
            var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
            Assert.NotEmpty(purchase!.Shares);
        }

        var removed = await new DeletePurchaseUseCase(_factory).ExecuteAsync(purchaseId);

        Assert.Equal(1, removed);
        using var verify = _factory.Create();
        Assert.Null(await verify.Purchases.GetByIdAsync(purchaseId));
    }

    [Fact]
    public async Task DeletePurchase_WithGroupRemovesAllInstallments()
    {
        var (cardId, _) = await ArrangeWithTwoPeopleAsync(closingDay: 15);
        var groupIds = await ArrangeInstallmentGroupAsync(cardId, 3);

        var removed = await new DeletePurchaseUseCase(_factory)
            .ExecuteAsync(groupIds[0], includeInstallmentGroup: true);

        Assert.Equal(3, removed);
        using var unitOfWork = _factory.Create();
        foreach (var id in groupIds)
        {
            Assert.Null(await unitOfWork.Purchases.GetByIdAsync(id));
        }
    }

    [Fact]
    public async Task DeletePurchase_WithoutGroupFlagKeepsSiblings()
    {
        var (cardId, _) = await ArrangeWithTwoPeopleAsync(closingDay: 15);
        var groupIds = await ArrangeInstallmentGroupAsync(cardId, 3);

        var removed = await new DeletePurchaseUseCase(_factory).ExecuteAsync(groupIds[0]);

        Assert.Equal(1, removed);
        using var unitOfWork = _factory.Create();
        Assert.Null(await unitOfWork.Purchases.GetByIdAsync(groupIds[0]));
        Assert.NotNull(await unitOfWork.Purchases.GetByIdAsync(groupIds[1]));
        Assert.NotNull(await unitOfWork.Purchases.GetByIdAsync(groupIds[2]));
    }

    [Fact]
    public async Task ReopenPurchase_ClearsSharesAndReturnsToPending()
    {
        var (cardId, people) = await ArrangeWithTwoPeopleAsync(closingDay: 15);
        var purchaseId = await ArrangeClassifiedPurchaseAsync(cardId, DateTime.Today, 30000, people);

        await new ReopenPurchaseUseCase(_factory).ExecuteAsync(purchaseId);

        using var unitOfWork = _factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
        Assert.Equal(PurchaseStatus.Pending, purchase!.Status);
        Assert.Null(purchase.ClassifiedAt);
        Assert.Empty(purchase.Shares);

        purchase.SetShares(new[] { (people[0], 30000L) });
        purchase.MarkClassified();
        Assert.Equal(PurchaseStatus.Classified, purchase.Status);
    }

    [Fact]
    public async Task ReopenPurchase_UnknownPurchaseThrows()
    {
        var useCase = new ReopenPurchaseUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    private async Task<Guid> ArrangeCardAsync(int closingDay)
    {
        using var unitOfWork = _factory.Create();
        var marcelo = new Person("Marcelo");
        unitOfWork.Persons.Add(marcelo);
        var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: closingDay, dueDay: 25);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync();
        return card.Id;
    }

    private async Task<(Guid CardId, Guid[] People)> ArrangeWithTwoPeopleAsync(int closingDay)
    {
        using var unitOfWork = _factory.Create();
        var marcelo = new Person("Marcelo");
        var joao = new Person("João");
        unitOfWork.Persons.Add(marcelo);
        unitOfWork.Persons.Add(joao);
        var card = new Card("Nubank", "nubank", "1234", marcelo.Id, closingDay: closingDay, dueDay: 25);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync();
        return (card.Id, new[] { marcelo.Id, joao.Id });
    }

    private async Task<Guid> ArrangeOrphanPurchaseAsync(Guid cardId, DateTime date)
    {
        using var unitOfWork = _factory.Create();
        var purchase = new Purchase(cardId, 12000, date, "Notebook");
        unitOfWork.Purchases.Add(purchase);
        await unitOfWork.SaveChangesAsync();
        return purchase.Id;
    }

    private async Task<Guid> ArrangeClassifiedPurchaseAsync(
        Guid cardId,
        DateTime date,
        long amountCents,
        Guid[]? people = null)
    {
        using var unitOfWork = _factory.Create();
        var purchase = new Purchase(cardId, amountCents, date, "Jantar");
        if (people is { Length: > 0 })
        {
            purchase.SetShares(new[]
            {
                (people[0], amountCents - 10000),
                (people[1], 10000L),
            });
        }
        else
        {
            var owner = await unitOfWork.Cards.GetByIdAsync(cardId);
            purchase.SetShares(new[] { (owner.OwnerPersonId, amountCents) });
        }

        purchase.MarkClassified();
        unitOfWork.Purchases.Add(purchase);
        foreach (var share in purchase.Shares)
        {
            unitOfWork.Purchases.AddShare(share);
        }

        await unitOfWork.SaveChangesAsync();
        return purchase.Id;
    }

    private async Task<List<Guid>> ArrangeInstallmentGroupAsync(Guid cardId, int totalInstallments)
    {
        using var unitOfWork = _factory.Create();
        var groupId = Guid.NewGuid();
        var ids = new List<Guid>();
        for (var number = 1; number <= totalInstallments; number++)
        {
            var purchase = new Purchase(
                cardId,
                4000,
                new DateTime(2026, 3, 20).AddMonths(number - 1),
                $"Notebook {number}/{totalInstallments}",
                installmentGroupId: groupId,
                installmentNumber: number,
                totalInstallments: totalInstallments);
            unitOfWork.Purchases.Add(purchase);
            ids.Add(purchase.Id);
        }

        await unitOfWork.SaveChangesAsync();
        return ids;
    }
}
