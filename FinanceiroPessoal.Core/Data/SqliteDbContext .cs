using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Data
{
    public class SqliteDbContext : FinanceiroDbContext
    {
        private readonly string _connectionString;

        public SqliteDbContext(string connectionString = "Data Source=financeiro.db")
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(_connectionString);
            }
        }
    }
}
