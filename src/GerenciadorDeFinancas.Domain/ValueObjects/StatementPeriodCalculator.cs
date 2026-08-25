using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Domain.ValueObjects;

public readonly record struct StatementPeriod(int YearMonth, DateOnly OpeningDate, DateOnly ClosingDate);

public static class StatementPeriodCalculator
{
    public static StatementPeriod Calculate(int closingDay, DateOnly purchaseDate)
    {
        if (closingDay is < 1 or > 31)
        {
            throw new DomainException("Dia de fechamento deve estar entre 1 e 31.");
        }

        var (year, month) = purchaseDate.Day <= closingDay
            ? (purchaseDate.Year, purchaseDate.Month)
            : NextMonth(purchaseDate.Year, purchaseDate.Month);

        var closing = GetClosingDate(year, month, closingDay);
        var (prevYear, prevMonth) = PreviousMonth(year, month);
        var opening = GetClosingDate(prevYear, prevMonth, closingDay).AddDays(1);

        return new StatementPeriod(year * 100 + month, opening, closing);
    }

    private static DateOnly GetClosingDate(int year, int month, int closingDay)
    {
        var day = Math.Min(closingDay, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    private static (int Year, int Month) NextMonth(int year, int month) =>
        month == 12 ? (year + 1, 1) : (year, month + 1);

    private static (int Year, int Month) PreviousMonth(int year, int month) =>
        month == 1 ? (year - 1, 12) : (year, month - 1);
}
