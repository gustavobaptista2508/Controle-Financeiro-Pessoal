using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Data
{
    public class MySqlDbContext : FinanceiroDbContext
    {
        public MySqlDbContext(DbContextOptions<FinanceiroDbContext> options)
            : base(options)
        {
        }
    }
}
