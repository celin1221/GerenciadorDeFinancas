using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class SetPersonActiveUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public SetPersonActiveUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var person = await unitOfWork.Persons.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Pessoa não encontrada.");

        if (isActive)
        {
            person.Reactivate();
        }
        else
        {
            var hasUnpaid = await unitOfWork.Purchases.HasUnpaidSharesForPersonAsync(id, cancellationToken);
            if (hasUnpaid)
            {
                throw new DomainException("Não é possível desativar esta pessoa: existem compras não pagas associadas a ela.");
            }

            person.Deactivate();
        }

        unitOfWork.Persons.Update(person);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
