using FinanceiroPessoal.WinForms.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Repositories
{
    public interface ICadastroAuxiliarRepository
    {
        Task<List<Pessoa>> ObterPessoas();
        Task<List<Categoria>> ObterCategorias();
        Task<List<Conta>> ObterContas();
    }
}
