using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public sealed class CardRow
{
    public Guid Id { get; }

    public string Name { get; }

    public bool IsActive { get; }

    public string BankText { get; }

    public string OwnerText { get; }

    public string StatusActionText => IsActive ? "Desativar" : "Reativar";

    public CardRow(Guid id, string name, string bankText, string ownerText, bool isActive)
    {
        Id = id;
        Name = name;
        BankText = bankText;
        OwnerText = ownerText;
        IsActive = isActive;
    }
}

public partial class CardsPage : ContentPage
{
    private readonly ListCardsUseCase _listUseCase;
    private readonly SetCardActiveUseCase _setActiveUseCase;
    private IReadOnlyList<CardRow> _rows = Array.Empty<CardRow>();

    public CardsPage(ListCardsUseCase listUseCase, SetCardActiveUseCase setActiveUseCase)
    {
        InitializeComponent();
        _listUseCase = listUseCase;
        _setActiveUseCase = setActiveUseCase;
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

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(CardFormPage));

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Guid id)
        {
            await Shell.Current.GoToAsync($"{nameof(CardFormPage)}?CardId={id}");
        }
    }

    private async void OnToggleActiveClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
        {
            return;
        }

        var row = _rows.FirstOrDefault(card => card.Id == id);
        if (row is null)
        {
            return;
        }

        try
        {
            await _setActiveUseCase.ExecuteAsync(id, !row.IsActive);
            await LoadAsync();
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async Task LoadAsync()
    {
        var items = await _listUseCase.ExecuteAsync();
        _rows = items
            .Select(card =>
            {
                var last4 = string.IsNullOrWhiteSpace(card.Last4Digits) ? null : $" • •••• {card.Last4Digits}";
                return new CardRow(
                    card.Id,
                    card.Name,
                    $"{card.BankDisplayName ?? card.BankId}{last4}",
                    $"Dono: {card.OwnerName} • Fecha dia {card.ClosingDay} • Vence dia {card.DueDay}",
                    card.IsActive);
            })
            .ToList();
        CardsView.ItemsSource = _rows;
    }
}
