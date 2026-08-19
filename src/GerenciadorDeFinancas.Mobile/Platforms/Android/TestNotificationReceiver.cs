#if DEBUG
using Android.App;
using Android.Content;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Infrastructure.Notifications;
using GerenciadorDeFinancas.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorDeFinancas;

[BroadcastReceiver(Exported = true, Label = "Teste de notificação")]
[IntentFilter(new[] { "com.gerenciadordefinancas.TEST_NOTIFICATION" })]
public sealed class TestNotificationReceiver : BroadcastReceiver
{
    private const string Tag = "GDF_Test";

    public override void OnReceive(Context? context, Intent? intent)
    {
        var packageName = intent?.GetStringExtra("package");
        var title = intent?.GetStringExtra("title");
        var text = intent?.GetStringExtra("text");

        if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(text))
        {
            Android.Util.Log.Warn(Tag, "Uso: adb shell am broadcast -n com.companyname.gerenciadordefinancas/.TestNotificationReceiver --es package \"com.nubank.nubank\" --es title \"Compra aprovada\" --es text \"Compra de R$ 50,00 em Padaria\"");
            return;
        }

        Android.Util.Log.Info(Tag, $"Recebido: pkg={packageName}, title={title}, text={text}");

        var services = MainApplication.Services;
        if (services is null)
        {
            Android.Util.Log.Error(Tag, "MainApplication.Services é null — app ainda não inicializado? Abra o app uma vez antes de testar.");
            return;
        }

        var raw = new NotificationRaw(
            PackageName: packageName,
            Title: title,
            Text: text,
            NotificationKey: $"test-{Guid.NewGuid():N}",
            PostedAt: DateTimeOffset.Now);

        var registry = services.GetRequiredService<INotificationParserRegistry>();
        if (!PurchaseNotificationGate.ShouldProcess(raw, registry))
        {
            Android.Util.Log.Warn(Tag, $"Gate rejeitou: pkg={packageName}. Verifique se é um dos pacotes suportados.");
            return;
        }

        Android.Util.Log.Info(Tag, "Gate passou — executando import...");

        var pendingResult = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                await services.GetRequiredService<IDbInitializer>().InitializeAsync();

                var useCase = services.GetRequiredService<ImportNotificationUseCase>();
                var result = await useCase.ExecuteAsync(raw);

                Android.Util.Log.Info(Tag, $"Resultado: {result.Outcome} (purchaseId={result.PurchaseId})");
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(Tag, $"Erro: {ex}");
            }
            finally
            {
                pendingResult.Finish();
            }
        });
    }
}
#endif
