using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;

namespace GerenciadorDeFinancas.UnitTests.Persistence;

public class FinanceDbContextTests
{
    [Fact]
    public async Task RoundTrip_PersistsPurchaseWithShares()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        Guid personId;
        Guid purchaseId;

        using (var unitOfWork = factory.Create())
        {
            var person = new Person("Marcelo");
            unitOfWork.Persons.Add(person);
            var card = new Card("Nubank", "nubank", "1234", person.Id, closingDay: 15, dueDay: 25);
            unitOfWork.Cards.Add(card);
            var statement = new Statement(
                card.Id,
                yearMonth: 202608,
                openingDate: new DateOnly(2026, 7, 25),
                closingDate: new DateOnly(2026, 8, 25));
            unitOfWork.Statements.Add(statement);
            await unitOfWork.SaveChangesAsync();
            personId = person.Id;

            var purchase = new Purchase(card.Id, 30000, new DateTime(2026, 8, 1), "Jantar");
            purchase.SetShares(new[] { (person.Id, 30000L) });
            purchase.MarkClassified();
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        using (var unitOfWork = factory.Create())
        {
            var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
            Assert.NotNull(purchase);
            Assert.Equal(PurchaseStatus.Classified, purchase.Status);
            Assert.Single(purchase.Shares);
            Assert.Equal(30000, purchase.ClassifiedAmountCents);
            Assert.Equal(personId, purchase.Shares.First().PersonId);
            Assert.NotNull(purchase.Card);
        }
    }

    [Fact]
    public async Task RoundTrip_PersistsOpenStatementAndLinksPurchase()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        Guid cardId;
        Guid statementId;
        Guid purchaseId;

        using (var unitOfWork = factory.Create())
        {
            var person = new Person("Marcelo");
            unitOfWork.Persons.Add(person);
            var card = new Card("Nubank", "nubank", "1234", person.Id, closingDay: 15, dueDay: 25);
            unitOfWork.Cards.Add(card);
            await unitOfWork.SaveChangesAsync();
            cardId = card.Id;

            var statement = new Statement(
                cardId,
                yearMonth: 202608,
                openingDate: new DateOnly(2026, 7, 25),
                closingDate: new DateOnly(2026, 8, 25));
            unitOfWork.Statements.Add(statement);
            await unitOfWork.SaveChangesAsync();
            statementId = statement.Id;

            var purchase = new Purchase(
                cardId,
                4590,
                new DateTime(2026, 8, 3),
                "iFood",
                statementId: statementId,
                dedupHash: "abc123");
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
            purchaseId = purchase.Id;
        }

        using (var unitOfWork = factory.Create())
        {
            var statement = await unitOfWork.Statements.GetByIdAsync(statementId);
            Assert.NotNull(statement);
            Assert.Equal(StatementStatus.Open, statement.Status);
            Assert.Equal(202608, statement.YearMonth);

            var byStatement = await unitOfWork.Purchases.ListByStatementAsync(statementId);
            Assert.Single(byStatement);
            Assert.Equal(purchaseId, byStatement[0].Id);

            var byDedup = await unitOfWork.Purchases.GetByDedupHashAsync("abc123");
            Assert.Equal(purchaseId, byDedup!.Id);
        }
    }
}
