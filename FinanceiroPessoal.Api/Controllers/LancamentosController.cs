using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using FinanceiroPessoal.WinForms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace FinanceiroPessoal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LancamentosController : ControllerBase
    {
        private readonly LancamentoService _service;
        private readonly MySqlDbContext _context;

        public LancamentosController(LancamentoService service, MySqlDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Filtrar(
            [FromQuery] string pessoa = "",
            [FromQuery] string status = "Todos",
            [FromQuery] string tipo = "Todos",
            [FromQuery] DateTime? inicio = null,
            [FromQuery] DateTime? fim = null)
        {
            var dataInicio = inicio ?? DateTime.Today.AddMonths(-1);
            var dataFim = fim ?? DateTime.Today;

            var lista = await _service.Filtrar(pessoa, status, tipo, dataInicio, dataFim);

            return Ok(lista.Select(x => new
            {
                x.Id,
                x.Descricao,
                x.Valor,
                Tipo = x.Tipo == TipoLancamento.Entrada ? "Entrada" : "Saída",
                x.Status,
                x.DataVencimento,
                x.DataPagamento,
                Categoria = x.Categoria?.Nome,
                Conta = x.Conta?.Nome,
                ContaTipo = x.Conta?.Tipo,
                ContaId = x.ContaId,
                Pessoa = x.Pessoa?.Nome,
                x.Competencia,
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] NovoLancamentoRequest request)
        {
            var lancamento = new Lancamento
            {
                Descricao = request.Descricao,
                Valor = request.Valor,
                Tipo = request.Tipo,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Pendente" : request.Status,
                DataVencimento = request.DataVencimento,
                DataPagamento = request.DataPagamento,
                Observacoes = request.Observacoes,
                Competencia = request.Competencia,
                CategoriaId = request.CategoriaId,
                ContaId = request.ContaId,
                PessoaId = request.PessoaId
            };

            if (!lancamento.CategoriaId.HasValue && !string.IsNullOrWhiteSpace(request.Categoria))
                lancamento.CategoriaId = await _context.Categorias
                    .Where(x => x.Nome == request.Categoria)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync();

            if (!lancamento.ContaId.HasValue && !string.IsNullOrWhiteSpace(request.Conta))
                lancamento.ContaId = await _context.Contas
                    .Where(x => x.Nome == request.Conta)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync();

            if (!lancamento.PessoaId.HasValue && !string.IsNullOrWhiteSpace(request.Pessoa))
                lancamento.PessoaId = await _context.Pessoas
                    .Where(x => x.Nome == request.Pessoa)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync();

            await _service.Adicionar(lancamento);
            return Ok(new { lancamento.Id });
        }

        [HttpPut("{id}/pagar")]
        public async Task<IActionResult> MarcarPago(int id)
        {
            await _service.MarcarComoPago(id);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            await _service.Excluir(id);
            return Ok();
        }
    }

    public class NovoLancamentoRequest
    {
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime? DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = "Pendente";
        public string? Observacoes { get; set; }
        public string? Competencia { get; set; }
        public TipoLancamento Tipo { get; set; } = TipoLancamento.Saida;
        public int? CategoriaId { get; set; }
        public int? ContaId { get; set; }
        public int? PessoaId { get; set; }
        public string? Categoria { get; set; }
        public string? Conta { get; set; }
        public string? Pessoa { get; set; }
    }
}
