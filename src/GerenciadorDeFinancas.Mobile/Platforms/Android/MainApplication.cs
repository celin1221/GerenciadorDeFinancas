using Android.App;
using Android.Runtime;

namespace GerenciadorDeFinancas
{
    [Application]
    public class MainApplication : MauiApplication
    {
        internal static new IServiceProvider? Services { get; private set; }

        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp()
        {
            var app = MauiProgram.CreateMauiApp();
            Services = app.Services;
            return app;
        }
    }
}
