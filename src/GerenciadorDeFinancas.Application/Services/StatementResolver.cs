using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.Application.Services;

public static class StatementResolver
{
    public static async Task<Statement> ResolveAsync(
        IUnitOfWork unitOfWork,
        Card card,
        DateTime purchaseDate,
        CancellationToken cancellationToken = default)
    {
        var period = StatementPeriodCalculator.Calculate(card.ClosingDay, DateOnly.FromDateTime(purchaseDate));
        var statement = await unitOfWork.Statements.GetByCardAndYearMonthAsync(card.Id, period.YearMonth, cancellationToken);
        if (statement is null)
        {
            statement = new Statement(card.Id, period.YearMonth, period.OpeningDate, period.ClosingDate);
            unitOfWork.Statements.Add(statement);
        }

        return statement;
    }
}
