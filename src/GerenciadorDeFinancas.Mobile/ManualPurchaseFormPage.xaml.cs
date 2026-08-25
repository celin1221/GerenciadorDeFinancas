using System.Globalization;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public partial class ManualPurchaseFormPage : ContentPage
{
    private readonly CreateManualPurchaseUseCase _createManualPurchaseUseCase;
    private readonly ListCardsUseCase _listCardsUseCase;
    private List<CardPickerRow> _cards = [];

    public ManualPurchaseFormPage(
        CreateManualPurchaseUseCase createManualPurchaseUseCase,
        ListCardsUseCase listCardsUseCase)
    {
        InitializeComponent();
        _createManualPurchaseUseCase = createManualPurchaseUseCase;
        _listCardsUseCase = listCardsUseCase;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var cards = await _listCardsUseCase.ExecuteAsync();
            _cards = cards
                .Where(card => card.IsActive)
                .OrderBy(card => card.Name)
                .Select(card => new CardPickerRow(card.Id, BuildCardDisplayName(card)))
                .ToList();
            CardPicker.ItemsSource = _cards;
            CardPicker.SelectedItem = _cards.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (CardPicker.SelectedItem is not CardPickerRow card)
        {
            await DisplayAlertAsync("Erro", "Selecione um cartão.", "OK");
            return;
        }

        var cents = ParseCents(AmountEntry.Text);
        if (cents is null or <= 0)
        {
            await DisplayAlertAsync("Erro", "Informe um valor válido.", "OK");
            return;
        }

        try
        {
            await _createManualPurchaseUseCase.ExecuteAsync(new ManualPurchaseInput(
                card.Id,
                cents.Value,
                DatePick.Date ?? DateTime.Today,
                DescriptionEntry.Text ?? string.Empty,
                string.IsNullOrWhiteSpace(MerchantEntry.Text) ? null : MerchantEntry.Text.Trim()));

            await DisplayAlertAsync("Pronto", "Compra lançada como pendente.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private static string BuildCardDisplayName(CardItem card)
    {
        if (string.IsNullOrWhiteSpace(card.Last4Digits))
        {
            return card.Name;
        }

        return $"{card.Name} ••{card.Last4Digits}";
    }

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
