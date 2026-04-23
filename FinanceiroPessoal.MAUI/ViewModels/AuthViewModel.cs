// ViewModels/AuthViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceiroPessoal.MAUI.Services;
using FinanceiroPessoal.MAUI.Views;

namespace FinanceiroPessoal.MAUI.ViewModels
{
    public partial class AuthViewModel : ObservableObject
    {
        private readonly AuthMauiService _authService;

        [ObservableProperty] private string codigo = string.Empty;
        [ObservableProperty] private string mensagemErro = string.Empty;
        [ObservableProperty] private bool erroVisivel = false;
        [ObservableProperty] private bool qrCodeVisivel = false;
        [ObservableProperty] private string qrCodeUri = string.Empty;
        [ObservableProperty] private string instrucao = string.Empty;
        //[ObservableProperty] private int segundosRestantes = 30;
        [ObservableProperty] private int _segundosRestantes = 30;

        public AuthViewModel(AuthMauiService authService)
        {
            _authService = authService;
            InicializarTimer();
            VerificarPrimeiroAcesso();
        }

        private void VerificarPrimeiroAcesso()
        {
            if (!_authService.ChaveExiste())
            {
                var base32 = _authService.GerarNovaChave();
                QrCodeUri = _authService.GerarUriQrCode(base32);
                QrCodeVisivel = true;
                Instrucao = "Primeiro acesso! Escaneie o QR Code com o Google Authenticator.";
            }
            else
            {
                QrCodeVisivel = false;
                Instrucao = "Digite o código de 6 dígitos do seu autenticador.";
            }
        }

        private void InicializarTimer()
        {
            var t = new System.Timers.Timer(1000);
            t.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SegundosRestantes = 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
                });
            };
            t.Start();
        }

        [RelayCommand]
        private async Task Entrar()
        {
            if (string.IsNullOrWhiteSpace(Codigo) || Codigo.Length != 6)
            {
                MensagemErro = "Digite um código válido de 6 dígitos.";
                ErroVisivel = true;
                return;
            }

            if (_authService.VerificarCodigo(Codigo))
            {
                await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
            }
            else
            {
                MensagemErro = "Código inválido ou expirado. Tente novamente.";
                ErroVisivel = true;
                Codigo = string.Empty;
            }
        }
    }
}