using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FinanceiroPessoal.Web.Models;

namespace FinanceiroPessoal.Web.Services;

public class AssistenteFinanceiroIaService(
    FinanceiroDbContext db,
    IOptions<OpenAIOptions> options,
    ILogger<AssistenteFinanceiroIaService> logger,
    HttpClient httpClient,
    IAssinaturaService assinaturaService) : IAssistenteFinanceiroIaService
{
    private const string MsgNaoConfigurada = "IA ainda não configurada.";
    private const string MsgFalhaIa = "Não foi possível consultar a IA no momento.";

    public async Task<bool> PodeUsarIaHojeAsync(int usuarioId)
    {
        var qtdHoje = await db.IaConversas.IgnoreQueryFilters().CountAsync(x => x.UsuarioId == usuarioId && x.DataCriacao.Date == DateTime.Today);
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId);
        var limite = usuario?.AssinaturaStatus == "ATIVA" ? 100 : 20;
        return qtdHoje < limite;
    }

    public async Task<string> GerarResumoMensalAsync(int usuarioId, int mes, int ano) => await PerguntarInternoAsync(usuarioId, "Faça um resumo do meu mês.", mes, ano);
    public async Task<string> AnalisarCategoriasAsync(int usuarioId, int mes, int ano) => await PerguntarInternoAsync(usuarioId, "Quais categorias mais pesaram?", mes, ano);
    public async Task<string> PerguntarAsync(int usuarioId, string pergunta) => await PerguntarInternoAsync(usuarioId, pergunta, null, null);

    public async Task<IReadOnlyList<IaConversa>> ListarUltimasConversasAsync(int usuarioId, int limite = 10)
        => await db.IaConversas.IgnoreQueryFilters().Where(x => x.UsuarioId == usuarioId).OrderByDescending(x => x.Id).Take(limite).ToListAsync();

    private async Task<string> PerguntarInternoAsync(int usuarioId, string pergunta, int? mes, int? ano)
    {
        if (pergunta.Contains("alterar", StringComparison.OrdinalIgnoreCase) || pergunta.Contains("excluir", StringComparison.OrdinalIgnoreCase))
            return "Nesta versão eu apenas analiso e explico. Para alterar dados, use a tela de lançamentos.";

        if (!await assinaturaService.UsuarioTemAcessoAsync(usuarioId)) return "Seu plano atual não permite uso da IA.";
        if (!await PodeUsarIaHojeAsync(usuarioId)) return "Você atingiu o limite diário de uso da IA.";

        var cfg = options.Value;
        if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.ApiKey)) return MsgNaoConfigurada;

        var lancamentos = await CarregarLancamentos(usuarioId, mes, ano);
        var resumo = MontarResumo(usuarioId, lancamentos, mes, ano);
        var resposta = await ConsultarOpenAiAsync(cfg, resumo + "\n\nPergunta do usuário: " + pergunta);

        db.IaConversas.Add(new IaConversa { UsuarioId = usuarioId, Pergunta = pergunta, Resposta = resposta, DataCriacao = DateTime.Now, TokensEstimados = (resumo.Length + resposta.Length) / 4 });
        await db.SaveChangesAsync();
        return resposta;
    }

    private async Task<List<Lancamento>> CarregarLancamentos(int usuarioId, int? mes, int? ano)
    {
        var query = db.Lancamentos.IgnoreQueryFilters().Include(x => x.Categoria).Include(x => x.Conta).Include(x => x.Pessoa).Where(x => x.UsuarioId == usuarioId);
        if (mes.HasValue && ano.HasValue)
        {
            query = query.Where(x => x.DataVencimento.HasValue && x.DataVencimento.Value.Month == mes.Value && x.DataVencimento.Value.Year == ano.Value);
        }
        else
        {
            var inicio = DateTime.Today.AddDays(-90);
            query = query.Where(x => (x.DataVencimento ?? x.DataPagamento ?? DateTime.MinValue) >= inicio);
        }

        return await query.OrderByDescending(x => x.DataVencimento ?? x.DataPagamento).Take(300).ToListAsync();
    }

    private static string MontarResumo(int usuarioId, List<Lancamento> lancamentos, int? mes, int? ano)
    {
        var cultura = new CultureInfo("pt-BR");
        var entradasPagas = lancamentos.Where(x => x.Tipo == TipoLancamento.Entrada && x.Status.Equals("Pago", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Valor);
        var saidasPagas = lancamentos.Where(x => x.Tipo == TipoLancamento.Saida && x.Status.Equals("Pago", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Valor);
        var pendentes = lancamentos.Where(x => !x.Status.Equals("Pago", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Valor);
        var saldoPrev = entradasPagas - saidasPagas;

        var sb = new StringBuilder();
        sb.AppendLine($"UsuárioId: {usuarioId}");
        sb.AppendLine($"Período: {(mes.HasValue && ano.HasValue ? $"{mes:00}/{ano}" : "últimos 90 dias")}");
        sb.AppendLine("Resumo:");
        sb.AppendLine($"- Entradas pagas: {entradasPagas.ToString("C", cultura)}");
        sb.AppendLine($"- Saídas pagas: {saidasPagas.ToString("C", cultura)}");
        sb.AppendLine($"- Pendentes: {pendentes.ToString("C", cultura)}");
        sb.AppendLine($"- Saldo previsto: {saldoPrev.ToString("C", cultura)}");
        sb.AppendLine("Categorias:");
        foreach (var c in lancamentos.Where(x => x.Tipo == TipoLancamento.Saida).GroupBy(x => x.Categoria?.Nome ?? "Sem categoria").Select(g => new { g.Key, V = g.Sum(x => x.Valor) }).OrderByDescending(x => x.V).Take(10)) sb.AppendLine($"- {c.Key}: {c.V.ToString("C", cultura)}");
        sb.AppendLine("Lançamentos relevantes:");
        int i=1; foreach (var l in lancamentos.Take(25)) sb.AppendLine($"{i++}. {(l.DataVencimento ?? l.DataPagamento ?? DateTime.Today):dd/MM/yyyy} - {l.Descricao} - {l.Tipo} - {l.Valor.ToString("C", cultura)} - {l.Status}");
        return sb.ToString();
    }

    private async Task<string> ConsultarOpenAiAsync(OpenAIOptions cfg, string promptUsuario)
    {
        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
            var body = new
            {
                model = string.IsNullOrWhiteSpace(cfg.Model) ? "gpt-5.5-mini" : cfg.Model,
                input = new object[] {
                    new { role = "system", content = "Você é o assistente financeiro do GranaOK. Analise os dados financeiros do usuário com clareza e responsabilidade. Explique receitas, despesas, saldo, categorias, contas e vencimentos. Não invente dados. Se não houver informação suficiente, diga que não há dados suficientes. Não dê aconselhamento financeiro profissional, jurídico ou contábil. Não prometa ganhos. Não altere dados do sistema. Responda em português do Brasil, de forma objetiva e prática." },
                    new { role = "user", content = promptUsuario }
                }
            };
            var response = await httpClient.PostAsync("https://api.openai.com/v1/responses", new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) return MsgFalhaIa;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("output_text", out var t)) return t.GetString() ?? MsgFalhaIa;
            return MsgFalhaIa;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao consultar OpenAI");
            return MsgFalhaIa;
        }
    }
}
