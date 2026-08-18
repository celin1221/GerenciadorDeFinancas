namespace GerenciadorDeFinancas
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(PersonFormPage), typeof(PersonFormPage));
            Routing.RegisterRoute(nameof(CardFormPage), typeof(CardFormPage));
            Routing.RegisterRoute(nameof(SplitPurchasePage), typeof(SplitPurchasePage));
            Routing.RegisterRoute(nameof(NotificationButtonsPage), typeof(NotificationButtonsPage));
            Routing.RegisterRoute(nameof(NotificationButtonFormPage), typeof(NotificationButtonFormPage));
        }
    }
}
