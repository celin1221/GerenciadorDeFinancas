using GerenciadorDeFinancas.Persistence;
using GerenciadorDeFinancas.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorDeFinancas
{
    public partial class App : global::Microsoft.Maui.Controls.Application
    {
        private readonly IDbInitializer _dbInitializer;
        private readonly IServiceProvider _services;

        public App(IDbInitializer dbInitializer, IServiceProvider services)
        {
            InitializeComponent();
            _dbInitializer = dbInitializer;
            _services = services;
            _dbInitializer.InitializeAsync().GetAwaiter().GetResult();
#if DEBUG
            _services.GetRequiredService<DemoDataInitializer>()
                .EnsureDemoDataAsync()
                .GetAwaiter()
                .GetResult();
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = _services.GetRequiredService<AppShell>();
            return new Window(shell);
        }

        protected override void OnStart()
        {
            base.OnStart();
            MainActivity.FlushPendingNavigation();
        }
    }
}
