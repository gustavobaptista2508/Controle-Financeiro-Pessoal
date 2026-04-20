using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Repositories;
using FinanceiroPessoal.WinForms.Services;


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
            FinanceiroDbContext context;
            var tipoBanco = TipoBanco.OnlineMySql;
            if (tipoBanco == TipoBanco.LocalSqlite)
                context = new SqliteDbContext();
            else
                context = new MySqlDbContext();
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
            Application.Run(new Form1());
        }
    }
}