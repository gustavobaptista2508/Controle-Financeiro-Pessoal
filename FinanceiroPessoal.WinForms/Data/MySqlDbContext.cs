using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Data
{
    public class MySqlDbContext: BaseDbContext
    {
        private readonly string _connectionString;

        public MySqlDbContext(string connectionString = "Server=gadobd.mysql.uhserver.com;Database=gadobd;Uid=gustavobaptista;Pwd=123;")
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(_connectionString,
                    new MySqlServerVersion(new Version(8, 0, 21))); // ✅ VERSÃO CORRETA
            }
        }
    }
}
