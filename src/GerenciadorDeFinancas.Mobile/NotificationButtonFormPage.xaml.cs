using GerenciadorDeFinancas.Application;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public sealed class PersonCheckRow
{
    public Guid Id { get; }

    public string Name { get; set; }

    public bool IsChecked { get; set; }

    public PersonCheckRow(Guid id, string name, bool isChecked)
    {
        Id = id;
        Name = name;
        IsChecked = isChecked;
    }
}

[QueryProperty(nameof(ButtonIdQueryString), "ButtonId")]
public partial class NotificationButtonFormPage : ContentPage
{
    private readonly GetNotificationButtonForEditUseCase _getForEditUseCase;
    private readonly SaveNotificationButtonUseCase _saveUseCase;
    private Guid? _editingButtonId;
    private List<PersonCheckRow> _personRows = new();

    public string? ButtonIdQueryString
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _editingButtonId = id;
                Title = "Editar Botão";
            }
        }
    }

    public NotificationButtonFormPage(
        GetNotificationButtonForEditUseCase getForEditUseCase,
        SaveNotificationButtonUseCase saveUseCase)
    {
        InitializeComponent();
        _getForEditUseCase = getForEditUseCase;
        _saveUseCase = saveUseCase;
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

    private async Task LoadAsync()
    {
        var data = await _getForEditUseCase.ExecuteAsync(_editingButtonId);

        if (data.CurrentLabel is not null)
        {
            LabelEntry.Text = data.CurrentLabel;
        }

        _personRows = data.ActivePersons
            .Select(p => new PersonCheckRow(p.Id, p.Name, data.SelectedPersonIds?.Contains(p.Id) == true))
            .ToList();

        PersonsView.ItemsSource = _personRows;
        UpdateHint();
        UpdatePreview();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (var item in e.CurrentSelection)
        {
            if (item is PersonCheckRow row)
            {
                row.IsChecked = true;
            }
        }

        foreach (var item in e.PreviousSelection)
        {
            if (item is PersonCheckRow row && !_personRows.Contains(row))
            {
                continue;
            }
        }

        PersonsView.SelectedItems?.Clear();
        foreach (var row in _personRows.Where(r => r.IsChecked))
        {
            PersonsView.SelectedItems?.Add(row);
        }

        UpdateHint();
        UpdatePreview();
    }

    private void UpdateHint()
    {
        var selected = _personRows.Count(r => r.IsChecked);
        HintLabel.Text = selected switch
        {
            0 => "Nenhuma pessoa selecionada",
            1 => "1 pessoa selecionada — compra será atribuída a ela",
            _ => $"{selected} pessoas selecionadas — compra será dividida igualmente"
        };
    }

    private void UpdatePreview()
    {
        var selectedNames = _personRows.Where(r => r.IsChecked).Select(r => r.Name).ToList();
        if (selectedNames.Count == 0)
        {
            PreviewLabel.Text = "";
            return;
        }

        var label = LabelGenerator.Generate(selectedNames);
        PreviewLabel.Text = $"Rótulo gerado: {label}";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var selectedPersonIds = _personRows
            .Where(r => r.IsChecked)
            .Select(r => r.Id)
            .ToList();

        if (selectedPersonIds.Count == 0)
        {
            await DisplayAlertAsync("Erro", "Selecione ao menos uma pessoa.", "OK");
            return;
        }

        var label = string.IsNullOrWhiteSpace(LabelEntry.Text)
            ? Application.LabelGenerator.Generate(_personRows.Where(r => r.IsChecked).Select(r => r.Name).ToList())
            : LabelEntry.Text.Trim();

        if (label.Length > 20)
        {
            await DisplayAlertAsync("Erro", "Rótulo deve ter no máximo 20 caracteres.", "OK");
            return;
        }

        try
        {
            await _saveUseCase.ExecuteAsync(_editingButtonId, label, selectedPersonIds);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainException ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }
}
