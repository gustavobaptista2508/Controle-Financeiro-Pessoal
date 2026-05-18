namespace FinanceiroPessoal.Core.Models;

public class Investimento
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public string Tipo { get; set; } = "Outros";

    public decimal ValorInvestido { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorAtualUnitario { get; set; }

    public DateTime DataCompra { get; set; } = DateTime.Today;
    public string? Observacao { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? DataAtualizacao { get; set; }

    public decimal PrecoMedio => Quantidade > 0 ? ValorInvestido / Quantidade : 0;

    public decimal ValorAtualTotal => ValorAtualUnitario * Quantidade;

    public decimal RentabilidadeValor => ValorAtualTotal - ValorInvestido;

    public decimal RentabilidadePercentual => ValorInvestido > 0 ? (RentabilidadeValor / ValorInvestido) * 100 : 0;
}
