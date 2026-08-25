using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;

namespace GerenciadorDeFinancas;

public sealed class PendingPurchaseRow
{
    public Guid Id { get; }

    public string Title { get; }

    public string CardName { get; }

    public long AmountCents { get; }

    public string DateText { get; }

    public string InstallmentText { get; }

    public bool HasInstallment => !string.IsNullOrEmpty(InstallmentText);

    public PendingPurchaseRow(PendingPurchaseItem item)
    {
        Id = item.Id;
        Title = item.MerchantName ?? item.Description;
        CardName = item.CardName;
        AmountCents = item.AmountCents;
        DateText = item.Date.ToString("dd/MM/yyyy");
        InstallmentText = item.InstallmentText;
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
        try
        {
            await LoadAsync();
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
            await LoadAsync();
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

    private async void OnSplitClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Guid id)
        {
            await Shell.Current.GoToAsync($"{nameof(SplitPurchasePage)}?PurchaseId={id}");
        }
    }

    private async void OnAddManualClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ManualPurchaseFormPage));
    }

    private async Task LoadAsync()
    {
        var items = await _listUseCase.ExecuteAsync();
        PendingView.ItemsSource = items
            .Select(item => new PendingPurchaseRow(item))
            .ToList();
    }
}
