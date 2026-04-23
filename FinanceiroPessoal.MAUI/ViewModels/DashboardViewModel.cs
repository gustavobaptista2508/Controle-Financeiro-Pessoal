// ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceiroPessoal.MAUI.Services;

namespace FinanceiroPessoal.MAUI.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ApiService _api;

        [ObservableProperty] private DashboardResumoDto resumo = new();
        [ObservableProperty] private List<ProximoVencimentoDto> proximosVencimentos = new();
        [ObservableProperty] private List<GastoCategoriaDto> gastosCategorias = new();
        [ObservableProperty] private bool carregando = false;
        [ObservableProperty] private DateTime referencia = DateTime.Today;

        public string ReferenciaFormatada => Referencia.ToString("MMMM/yyyy");
        public string SaldoColor => Resumo.SaldoMes >= 0 ? "#15803D" : "#DC2626";

        public DashboardViewModel(ApiService api)
            => _api = api;

        [RelayCommand]
        public async Task Carregar()
        {
            Carregando = true;
            try
            {
                Resumo = await _api.ObterResumo(Referencia.Year, Referencia.Month);
                ProximosVencimentos = await _api.ObterProximosVencimentos(Referencia.Year, Referencia.Month);
                GastosCategorias = await _api.ObterGastosPorCategoria(Referencia.Year, Referencia.Month);
                OnPropertyChanged(nameof(SaldoColor));
                OnPropertyChanged(nameof(ReferenciaFormatada));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                Carregando = false;
            }
        }

        [RelayCommand]
        private void MesAnterior()
        {
            Referencia = Referencia.AddMonths(-1);
            _ = Carregar();
        }

        [RelayCommand]
        private void ProximoMes()
        {
            Referencia = Referencia.AddMonths(1);
            _ = Carregar();
        }
    }
}