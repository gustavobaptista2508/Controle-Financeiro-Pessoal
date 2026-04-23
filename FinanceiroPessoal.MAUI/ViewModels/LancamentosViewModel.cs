// ViewModels/LancamentosViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceiroPessoal.MAUI.Services;
using FinanceiroPessoal.MAUI.Views;

namespace FinanceiroPessoal.MAUI.ViewModels
{
    public partial class LancamentosViewModel : ObservableObject
    {
        private readonly ApiService _api;

        [ObservableProperty] private List<LancamentoDto> lancamentos = new();
        [ObservableProperty] private List<object> listaAgrupada = new();
        [ObservableProperty] private bool carregando = false;
        [ObservableProperty] private string filtroStatus = "Todos";
        [ObservableProperty] private string filtroTipo = "Todos";
        [ObservableProperty] private DateTime dataInicio = DateTime.Today.AddMonths(-1);
        [ObservableProperty] private DateTime dataFim = DateTime.Today;

        public LancamentosViewModel(ApiService api)
            => _api = api;

        [RelayCommand]
        public async Task Carregar()
        {
            Carregando = true;
            try
            {
                var lista = await _api.ObterLancamentos(
                    status: FiltroStatus, tipo: FiltroTipo,
                    inicio: DataInicio, fim: DataFim);

                // Separa cartões e agrupa
                var normais = lista.Where(x => !x.IsCartao).ToList();
                var faturas = lista
                    .Where(x => x.IsCartao)
                    .GroupBy(x => new { x.ContaId, x.Conta })
                    .Select(g => new LancamentoDto
                    {
                        Id = -(g.Key.ContaId ?? 0),
                        Descricao = $"Fatura {g.Key.Conta}  •  {g.Count()} lançamento(s)",
                        Valor = g.Sum(x => x.Valor),
                        Tipo = "Saída",
                        Status = g.All(x => x.Status == "Pago") ? "Pago" :
                                    g.Any(x => x.Status == "Pago") ? "Parcial" :
                                    g.Any(x => x.Status == "Atrasado") ? "Atrasado" : "Pendente",
                        ContaId = g.Key.ContaId,
                        Conta = g.Key.Conta,
                        ContaTipo = "Cartão",
                        DataVencimento = g.Max(x => x.DataVencimento),
                    })
                    .ToList();

                Lancamentos = normais.Concat(faturas)
                    .OrderBy(x => x.DataVencimento)
                    .ToList();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally { Carregando = false; }
        }

        [RelayCommand]
        private async Task AbrirExtrato(LancamentoDto item)
        {
            if (!item.IsCartao || item.ContaId == null) return;

            await Shell.Current.GoToAsync(
                $"{nameof(ExtratoCartaoPage)}" +
                $"?contaId={item.ContaId}" +
                $"&nomeCartao={item.Conta}" +
                $"&ano={DateTime.Today.Year}" +
                $"&mes={DateTime.Today.Month}");
        }

        [RelayCommand]
        private async Task MarcarPago(LancamentoDto item)
        {
            if (item.Id <= 0) return;
            await _api.MarcarPago(item.Id);
            await Carregar();
        }
    }
}