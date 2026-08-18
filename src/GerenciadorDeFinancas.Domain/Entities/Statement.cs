using GerenciadorDeFinancas.Domain.Enums;

namespace GerenciadorDeFinancas.Domain.Entities;

public sealed class Statement
{
    private readonly List<Purchase> _purchases = new();

    public Guid Id { get; }

    public Guid CardId { get; }

    public Card? Card { get; private set; }

    public int YearMonth { get; private set; }

    public DateOnly OpeningDate { get; }

    public DateOnly ClosingDate { get; }

    public StatementStatus Status { get; private set; }

    public IReadOnlyCollection<Purchase> Purchases => _purchases;

    private Statement()
    {
    }

    public Statement(Guid cardId, int yearMonth, DateOnly openingDate, DateOnly closingDate)
    {
        Id = Guid.NewGuid();
        CardId = cardId;
        SetYearMonth(yearMonth);
        OpeningDate = openingDate;
        ClosingDate = closingDate;
        Status = StatementStatus.Open;
    }

    public void SetYearMonth(int yearMonth)
    {
        var year = yearMonth / 100;
        var month = yearMonth % 100;
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            throw new Exceptions.DomainException("Mês de referência inválido.");
        }

        YearMonth = yearMonth;
    }

    public void Close() => Status = StatementStatus.Closed;

    public void MarkPaid() => Status = StatementStatus.Paid;

    public void Reopen() => Status = StatementStatus.Open;
}
