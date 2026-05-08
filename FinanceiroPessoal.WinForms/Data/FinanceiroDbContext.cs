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
        public DbSet<Usuario> Usuarios => Set<Usuario>();

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

                // ← ADICIONAR ISSO
                entity.Property(x => x.Tipo)
                    .HasMaxLength(20)
                    .HasConversion<string>();  // converte enum <-> varchar
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

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Nome).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
                entity.HasIndex(x => x.Email).IsUnique();
                entity.Property(x => x.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255).IsRequired();
                entity.Property(x => x.Telefone).HasMaxLength(20);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.EmailConfirmado).HasColumnName("email_confirmado");
                entity.Property(x => x.TokenRecuperacao).HasColumnName("token_recuperacao").HasMaxLength(255);
                entity.Property(x => x.TokenExpiracao).HasColumnName("token_expiracao");
                entity.Property(x => x.UltimoLogin).HasColumnName("ultimo_login");
                entity.Property(x => x.DataCriacao).HasColumnName("data_criacao").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
