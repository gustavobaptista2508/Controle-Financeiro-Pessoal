using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Data
{
    public class MySqlDbContext : FinanceiroDbContext
    {
        private readonly string _connectionString;

        public MySqlDbContext(string connectionString = "Server=gadobd.mysql.uhserver.com;Database=gadobd;Uid=gustavobaptista;Pwd=Senh@102030;")
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
            }
        }
    }
}
