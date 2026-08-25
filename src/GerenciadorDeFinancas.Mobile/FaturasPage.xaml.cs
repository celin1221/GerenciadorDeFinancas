using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;

namespace GerenciadorDeFinancas;

public sealed class CardPickerRow
{
    public Guid Id { get; }

    public string DisplayName { get; }

    public CardPickerRow(Guid id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}

public sealed class InvoiceRow
{
    public Guid StatementId { get; }

    public string Title { get; }

    public string Detail { get; }

    public long TotalCents { get; }

    public string Status { get; }

    public InvoiceRow(InvoiceItem item)
    {
        StatementId = item.StatementId;
        Title = $"Fatura {item.YearMonth / 100:D4}/{item.YearMonth % 100:D2}";
        var pendingText = item.PendingCount > 0 ? $", {item.PendingCount} sem divisão" : string.Empty;
        Detail = $"{item.CardName} — {item.PurchaseCount} compras{pendingText}";
        TotalCents = item.TotalCents;
        Status = item.Status;
    }
}

public partial class FaturasPage : ContentPage
{
    private readonly ListInvoicesUseCase _listInvoicesUseCase;
    private readonly ListCardsUseCase _listCardsUseCase;
    private List<CardPickerRow> _cards = [];

    public FaturasPage(ListInvoicesUseCase listInvoicesUseCase, ListCardsUseCase listCardsUseCase)
    {
        InitializeComponent();
        _listInvoicesUseCase = listInvoicesUseCase;
        _listCardsUseCase = listCardsUseCase;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await LoadCardsAsync();
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        try
        {
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }

    private async void OnCardSelected(object? sender, EventArgs e)
    {
        await LoadInvoicesAsync();
    }

    private async void OnInvoiceSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not InvoiceRow row)
        {
            return;
        }

        ((CollectionView)sender!).SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(InvoiceDetailPage)}?StatementId={row.StatementId}");
    }

    private async Task LoadCardsAsync()
    {
        var selectedId = (CardPicker.SelectedItem as CardPickerRow)?.Id;
        var cards = await _listCardsUseCase.ExecuteAsync();
        _cards =
        [
            new CardPickerRow(Guid.Empty, "Todos os cartões"),
            ..cards
                .Where(card => card.IsActive)
                .OrderBy(card => card.Name)
                .Select(card => new CardPickerRow(card.Id, BuildCardDisplayName(card))),
        ];

        CardPicker.ItemsSource = _cards;
        CardPicker.SelectedItem = _cards.FirstOrDefault(card => card.Id == selectedId) ?? _cards[0];
    }

    private async Task LoadInvoicesAsync()
    {
        var selectedCardId = (CardPicker.SelectedItem as CardPickerRow)?.Id;
        var invoices = await _listInvoicesUseCase.ExecuteAsync(selectedCardId == Guid.Empty ? null : selectedCardId);
        InvoicesView.ItemsSource = invoices.Select(invoice => new InvoiceRow(invoice)).ToList();
    }

    private static string BuildCardDisplayName(CardItem card)
    {
        if (string.IsNullOrWhiteSpace(card.Last4Digits))
        {
            return card.Name;
        }

        return $"{card.Name} ••{card.Last4Digits}";
    }
}
