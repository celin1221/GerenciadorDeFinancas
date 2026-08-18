using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Application.UseCases;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Infrastructure.Notifications;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;
using GerenciadorDeFinancas.Persistence;
using GerenciadorDeFinancas.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GerenciadorDeFinancas
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var services = builder.Services;

            services.AddSingleton<IDbPathProvider, DbPathProvider>();
            services.AddSingleton<IDbInitializer, DbInitializer>();

            services.AddDbContextFactory<FinanceDbContext>((provider, options) =>
            {
                var pathProvider = provider.GetRequiredService<IDbPathProvider>();
                options.UseSqlite($"Data Source={pathProvider.DatabasePath}");
            });

            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

            services.AddTransient<GetDashboardSummaryUseCase>();
            services.AddTransient<ImportNotificationUseCase>();
            services.AddTransient<ClassifyPurchaseUseCase>();
            services.AddTransient<SplitPurchaseUseCase>();
            services.AddTransient<CreatePersonUseCase>();
            services.AddTransient<UpdatePersonUseCase>();
            services.AddTransient<SetPersonActiveUseCase>();
            services.AddTransient<ListPersonsUseCase>();
            services.AddTransient<CreateCardUseCase>();
            services.AddTransient<UpdateCardUseCase>();
            services.AddTransient<SetCardActiveUseCase>();
            services.AddTransient<ListCardsUseCase>();
            services.AddTransient<ListPendingPurchasesUseCase>();
            services.AddTransient<GetPendingPurchaseUseCase>();

            services.AddTransient<AppShell>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<PendingPurchasesPage>();
            services.AddTransient<SplitPurchasePage>();
            services.AddTransient<PeoplePage>();
            services.AddTransient<PersonFormPage>();
            services.AddTransient<CardsPage>();
            services.AddTransient<CardFormPage>();
            services.AddTransient<NotificationButtonsPage>();
            services.AddTransient<NotificationButtonFormPage>();

#if DEBUG
            services.AddTransient<DemoDataInitializer>();
#endif

            services.AddSingleton<IClassificationPrompter, NotificationClassificationPrompter>();

            services.AddSingleton<INotificationParser, NubankNotificationParser>();
            services.AddSingleton<INotificationParser, MercadoPagoNotificationParser>();
            services.AddSingleton<INotificationParser, InterNotificationParser>();
            services.AddSingleton<INotificationParser, BancoDoBrasilNotificationParser>();
            services.AddSingleton<INotificationParser, GenericNotificationParser>();
            services.AddSingleton<INotificationParserRegistry, NotificationParserRegistry>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
