using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;
using Microsoft.EntityFrameworkCore;

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
            return tipo == TipoBanco.LocalSqlite
                ? new SqliteLancamentoRepository(_context)
                : new MySqlLancamentoRepository(_context);
        }

        public ICadastroAuxiliarRepository CriarCadastroAuxiliarRepository(TipoBanco tipo = TipoBanco.OnlineMySql)
        {
            return tipo == TipoBanco.LocalSqlite
                ? new SqliteCadastroAuxiliarRepository(_context)
                : new MySqlCadastroAuxiliarRepository(_context);
        }

        public static ILancamentoRepository CriarLancamentoRepository(TipoBanco tipoBanco)
        {
            var context = CriarContextoParaWinForms(tipoBanco);

            return tipoBanco == TipoBanco.OnlineMySql
                ? new MySqlLancamentoRepository(context)
                : new SqliteLancamentoRepository(context);
        }

        public static ICadastroAuxiliarRepository CriarCadastroAuxiliarRepository(TipoBanco tipoBanco)
        {
            var context = CriarContextoParaWinForms(tipoBanco);

            return tipoBanco == TipoBanco.OnlineMySql
                ? new MySqlCadastroAuxiliarRepository(context)
                : new SqliteCadastroAuxiliarRepository(context);
        }

        private static FinanceiroDbContext CriarContextoParaWinForms(TipoBanco tipoBanco)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FinanceiroDbContext>();

            if (tipoBanco == TipoBanco.OnlineMySql)
            {
                var connectionString = ObterConnectionStringMySqlWinForms();
                optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
            }
            else
            {
                throw new NotSupportedException("SQLite não é o foco atual. Use MySQL para o projeto Web.");
            }

            return new FinanceiroDbContext(optionsBuilder.Options);
        }

        private static string ObterConnectionStringMySqlWinForms()
        {
            var connectionString = Environment.GetEnvironmentVariable("GRANAOK_MYSQL");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Configure a variável de ambiente GRANAOK_MYSQL com a connection string real para uso no WinForms.");
            }

            return connectionString;
        }
    }
}
