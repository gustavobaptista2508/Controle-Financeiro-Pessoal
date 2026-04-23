using System.Net.Http.Json;
using System.Text.Json;

namespace FinanceiroPessoal.MAUI.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(HttpClient http)
            => _http = http;

        // Dashboard
        public async Task<DashboardResumoDto> ObterResumo(int ano, int mes)
        {
            var response = await _http.GetFromJsonAsync<DashboardResumoDto>(
                $"dashboard/{ano}/{mes}", _jsonOptions);
            return response ?? new DashboardResumoDto();
        }

        public async Task<List<ProximoVencimentoDto>> ObterProximosVencimentos(int ano, int mes)
        {
            var response = await _http.GetFromJsonAsync<List<ProximoVencimentoDto>>(
                $"dashboard/{ano}/{mes}/proximos-vencimentos", _jsonOptions);
            return response ?? new();
        }

        public async Task<List<GastoCategoriaDto>> ObterGastosPorCategoria(int ano, int mes)
        {
            var response = await _http.GetFromJsonAsync<List<GastoCategoriaDto>>(
                $"dashboard/{ano}/{mes}/gastos-categoria", _jsonOptions);
            return response ?? new();
        }

        // Lançamentos
        public async Task<List<LancamentoDto>> ObterLancamentos(
            string pessoa = "", string status = "Todos", string tipo = "Todos",
            DateTime? inicio = null, DateTime? fim = null)
        {
            var dataInicio = (inicio ?? DateTime.Today.AddMonths(-1)).ToString("yyyy-MM-dd");
            var dataFim = (fim ?? DateTime.Today).ToString("yyyy-MM-dd");

            var url = $"lancamentos?pessoa={pessoa}&status={status}&tipo={tipo}" +
                      $"&inicio={dataInicio}&fim={dataFim}";

            var response = await _http.GetFromJsonAsync<List<LancamentoDto>>(url, _jsonOptions);
            return response ?? new();
        }

        public async Task MarcarPago(int id)
            => await _http.PutAsync($"lancamentos/{id}/pagar", null);

        public async Task Excluir(int id)
            => await _http.DeleteAsync($"lancamentos/{id}");

        public async Task<List<CadastroAuxiliarDto>> ObterCategorias()
        {
            var response = await _http.GetFromJsonAsync<List<CadastroAuxiliarDto>>(
                "cadastroauxiliar/categorias", _jsonOptions);
            return response ?? new();
        }

        public async Task<List<CadastroAuxiliarDto>> ObterContas()
        {
            var response = await _http.GetFromJsonAsync<List<CadastroAuxiliarDto>>(
                "cadastroauxiliar/contas", _jsonOptions);
            return response ?? new();
        }

        public async Task<List<CadastroAuxiliarDto>> ObterPessoas()
        {
            var response = await _http.GetFromJsonAsync<List<CadastroAuxiliarDto>>(
                "cadastroauxiliar/pessoas", _jsonOptions);
            return response ?? new();
        }
    }
}