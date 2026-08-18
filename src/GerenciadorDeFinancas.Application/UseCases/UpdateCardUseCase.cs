using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class UpdateCardUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public UpdateCardUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(
        Guid id,
        string name,
        string bankId,
        string? last4Digits,
        Guid ownerPersonId,
        int closingDay,
        int dueDay,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var card = await unitOfWork.Cards.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Cartão não encontrado.");

        if (await unitOfWork.Persons.GetByIdAsync(ownerPersonId, cancellationToken) is null)
        {
            throw new DomainException("Pessoa dona do cartão não encontrada.");
        }

        card.SetName(name);
        card.SetBank(bankId);
        card.SetLast4Digits(last4Digits);
        card.SetOwner(ownerPersonId);
        card.SetClosingDay(closingDay);
        card.SetDueDay(dueDay);

        unitOfWork.Cards.Update(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
