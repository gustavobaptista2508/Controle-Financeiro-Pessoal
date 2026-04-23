using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Models
{
    public class DashboardResumo
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

        public int TotalLancamentosMes { get; set; }
        public decimal TotalAtrasados { get; set; }
        public decimal TotalVencemHoje { get; set; }
    }
}
