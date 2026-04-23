using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Models
{
    public class FaturaCartaoDto
    {
        public int ContaId { get; set; }
        public string NomeCartao { get; set; } = string.Empty;
        public decimal TotalFatura { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Vencimento { get; set; }
        public bool IsCartao => true;

        // Usado para exibir no grid igual aos lancamentos normais
        public string Descricao => $"Fatura {NomeCartao}  •  {QuantidadeLancamentos} lançamento(s)";
        public string ValorFormatado => TotalFatura.ToString("N2");
    }
}
