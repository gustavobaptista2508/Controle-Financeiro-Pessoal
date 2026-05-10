namespace FinanceiroPessoal.Core.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime? UltimoLogin { get; set; }
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Conta> Contas { get; set; } = new List<Conta>();
    public ICollection<Pessoa> Pessoas { get; set; } = new List<Pessoa>();
}
