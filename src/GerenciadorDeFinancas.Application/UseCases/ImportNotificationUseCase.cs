using System.Security.Cryptography;
using System.Text;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ImportNotificationUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly INotificationParserRegistry _parserRegistry;
    private readonly IClassificationPrompter _prompter;

    public ImportNotificationUseCase(
        IUnitOfWorkFactory unitOfWorkFactory,
        INotificationParserRegistry parserRegistry,
        IClassificationPrompter prompter)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _parserRegistry = parserRegistry;
        _prompter = prompter;
    }

    public async Task<ImportNotificationResult> ExecuteAsync(
        NotificationRaw notification,
        CancellationToken cancellationToken = default)
    {
        var parser = _parserRegistry.Find(notification);
        if (parser is null)
        {
            System.Diagnostics.Debug.WriteLine("GDF_Import: nenhum parser encontrado");
            return ImportNotificationResult.Unsupported();
        }

        var parsed = parser.TryParse(notification);
        if (parsed is null)
        {
            System.Diagnostics.Debug.WriteLine($"GDF_Import: TryParse retornou null (parser={parser.BankId})");
            return ImportNotificationResult.ParseFailed();
        }

        System.Diagnostics.Debug.WriteLine($"GDF_Import: parse ok — bank={parsed.BankId}, merchant={parsed.MerchantName}, amount={parsed.AmountCents}, card4={parsed.CardLast4}");

        using var unitOfWork = _unitOfWorkFactory.Create();

        var dedupHash = ComputeDedupHash(parsed, notification);
        if (await unitOfWork.Purchases.GetByDedupHashAsync(dedupHash, cancellationToken) is not null)
        {
            System.Diagnostics.Debug.WriteLine($"GDF_Import: duplicata detectada (hash={dedupHash[..12]}...)");
            return ImportNotificationResult.Duplicate();
        }

        var card = await ResolveCardAsync(unitOfWork, parsed, cancellationToken);
        if (card is null)
        {
            System.Diagnostics.Debug.WriteLine("GDF_Import: CardNotMatched — nenhuma pessoa ativa ou cartão disponível");
            return ImportNotificationResult.CardNotMatched();
        }

        System.Diagnostics.Debug.WriteLine($"GDF_Import: cartão resolvido — cardId={card.Id}, last4={card.Last4Digits}");

        var merchant = await ResolveMerchantAsync(unitOfWork, parsed, cancellationToken);
        var statement = await unitOfWork.Statements.GetOpenForCardAsync(card.Id, cancellationToken);

        var purchase = new Purchase(
            cardId: card.Id,
            amountCents: parsed.AmountCents,
            date: parsed.Date,
            description: parsed.Description,
            merchantId: merchant?.Id,
            statementId: statement?.Id,
            bankRefId: parsed.BankRefId,
            dedupHash: dedupHash,
            rawNotificationText: notification.Text,
            postingDate: parsed.Date);

        unitOfWork.Purchases.Add(purchase);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        System.Diagnostics.Debug.WriteLine($"GDF_Import: compra criada — purchaseId={purchase.Id}");

        _prompter.Prompt(new ClassificationPrompt(
            purchase.Id,
            purchase.AmountCents,
            merchant?.DisplayName ?? parsed.MerchantName,
            new[] { card.OwnerPersonId }));

        return ImportNotificationResult.Created(purchase.Id, purchase.AmountCents, merchant?.DisplayName ?? parsed.MerchantName, card.OwnerPersonId);
    }

    private static async Task<Card?> ResolveCardAsync(
        IUnitOfWork unitOfWork,
        ParsedPurchase parsed,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parsed.CardLast4))
        {
            var match = await unitOfWork.Cards.GetByBankAndLast4Async(parsed.BankId, parsed.CardLast4, cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }
        else
        {
            var cards = await unitOfWork.Cards.ListByBankAsync(parsed.BankId, cancellationToken);
            if (cards.Count == 1)
            {
                return cards[0];
            }
        }

        return await CreateGenericCardAsync(unitOfWork, parsed, cancellationToken);
    }

    private static async Task<Card?> CreateGenericCardAsync(
        IUnitOfWork unitOfWork,
        ParsedPurchase parsed,
        CancellationToken cancellationToken)
    {
        var persons = await unitOfWork.Persons.ListActiveAsync(cancellationToken);
        var owner = persons.FirstOrDefault();
        if (owner is null)
        {
            return null;
        }

        var card = new Card(
            name: parsed.BankId,
            bankId: parsed.BankId,
            last4Digits: null,
            ownerPersonId: owner.Id,
            closingDay: 1,
            dueDay: 10);

        unitOfWork.Cards.Add(card);
        return card;
    }

    private static async Task<Merchant?> ResolveMerchantAsync(
        IUnitOfWork unitOfWork,
        ParsedPurchase parsed,
        CancellationToken cancellationToken)
    {
        var normalizedName = Merchant.Normalize(parsed.MerchantName);
        var merchant = await unitOfWork.Merchants.GetByNormalizedNameAsync(normalizedName, cancellationToken);
        if (merchant is not null)
        {
            return merchant;
        }

        merchant = new Merchant(parsed.MerchantName);
        unitOfWork.Merchants.Add(merchant);
        return merchant;
    }

    private static string ComputeDedupHash(ParsedPurchase parsed, NotificationRaw notification)
    {
        var normalizedMerchant = Merchant.Normalize(parsed.MerchantName);
        var notificationKey = notification.NotificationKey ?? notification.PostedAt.Ticks.ToString();
        var payload = $"{parsed.BankId}|{normalizedMerchant}|{parsed.AmountCents}|{parsed.Date:yyyyMMdd}|{notificationKey}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
