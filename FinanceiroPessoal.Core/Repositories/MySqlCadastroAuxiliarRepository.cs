using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Core.Repositories
{
    public class MySqlCadastroAuxiliarRepository : ICadastroAuxiliarRepository
    {
        public readonly MySqlDbContext _context;

        public MySqlCadastroAuxiliarRepository() : this(new MySqlDbContext()) { }
        public MySqlCadastroAuxiliarRepository(MySqlDbContext context) { _context = context; }

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
