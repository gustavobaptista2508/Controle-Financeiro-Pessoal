namespace FinanceiroPessoal.Core.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    }
}