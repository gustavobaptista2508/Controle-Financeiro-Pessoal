using FinanceiroPessoal.WinForms.Models;
using FinanceiroPessoal.WinForms.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceiroPessoal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LancamentosController : ControllerBase
    {
        private readonly LancamentoService _service;

        public LancamentosController(LancamentoService service)
            => _service = service;

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
        public async Task<IActionResult> Criar([FromBody] Lancamento lancamento)
        {
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
}