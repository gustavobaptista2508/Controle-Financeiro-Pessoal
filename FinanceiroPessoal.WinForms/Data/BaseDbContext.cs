using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace FinanceiroPessoal.WinForms.Data
{
    public abstract class BaseDbContext : DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<Lancamento> Lancamentos { get; set; }
        public Microsoft.EntityFrameworkCore.DbSet<Categoria> Categorias { get; set; }
        public Microsoft.EntityFrameworkCore.DbSet<Conta> Contas { get; set; }
        public Microsoft.EntityFrameworkCore.DbSet<Pessoa> Pessoas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SEU CÓDIGO ATUAL - FUNCIONA NOS DOIS!
            modelBuilder.Entity<Lancamento>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Valor).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Status).HasMaxLength(30);
                entity.Property(x => x.Observacoes).HasMaxLength(1000);
                entity.Property(x => x.Competencia).HasMaxLength(20);
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