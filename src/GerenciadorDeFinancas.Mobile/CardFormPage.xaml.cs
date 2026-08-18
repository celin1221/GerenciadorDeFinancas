using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public partial class CardFormPage : ContentPage, IQueryAttributable
{
    private readonly CreateCardUseCase _createUseCase;
    private readonly UpdateCardUseCase _updateUseCase;
    private readonly ListCardsUseCase _listCardsUseCase;
    private readonly ListPersonsUseCase _listPersonsUseCase;
    private IReadOnlyList<PersonItem> _people = Array.Empty<PersonItem>();
    private Guid? _cardId;

    public CardFormPage(
        CreateCardUseCase createUseCase,
        UpdateCardUseCase updateUseCase,
        ListCardsUseCase listCardsUseCase,
        ListPersonsUseCase listPersonsUseCase)
    {
        InitializeComponent();
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _listCardsUseCase = listCardsUseCase;
        _listPersonsUseCase = listPersonsUseCase;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CardId", out var value) && value is Guid id)
        {
            _cardId = id;
            Title = "Editar cartão";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var days = Enumerable.Range(1, 31).Select(day => day.ToString()).ToList();
        BankPicker.ItemsSource = KnownBanks.All.ToList();
        BankPicker.ItemDisplayBinding = new Binding(nameof(BankOption.DisplayName));
        ClosingDayPicker.ItemsSource = days;
        DueDayPicker.ItemsSource = days;
        ClosingDayPicker.SelectedItem = "15";
        DueDayPicker.SelectedItem = "25";

        await LoadPeopleAsync();

        if (_cardId is Guid id)
        {
            await LoadCardAsync(id);
        }
    }

    private async Task LoadPeopleAsync()
    {
        var items = await _listPersonsUseCase.ExecuteAsync();
        _people = items
            .Where(person => person.IsActive)
            .OrderBy(person => person.Name)
            .ToList();
        OwnerPicker.ItemsSource = _people.ToList();
        OwnerPicker.ItemDisplayBinding = new Binding(nameof(PersonItem.Name));
    }

    private async Task LoadCardAsync(Guid id)
    {
        var cards = await _listCardsUseCase.ExecuteAsync();
        var card = cards.FirstOrDefault(item => item.Id == id);
        if (card is null)
        {
            return;
        }

        NameEntry.Text = card.Name;
        Last4Entry.Text = card.Last4Digits;
        BankPicker.SelectedItem = KnownBanks.All.FirstOrDefault(bank => bank.BankId == card.BankId);
        OwnerPicker.SelectedItem = _people.FirstOrDefault(person => person.Id == card.OwnerPersonId);
        ClosingDayPicker.SelectedItem = card.ClosingDay.ToString();
        DueDayPicker.SelectedItem = card.DueDay.ToString();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            if (BankPicker.SelectedItem is not BankOption bank)
            {
                await DisplayAlertAsync("Erro", "Selecione o banco emissor.", "OK");
                return;
            }

            if (OwnerPicker.SelectedItem is not PersonItem owner)
            {
                await DisplayAlertAsync("Erro", "Selecione o dono do cartão.", "OK");
                return;
            }

            var last4 = string.IsNullOrWhiteSpace(Last4Entry.Text) ? null : Last4Entry.Text.Trim();
            var closingDay = int.Parse((string)ClosingDayPicker.SelectedItem);
            var dueDay = int.Parse((string)DueDayPicker.SelectedItem);

            if (_cardId is Guid id)
            {
                await _updateUseCase.ExecuteAsync(id, NameEntry.Text, bank.BankId, last4, owner.Id, closingDay, dueDay);
            }
            else
            {
                await _createUseCase.ExecuteAsync(NameEntry.Text, bank.BankId, last4, owner.Id, closingDay, dueDay);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
