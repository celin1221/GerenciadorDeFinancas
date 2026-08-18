using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ListCardsUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ListCardsUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<CardItem>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var cards = await unitOfWork.Cards.ListAsync(cancellationToken);

        return cards
            .Select(card => new CardItem(
                card.Id,
                card.Name,
                card.BankId,
                KnownBanks.DisplayName(card.BankId),
                card.Last4Digits,
                card.OwnerPersonId,
                card.Owner.Name,
                card.ClosingDay,
                card.DueDay,
                card.IsActive))
            .ToList();
    }
}
