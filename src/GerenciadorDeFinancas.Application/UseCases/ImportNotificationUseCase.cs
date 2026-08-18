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
            return ImportNotificationResult.Unsupported();
        }

        var parsed = parser.TryParse(notification);
        if (parsed is null)
        {
            return ImportNotificationResult.ParseFailed();
        }

        using var unitOfWork = _unitOfWorkFactory.Create();

        var dedupHash = ComputeDedupHash(parsed);
        if (await unitOfWork.Purchases.GetByDedupHashAsync(dedupHash, cancellationToken) is not null)
        {
            return ImportNotificationResult.Duplicate();
        }

        var card = await ResolveCardAsync(unitOfWork, parsed, cancellationToken);
        if (card is null)
        {
            return ImportNotificationResult.CardNotMatched();
        }

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

        _prompter.Prompt(new ClassificationPrompt(
            purchase.Id,
            purchase.AmountCents,
            merchant?.DisplayName ?? parsed.MerchantName,
            new[] { card.OwnerPersonId }));

        return ImportNotificationResult.Created(purchase.Id);
    }

    private static async Task<Card?> ResolveCardAsync(
        IUnitOfWork unitOfWork,
        ParsedPurchase parsed,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parsed.CardLast4))
        {
            return await unitOfWork.Cards.GetByBankAndLast4Async(parsed.BankId, parsed.CardLast4, cancellationToken);
        }

        var cards = await unitOfWork.Cards.ListByBankAsync(parsed.BankId, cancellationToken);
        return cards.Count == 1 ? cards[0] : null;
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

    private static string ComputeDedupHash(ParsedPurchase parsed)
    {
        var normalizedMerchant = Merchant.Normalize(parsed.MerchantName);
        var payload = $"{parsed.BankId}|{normalizedMerchant}|{parsed.AmountCents}|{parsed.Date:yyyyMMdd}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
