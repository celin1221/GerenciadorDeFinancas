using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class CreateCardUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public CreateCardUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Guid> ExecuteAsync(
        string name,
        string bankId,
        string? last4Digits,
        Guid ownerPersonId,
        int closingDay,
        int dueDay,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        if (await unitOfWork.Persons.GetByIdAsync(ownerPersonId, cancellationToken) is null)
        {
            throw new DomainException("Pessoa dona do cartão não encontrada.");
        }

        var card = new Card(name, bankId, last4Digits, ownerPersonId, closingDay, dueDay);
        unitOfWork.Cards.Add(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return card.Id;
    }
}
