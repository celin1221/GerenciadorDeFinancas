using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Enums;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class GetInvoiceDetailUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public GetInvoiceDetailUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<InvoiceDetail?> ExecuteAsync(
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var statement = await unitOfWork.Statements.GetByIdAsync(statementId, cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var purchases = statement.Purchases
            .OrderByDescending(purchase => purchase.Date)
            .Select(purchase => new InvoicePurchaseItem(
                purchase.Id,
                purchase.Description,
                purchase.Merchant?.DisplayName,
                purchase.Date,
                purchase.AmountCents,
                purchase.Status == PurchaseStatus.Pending,
                purchase.Status == PurchaseStatus.Ignored,
                purchase.InstallmentNumber,
                purchase.TotalInstallments))
            .ToList();

        return new InvoiceDetail(
            statement.Id,
            statement.CardId,
            statement.Card?.Name ?? "Cartão",
            statement.YearMonth,
            statement.ClosingDate,
            EstimateDueDate(statement.ClosingDate, statement.Card?.DueDay),
            purchases.Sum(purchase => purchase.AmountCents),
            purchases);
    }

    internal static DateOnly EstimateDueDate(DateOnly closingDate, int? dueDay)
    {
        if (dueDay is not { } day)
        {
            return closingDate;
        }

        var (year, month) = day <= closingDate.Day
            ? NextMonth(closingDate.Year, closingDate.Month)
            : (closingDate.Year, closingDate.Month);

        var clampedDay = Math.Min(day, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, clampedDay);
    }

    private static (int Year, int Month) NextMonth(int year, int month) =>
        month == 12 ? (year + 1, 1) : (year, month + 1);
}
