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
        private readonly FinanceiroDbContext _context;

        public MySqlCadastroAuxiliarRepository(FinanceiroDbContext context)
        {
            _context = context;
        }

        public async Task<List<Categoria>> ObterCategorias()
        {
            var usuarioId = Services.SessaoUsuario.UsuarioId;
            return await _context.Categorias
                .Where(x => x.UsuarioId == usuarioId)
                .OrderBy(x => x.Nome)
                .ToListAsync();
        }

        public async Task<List<Conta>> ObterContas()
        {
            var usuarioId = Services.SessaoUsuario.UsuarioId;
            return await _context.Contas
                .Where(x => x.UsuarioId == usuarioId)
                .OrderBy(x => x.Nome)
                .ToListAsync();
        }

        public async Task<List<Pessoa>> ObterPessoas()
        {
            var usuarioId = Services.SessaoUsuario.UsuarioId;
            return await _context.Pessoas
                .Where(x => x.UsuarioId == usuarioId)
                .OrderBy(x => x.Nome)
                .ToListAsync();
        }


        public async Task<Categoria> AdicionarCategoriaAsync(Categoria categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return categoria;
        }

        public async Task<Pessoa> AdicionarPessoaAsync(Pessoa pessoa)
        {
            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();
            return pessoa;
        }

        public async Task<Conta> AdicionarContaAsync(Conta conta)
        {
            _context.Contas.Add(conta);
            await _context.SaveChangesAsync();
            return conta;
        }
    }
}
