using GerenciadorDeFinancas.Application.Services;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed record ManualPurchaseInput(
    Guid CardId,
    long AmountCents,
    DateTime Date,
    string Description,
    string? MerchantName);

public sealed class CreateManualPurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public CreateManualPurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Guid> ExecuteAsync(
        ManualPurchaseInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.AmountCents <= 0)
        {
            throw new DomainException("Valor da compra deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            throw new DomainException("Descrição da compra é obrigatória.");
        }

        using var unitOfWork = _unitOfWorkFactory.Create();

        var card = await unitOfWork.Cards.GetByIdAsync(input.CardId, cancellationToken)
            ?? throw new DomainException("Cartão não encontrado.");

        Guid? merchantId = null;
        if (!string.IsNullOrWhiteSpace(input.MerchantName))
        {
            merchantId = await ResolveMerchantIdAsync(unitOfWork, input.MerchantName, cancellationToken);
        }

        var statement = await StatementResolver.ResolveAsync(unitOfWork, card, input.Date, cancellationToken);
        var purchase = new Purchase(
            cardId: card.Id,
            amountCents: input.AmountCents,
            date: input.Date,
            description: input.Description.Trim(),
            merchantId: merchantId,
            statementId: statement.Id);

        unitOfWork.Purchases.Add(purchase);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return purchase.Id;
    }

    private static async Task<Guid> ResolveMerchantIdAsync(
        IUnitOfWork unitOfWork,
        string merchantName,
        CancellationToken cancellationToken)
    {
        var normalizedName = Merchant.Normalize(merchantName);
        var merchant = await unitOfWork.Merchants.GetByNormalizedNameAsync(normalizedName, cancellationToken);
        if (merchant is not null)
        {
            return merchant.Id;
        }

        merchant = new Merchant(merchantName.Trim());
        unitOfWork.Merchants.Add(merchant);
        return merchant.Id;
    }
}
