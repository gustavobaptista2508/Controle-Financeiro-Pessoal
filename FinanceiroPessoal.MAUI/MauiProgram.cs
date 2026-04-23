using Android.Net;
using CommunityToolkit.Maui;
using FinanceiroPessoal.MAUI.Services;
using FinanceiroPessoal.MAUI.ViewModels;
using FinanceiroPessoal.MAUI.Views;

namespace FinanceiroPessoal.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
                });

            // HTTP Client
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                // troque pelo IP da sua máquina quando testar no celular
                client.BaseAddress = new System.Uri("http://10.0.2.2:5000/api/"); // emulador
                // client.BaseAddress = new Uri("http://SEU_IP:5000/api/"); // celular físico
            });

            // Services
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AuthMauiService>();

            // ViewModels
            builder.Services.AddTransient<AuthViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<LancamentosViewModel>();
            builder.Services.AddTransient<ExtratoCartaoViewModel>();

            // Pages
            builder.Services.AddTransient<AuthPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<LancamentosPage>();
            builder.Services.AddTransient<ExtratoCartaoPage>();

            return builder.Build();
        }
    }
}