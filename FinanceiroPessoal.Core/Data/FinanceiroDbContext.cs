using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Data
{
    public class FinanceiroDbContext : DbContext
    {
        public FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options)
            : base(options)
        {
        }


        public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Conta> Contas => Set<Conta>();
        public DbSet<Pessoa> Pessoas => Set<Pessoa>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Plano> Planos => Set<Plano>();
        public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
        public DbSet<IaConversa> IaConversas => Set<IaConversa>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new InvalidOperationException(
                    "FinanceiroDbContext não foi configurado. Configure no Program.cs usando AddDbContext.");
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

                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
                entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
                entity.Property(x => x.SenhaHash).HasColumnName("senha_hash");
                entity.Property(x => x.Telefone).HasColumnName("telefone");
                entity.Property(x => x.Ativo).HasColumnName("ativo");
                entity.Property(x => x.EmailConfirmado).HasColumnName("email_confirmado");
                entity.Property(x => x.TokenRecuperacao).HasColumnName("token_recuperacao");
                entity.Property(x => x.TokenExpiracao).HasColumnName("token_expiracao");
                entity.Property(x => x.UltimoLogin).HasColumnName("ultimo_login");
                entity.Property(x => x.DataCriacao).HasColumnName("data_criacao");
                entity.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao");
                entity.Property(x => x.PlanoId).HasColumnName("plano_id");
                entity.Property(x => x.AssinaturaStatus).HasColumnName("assinatura_status").HasMaxLength(30);
                entity.Property(x => x.TrialExpiraEm).HasColumnName("trial_expira_em");
                entity.Property(x => x.StripeCustomerId).HasColumnName("stripe_customer_id").HasMaxLength(120);
                entity.Property(x => x.StripeSubscriptionId).HasColumnName("stripe_subscription_id").HasMaxLength(120);
                entity.Property(x => x.AssinaturaExpiraEm).HasColumnName("assinatura_expira_em");

                entity.HasIndex(x => x.Email).IsUnique();
            });


            modelBuilder.Entity<Plano>(entity =>
            {
                entity.ToTable("planos");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
                entity.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(255);
                entity.Property(x => x.Preco).HasColumnName("preco").HasColumnType("decimal(18,2)");
                entity.Property(x => x.Intervalo).HasColumnName("intervalo").HasMaxLength(20).IsRequired();
                entity.Property(x => x.StripePriceId).HasColumnName("stripe_price_id").HasMaxLength(120);
                entity.Property(x => x.Ativo).HasColumnName("ativo");
            });

            modelBuilder.Entity<Assinatura>(entity =>
            {
                entity.ToTable("assinaturas");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.PlanoId).HasColumnName("plano_id");
                entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(30);
                entity.Property(x => x.ProviderCustomerId).HasColumnName("provider_customer_id").HasMaxLength(120);
                entity.Property(x => x.ProviderSubscriptionId).HasColumnName("provider_subscription_id").HasMaxLength(120);
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
                entity.Property(x => x.Inicio).HasColumnName("inicio");
                entity.Property(x => x.FimPeriodo).HasColumnName("fim_periodo");
                entity.Property(x => x.CanceladaEm).HasColumnName("cancelada_em");
                entity.Property(x => x.DataCriacao).HasColumnName("data_criacao");
                entity.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao");
            });


            modelBuilder.Entity<IaConversa>(entity =>
            {
                entity.ToTable("ia_conversas");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
                entity.Property(x => x.Pergunta).HasColumnName("pergunta").IsRequired();
                entity.Property(x => x.Resposta).HasColumnName("resposta").IsRequired();
                entity.Property(x => x.DataCriacao).HasColumnName("data_criacao");
                entity.Property(x => x.TokensEstimados).HasColumnName("tokens_estimados");
            });

            modelBuilder.Entity<Lancamento>().HasOne(x => x.Usuario).WithMany(x => x.Lancamentos).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Categoria>().HasOne(x => x.Usuario).WithMany(x => x.Categorias).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Conta>().HasOne(x => x.Usuario).WithMany(x => x.Contas).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Pessoa>().HasOne(x => x.Usuario).WithMany(x => x.Pessoas).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IaConversa>().HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lancamento>().HasIndex(x => x.UsuarioId);
            modelBuilder.Entity<Categoria>().HasIndex(x => x.UsuarioId);
            modelBuilder.Entity<Conta>().HasIndex(x => x.UsuarioId);
            modelBuilder.Entity<Pessoa>().HasIndex(x => x.UsuarioId);
            modelBuilder.Entity<IaConversa>().HasIndex(x => x.UsuarioId);

            modelBuilder.Entity<Lancamento>().HasQueryFilter(x => x.UsuarioId == FinanceiroPessoal.Core.Services.SessaoUsuario.UsuarioId);
            modelBuilder.Entity<Categoria>().HasQueryFilter(x => x.UsuarioId == FinanceiroPessoal.Core.Services.SessaoUsuario.UsuarioId);
            modelBuilder.Entity<Conta>().HasQueryFilter(x => x.UsuarioId == FinanceiroPessoal.Core.Services.SessaoUsuario.UsuarioId);
            modelBuilder.Entity<Pessoa>().HasQueryFilter(x => x.UsuarioId == FinanceiroPessoal.Core.Services.SessaoUsuario.UsuarioId);
            modelBuilder.Entity<IaConversa>().HasQueryFilter(x => x.UsuarioId == FinanceiroPessoal.Core.Services.SessaoUsuario.UsuarioId);
        }

        public override int SaveChanges()
        {
            PreencherUsuarioId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            PreencherUsuarioId();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void PreencherUsuarioId()
        {
            var usuarioId = FinanceiroPessoal.Core.Services.SessaoUsuario.UsuarioId;
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            {
                switch (entry.Entity)
                {
                    case Lancamento l when l.UsuarioId == 0: l.UsuarioId = usuarioId; break;
                    case Categoria c when c.UsuarioId == 0: c.UsuarioId = usuarioId; break;
                    case Conta c when c.UsuarioId == 0: c.UsuarioId = usuarioId; break;
                    case Pessoa p when p.UsuarioId == 0: p.UsuarioId = usuarioId; break;
                }
            }
        }
    }
}
