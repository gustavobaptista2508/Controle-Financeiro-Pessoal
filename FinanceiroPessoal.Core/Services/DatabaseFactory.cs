using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Core.Services
{
    public class DatabaseFactory
    {
        private readonly FinanceiroDbContext _context;

        public DatabaseFactory(FinanceiroDbContext context)
        {
            _context = context;
        }

        public ILancamentoRepository CriarLancamentoRepository(TipoBanco tipo = TipoBanco.OnlineMySql)
        {
            return new MySqlLancamentoRepository(_context);
        }

        public ICadastroAuxiliarRepository CriarCadastroAuxiliarRepository(TipoBanco tipo = TipoBanco.OnlineMySql)
        {
            return new MySqlCadastroAuxiliarRepository(_context);
        }
    }
}
