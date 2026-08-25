namespace GerenciadorDeFinancas
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(PersonFormPage), typeof(PersonFormPage));
            Routing.RegisterRoute(nameof(PersonDetailPage), typeof(PersonDetailPage));
            Routing.RegisterRoute(nameof(CardFormPage), typeof(CardFormPage));
            Routing.RegisterRoute(nameof(SplitPurchasePage), typeof(SplitPurchasePage));
            Routing.RegisterRoute(nameof(NotificationButtonsPage), typeof(NotificationButtonsPage));
            Routing.RegisterRoute(nameof(NotificationButtonFormPage), typeof(NotificationButtonFormPage));
            Routing.RegisterRoute(nameof(ManualPurchaseFormPage), typeof(ManualPurchaseFormPage));
            Routing.RegisterRoute(nameof(InvoiceDetailPage), typeof(InvoiceDetailPage));
            Routing.RegisterRoute(nameof(EditPurchaseFormPage), typeof(EditPurchaseFormPage));
            Routing.RegisterRoute(nameof(AllPurchasesPage), typeof(AllPurchasesPage));
        }
    }
}
