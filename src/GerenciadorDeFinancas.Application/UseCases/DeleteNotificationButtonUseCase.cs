using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class DeleteNotificationButtonUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public DeleteNotificationButtonUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(
        Guid buttonId,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var button = await unitOfWork.NotificationButtons.GetByIdAsync(buttonId, cancellationToken)
            ?? throw new DomainException("Botão não encontrado.");

        unitOfWork.NotificationButtons.Remove(button);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
