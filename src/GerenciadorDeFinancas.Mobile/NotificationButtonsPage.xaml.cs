using GerenciadorDeFinancas.Application;
using GerenciadorDeFinancas.Domain.Abstractions;

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
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public NotificationButtonsPage(IUnitOfWorkFactory unitOfWorkFactory)
    {
        InitializeComponent();
        _unitOfWorkFactory = unitOfWorkFactory;
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

        using var unitOfWork = _unitOfWorkFactory.Create();
        var button = await unitOfWork.NotificationButtons.GetByIdAsync(id);
        if (button is not null)
        {
            unitOfWork.NotificationButtons.Remove(button);
            await unitOfWork.SaveChangesAsync();
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var unitOfWork = _unitOfWorkFactory.Create();
        var buttons = await unitOfWork.NotificationButtons.ListOrderedAsync();

        var rows = new List<NotificationButtonRow>();
        foreach (var button in buttons)
        {
            var personNames = button.Persons
                .Select(bp => bp.Person?.Name ?? "?")
                .ToList();
            var label = personNames.Count > 0
                ? LabelGenerator.Generate(personNames)
                : button.Label;
            var actionDescription = personNames.Count == 1
                ? $"Atribui tudo a {personNames[0]}"
                : $"Divide entre {string.Join(" e ", personNames)}";

            rows.Add(new NotificationButtonRow(button.Id, button.Label, string.Join(", ", personNames), actionDescription));
        }

        ButtonsView.ItemsSource = rows;
    }
}
