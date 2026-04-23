using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Models
{
    public class Lancamento
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime? DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = "Pendente";
        public string? Observacoes { get; set; }
        public string? Competencia { get; set; }

        public TipoLancamento Tipo { get; set; } = TipoLancamento.Saida;

        public int? CategoriaId { get; set; }
        public virtual Categoria? Categoria { get; set; }

        public int? ContaId { get; set; }
        public virtual Conta? Conta { get; set; }

        public int? PessoaId { get; set; }
        public virtual Pessoa? Pessoa { get; set; }
    }
}
