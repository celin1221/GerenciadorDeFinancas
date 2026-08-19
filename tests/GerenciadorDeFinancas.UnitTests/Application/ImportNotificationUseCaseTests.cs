using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Infrastructure.Notifications;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Application;

public class ImportNotificationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesPendingPurchaseAndPrompts()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        var ownerId = await CreateCardAsync(factory, "generic", last4: null);

        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new GenericNotificationParser()), prompter);

        var result = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.Created, result.Outcome);
        Assert.NotNull(result.PurchaseId);
        Assert.Single(prompter.Prompts);
        Assert.Equal(result.PurchaseId, prompter.Prompts[0].PurchaseId);
        Assert.Contains(ownerId, prompter.Prompts[0].SuggestedPersonIds);

        using var unitOfWork = factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(result.PurchaseId!.Value);
        Assert.NotNull(purchase);
        Assert.Equal(18000, purchase.AmountCents);
        Assert.Equal(PurchaseStatus.Pending, purchase.Status);
        Assert.NotNull(purchase.Merchant);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateNotificationIsIgnored()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        await CreateCardAsync(factory, "generic", last4: null);
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new GenericNotificationParser()), prompter);

        var first = await useCase.ExecuteAsync(CreateRaw());
        var second = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.Created, first.Outcome);
        Assert.Equal(ImportOutcome.Duplicate, second.Outcome);
        Assert.Single(prompter.Prompts);

        using var unitOfWork = factory.Create();
        var pending = await unitOfWork.Purchases.ListPendingAsync();
        Assert.Single(pending);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedWhenNoParserMatches()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(), prompter);

        var result = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.Unsupported, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_CardNotMatchedWhenNoCardRegistered()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new GenericNotificationParser()), prompter);

        var result = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.CardNotMatched, result.Outcome);
        Assert.Empty(prompter.Prompts);
    }

    [Fact]
    public async Task ExecuteAsync_AutoCreatesGenericCardWhenMultipleCardsExist()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        await CreateCardAsync(factory, "generic", last4: null);
        await CreateCardAsync(factory, "generic", last4: null);
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new GenericNotificationParser()), prompter);

        var result = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.Created, result.Outcome);
        Assert.NotNull(result.PurchaseId);
        Assert.Single(prompter.Prompts);

        using var unitOfWork = factory.Create();
        var cards = await unitOfWork.Cards.ListByBankAsync("generic");
        Assert.Equal(3, cards.Count);
        var genericCard = cards.Single(c => c.Last4Digits is null && c.Name == "generic");
        Assert.Equal(1, genericCard.ClosingDay);
        Assert.Equal(10, genericCard.DueDay);
    }

    [Fact]
    public async Task ExecuteAsync_AutoCreatesGenericCardWhenNoCardMatches()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        var personId = await CreatePersonAsync(factory);
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new GenericNotificationParser()), prompter);

        var result = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.Created, result.Outcome);
        Assert.NotNull(result.PurchaseId);
        Assert.Single(prompter.Prompts);

        using var unitOfWork = factory.Create();
        var cards = await unitOfWork.Cards.ListByBankAsync("generic");
        Assert.Single(cards);
        var card = cards[0];
        Assert.Equal("generic", card.Name);
        Assert.Equal("generic", card.BankId);
        Assert.Null(card.Last4Digits);
        Assert.Equal(personId, card.OwnerPersonId);

        var purchase = await unitOfWork.Purchases.GetByIdAsync(result.PurchaseId!.Value);
        Assert.NotNull(purchase);
        Assert.Equal(card.Id, purchase.CardId);
    }

    [Fact]
    public async Task ExecuteAsync_CardNotMatchedWhenNoActivePersons()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new GenericNotificationParser()), prompter);

        var result = await useCase.ExecuteAsync(CreateRaw());

        Assert.Equal(ImportOutcome.CardNotMatched, result.Outcome);
        Assert.Empty(prompter.Prompts);
    }

    [Fact]
    public async Task ExecuteAsync_BankParser_CreatesPurchaseAndSuggestsOwner()
    {
        var factory = TestDb.CreateUnitOfWorkFactory();
        var prompter = new RecordingPrompter();
        var ownerId = await CreateCardAsync(factory, "nubank", last4: "1234");
        var useCase = new ImportNotificationUseCase(factory, CreateRegistry(new NubankNotificationParser()), prompter);

        var result = await useCase.ExecuteAsync(CreateNubankRaw());

        Assert.Equal(ImportOutcome.Created, result.Outcome);
        Assert.NotNull(result.PurchaseId);
        Assert.Single(prompter.Prompts);
        Assert.Equal(result.PurchaseId, prompter.Prompts[0].PurchaseId);
        Assert.Contains(ownerId, prompter.Prompts[0].SuggestedPersonIds);

        using var unitOfWork = factory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(result.PurchaseId!.Value);
        Assert.NotNull(purchase);
        Assert.Equal(10000, purchase.AmountCents);
        Assert.Equal(PurchaseStatus.Pending, purchase.Status);
        Assert.Equal("Mercado Livre", purchase.Merchant?.DisplayName);
    }

    private static NotificationRaw CreateRaw() =>
        new(
            PackageName: "com.banco.desconhecido",
            Title: "Compra aprovada",
            Text: "Compra de R$ 180,00 no Supermercado X",
            NotificationKey: "key-1",
            PostedAt: DateTimeOffset.UtcNow);

    private static NotificationRaw CreateNubankRaw() =>
        new(
            PackageName: KnownBanks.NubankPackage,
            Title: "Compra no crédito aprovada",
            Text: "Compra de R$ 300,00 APROVADA em Mercado Livre em 3x de R$ 100,00 para o cartão com final 1234.",
            NotificationKey: "key-nubank-1",
            PostedAt: DateTimeOffset.UtcNow);

    private static INotificationParserRegistry CreateRegistry(params INotificationParser[] parsers) =>
        new NotificationParserRegistry(parsers);

    private static async Task<Guid> CreateCardAsync(IUnitOfWorkFactory factory, string bankId, string? last4)
    {
        using var unitOfWork = factory.Create();
        var owner = new Person("Dona do cartão");
        unitOfWork.Persons.Add(owner);
        unitOfWork.Cards.Add(new Card("Cartão", bankId, last4, owner.Id, closingDay: 15, dueDay: 25));
        await unitOfWork.SaveChangesAsync();
        return owner.Id;
    }

    private static async Task<Guid> CreatePersonAsync(IUnitOfWorkFactory factory)
    {
        using var unitOfWork = factory.Create();
        var person = new Person("Pessoa");
        unitOfWork.Persons.Add(person);
        await unitOfWork.SaveChangesAsync();
        return person.Id;
    }
}
