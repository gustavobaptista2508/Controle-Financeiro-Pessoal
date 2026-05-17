namespace FinanceiroPessoal.Core.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
    public int? PlanoId { get; set; }
    public string? AssinaturaStatus { get; set; }
    public DateTime? TrialExpiraEm { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? AssinaturaExpiraEm { get; set; }
    public bool EmailConfirmado { get; set; }
    public string? TokenRecuperacao { get; set; }
    public DateTime? TokenExpiracao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Conta> Contas { get; set; } = new List<Conta>();
    public ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
