using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Repositories;
using FinanceiroPessoal.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var tipoBanco = TipoBanco.OnlineMySql;
            var optionsBuilder = new DbContextOptionsBuilder<FinanceiroDbContext>();

            if (tipoBanco == TipoBanco.LocalSqlite)
            {
                throw new NotSupportedException("SQLite não é o foco atual. Use MySQL para o projeto Web.");
            }

            var connectionString = Environment.GetEnvironmentVariable("GRANAOK_MYSQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show(
                    "Configure a variável de ambiente GRANAOK_MYSQL com a connection string real.",
                    "Configuração inválida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
            using var context = new FinanceiroDbContext(optionsBuilder.Options);

            try
            {
                context.Database.EnsureCreated();
                RepositoryDbInitializer.Initialize(context);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar o banco de dados: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using var auth = new FrmLogin();
            if (auth.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Login cancelado. Encerrando o aplicativo.", "Acesso Negado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Application.Run(new Form1());
        }
    }
}
