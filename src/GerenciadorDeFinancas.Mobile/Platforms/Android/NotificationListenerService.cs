using Android.App;
using Android.Content;
using Android.OS;
using Android.Service.Notification;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.ValueObjects;
using GerenciadorDeFinancas.Infrastructure.Notifications;
using GerenciadorDeFinancas.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorDeFinancas;

[Service(
    Exported = true,
    Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE",
    Label = "Captura de compras")]
[IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
public sealed class PurchaseNotificationListener : NotificationListenerService
{
    private const string FeedbackChannelId = "capture_feedback";
    private const string FeedbackChannelName = "Resultado da captura";
    private const int FeedbackNotificationId = 1001;

    private static readonly SemaphoreSlim DbLock = new(1, 1);
    private static bool _dbReady;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateFeedbackChannel();
    }

    public override void OnNotificationPosted(StatusBarNotification? sbn)
    {
        if (sbn is null)
        {
            return;
        }

        if (string.Equals(sbn.PackageName, PackageName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var notification = sbn.Notification;
        if (notification is null)
        {
            return;
        }

        var title = GetExtra(notification, Notification.ExtraTitle);
        var text = GetExtra(notification, Notification.ExtraText)
                   ?? GetExtra(notification, Notification.ExtraBigText);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var raw = new NotificationRaw(
            PackageName: sbn.PackageName ?? string.Empty,
            Title: title ?? string.Empty,
            Text: text ?? string.Empty,
            NotificationKey: sbn.Key,
            PostedAt: DateTimeOffset.FromUnixTimeMilliseconds(sbn.PostTime));

        var services = MainApplication.Services;
        if (services is null)
        {
            return;
        }

        var registry = services.GetRequiredService<INotificationParserRegistry>();
        if (!PurchaseNotificationGate.ShouldProcess(raw, registry))
        {
            return;
        }

        _ = ProcessAsync(raw);
    }

    private async Task ProcessAsync(NotificationRaw raw)
    {
        var services = MainApplication.Services;
        if (services is null)
        {
            return;
        }

        try
        {
            await EnsureDatabaseReadyAsync(services);

            var useCase = services.GetRequiredService<ImportNotificationUseCase>();
            var result = await useCase.ExecuteAsync(raw);

            if (result.Outcome == ImportOutcome.Created)
            {
                return;
            }

            var message = result.Outcome switch
            {
                ImportOutcome.Duplicate => "Notificação duplicada, já cadastrada.",
                ImportOutcome.CardNotMatched => "Nenhum cartão cadastrado para esse banco.",
                ImportOutcome.Unsupported => "Notificação não suportada.",
                _ => "Não foi possível interpretar a notificação.",
            };

            PostFeedback(message, isSuccess: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GerenciadorDeFinancas: erro ao processar notificação: {ex}");
            PostFeedback("Erro ao processar a notificação.", isSuccess: false);
        }
    }

    private static async Task<string> DescribeCreatedAsync(IServiceProvider services, Guid? purchaseId)
    {
        if (purchaseId is null)
        {
            return "Compra cadastrada.";
        }

        using var unitOfWork = services.GetRequiredService<GerenciadorDeFinancas.Domain.Abstractions.IUnitOfWorkFactory>().Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId.Value);
        if (purchase is null)
        {
            return "Compra cadastrada.";
        }

        var merchant = purchase.Merchant?.DisplayName;
        return merchant is null
            ? $"Compra de {Money.FromCents(purchase.AmountCents)} cadastrada."
            : $"Compra de {Money.FromCents(purchase.AmountCents)} em {merchant} cadastrada.";
    }

    private static async Task EnsureDatabaseReadyAsync(IServiceProvider services)
    {
        if (_dbReady)
        {
            return;
        }

        await DbLock.WaitAsync();
        try
        {
            if (_dbReady)
            {
                return;
            }

            await services.GetRequiredService<IDbInitializer>().InitializeAsync();
            _dbReady = true;
        }
        finally
        {
            DbLock.Release();
        }
    }

    private static string? GetExtra(Notification notification, string key) =>
        notification.Extras?.GetCharSequence(key)?.ToString();

    private void CreateFeedbackChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var channel = new NotificationChannel(FeedbackChannelId, FeedbackChannelName, NotificationImportance.Default)
        {
            Description = "Notificações com o resultado da captura de compras.",
        };
        var manager = (NotificationManager?)GetSystemService(Context.NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private void PostFeedback(string message, bool isSuccess)
    {
        var manager = (NotificationManager?)GetSystemService(Context.NotificationService);
        if (manager is null)
        {
            return;
        }

        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(this, FeedbackChannelId)
            : new Notification.Builder(this);
        builder
            .SetSmallIcon(Android.Resource.Drawable.SymDefAppIcon)
            .SetContentTitle(isSuccess ? "Compra capturada" : "Captura de compras")
            .SetContentText(message)
            .SetAutoCancel(true);

        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            builder.SetPriority((int)NotificationPriority.Default);
        }

        manager.Notify(FeedbackNotificationId, builder.Build());
    }
}
