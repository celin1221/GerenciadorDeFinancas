using System.ComponentModel;
using System.Globalization;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas;

public sealed class SplitPersonRow : INotifyPropertyChanged
{
    public Guid Id { get; }

    public string Name { get; }

    public string? Color { get; }

    private bool _isChecked;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }

    public string AmountText { get; set; } = string.Empty;

    public SplitPersonRow(Guid id, string name, string? color, bool isChecked)
    {
        Id = id;
        Name = name;
        Color = color;
        _isChecked = isChecked;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class SplitPurchasePage : ContentPage, IQueryAttributable
{
    private readonly GetPendingPurchaseUseCase _getPurchaseUseCase;
    private readonly ListPersonsUseCase _listPersonsUseCase;
    private readonly SplitPurchaseUseCase _splitUseCase;
    private readonly ClassifyPurchaseUseCase _classifyUseCase;
    private readonly DeclareInstallmentsUseCase _declareInstallmentsUseCase;
    private readonly DeletePurchaseUseCase _deletePurchaseUseCase;
    private IReadOnlyList<SplitPersonRow> _rows = Array.Empty<SplitPersonRow>();
    private Guid _purchaseId;

    public SplitPurchasePage(
        GetPendingPurchaseUseCase getPurchaseUseCase,
        ListPersonsUseCase listPersonsUseCase,
        SplitPurchaseUseCase splitUseCase,
        ClassifyPurchaseUseCase classifyUseCase,
        DeclareInstallmentsUseCase declareInstallmentsUseCase,
        DeletePurchaseUseCase deletePurchaseUseCase)
    {
        InitializeComponent();
        _getPurchaseUseCase = getPurchaseUseCase;
        _listPersonsUseCase = listPersonsUseCase;
        _splitUseCase = splitUseCase;
        _classifyUseCase = classifyUseCase;
        _declareInstallmentsUseCase = declareInstallmentsUseCase;
        _deletePurchaseUseCase = deletePurchaseUseCase;
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
        PurchaseTitleLabel.Text = purchase.MerchantName ?? purchase.Description;
        PurchaseCardLabel.Text = purchase.CardName;
        PurchaseAmountLabel.Text = Money.FromCents(purchase.AmountCents).ToString();

        if (purchase.TotalInstallments > 1)
        {
            InstallmentsLayout.IsVisible = false;
            DeclareInstallmentsButton.IsVisible = false;
            InstallmentBadgeLabel.IsVisible = true;
            InstallmentBadgeLabel.Text = $"Parcela {purchase.InstallmentNumber} de {purchase.TotalInstallments}";
        }
        else
        {
            InstallmentsLayout.IsVisible = true;
            DeclareInstallmentsButton.IsVisible = true;
        }

        var people = await _listPersonsUseCase.ExecuteAsync();
        _rows = people
            .Where(person => person.IsActive)
            .OrderBy(person => person.Name)
            .Select(person => new SplitPersonRow(
                person.Id,
                person.Name,
                person.Color,
                person.Id == purchase.OwnerPersonId))
            .ToList();
        foreach (var row in _rows)
        {
            row.PropertyChanged += OnRowChanged;
        }

        PeopleView.ItemsSource = _rows;
        UpdateActionButtons();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SplitPersonRow.IsChecked))
        {
            UpdateActionButtons();
        }
    }

    private void UpdateActionButtons()
    {
        AssignSingleButton.IsEnabled = _rows.Count(row => row.IsChecked) == 1;
    }

    private async void OnEqualSplitClicked(object? sender, EventArgs e)
    {
        var personIds = _rows.Where(row => row.IsChecked).Select(row => row.Id).ToList();
        try
        {
            await _splitUseCase.ExecuteEqualAsync(_purchaseId, personIds);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnAssignSingleClicked(object? sender, EventArgs e)
    {
        var personIds = _rows.Where(row => row.IsChecked).Select(row => row.Id).ToList();
        if (personIds.Count != 1)
        {
            return;
        }

        try
        {
            await _classifyUseCase.ExecuteAsync(_purchaseId, personIds[0]);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnCustomSaveClicked(object? sender, EventArgs e)
    {
        var shares = new List<(Guid PersonId, long AmountCents)>();
        foreach (var row in _rows.Where(row => row.IsChecked))
        {
            var cents = ParseCents(row.AmountText);
            if (cents is null or <= 0)
            {
                await DisplayAlertAsync("Erro", $"Valor inválido para {row.Name}.", "OK");
                return;
            }

            shares.Add((row.Id, cents.Value));
        }

        if (shares.Count == 0)
        {
            await DisplayAlertAsync("Erro", "Selecione ao menos uma pessoa para a divisão.", "OK");
            return;
        }

        try
        {
            await _splitUseCase.ExecuteCustomAsync(_purchaseId, shares);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnDeclareInstallmentsClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(InstallmentsEntry.Text?.Trim(), out var total) || total < 2)
        {
            await DisplayAlertAsync("Erro", "Informe um número de parcelas maior que 1.", "OK");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Parcelar compra",
            $"A compra será dividida em {total} compras mensais (uma por fatura). A divisão entre pessoas que você fizer na primeira será copiada para as outras. Continuar?",
            "Parcelar",
            "Cancelar");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await _declareInstallmentsUseCase.ExecuteAsync(_purchaseId, total);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnEditPurchaseClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(EditPurchaseFormPage)}?PurchaseId={_purchaseId}");
    }

    private async void OnDeletePurchaseClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Excluir compra",
            "A compra e suas divisões serão removidas. Continuar?",
            "Excluir",
            "Cancelar");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await _deletePurchaseUseCase.ExecuteAsync(_purchaseId);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private static long? ParseCents(string? text)
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
