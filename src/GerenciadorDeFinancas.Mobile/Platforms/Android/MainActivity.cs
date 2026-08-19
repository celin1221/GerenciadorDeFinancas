using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace GerenciadorDeFinancas
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private static string? _pendingAction;
        private static string? _pendingPurchaseId;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _ = RequestNotificationPermissionAsync();
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
