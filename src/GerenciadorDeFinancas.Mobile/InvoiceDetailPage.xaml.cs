using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas;

public sealed class InvoicePurchaseRow
{
    public Guid Id { get; }

    public string Title { get; }

    public string DateText { get; }

    public long AmountCents { get; }

    public string Badge { get; }

    public bool HasBadge => !string.IsNullOrEmpty(Badge);

    public string StatusText { get; }

    public bool ShowStatus => !string.IsNullOrEmpty(StatusText);

    public bool IsPending { get; }

    public bool IsIgnored { get; }

    public int TotalInstallments { get; }

    public InvoicePurchaseRow(InvoicePurchaseItem item)
    {
        Id = item.Id;
        Title = item.MerchantName ?? item.Description;
        DateText = item.Date.ToString("dd/MM/yyyy");
        AmountCents = item.AmountCents;
        Badge = item.TotalInstallments > 1 ? $"{item.InstallmentNumber}/{item.TotalInstallments}" : string.Empty;
        StatusText = item.IsPending ? "Aguardando divisão" : item.IsIgnored ? "Ignorada" : string.Empty;
        IsPending = item.IsPending;
        IsIgnored = item.IsIgnored;
        TotalInstallments = item.TotalInstallments;
    }
}

public partial class InvoiceDetailPage : ContentPage, IQueryAttributable
{
    private readonly GetInvoiceDetailUseCase _getInvoiceDetailUseCase;
    private readonly UpdatePurchaseUseCase _updatePurchaseUseCase;
    private readonly ReopenPurchaseUseCase _reopenPurchaseUseCase;
    private readonly DeletePurchaseUseCase _deletePurchaseUseCase;
    private Guid _statementId;

    public InvoiceDetailPage(
        GetInvoiceDetailUseCase getInvoiceDetailUseCase,
        UpdatePurchaseUseCase updatePurchaseUseCase,
        ReopenPurchaseUseCase reopenPurchaseUseCase,
        DeletePurchaseUseCase deletePurchaseUseCase)
    {
        InitializeComponent();
        _getInvoiceDetailUseCase = getInvoiceDetailUseCase;
        _updatePurchaseUseCase = updatePurchaseUseCase;
        _reopenPurchaseUseCase = reopenPurchaseUseCase;
        _deletePurchaseUseCase = deletePurchaseUseCase;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("StatementId", out var value))
        {
            return;
        }

        if (value is Guid guidId)
        {
            _statementId = guidId;
        }
        else if (value is string strId && Guid.TryParse(strId, out var parsedId))
        {
            _statementId = parsedId;
        }
        else
        {
            return;
        }

        _ = LoadAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadAsync();
        Refresh.IsRefreshing = false;
    }

    private async void OnPurchaseTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Guid purchaseId)
        {
            return;
        }

        var row = (PurchasesView.ItemsSource as IEnumerable<InvoicePurchaseRow>)
            ?.FirstOrDefault(item => item.Id == purchaseId);
        if (row is null)
        {
            return;
        }

        var action = await DisplayActionSheetAsync(row.Title, "Cancelar", null,
            row.IsPending ? new[] { "Editar dados", "Excluir compra" } :
            row.IsIgnored ? new[] { "Excluir compra" } :
            new[] { "Reabrir divisão", "Excluir compra" });

        switch (action)
        {
            case "Editar dados":
                await Shell.Current.GoToAsync($"{nameof(EditPurchaseFormPage)}?PurchaseId={row.Id}");
                break;
            case "Reabrir divisão":
                await ReopenAsync(row);
                break;
            case "Excluir compra":
                await DeleteAsync(row);
                break;
        }
    }

    private async Task ReopenAsync(InvoicePurchaseRow row)
    {
        try
        {
            await _reopenPurchaseUseCase.ExecuteAsync(row.Id);
            await LoadAsync();
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async Task DeleteAsync(InvoicePurchaseRow row)
    {
        bool includeGroup = false;
        if (row.TotalInstallments > 1)
        {
            var scope = await DisplayActionSheetAsync(
                "Parcelamento",
                "Cancelar",
                null,
                "Só esta parcela",
                $"Todas as {row.TotalInstallments} parcelas");
            includeGroup = scope == $"Todas as {row.TotalInstallments} parcelas";
            if (scope is null or "Cancelar")
            {
                return;
            }
        }
        else if (!await DisplayAlertAsync(
                "Excluir compra",
                "A compra e suas divisões serão removidas. Continuar?",
                "Excluir",
                "Cancelar"))
        {
            return;
        }

        try
        {
            await _deletePurchaseUseCase.ExecuteAsync(row.Id, includeGroup);
            await LoadAsync();
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async Task LoadAsync()
    {
        var invoice = await _getInvoiceDetailUseCase.ExecuteAsync(_statementId);
        if (invoice is null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        Title = $"Fatura {invoice.YearMonth / 100:D4}/{invoice.YearMonth % 100:D2}";
        TitleLabel.Text = Title;
        CardLabel.Text = invoice.CardName;
        DatesLabel.Text =
            $"Fechamento {invoice.ClosingDate:dd/MM} — vencimento estimado {invoice.EstimatedDueDate:dd/MM/yyyy}";
        TotalLabel.Text = Money.FromCents(invoice.TotalCents).ToString();

        PurchasesView.ItemsSource = invoice.Purchases
            .Select(purchase => new InvoicePurchaseRow(purchase))
            .ToList();
    }
}
