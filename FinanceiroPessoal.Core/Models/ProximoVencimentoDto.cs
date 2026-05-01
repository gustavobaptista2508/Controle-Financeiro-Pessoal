using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Models
{
    public class ProximoVencimentoDto
    {
        public int Id { get; set; }
        public DateTime? Vencimento { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Conta { get; set; } = string.Empty;
        public string Pessoa { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }
}
