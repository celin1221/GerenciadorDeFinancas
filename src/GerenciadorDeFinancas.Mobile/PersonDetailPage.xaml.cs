using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas;

public sealed class PersonCardRow
{
    public string CardName { get; }

    public string AmountText { get; }

    public PersonCardRow(string cardName, long totalCents)
    {
        CardName = cardName;
        AmountText = Money.FromCents(totalCents).ToString();
    }
}

public sealed class PersonPurchaseRow
{
    public string Title { get; }

    public string DateText { get; }

    public string AmountText { get; }

    public string DetailText { get; }

    public bool IsSplit { get; }

    public PersonPurchaseRow(
        string title,
        DateTime date,
        long purchaseTotalCents,
        long personShareCents,
        bool isSplit,
        int shareCount)
    {
        Title = title;
        DateText = date.ToString("dd/MM/yyyy");
        IsSplit = isSplit;
        DetailText = isSplit ? $"Dividida entre {shareCount} pessoas" : string.Empty;
        AmountText = isSplit
            ? $"{Money.FromCents(purchaseTotalCents)} → {Money.FromCents(personShareCents)}"
            : Money.FromCents(personShareCents).ToString();
    }
}

public partial class PersonDetailPage : ContentPage, IQueryAttributable
{
    private readonly GetPersonDetailUseCase _useCase;
    private Guid _personId;

    public PersonDetailPage(GetPersonDetailUseCase useCase)
    {
        InitializeComponent();
        _useCase = useCase;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("PersonId", out var value))
        {
            return;
        }

        if (value is Guid guidId)
        {
            _personId = guidId;
        }
        else if (value is string strId && Guid.TryParse(strId, out var parsedId))
        {
            _personId = parsedId;
        }
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

    private async Task LoadAsync()
    {
        if (_personId == Guid.Empty)
        {
            return;
        }

        var detail = await _useCase.ExecuteAsync(_personId);
        if (detail is null)
        {
            await DisplayAlertAsync("Erro", "Pessoa não encontrada.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        Title = detail.Name;
        PersonNameLabel.Text = detail.Name;
        TotalLabel.Text = Money.FromCents(detail.TotalCents).ToString();
        HeaderDot.Fill = new SolidColorBrush(GetColorOrDefault(detail.Color));

        CardsView.ItemsSource = detail.Cards
            .Select(card => new PersonCardRow(card.CardName, card.TotalCents))
            .ToList();

        PurchasesView.ItemsSource = detail.Purchases
            .Select(purchase => new PersonPurchaseRow(
                purchase.Title,
                purchase.Date,
                purchase.PurchaseTotalCents,
                purchase.PersonShareCents,
                purchase.IsSplit,
                purchase.ShareCount))
            .ToList();
    }

    private static Color GetColorOrDefault(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return Color.FromArgb(hex);
            }
            catch
            {
            }
        }

        return Colors.Gray;
    }
}
