using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FinanceiroPessoal.Core.Services
{
    public class ExcelImportService
    {
        private static bool TryParseDecimal(string texto, out decimal valor)
        {
            texto = texto.Replace("R$", "").Trim();

            return decimal.TryParse(
                texto,
                NumberStyles.Any,
                new CultureInfo("pt-BR"),
                out valor);
        }

        private static Categoria? ObterOuCriarCategoria(FinanceiroDbContext context, string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return null;

            var categoria = context.Categorias.FirstOrDefault(x => x.Nome == nome);
            if (categoria != null)
                return categoria;

            categoria = new Categoria { Nome = nome };
            context.Categorias.Add(categoria);
            context.SaveChanges();
            return categoria;
        }

        private static Conta? ObterOuCriarConta(FinanceiroDbContext context, string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return null;

            var conta = context.Contas.FirstOrDefault(x => x.Nome == nome);
            if (conta != null)
                return conta;

            conta = new Conta { Nome = nome, Tipo = "Outro" };
            context.Contas.Add(conta);
            context.SaveChanges();
            return conta;
        }

        private static Pessoa? ObterOuCriarPessoa(FinanceiroDbContext context, string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return null;

            var pessoa = context.Pessoas.FirstOrDefault(x => x.Nome == nome);
            if (pessoa != null)
                return pessoa;

            pessoa = new Pessoa { Nome = nome };
            context.Pessoas.Add(pessoa);
            context.SaveChanges();
            return pessoa;
        }
    }
}
