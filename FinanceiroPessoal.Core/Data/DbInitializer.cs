namespace FinanceiroPessoal.Core.Data
{
    public class DbInitializer
    {
        private readonly FinanceiroDbContext _db;

        public DbInitializer(FinanceiroDbContext db)
        {
            _db = db;
        }

        public async Task InicializarAsync()
        {
            await _db.Database.EnsureCreatedAsync();
        }
    }
}
