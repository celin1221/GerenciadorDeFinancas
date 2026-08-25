using System.Globalization;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public partial class EditPurchaseFormPage : ContentPage, IQueryAttributable
{
    private readonly GetPendingPurchaseUseCase _getPurchaseUseCase;
    private readonly UpdatePurchaseUseCase _updatePurchaseUseCase;
    private Guid _purchaseId;
    private long _originalAmountCents;

    public EditPurchaseFormPage(
        GetPendingPurchaseUseCase getPurchaseUseCase,
        UpdatePurchaseUseCase updatePurchaseUseCase)
    {
        InitializeComponent();
        _getPurchaseUseCase = getPurchaseUseCase;
        _updatePurchaseUseCase = updatePurchaseUseCase;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("PurchaseId", out var value))
        {
            return;
        }

        if (value is Guid guidId)
        {
            _purchaseId = guidId;
        }
        else if (value is string strId && Guid.TryParse(strId, out var parsedId))
        {
            _purchaseId = parsedId;
        }
        else
        {
            return;
        }

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var purchase = await _getPurchaseUseCase.ExecuteAsync(_purchaseId);
        CardLabel.Text = purchase.CardName + (purchase.TotalInstallments > 1
            ? $" — parcela {purchase.InstallmentNumber}/{purchase.TotalInstallments}"
            : string.Empty);
        _originalAmountCents = purchase.AmountCents;
        AmountEntry.Text = (purchase.AmountCents / 100m).ToString("0.00", CultureInfo.InvariantCulture);
        DatePick.Date = purchase.Date;
        DescriptionEntry.Text = purchase.Description;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var cents = ParseCents(AmountEntry.Text);
        if (cents is null or <= 0)
        {
            await DisplayAlertAsync("Erro", "Informe um valor válido.", "OK");
            return;
        }

        try
        {
            var amountChanged = cents.Value != _originalAmountCents;
            await _updatePurchaseUseCase.ExecuteAsync(new UpdatePurchaseInput(
                _purchaseId,
                Description: DescriptionEntry.Text?.Trim(),
                AmountCents: amountChanged ? cents : null,
                Date: DatePick.Date));

            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    internal static long? ParseCents(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleaned = text.Trim()
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace(",", ".");
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? (long)Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero)
            : null;
    }
}
