using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.ValueObjects;
using Microsoft.Maui.ApplicationModel;

namespace GerenciadorDeFinancas;

public partial class DashboardPage : ContentPage
{
    private readonly GetDashboardSummaryUseCase _useCase;
    private readonly ImportNotificationUseCase _importUseCase;

    public DashboardPage(GetDashboardSummaryUseCase useCase, ImportNotificationUseCase importUseCase)
    {
        InitializeComponent();
        _useCase = useCase;
        _importUseCase = importUseCase;
        EnableAccessButton.IsVisible = DeviceInfo.Platform == DevicePlatform.Android;
#if !DEBUG
        TestNotificationButton.IsVisible = false;
#endif
    }

    private void OnEnableNotificationAccessClicked(object? sender, EventArgs e)
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var activity = Platform.CurrentActivity;
            if (activity is not null &&
                activity.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
            {
                activity.RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 0);
            }
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(22))
        {
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionNotificationListenerSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
#endif
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

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NotificationButtonsPage));
    }

    private async void OnPersonTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Guid personId)
        {
            await Shell.Current.GoToAsync($"{nameof(PersonDetailPage)}?PersonId={personId}");
        }
    }

    private async void OnTestNotificationClicked(object? sender, EventArgs e)
    {
        var raw = new NotificationRaw(
            PackageName: "com.nubank.nubank",
            Title: "Compra no crédito aprovada",
            Text: "Compra de R$ 65,00 APROVADA em AMPM para o cartão com final 2648",
            NotificationKey: $"test-{Guid.NewGuid():N}",
            PostedAt: DateTimeOffset.UtcNow);

        TestNotificationButton.IsEnabled = false;
        try
        {
            var result = await _importUseCase.ExecuteAsync(raw);
            var message = result.Outcome switch
            {
                ImportOutcome.Created => "Compra capturada! Verifique a aba Pendentes.",
                ImportOutcome.Duplicate => "Compra duplicada (já existe no banco).",
                ImportOutcome.CardNotMatched => "Nenhum cartão Nubank cadastrado. Cadastre um cartão com final 2648.",
                ImportOutcome.ParseFailed => "Falha ao interpretar a notificação.",
                _ => "Não foi possível processar."
            };
            await DisplayAlertAsync("Teste", message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
        finally
        {
            TestNotificationButton.IsEnabled = true;
        }
    }

    private async Task LoadAsync()
    {
        var summary = await _useCase.ExecuteAsync();
        Render(summary);
    }

    private void Render(DashboardSummary summary)
    {
        TotalLabel.Text = Money.FromCents(summary.ClassifiedCents).ToString();
        TotalDetailLabel.Text = $"{summary.ClassifiedCount} compras classificadas";
        PendingAmountLabel.Text = Money.FromCents(summary.PendingCents).ToString();
        PendingCountLabel.Text = $"{summary.PendingCount} compras";
        IgnoredLabel.Text = $"{summary.IgnoredCount}";
        PersonsView.ItemsSource = summary.Persons;
        CardsView.ItemsSource = summary.Cards;
    }
}
