using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class UpdatePersonUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_UpdatesNameAndColor()
    {
        var id = await CreatePersonAsync();
        var useCase = new UpdatePersonUseCase(_factory);

        await useCase.ExecuteAsync(id, "João", "#112233");

        using var unitOfWork = _factory.Create();
        var person = await unitOfWork.Persons.GetByIdAsync(id);
        Assert.Equal("João", person!.Name);
        Assert.Equal("#112233", person.Color);
    }

    [Fact]
    public async Task ExecuteAsync_NotFound_Throws()
    {
        var useCase = new UpdatePersonUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Guid.NewGuid(), "X", null));
    }

    [Fact]
    public async Task ExecuteAsync_BlankName_Throws()
    {
        var id = await CreatePersonAsync();
        var useCase = new UpdatePersonUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(id, string.Empty, null));
    }

    private async Task<Guid> CreatePersonAsync()
    {
        var useCase = new CreatePersonUseCase(_factory);
        return await useCase.ExecuteAsync("Marcelo", null);
    }
}

public class SetPersonActiveUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_DeactivatesAndReactivates()
    {
        var id = await new CreatePersonUseCase(_factory).ExecuteAsync("Marcelo", null);
        var useCase = new SetPersonActiveUseCase(_factory);

        await useCase.ExecuteAsync(id, isActive: false);

        using (var unitOfWork = _factory.Create())
        {
            var person = await unitOfWork.Persons.GetByIdAsync(id);
            Assert.False(person!.IsActive);
        }

        await useCase.ExecuteAsync(id, isActive: true);

        using (var unitOfWork = _factory.Create())
        {
            var person = await unitOfWork.Persons.GetByIdAsync(id);
            Assert.True(person!.IsActive);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NotFound_Throws()
    {
        var useCase = new SetPersonActiveUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Guid.NewGuid(), isActive: false));
    }

    [Fact]
    public async Task ExecuteAsync_DeactivateBlocked_WhenPersonHasUnpaidShares()
    {
        var personId = await CreatePersonWithClassifiedPurchaseAsync(hasStatement: false);
        var useCase = new SetPersonActiveUseCase(_factory);

        var ex = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(personId, isActive: false));
        Assert.Contains("não pagas", ex.Message);

        using var unitOfWork = _factory.Create();
        var person = await unitOfWork.Persons.GetByIdAsync(personId);
        Assert.True(person!.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_DeactivateBlocked_WhenSharesInOpenStatement()
    {
        var personId = await CreatePersonWithClassifiedPurchaseAsync(hasStatement: true, statementPaid: false);
        var useCase = new SetPersonActiveUseCase(_factory);

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(personId, isActive: false));
    }

    [Fact]
    public async Task ExecuteAsync_DeactivateAllowed_WhenSharesOnlyInPaidStatements()
    {
        var personId = await CreatePersonWithClassifiedPurchaseAsync(hasStatement: true, statementPaid: true);
        var useCase = new SetPersonActiveUseCase(_factory);

        await useCase.ExecuteAsync(personId, isActive: false);

        using var unitOfWork = _factory.Create();
        var person = await unitOfWork.Persons.GetByIdAsync(personId);
        Assert.False(person!.IsActive);
    }

    private async Task<Guid> CreatePersonWithClassifiedPurchaseAsync(bool hasStatement, bool statementPaid = false)
    {
        Guid personId;
        Guid cardId;

        using (var unitOfWork = _factory.Create())
        {
            var person = new Person("Teste");
            unitOfWork.Persons.Add(person);
            var card = new Card("Nubank", "nubank", "1234", person.Id, closingDay: 15, dueDay: 25);
            unitOfWork.Cards.Add(card);
            await unitOfWork.SaveChangesAsync();
            personId = person.Id;
            cardId = card.Id;
        }

        Guid? statementId = null;
        if (hasStatement)
        {
            using (var unitOfWork = _factory.Create())
            {
                var statement = new Statement(cardId, 202601, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
                if (statementPaid)
                {
                    statement.MarkPaid();
                }

                unitOfWork.Statements.Add(statement);
                await unitOfWork.SaveChangesAsync();
                statementId = statement.Id;
            }
        }

        using (var unitOfWork = _factory.Create())
        {
            var purchase = new Purchase(cardId, 30000, DateTime.UtcNow, "Compra teste", statementId: statementId);
            purchase.SetShares(new[] { (personId, 30000L) });
            purchase.MarkClassified();
            unitOfWork.Purchases.Add(purchase);
            await unitOfWork.SaveChangesAsync();
        }

        return personId;
    }
}

public class ListPersonsUseCaseTests
{
    private readonly IUnitOfWorkFactory _factory = TestDb.CreateUnitOfWorkFactory();

    [Fact]
    public async Task ExecuteAsync_ReturnsAllPersonsWithActiveStatus()
    {
        var createUseCase = new CreatePersonUseCase(_factory);
        var marceloId = await createUseCase.ExecuteAsync("Marcelo", null);
        await createUseCase.ExecuteAsync("João", null);

        using (var unitOfWork = _factory.Create())
        {
            var marcelo = await unitOfWork.Persons.GetByIdAsync(marceloId);
            marcelo!.Deactivate();
            unitOfWork.Persons.Update(marcelo);
            await unitOfWork.SaveChangesAsync();
        }

        var useCase = new ListPersonsUseCase(_factory);
        var items = await useCase.ExecuteAsync();

        Assert.Equal(2, items.Count);
        Assert.Contains(items, person => person.Name == "João" && person.IsActive);
        Assert.Contains(items, person => person.Name == "Marcelo" && !person.IsActive);
    }
}
