using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public sealed class PersonRow
{
    public Guid Id { get; }

    public string Name { get; }

    public string? Color { get; }

    public bool IsActive { get; }

    public string StatusActionText => IsActive ? "Desativar" : "Reativar";

    public PersonRow(Guid id, string name, string? color, bool isActive)
    {
        Id = id;
        Name = name;
        Color = color;
        IsActive = isActive;
    }
}

public partial class PeoplePage : ContentPage
{
    private readonly ListPersonsUseCase _listUseCase;
    private readonly SetPersonActiveUseCase _setActiveUseCase;
    private IReadOnlyList<PersonRow> _rows = Array.Empty<PersonRow>();

    public PeoplePage(ListPersonsUseCase listUseCase, SetPersonActiveUseCase setActiveUseCase)
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
        await Shell.Current.GoToAsync(nameof(PersonFormPage));

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Guid id)
        {
            await Shell.Current.GoToAsync($"{nameof(PersonFormPage)}?PersonId={id}");
        }
    }

    private async void OnToggleActiveClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
        {
            return;
        }

        var row = _rows.FirstOrDefault(person => person.Id == id);
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
            .Select(person => new PersonRow(person.Id, person.Name, person.Color, person.IsActive))
            .ToList();
        PeopleView.ItemsSource = _rows;
    }
}
