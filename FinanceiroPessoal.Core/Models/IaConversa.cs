namespace FinanceiroPessoal.Core.Models;

public class IaConversa
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Pergunta { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public int? TokensEstimados { get; set; }
    public Usuario? Usuario { get; set; }
}
