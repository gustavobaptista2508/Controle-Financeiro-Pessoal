// Api/Controllers/CadastroAuxiliarController.cs
using FinanceiroPessoal.WinForms.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceiroPessoal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CadastroAuxiliarController : ControllerBase
    {
        private readonly CadastroAuxiliarService _service;

        public CadastroAuxiliarController(CadastroAuxiliarService service)
            => _service = service;

        [HttpGet("categorias")]
        public async Task<IActionResult> ObterCategorias()
            => Ok(await _service.ObterCategorias());

        [HttpGet("contas")]
        public async Task<IActionResult> ObterContas()
            => Ok(await _service.ObterContas());

        [HttpGet("pessoas")]
        public async Task<IActionResult> ObterPessoas()
            => Ok(await _service.ObterPessoas());
    }
}