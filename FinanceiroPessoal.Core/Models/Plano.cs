namespace FinanceiroPessoal.Core.Models;

public class Plano
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public string Intervalo { get; set; } = "month";
    public string? StripePriceId { get; set; }
    public bool Ativo { get; set; } = true;
}
