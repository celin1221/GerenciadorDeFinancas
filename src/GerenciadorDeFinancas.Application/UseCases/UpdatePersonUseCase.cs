using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class UpdatePersonUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public UpdatePersonUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(
        Guid id,
        string name,
        string? color,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var person = await unitOfWork.Persons.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Pessoa não encontrada.");

        person.SetName(name);
        person.SetColor(color);

        unitOfWork.Persons.Update(person);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
