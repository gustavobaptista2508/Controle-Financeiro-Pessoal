using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms.Repositories
{
    public class SqliteCadastroAuxiliarRepository : ICadastroAuxiliarRepository
    {
        private readonly SqliteDbContext _context = new();

        public async Task<List<Categoria>> ObterCategorias()
        {
            return await _context.Categorias
                .OrderBy(x => x.Nome)
                .ToListAsync();
        }

        public async Task<List<Conta>> ObterContas()
        {
            return await _context.Contas
                .OrderBy(x => x.Nome)
                .ToListAsync();
        }

        public async Task<List<Pessoa>> ObterPessoas()
        {
            return await _context.Pessoas
                .OrderBy(x => x.Nome)
                .ToListAsync();
        }
    }
}
