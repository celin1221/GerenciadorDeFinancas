using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.UseCases;

namespace GerenciadorDeFinancas;

public sealed class NotificationButtonRow
{
    public Guid Id { get; }

    public string Label { get; }

    public string PersonNames { get; }

    public string ActionDescription { get; }

    public NotificationButtonRow(Guid id, string label, string personNames, string actionDescription)
    {
        Id = id;
        Label = label;
        PersonNames = personNames;
        ActionDescription = actionDescription;
    }
}

public partial class NotificationButtonsPage : ContentPage
{
    private readonly ListNotificationButtonsUseCase _listUseCase;
    private readonly DeleteNotificationButtonUseCase _deleteUseCase;

    public NotificationButtonsPage(
        ListNotificationButtonsUseCase listUseCase,
        DeleteNotificationButtonUseCase deleteUseCase)
    {
        InitializeComponent();
        _listUseCase = listUseCase;
        _deleteUseCase = deleteUseCase;
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

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NotificationButtonFormPage));
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Guid id)
        {
            await Shell.Current.GoToAsync($"{nameof(NotificationButtonFormPage)}?ButtonId={id}");
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Guid id)
        {
            return;
        }

        var confirmed = await DisplayAlertAsync("Excluir", "Excluir este botão?", "Sim", "Não");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await _deleteUseCase.ExecuteAsync(id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async Task LoadAsync()
    {
        var items = await _listUseCase.ExecuteAsync();
        ButtonsView.ItemsSource = items
            .Select(item => new NotificationButtonRow(item.Id, item.Label, item.PersonNames, item.ActionDescription))
            .ToList();
    }
}
