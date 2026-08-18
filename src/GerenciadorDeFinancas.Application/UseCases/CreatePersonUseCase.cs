using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class CreatePersonUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public CreatePersonUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Guid> ExecuteAsync(
        string name,
        string? color,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var person = new Person(name, color);
        unitOfWork.Persons.Add(person);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return person.Id;
    }
}
