using System;

﻿// Models/Dtos.cs
namespace FinanceiroPessoal.MAUI
{
    public class DashboardResumoDto
    {
        public decimal TotalEntradas { get; set; }
        public int QuantidadeEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public int QuantidadeSaidas { get; set; }
        public decimal SaldoMes { get; set; }
        public decimal TotalPendente { get; set; }
        public decimal TotalPago { get; set; }
        public decimal TotalSemana { get; set; }
        public int QuantidadeSemana { get; set; }
        public decimal TotalAtrasados { get; set; }
        public decimal TotalVencemHoje { get; set; }
        public int TotalLancamentosMes { get; set; }
    }

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
        public string? ContaTipo { get; set; }
        public int? ContaId { get; set; }
        public string? Pessoa { get; set; }
        public bool IsCartao => ContaTipo == "Cartão";
        public bool PodeMarcarPago => Id > 0 && !IsCartao && !string.Equals(Status, "Pago", StringComparison.OrdinalIgnoreCase);
    }

    public class ProximoVencimentoDto
    {
        public int Id { get; set; }
        public DateTime? Vencimento { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string? Conta { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GastoCategoriaDto
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    public class CadastroAuxiliarDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Tipo { get; set; }
    }
}
