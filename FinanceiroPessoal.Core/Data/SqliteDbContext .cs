using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Data
{
    public class SqliteDbContext : FinanceiroDbContext
    {
        public SqliteDbContext(DbContextOptions<FinanceiroDbContext> options)
            : base(options)
        {
        }
    }
}
