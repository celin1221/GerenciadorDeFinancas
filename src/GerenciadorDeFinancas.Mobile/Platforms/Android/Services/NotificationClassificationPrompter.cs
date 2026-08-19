using Android.App;
using Android.Content;
using GerenciadorDeFinancas.Application;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorDeFinancas.Services;

public sealed class NotificationClassificationPrompter : IClassificationPrompter
{
    private const string Tag = "GDF_Classify";
    private const string ChannelId = "capture_feedback";
    private const int MaxCustomButtons = 3;

    private readonly IServiceProvider _services;

    public NotificationClassificationPrompter(IServiceProvider services)
    {
        _services = services;
    }

    public void Prompt(ClassificationPrompt prompt)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await PostNotificationAsync(prompt);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(Tag, $"Erro ao postar notificação de classificação: {ex}");
            }
        });
    }

    private async Task PostNotificationAsync(ClassificationPrompt prompt)
    {
        var context = _services.GetService<Context>();
        if (context is null)
        {
            Android.Util.Log.Warn(Tag, "Context é null — não é possível postar notificação");
            return;
        }

        var factory = _services.GetRequiredService<IUnitOfWorkFactory>();
        using var unitOfWork = factory.Create();

        var buttons = await unitOfWork.NotificationButtons.ListOrderedAsync();

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null)
        {
            Android.Util.Log.Warn(Tag, "NotificationManager é null");
            return;
        }

        EnsureChannelExists(manager);

        var contentText = BuildContentText(prompt);
        var contentIntent = CreateContentIntent(context, prompt.PurchaseId);

        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(context, ChannelId)
            : new Notification.Builder(context);

        builder
            .SetSmallIcon(Android.Resource.Drawable.SymDefAppIcon)
            .SetContentTitle("Compra capturada")
            .SetContentText(contentText)
            .SetStyle(new Notification.BigTextStyle().BigText(contentText))
            .SetContentIntent(contentIntent)
            .SetAutoCancel(true);

        var customButtons = buttons.Take(MaxCustomButtons).ToList();
        foreach (var button in customButtons)
        {
            var personIds = button.Persons.Select(bp => bp.PersonId).ToList();
            var actionIntent = CreateActionIntent(context, prompt.PurchaseId, button.Id, personIds);
            var action = new Notification.Action.Builder(
                null, button.Label, actionIntent).Build();
            builder.AddAction(action);
        }

        var notificationId = prompt.PurchaseId.GetHashCode() & 0x7FFFFFFF;
        Android.Util.Log.Info(Tag, $"Postando notificação: id={notificationId}, buttons={customButtons.Count}, text={contentText}");
        manager.Notify(notificationId, builder.Build());
        Android.Util.Log.Info(Tag, "Notificação postada com sucesso");
    }

    private static string BuildContentText(ClassificationPrompt prompt)
    {
        var amount = Money.FromCents(prompt.AmountCents);
        return string.IsNullOrWhiteSpace(prompt.MerchantName)
            ? $"Compra de {amount} cadastrada."
            : $"Compra de {amount} em {prompt.MerchantName} cadastrada.";
    }

    private static PendingIntent CreateContentIntent(Context context, Guid purchaseId)
    {
        var packageName = context.PackageName ?? string.Empty;
        var intent = context.PackageManager?.GetLaunchIntentForPackage(packageName)
            ?? new Intent(Intent.ActionMain);

        intent.PutExtra("action", "OPEN_PENDING");
        intent.PutExtra("purchase_id", purchaseId.ToString());
        intent.AddCategory(Intent.CategoryLauncher);
        intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop | ActivityFlags.SingleTop);

        var flags = OperatingSystem.IsAndroidVersionAtLeast(23)
            ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.UpdateCurrent;

        return PendingIntent.GetActivity(context, purchaseId.GetHashCode(), intent, flags);
    }

    private static PendingIntent CreateActionIntent(Context context, Guid purchaseId, Guid buttonId, IReadOnlyList<Guid> personIds)
    {
        var intent = new Intent(context, typeof(PurchaseActionReceiver));
        intent.SetAction("ACTION_BUTTON");
        intent.PutExtra("purchase_id", purchaseId.ToString());
        intent.PutExtra("button_id", buttonId.ToString());
        intent.PutExtra("person_ids", personIds.Select(id => id.ToString()).ToArray());

        var flags = OperatingSystem.IsAndroidVersionAtLeast(23)
            ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.UpdateCurrent;

        return PendingIntent.GetBroadcast(
            context, purchaseId.GetHashCode() ^ buttonId.GetHashCode(), intent, flags);
    }

    private static void EnsureChannelExists(NotificationManager manager)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var existing = manager.GetNotificationChannel(ChannelId);
        if (existing is not null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, "Resultado da captura", NotificationImportance.Default)
        {
            Description = "Notificações com o resultado da captura de compras.",
        };
        manager.CreateNotificationChannel(channel);
    }
}
