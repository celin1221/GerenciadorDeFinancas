using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas;

public partial class PersonFormPage : ContentPage, IQueryAttributable
{
    private static readonly IReadOnlyList<string> PaletteColors = new[]
    {
        "#E53935", "#F06292", "#D81B60", "#FB8C00",
        "#FDD835", "#7CB342", "#43A047", "#00897B",
        "#00ACC1", "#1E88E5", "#3949AB", "#5E35B1",
        "#6D4C41", "#757575",
    };

    private readonly CreatePersonUseCase _createUseCase;
    private readonly UpdatePersonUseCase _updateUseCase;
    private readonly ListPersonsUseCase _listUseCase;
    private readonly Dictionary<string, Button> _swatches = new();
    private Guid? _personId;
    private string? _selectedColor;
    private bool _updatingSliders;

    public PersonFormPage(
        CreatePersonUseCase createUseCase,
        UpdatePersonUseCase updateUseCase,
        ListPersonsUseCase listUseCase)
    {
        InitializeComponent();
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _listUseCase = listUseCase;
        BuildPalette();
        SetSelected(null);
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

        if (TryParseHex(person.Color, out var red, out var green, out var blue))
        {
            SetSelected($"#{red:X2}{green:X2}{blue:X2}");
        }
        else
        {
            SetSelected(null);
        }
    }

    private void BuildPalette()
    {
        foreach (var hex in PaletteColors)
        {
            var button = new Button
            {
                BackgroundColor = Color.FromArgb(hex),
                CornerRadius = 8,
                WidthRequest = 44,
                HeightRequest = 32,
                Margin = new Thickness(0, 0, 8, 8),
                BorderWidth = 0,
            };
            button.CommandParameter = hex;
            button.Clicked += OnPaletteClicked;
            _swatches[hex] = button;
            PaletteLayout.Add(button);
        }
    }

    private void OnPaletteClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not string hex)
        {
            return;
        }

        SetSelected(hex);
    }

    private void OnNoColorClicked(object? sender, EventArgs e) =>
        SetSelected(null);

    private void OnComponentChanged(object? sender, EventArgs e)
    {
        if (_updatingSliders)
        {
            return;
        }

        UpdateComponentLabels();
        SetSelected(
            $"#{(int)RedSlider.Value:X2}{(int)GreenSlider.Value:X2}{(int)BlueSlider.Value:X2}",
            syncSliders: false);
    }

    private void SetSelected(string? color, bool syncSliders = true)
    {
        _selectedColor = color;

        if (syncSliders && color is not null && TryParseHex(color, out var red, out var green, out var blue))
        {
            _updatingSliders = true;
            RedSlider.Value = red;
            GreenSlider.Value = green;
            BlueSlider.Value = blue;
            _updatingSliders = false;
            UpdateComponentLabels();
        }

        MarkSwatches();
        UpdatePreview();
    }

    private void MarkSwatches()
    {
        foreach (var (hex, button) in _swatches)
        {
            if (hex == _selectedColor)
            {
                button.BorderWidth = 3;
                button.BorderColor = GetContrastColor(hex);
            }
            else
            {
                button.BorderWidth = 0;
            }
        }
    }

    private void UpdatePreview()
    {
        if (_selectedColor is null)
        {
            PreviewDot.Fill = new SolidColorBrush(Colors.Gray);
            PreviewHexLabel.Text = "Sem cor (cinza)";
            return;
        }

        PreviewDot.Fill = new SolidColorBrush(Color.FromArgb(_selectedColor));
        PreviewHexLabel.Text = _selectedColor;
    }

    private void UpdateComponentLabels()
    {
        RedValueLabel.Text = ((int)RedSlider.Value).ToString();
        GreenValueLabel.Text = ((int)GreenSlider.Value).ToString();
        BlueValueLabel.Text = ((int)BlueSlider.Value).ToString();
    }

    private static Color GetContrastColor(string hex)
    {
        if (!TryParseHex(hex, out var red, out var green, out var blue))
        {
            return Colors.Black;
        }

        var luminance = (0.299 * red + 0.587 * green + 0.114 * blue) / 255d;
        return luminance > 0.6 ? Colors.Black : Colors.White;
    }

    private static bool TryParseHex(string? value, out int red, out int green, out int blue)
    {
        red = 0;
        green = 0;
        blue = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim().TrimStart('#');
        if (hex.Length != 6 || !hex.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        try
        {
            red = Convert.ToInt32(hex[..2], 16);
            green = Convert.ToInt32(hex.Substring(2, 2), 16);
            blue = Convert.ToInt32(hex.Substring(4, 2), 16);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_personId is Guid id)
            {
                await _updateUseCase.ExecuteAsync(id, NameEntry.Text, _selectedColor);
            }
            else
            {
                await _createUseCase.ExecuteAsync(NameEntry.Text, _selectedColor);
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
