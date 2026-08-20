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
    Label = "Captura de compras",
    ForegroundServiceType = Android.Content.PM.ForegroundService.TypeSpecialUse)]
[IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
public sealed class PurchaseNotificationListener : NotificationListenerService
{
    private const string Tag = "GDF_Capture";
    private const string FeedbackChannelId = "capture_feedback";
    private const string FeedbackChannelName = "Resultado da captura";
    private const int FeedbackNotificationId = 1001;
    private const string ForegroundChannelId = "capture_foreground";
    private const string ForegroundChannelName = "Monitoramento de compras";
    private const int ForegroundNotificationId = 1000;

    private static readonly SemaphoreSlim DbLock = new(1, 1);
    private static bool _dbReady;
    private static IServiceProvider? _fallbackServices;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateFeedbackChannel();
        CreateForegroundChannel();
        StartForegroundService();
        Android.Util.Log.Info(Tag, "PurchaseNotificationListener criado");
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

        Android.Util.Log.Info(Tag, $"Notificação recebida: pkg={sbn.PackageName}, title={title}, text={text?.Substring(0, Math.Min(text.Length, 120))}");

        var raw = new NotificationRaw(
            PackageName: sbn.PackageName ?? string.Empty,
            Title: title ?? string.Empty,
            Text: text ?? string.Empty,
            NotificationKey: sbn.Key,
            PostedAt: DateTimeOffset.FromUnixTimeMilliseconds(sbn.PostTime));

        var services = ResolveServices();
        if (services is null)
        {
            Android.Util.Log.Warn(Tag, "Não foi possível resolver serviços — notificação ignorada");
            return;
        }

        var registry = services.GetRequiredService<INotificationParserRegistry>();
        if (!PurchaseNotificationGate.ShouldProcess(raw, registry))
        {
            Android.Util.Log.Info(Tag, $"Gate rejeitou: pkg={raw.PackageName}");
            return;
        }

        Android.Util.Log.Info(Tag, "Gate passou — processando notificação");
        _ = ProcessAsync(raw, services);
    }

    private async Task ProcessAsync(NotificationRaw raw, IServiceProvider services)
    {
        try
        {
            await EnsureDatabaseReadyAsync(services);

            var useCase = services.GetRequiredService<ImportNotificationUseCase>();
            var result = await useCase.ExecuteAsync(raw);

            Android.Util.Log.Info(Tag, $"Resultado do import: {result.Outcome} (purchaseId={result.PurchaseId})");

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
            Android.Util.Log.Error(Tag, "Erro ao processar notificação", ex);
            PostFeedback("Erro ao processar a notificação.", isSuccess: false);
        }
    }

    private static IServiceProvider? ResolveServices()
    {
        if (MainApplication.Services is { } services)
        {
            return services;
        }

        if (_fallbackServices is { } cached)
        {
            return cached;
        }

        try
        {
            Android.Util.Log.Warn(Tag, "MainApplication.Services é null — inicializando container de fallback");
            var app = MauiProgram.CreateMauiApp();
            _fallbackServices = app.Services;
            return _fallbackServices;
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error(Tag, "Falha ao criar container de fallback", ex);
            return null;
        }
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

            Android.Util.Log.Info(Tag, "Inicializando banco de dados...");
            await services.GetRequiredService<IDbInitializer>().InitializeAsync();
            _dbReady = true;
            Android.Util.Log.Info(Tag, "Banco de dados inicializado");
        }
        finally
        {
            DbLock.Release();
        }
    }

    private void StartForegroundService()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var builder = new Notification.Builder(this, ForegroundChannelId)
            .SetSmallIcon(Android.Resource.Drawable.SymDefAppIcon)
            .SetContentTitle("Gerenciador de Finanças")
            .SetContentText("Monitorando compras...")
            .SetOngoing(true);

        StartForeground(ForegroundNotificationId, builder.Build());
        Android.Util.Log.Info(Tag, "Foreground service iniciado");
    }

    private void CreateForegroundChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var channel = new NotificationChannel(ForegroundChannelId, ForegroundChannelName, NotificationImportance.Low)
        {
            Description = "Notificação persistente de monitoramento de compras.",
        };
        var manager = (NotificationManager?)GetSystemService(Context.NotificationService);
        manager?.CreateNotificationChannel(channel);
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
            Android.Util.Log.Warn(Tag, "PostFeedback: NotificationManager é null");
            return;
        }

        Android.Util.Log.Info(Tag, $"PostFeedback: '{message}' (success={isSuccess})");

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
