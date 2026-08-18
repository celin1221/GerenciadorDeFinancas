using GerenciadorDeFinancas.Application;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;

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
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
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

    public NotificationButtonFormPage(IUnitOfWorkFactory unitOfWorkFactory)
    {
        InitializeComponent();
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var unitOfWork = _unitOfWorkFactory.Create();
        var activePersons = await unitOfWork.Persons.ListActiveAsync();

        HashSet<Guid>? selectedPersonIds = null;
        if (_editingButtonId.HasValue)
        {
            var button = await unitOfWork.NotificationButtons.GetWithPersonsAsync(_editingButtonId.Value);
            if (button is not null)
            {
                LabelEntry.Text = button.Label;
                selectedPersonIds = button.Persons.Select(bp => bp.PersonId).ToHashSet();
            }
        }

        _personRows = activePersons
            .Select(p => new PersonCheckRow(p.Id, p.Name, selectedPersonIds?.Contains(p.Id) == true))
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
            ? LabelGenerator.Generate(_personRows.Where(r => r.IsChecked).Select(r => r.Name).ToList())
            : LabelEntry.Text.Trim();

        if (label.Length > 20)
        {
            await DisplayAlertAsync("Erro", "Rótulo deve ter no máximo 20 caracteres.", "OK");
            return;
        }

        using var unitOfWork = _unitOfWorkFactory.Create();

        if (_editingButtonId.HasValue)
        {
            var button = await unitOfWork.NotificationButtons.GetByIdAsync(_editingButtonId.Value);
            if (button is not null)
            {
                button.SetLabel(label);
                button.SetPersons(selectedPersonIds);
            }
        }
        else
        {
            var existingButtons = await unitOfWork.NotificationButtons.ListOrderedAsync();
            if (existingButtons.Count >= 3)
            {
                await DisplayAlertAsync("Limite", "Máximo de 3 botões permitidos.", "OK");
                return;
            }

            var button = new NotificationButton(label, existingButtons.Count);
            button.SetPersons(selectedPersonIds);
            unitOfWork.NotificationButtons.Add(button);
        }

        await unitOfWork.SaveChangesAsync();
        await Shell.Current.GoToAsync("..");
    }
}
