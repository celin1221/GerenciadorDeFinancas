#if DEBUG
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Services;

public sealed class DemoDataInitializer
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public DemoDataInitializer(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task EnsureDemoDataAsync()
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var existing = await unitOfWork.Cards.ListByBankAsync("generic");
        if (existing.Count > 0)
        {
            return;
        }

        var person = new Person("Você", "#512BD4");
        unitOfWork.Persons.Add(person);

        var card = new Card("Cartão de exemplo", "generic", "1234", person.Id, closingDay: 15, dueDay: 25);
        unitOfWork.Cards.Add(card);

        await unitOfWork.SaveChangesAsync();
    }
}
#endif
