using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceiroPessoal.Shared.DTOs
{
    public class LancamentoDto
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string? Categoria { get; set; }
        public string? Conta { get; set; }
        public string? Pessoa { get; set; }
        public string? Competencia { get; set; }
    }

    public class DashboardResumoDto
    {
        public decimal TotalEntradas { get; set; }
        public int QuantidadeEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public int QuantidadeSaidas { get; set; }
        public decimal SaldoMes { get; set; }
        public decimal TotalPendente { get; set; }
        public int QuantidadePendentes { get; set; }
        public decimal TotalPago { get; set; }
        public int QuantidadePagos { get; set; }
        public decimal TotalSemana { get; set; }
        public int QuantidadeSemana { get; set; }
        public decimal TotalAtrasados { get; set; }
        public decimal TotalVencemHoje { get; set; }
        public int TotalLancamentosMes { get; set; }
    }

    public class FaturaCartaoDto
    {
        public int ContaId { get; set; }
        public string NomeCartao { get; set; } = string.Empty;
        public decimal TotalFatura { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? Vencimento { get; set; }
    }

    public class NovoLancamentoDto
    {
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Status { get; set; } = "Pendente";
        public DateTime? DataVencimento { get; set; }
        public int? CategoriaId { get; set; }
        public int? ContaId { get; set; }
        public int? PessoaId { get; set; }
        public string? Competencia { get; set; }
    }

    public class CadastroAuxiliarDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Tipo { get; set; }
    }
}
