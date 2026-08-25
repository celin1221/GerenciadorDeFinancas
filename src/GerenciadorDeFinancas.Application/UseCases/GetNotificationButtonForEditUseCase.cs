using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class GetNotificationButtonForEditUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public GetNotificationButtonForEditUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<NotificationButtonEditData> ExecuteAsync(
        Guid? buttonId,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var activePersons = await unitOfWork.Persons.ListActiveAsync(cancellationToken);

        Guid[]? selectedPersonIds = null;
        string? currentLabel = null;

        if (buttonId.HasValue)
        {
            var button = await unitOfWork.NotificationButtons.GetWithPersonsAsync(buttonId.Value, cancellationToken);
            if (button is not null)
            {
                currentLabel = button.Label;
                selectedPersonIds = button.Persons.Select(bp => bp.PersonId).ToArray();
            }
        }

        var persons = activePersons
            .Select(p => new PersonItem(p.Id, p.Name, p.Color, p.IsActive))
            .ToList();

        return new NotificationButtonEditData(persons, selectedPersonIds, currentLabel);
    }
}

public sealed record NotificationButtonEditData(
    IReadOnlyList<PersonItem> ActivePersons,
    Guid[]? SelectedPersonIds,
    string? CurrentLabel);
