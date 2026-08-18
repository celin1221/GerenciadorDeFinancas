using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorDeFinancas;

[BroadcastReceiver(Exported = false)]
public sealed class PurchaseActionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != "ACTION_BUTTON" || context is null)
        {
            return;
        }

        var purchaseIdStr = intent.GetStringExtra("purchase_id");
        var personIdsStr = intent.GetStringArrayExtra("person_ids");
        if (string.IsNullOrEmpty(purchaseIdStr) || personIdsStr is null || personIdsStr.Length == 0)
        {
            return;
        }

        if (!Guid.TryParse(purchaseIdStr, out var purchaseId))
        {
            return;
        }

        var personIds = personIdsStr
            .Select(s => Guid.TryParse(s, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (personIds.Count == 0)
        {
            return;
        }

        var pendingResult = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                var services = MainApplication.Services;
                if (services is null)
                {
                    return;
                }

                var factory = services.GetRequiredService<IUnitOfWorkFactory>();
                using var unitOfWork = factory.Create();

                var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId);
                if (purchase is null || purchase.Status != PurchaseStatus.Pending)
                {
                    ShowToast(context, "Compra já classificada.");
                    return;
                }

                if (personIds.Count == 1)
                {
                    purchase.AssignToSingle(personIds[0]);
                }
                else
                {
                    var parts = Money.SplitEvenlyIntoParts(Money.FromCents(purchase.AmountCents), personIds.Count);
                    var shares = personIds.Select((id, index) => (id, parts[index].Cents)).ToList();
                    purchase.SetShares(shares);
                    purchase.MarkClassified();
                }

                foreach (var share in purchase.Shares)
                {
                    unitOfWork.Purchases.AddShare(share);
                }

                await unitOfWork.SaveChangesAsync();

                var notificationId = purchaseId.GetHashCode() & 0x7FFFFFFF;
                var manager = (NotificationManager?)context!.GetSystemService(Context.NotificationService);
                manager?.Cancel(notificationId);

                var message = personIds.Count == 1
                    ? "Compra atribuída."
                    : $"Compra dividida entre {personIds.Count} pessoas.";
                ShowToast(context, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GerenciadorDeFinancas: erro ao processar ação: {ex}");
                ShowToast(context, "Erro ao processar ação.");
            }
            finally
            {
                pendingResult.Finish();
            }
        });
    }

    private static void ShowToast(Context context, string message)
    {
        new Handler(Looper.MainLooper!).Post(() =>
        {
            Toast.MakeText(context, message, ToastLength.Short)?.Show();
        });
    }
}
