using FinanceiroPessoal.Core.Data;

namespace FinanceiroPessoal.Core.Repositories
{
    public class RepositoryDbInitializer
    {
        public static void Initialize(FinanceiroDbContext context)
        {
            context.Database.EnsureCreated();
        }
    }
}
