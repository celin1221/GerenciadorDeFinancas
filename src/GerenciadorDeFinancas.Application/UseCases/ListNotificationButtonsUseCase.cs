using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ListNotificationButtonsUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ListNotificationButtonsUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<NotificationButtonItem>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();
        var buttons = await unitOfWork.NotificationButtons.ListOrderedAsync(cancellationToken);

        var items = new List<NotificationButtonItem>();
        foreach (var button in buttons)
        {
            var personNames = button.Persons
                .Select(bp => bp.Person?.Name ?? "?")
                .ToList();
            var actionDescription = personNames.Count == 1
                ? $"Atribui tudo a {personNames[0]}"
                : $"Divide entre {string.Join(" e ", personNames)}";

            items.Add(new NotificationButtonItem(
                button.Id,
                button.Label,
                string.Join(", ", personNames),
                actionDescription));
        }

        return items;
    }
}
