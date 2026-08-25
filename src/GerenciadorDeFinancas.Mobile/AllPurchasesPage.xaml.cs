using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;

namespace GerenciadorDeFinancas;

public sealed class AllPurchaseRow
{
    public Guid Id { get; }

    public string Title { get; }

    public string DateText { get; }

    public string CardText { get; }

    public long AmountCents { get; }

    public string Badge { get; }

    public bool HasBadge => !string.IsNullOrEmpty(Badge);

    public string SplitText { get; }

    public bool IsSplit { get; }

    public AllPurchaseRow(AllPurchaseItem item)
    {
        Id = item.PurchaseId;
        Title = item.MerchantName ?? item.Title;
        DateText = item.Date.ToString("dd/MM");
        CardText = item.CardName;
        AmountCents = item.AmountCents;
        Badge = item.TotalInstallments > 1 ? $"{item.InstallmentNumber}/{item.TotalInstallments}" : string.Empty;
        IsSplit = item.ShareCount > 1;
        SplitText = IsSplit ? $"Dividida entre {item.ShareCount} pessoas" : string.Empty;
    }
}

public partial class AllPurchasesPage : ContentPage
{
    private readonly GetAllPurchasesUseCase _useCase;

    public AllPurchasesPage(GetAllPurchasesUseCase useCase)
    {
        InitializeComponent();
        _useCase = useCase;
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

    private async Task LoadAsync()
    {
        var purchases = await _useCase.ExecuteAsync();
        CountLabel.Text = $"{purchases.Count} compras";
        PurchasesView.ItemsSource = purchases
            .Select(p => new AllPurchaseRow(p))
            .ToList();
    }
}
