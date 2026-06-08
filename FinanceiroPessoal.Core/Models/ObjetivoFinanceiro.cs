namespace FinanceiroPessoal.Core.Models;

public class ObjetivoFinanceiro
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Nome { get; set; } = string.Empty;
    public decimal ValorAlvo { get; set; }
    public decimal ValorAtual { get; set; }
    public DateTime? DataMeta { get; set; }
    public string Cor { get; set; } = "#2563eb";
    public string Icone { get; set; } = "savings";
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? DataAtualizacao { get; set; }

    public decimal Percentual => ValorAlvo > 0 ? Math.Min((ValorAtual / ValorAlvo) * 100, 100) : 0;

    public decimal ValorFaltante => Math.Max(ValorAlvo - ValorAtual, 0);

    public int? DiasRestantes => DataMeta.HasValue
        ? (int)Math.Ceiling((DataMeta.Value.Date - DateTime.Today).TotalDays)
        : null;
}
