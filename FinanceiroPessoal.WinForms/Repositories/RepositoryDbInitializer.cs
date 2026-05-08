using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using System.Security.Cryptography;
using System.Text;

namespace FinanceiroPessoal.WinForms.Repositories
{
    public class RepositoryDbInitializer
    {
        public static void Initialize(FinanceiroDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Pessoas.Any())
            {
                context.Pessoas.AddRange(
                    new Pessoa { Nome = "Gustavo" },
                    new Pessoa { Nome = "Wanessa" }
                );
            }

            if (!context.Categorias.Any())
            {
                context.Categorias.AddRange(
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

            if (!context.Contas.Any())
            {
                context.Contas.AddRange(
                    new Conta { Nome = "Conta Principal", Tipo = "Conta Corrente" },
                    new Conta { Nome = "Sicredi", Tipo = "Cartão" },
                    new Conta { Nome = "Neon", Tipo = "Cartão" },
                    new Conta { Nome = "Mercado Pago", Tipo = "Cartão" }
                );
            }

            if (!context.Usuarios.Any())
            {
                context.Usuarios.Add(new Usuario
                {
                    Nome = "Administrador",
                    Email = "admin@financeiro.local",
                    SenhaHash = GerarSha256("123456"),
                    Ativo = true,
                    EmailConfirmado = true,
                    DataCriacao = DateTime.Now,
                    DataAtualizacao = DateTime.Now
                });
            }

            context.SaveChanges();
        }

        private static string GerarSha256(string senha)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(senha));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
