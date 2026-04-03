using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Data
{
    public class FinanceiroDbContext : DbContext
    {
        public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Conta> Contas => Set<Conta>();
        public DbSet<Pessoa> Pessoas => Set<Pessoa>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=financeiro.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lancamento>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Descricao)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Valor)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Status)
                    .HasMaxLength(30);

                entity.Property(x => x.Observacoes)
                    .HasMaxLength(1000);

                entity.Property(x => x.Competencia)
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            });

            modelBuilder.Entity<Conta>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Nome).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<Pessoa>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            });
        }
    }
}
