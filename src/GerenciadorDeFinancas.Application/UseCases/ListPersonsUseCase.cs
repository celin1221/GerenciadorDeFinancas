using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ListPersonsUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ListPersonsUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<PersonItem>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var persons = await unitOfWork.Persons.ListAsync(cancellationToken);

        return persons
            .Select(person => new PersonItem(
                person.Id,
                person.Name,
                person.Color,
                person.IsActive))
            .ToList();
    }
}
