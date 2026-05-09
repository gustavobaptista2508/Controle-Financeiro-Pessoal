using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Web.Services;

public class InMemoryLancamentoRepository(WebAuthSessionService session) : ILancamentoRepository
{
    private TenantData Current => MultiTenantDataStore.GetOrCreate(session.CurrentUserEmail ?? "guest@local");
    private List<Lancamento> Dados => Current.Lancamentos;
    public Task<List<Lancamento>> ObterTodos() => Task.FromResult(Dados.OrderBy(x => x.DataVencimento).ThenBy(x => x.Descricao).ToList());
    public Task<Lancamento?> ObterPorId(int id) => Task.FromResult(Dados.FirstOrDefault(x => x.Id == id));
    public Task Adicionar(Lancamento l){ if(l.Id<=0) l.Id=Current.NextLancamentoId++; Dados.Add(l); return Task.CompletedTask; }
    public Task Atualizar(Lancamento l){ var i=Dados.FindIndex(x=>x.Id==l.Id); if(i>=0) Dados[i]=l; return Task.CompletedTask; }
    public Task Excluir(int id){ Dados.RemoveAll(x=>x.Id==id); return Task.CompletedTask; }
    public Task MarcarComoPago(int id, DateTime? dataPagamento = null){ var l=Dados.FirstOrDefault(x=>x.Id==id); if(l is not null){ l.Status="Pago"; l.DataPagamento=dataPagamento??DateTime.Now;} return Task.CompletedTask; }
    public Task<List<Lancamento>> Filtrar(string pessoa,string status,string tipo,DateTime di,DateTime df)=>Task.FromResult(Dados.ToList());
    public Task<decimal> CalcularSaldoConta(string p,string s,string t,DateTime? di,DateTime? df)=>Task.FromResult(Dados.Where(x=>x.Tipo==TipoLancamento.Entrada).Sum(x=>x.Valor)-Dados.Where(x=>x.Tipo==TipoLancamento.Saida&&x.Status=="Pago").Sum(x=>x.Valor));
    public Task<decimal> ObterTotalPendenteSaidas()=>Task.FromResult(Dados.Where(x=>x.Tipo==TipoLancamento.Saida&&x.Status=="Pendente").Sum(x=>x.Valor));
    public Task<decimal> ObterTotalPagoSaidas()=>Task.FromResult(Dados.Where(x=>x.Tipo==TipoLancamento.Saida&&x.Status=="Pago").Sum(x=>x.Valor));
    public Task<List<Lancamento>> ObterLancamentosPorPeriodoAsync(DateTime i,DateTime f)=>Task.FromResult(Dados.ToList());
    public Task<List<Lancamento>> ObterVencimentosSemanaAsync(DateTime i,DateTime f)=>Task.FromResult(Dados.ToList());
    public Task<List<Lancamento>> ObterAtrasadosAsync()=>Task.FromResult(Dados.Where(x=>x.DataVencimento<DateTime.Today&&x.Status!="Pago").ToList());
    public Task<List<Lancamento>> ObterVencemHojeAsync(DateTime d)=>Task.FromResult(Dados.Where(x=>x.DataVencimento?.Date==d.Date).ToList());
    public Task<List<ProximoVencimentoDto>> ObterProximosVencimentosAsync(int q)=>Task.FromResult(Dados.Where(x=>x.DataVencimento.HasValue&&x.Tipo==TipoLancamento.Saida&&x.Status!="Pago").OrderBy(x=>x.DataVencimento).Take(q).Select(x=>new ProximoVencimentoDto{Id=x.Id,Vencimento=x.DataVencimento,Descricao=x.Descricao,Valor=x.Valor,Conta=x.Conta?.Nome??"",Pessoa=x.Pessoa?.Nome??"",Status=x.Status,Tipo=x.Tipo==TipoLancamento.Entrada?"Entrada":"Saída"}).ToList());
    public Task<List<Lancamento>> ObterPagosPorPeriodoAsync(DateTime i,DateTime f)=>Task.FromResult(Dados.Where(x=>x.Status=="Pago").ToList());
}
