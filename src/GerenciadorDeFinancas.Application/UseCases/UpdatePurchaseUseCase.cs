using GerenciadorDeFinancas.Application.Services;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed record UpdatePurchaseInput(
    Guid PurchaseId,
    string? Description = null,
    long? AmountCents = null,
    DateTime? Date = null);

public sealed class UpdatePurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public UpdatePurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(UpdatePurchaseInput input, CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchase = await unitOfWork.Purchases.GetByIdAsync(input.PurchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");

        if (purchase.Status == PurchaseStatus.Ignored)
        {
            throw new DomainException("Compra ignorada não pode ser editada.");
        }

        var dateChanged = input.Date is { } newDate && newDate != purchase.Date;

        purchase.UpdateDetails(
            description: input.Description,
            amountCents: input.AmountCents,
            date: input.Date);

        if (dateChanged && purchase.Card is not null)
        {
            var statement = await StatementResolver.ResolveAsync(unitOfWork, purchase.Card, purchase.Date, cancellationToken);
            purchase.MoveToStatement(statement.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
