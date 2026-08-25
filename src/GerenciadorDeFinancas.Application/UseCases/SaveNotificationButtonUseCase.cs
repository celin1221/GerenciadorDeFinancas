using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class SaveNotificationButtonUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public SaveNotificationButtonUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(
        Guid? buttonId,
        string label,
        IReadOnlyList<Guid> personIds,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        if (buttonId.HasValue)
        {
            var button = await unitOfWork.NotificationButtons.GetByIdAsync(buttonId.Value, cancellationToken)
                ?? throw new DomainException("Botão não encontrado.");

            button.SetLabel(label);
            button.SetPersons(personIds);
        }
        else
        {
            var existingButtons = await unitOfWork.NotificationButtons.ListOrderedAsync(cancellationToken);
            if (existingButtons.Count >= 3)
            {
                throw new DomainException("Máximo de 3 botões permitidos.");
            }

            var button = new NotificationButton(label, existingButtons.Count);
            button.SetPersons(personIds);
            unitOfWork.NotificationButtons.Add(button);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
