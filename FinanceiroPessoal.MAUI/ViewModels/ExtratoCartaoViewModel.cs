// ViewModels/ExtratoCartaoViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceiroPessoal.MAUI.Services;

namespace FinanceiroPessoal.MAUI.ViewModels
{
    [QueryProperty(nameof(ContaId), "contaId")]
    [QueryProperty(nameof(NomeCartao), "nomeCartao")]
    [QueryProperty(nameof(Ano), "ano")]
    [QueryProperty(nameof(Mes), "mes")]
    public partial class ExtratoCartaoViewModel : ObservableObject
    {
        private readonly ApiService _api;

        [ObservableProperty] private int contaId;
        [ObservableProperty] private string nomeCartao = string.Empty;
        [ObservableProperty] private int ano;
        [ObservableProperty] private int mes;
        [ObservableProperty] private List<LancamentoDto> lancamentos = new();
        [ObservableProperty] private decimal totalFatura;
        [ObservableProperty] private decimal totalPago;
        [ObservableProperty] private decimal totalPendente;
        [ObservableProperty] private bool carregando = false;

        public ExtratoCartaoViewModel(ApiService api)
            => _api = api;

        [RelayCommand]
        public async Task Carregar()
        {
            Carregando = true;
            try
            {
                var inicio = new DateTime(Ano, Mes, 1);
                var fim = inicio.AddMonths(1).AddDays(-1);

                var lista = await _api.ObterLancamentos(
                    status: "Todos", tipo: "Saída",
                    inicio: inicio, fim: fim);

                Lancamentos = lista.Where(x => x.ContaId == ContaId).ToList();
                TotalFatura = Lancamentos.Sum(x => x.Valor);
                TotalPago = Lancamentos.Where(x => x.Status == "Pago").Sum(x => x.Valor);
                TotalPendente = Lancamentos.Where(x => x.Status != "Pago").Sum(x => x.Valor);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally { Carregando = false; }
        }

        [RelayCommand]
        private async Task MarcarFaturaPaga()
        {
            var confirmar = await Shell.Current.DisplayAlert(
                "Confirmar", $"Marcar toda a fatura de {NomeCartao} como paga?", "Sim", "Não");

            if (!confirmar) return;

            var pendentes = Lancamentos.Where(x => x.Status != "Pago" && x.Id > 0).ToList();
            foreach (var l in pendentes)
                await _api.MarcarPago(l.Id);

            await Carregar();
            await Shell.Current.DisplayAlert("Sucesso", "Fatura marcada como paga!", "OK");
        }
    }
}