using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace GerenciadorDeFinancas
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const string Tag = "GDF_Main";
        private static string? _pendingAction;
        private static string? _pendingPurchaseId;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _ = RequestNotificationPermissionAsync();
            _ = CheckNotificationListenerAsync();
            HandleIntent(Intent);
        }

        private async Task RequestNotificationPermissionAsync()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return;
            }

            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            System.Diagnostics.Debug.WriteLine($"GerenciadorDeFinancas: POST_NOTIFICATIONS permission = {status}");
        }

        private async Task CheckNotificationListenerAsync()
        {
            await Task.Delay(1000);

            try
            {
                var enabledSetting = Android.Provider.Settings.Secure.GetString(
                    ContentResolver, "enabled_notification_listeners") ?? string.Empty;
                var myPackage = PackageName ?? string.Empty;

                if (enabledSetting.Contains(myPackage))
                {
                    Android.Util.Log.Info(Tag, "Notification listener habilitado");
                    return;
                }

                Android.Util.Log.Warn(Tag, "Notification listener NÃO habilitado — guiando usuário");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Microsoft.Maui.Controls.Application.Current?.Windows[0]?.Page;
                    if (page is null)
                    {
                        return;
                    }

                    var result = await page.DisplayAlertAsync(
                        "Acesso a notificações",
                        "Para capturar compras automaticamente, é necessário habilitar o acesso a notificações do app.\n\nDeseja abrir as configurações?",
                        "Abrir configurações",
                        "Agora não");

                    if (result)
                    {
                        var intent = new Intent(Android.Provider.Settings.ActionNotificationListenerSettings);
                        intent.AddFlags(ActivityFlags.NewTask);
                        StartActivity(intent);
                    }
                });
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(Tag, $"Erro ao verificar notification listener: {ex.Message}");
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        private void HandleIntent(Intent? intent)
        {
            var action = intent?.GetStringExtra("action");
            var purchaseId = intent?.GetStringExtra("purchase_id");

            if (string.IsNullOrEmpty(action))
            {
                return;
            }

            if (Microsoft.Maui.Controls.Application.Current?.Windows[0]?.Page?.Navigation is not null)
            {
                NavigateToAction(action, purchaseId);
            }
            else
            {
                _pendingAction = action;
                _pendingPurchaseId = purchaseId;
            }
        }

        private static void NavigateToAction(string action, string? purchaseId)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                switch (action)
                {
                    case "OPEN_PENDING":
                        await Shell.Current.GoToAsync("//Pendentes");
                        break;
                    case "OPEN_BUTTONS":
                        await Shell.Current.GoToAsync(nameof(NotificationButtonsPage));
                        break;
                    case "OPEN_PURCHASE":
                        if (!string.IsNullOrEmpty(purchaseId) && Guid.TryParse(purchaseId, out var id))
                        {
                            await Shell.Current.GoToAsync($"{nameof(SplitPurchasePage)}?PurchaseId={id}");
                        }
                        break;
                }
            });
        }

        internal static void FlushPendingNavigation()
        {
            if (_pendingAction is not null)
            {
                NavigateToAction(_pendingAction, _pendingPurchaseId);
                _pendingAction = null;
                _pendingPurchaseId = null;
            }
        }
    }
}
