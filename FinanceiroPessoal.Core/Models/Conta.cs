namespace FinanceiroPessoal.Core.Models
{
    public class Conta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Outro";

        public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    }
}