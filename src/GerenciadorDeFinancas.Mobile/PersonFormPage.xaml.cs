using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public partial class PersonFormPage : ContentPage, IQueryAttributable
{
    private readonly CreatePersonUseCase _createUseCase;
    private readonly UpdatePersonUseCase _updateUseCase;
    private readonly ListPersonsUseCase _listUseCase;
    private Guid? _personId;

    public PersonFormPage(
        CreatePersonUseCase createUseCase,
        UpdatePersonUseCase updateUseCase,
        ListPersonsUseCase listUseCase)
    {
        InitializeComponent();
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _listUseCase = listUseCase;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("PersonId", out var value) && value is Guid id)
        {
            _personId = id;
            Title = "Editar pessoa";
            _ = LoadAsync(id);
        }
    }

    private async Task LoadAsync(Guid id)
    {
        var items = await _listUseCase.ExecuteAsync();
        var person = items.FirstOrDefault(item => item.Id == id);
        if (person is null)
        {
            return;
        }

        NameEntry.Text = person.Name;
        ColorEntry.Text = person.Color;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var color = string.IsNullOrWhiteSpace(ColorEntry.Text) ? null : ColorEntry.Text.Trim();

            if (_personId is Guid id)
            {
                await _updateUseCase.ExecuteAsync(id, NameEntry.Text, color);
            }
            else
            {
                await _createUseCase.ExecuteAsync(NameEntry.Text, color);
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
