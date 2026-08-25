using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Persistence;
using GerenciadorDeFinancas.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorDeFinancas
{
    public partial class App : global::Microsoft.Maui.Controls.Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new LoadingPage());
        }

        protected override async void OnStart()
        {
            base.OnStart();
            try
            {
                var dbInitializer = _services.GetRequiredService<IDbInitializer>();
                await dbInitializer.InitializeAsync();

                await _services.GetRequiredService<AssignMissingStatementsUseCase>()
                    .ExecuteAsync();

#if DEBUG
                await _services.GetRequiredService<DemoDataInitializer>()
                    .EnsureDemoDataAsync();
#endif

                var shell = _services.GetRequiredService<AppShell>();
                Windows[0].Page = shell;

#if ANDROID
                MainActivity.FlushPendingNavigation();
#endif
            }
            catch (Exception ex)
            {
                Windows[0].Page = new ContentPage
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = 40,
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                Text = "Erro ao inicializar",
                                FontSize = 18,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Label
                            {
                                Text = ex.Message,
                                FontSize = 14,
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }
                };
            }
        }
    }
}
