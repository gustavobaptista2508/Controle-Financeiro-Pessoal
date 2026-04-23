using FinanceiroPessoal.WinForms.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceiroPessoal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _service;

        public DashboardController(DashboardService service)
            => _service = service;

        [HttpGet("{ano}/{mes}")]
        public async Task<IActionResult> ObterResumo(int ano, int mes)
        {
            var referencia = new DateTime(ano, mes, 1);
            var resumo = await _service.ObterResumo(referencia);
            return Ok(resumo);
        }

        [HttpGet("{ano}/{mes}/proximos-vencimentos")]
        public async Task<IActionResult> ObterProximosVencimentos(int ano, int mes, int quantidade = 8)
        {
            var referencia = new DateTime(ano, mes, 1);
            var lista = await _service.ObterProximosVencimentos(quantidade);
            return Ok(lista);
        }

        [HttpGet("{ano}/{mes}/gastos-categoria")]
        public async Task<IActionResult> ObterGastosPorCategoria(int ano, int mes)
        {
            var referencia = new DateTime(ano, mes, 1);
            var lista = await _service.ObterGastosPorCategoria(referencia);
            return Ok(lista);
        }
    }
}