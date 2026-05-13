namespace FinanceiroPessoal.Core.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public int? PlanoId { get; set; }
    public string AssinaturaStatus { get; set; } = "PENDENTE";
    public DateTime? TrialExpiraEm { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? AssinaturaExpiraEm { get; set; }
    public bool EmailConfirmado { get; set; } = false;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
    public DateTime? UltimoLogin { get; set; }
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Conta> Contas { get; set; } = new List<Conta>();
    public ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
