using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System;

namespace FinanceiroPessoal.Core.Services
{
    public static class DatabaseFactory
    {
        public static ILancamentoRepository CriarLancamentoRepository(TipoBanco tipoBanco)
        {
            var context = CriarContexto(tipoBanco);

            return tipoBanco == TipoBanco.OnlineMySql
                ? new MySqlLancamentoRepository(context)
                : new SqliteLancamentoRepository(context);
        }

        public static ICadastroAuxiliarRepository CriarCadastroAuxiliarRepository(TipoBanco tipoBanco)
        {
            var context = CriarContexto(tipoBanco);

            return tipoBanco == TipoBanco.OnlineMySql
                ? new MySqlCadastroAuxiliarRepository(context)
                : new SqliteCadastroAuxiliarRepository(context);
        }

        private static FinanceiroDbContext CriarContexto(TipoBanco tipoBanco)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FinanceiroDbContext>();

            if (tipoBanco == TipoBanco.OnlineMySql)
            {
                var connectionString =
                    Environment.GetEnvironmentVariable("GRANAOK_MYSQL")
                    ?? "Server=localhost;Port=3306;Database=gadobd;User=root;Password=SUA_SENHA;SslMode=None;AllowPublicKeyRetrieval=True;";

                if (connectionString.Contains("SUA_SENHA"))
                {
                    throw new InvalidOperationException("Configure a variável GRANAOK_MYSQL com a senha real do MySQL.");
                }

                optionsBuilder.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36))
                );

                return new FinanceiroDbContext(optionsBuilder.Options);
            }

            throw new NotSupportedException("SQLite não é prioridade no Web. Ajustar somente se WinForms exigir.");
        }
    }
}
