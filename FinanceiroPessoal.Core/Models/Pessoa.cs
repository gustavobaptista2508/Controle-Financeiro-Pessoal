namespace FinanceiroPessoal.WinForms.Models
{
    public class Pessoa
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    }
}