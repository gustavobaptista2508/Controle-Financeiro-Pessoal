namespace FinanceiroPessoal.WinForms.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
    public bool EmailConfirmado { get; set; }
    public string? TokenRecuperacao { get; set; }
    public DateTime? TokenExpiracao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
}
