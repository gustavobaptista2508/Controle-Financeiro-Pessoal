using FinanceiroPessoal.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Data
{
    public class DbInitializer
    {
        private readonly FinanceiroDbContext _db;

        public DbInitializer(FinanceiroDbContext db)
        {
            _db = db;
        }

        public async Task InicializarAsync()
        {
            await _db.Database.EnsureCreatedAsync();

            if (!_db.Pessoas.Any())
            {
                _db.Pessoas.AddRange(
                    new Pessoa { Nome = "Gustavo" },
                    new Pessoa { Nome = "Wanessa" }
                );
            }

            if (!_db.Categorias.Any())
            {
                _db.Categorias.AddRange(
                    new Categoria { Nome = "Salário" },
                new Categoria { Nome = "Comissão" },
                new Categoria { Nome = "Recebimento" },
                new Categoria { Nome = "Moradia" },
                new Categoria { Nome = "Cartão" },
                new Categoria { Nome = "Financiamento" },
                new Categoria { Nome = "Alimentação" },
                new Categoria { Nome = "Saúde" },
                new Categoria { Nome = "Transporte" },
                new Categoria { Nome = "Outros" },
                new Categoria { Nome = "Lazer" },
                new Categoria { Nome = "Educação" },
                new Categoria { Nome = "Impostos" }
                );
            }

            if (!_db.Contas.Any())
            {
                _db.Contas.AddRange(
                    new Conta { Nome = "Conta Principal", Tipo = "Conta Corrente" },
                    new Conta { Nome = "Sicredi", Tipo = "Cartão" },
                    new Conta { Nome = "Neon", Tipo = "Cartão" },
                    new Conta { Nome = "Mercado Pago", Tipo = "Cartão" }
                );
            }

            await _db.SaveChangesAsync();
        }
    }
}
