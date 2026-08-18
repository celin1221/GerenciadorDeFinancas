using GerenciadorDeFinancas.Application.UseCases;

namespace GerenciadorDeFinancas;

public sealed class PendingPurchaseRow
{
    public Guid Id { get; }

    public string Title { get; }

    public string CardName { get; }

    public long AmountCents { get; }

    public string DateText { get; }

    public PendingPurchaseRow(Guid id, string title, string cardName, long amountCents, string dateText)
    {
        Id = id;
        Title = title;
        CardName = cardName;
        AmountCents = amountCents;
        DateText = dateText;
    }
}

public partial class PendingPurchasesPage : ContentPage
{
    private readonly ListPendingPurchasesUseCase _listUseCase;

    public PendingPurchasesPage(ListPendingPurchasesUseCase listUseCase)
    {
        InitializeComponent();
        _listUseCase = listUseCase;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadAsync();
        Refresh.IsRefreshing = false;
    }

    private async void OnSplitClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Guid id)
        {
            await Shell.Current.GoToAsync($"{nameof(SplitPurchasePage)}?PurchaseId={id}");
        }
    }

    private async Task LoadAsync()
    {
        var items = await _listUseCase.ExecuteAsync();
        PendingView.ItemsSource = items
            .Select(purchase => new PendingPurchaseRow(
                purchase.Id,
                purchase.MerchantName ?? purchase.Description,
                purchase.CardName,
                purchase.AmountCents,
                purchase.Date.ToString("dd/MM/yyyy")))
            .ToList();
    }
}
