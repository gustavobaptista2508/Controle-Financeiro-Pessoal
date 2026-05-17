namespace FinanceiroPessoal.Core.Models;

public class Assinatura
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? PlanoId { get; set; }
    public string Provider { get; set; } = "STRIPE";
    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public string Status { get; set; } = "PENDENTE";
    public DateTime? Inicio { get; set; }
    public DateTime? FimPeriodo { get; set; }
    public DateTime? CanceladaEm { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
    public Usuario? Usuario { get; set; }
}
