using ClosedXML.Excel;
using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services
{
    public class ExcelImportService
    {
        public string Importar(string caminhoArquivo)
        {
            int totalLidos = 0;
            int totalImportados = 0;
            int totalIgnorados = 0;
            var avisos = new List<string>();

            using var context = new FinanceiroDbContext();
            using var workbook = new XLWorkbook(caminhoArquivo);

            foreach (var worksheet in workbook.Worksheets)
            {
                var rows = worksheet.RangeUsed()?.RowsUsed().ToList();
                if (rows == null || rows.Count <= 1)
                    continue;

                foreach (var row in rows.Skip(1))
                {
                    totalLidos++;

                    try
                    {
                        string descricao = row.Cell(1).GetString().Trim();
                        string valorTexto = row.Cell(2).GetString().Trim();
                        string vencimentoTexto = row.Cell(3).GetString().Trim();
                        string status = row.Cell(4).GetString().Trim();
                        string categoriaNome = row.Cell(5).GetString().Trim();
                        string contaNome = row.Cell(6).GetString().Trim();
                        string pessoaNome = row.Cell(7).GetString().Trim();
                        string observacoes = row.Cell(8).GetString().Trim();
                        string competencia = row.Cell(9).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(descricao))
                        {
                            totalIgnorados++;
                            continue;
                        }

                        if (!TryParseDecimal(valorTexto, out decimal valor))
                        {
                            totalIgnorados++;
                            avisos.Add($"Valor inválido em: {descricao}");
                            continue;
                        }

                        DateTime? vencimento = null;
                        if (DateTime.TryParse(vencimentoTexto, out var data))
                            vencimento = data;

                        var categoria = ObterOuCriarCategoria(context, categoriaNome);
                        var conta = ObterOuCriarConta(context, contaNome);
                        var pessoa = ObterOuCriarPessoa(context, pessoaNome);

                        bool existe = context.Lancamentos.Any(x =>
                            x.Descricao == descricao &&
                            x.Valor == valor &&
                            x.Competencia == competencia);

                        if (existe)
                        {
                            totalIgnorados++;
                            continue;
                        }

                        context.Lancamentos.Add(new Lancamento
                        {
                            Descricao = descricao,
                            Valor = valor,
                            DataVencimento = vencimento,
                            Status = string.IsNullOrWhiteSpace(status) ? "Pendente" : status,
                            CategoriaId = categoria?.Id,
                            ContaId = conta?.Id,
                            PessoaId = pessoa?.Id,
                            Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes,
                            Competencia = string.IsNullOrWhiteSpace(competencia) ? null : competencia
                        });

                        totalImportados++;
                    }
                    catch (Exception ex)
                    {
                        totalIgnorados++;
                        avisos.Add($"Erro na aba {worksheet.Name}, linha {row.RowNumber()}: {ex.Message}");
                    }
                }
            }

            context.SaveChanges();

            return
                $"Importação concluída.\n\n" +
                $"Lidos: {totalLidos}\n" +
                $"Importados: {totalImportados}\n" +
                $"Ignorados: {totalIgnorados}\n\n" +
                (avisos.Count > 0 ? string.Join("\n", avisos.Take(20)) : "Sem avisos.");
        }

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
