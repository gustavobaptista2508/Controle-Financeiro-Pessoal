using FinanceiroPessoal.Core.Models;

namespace FinanceiroPessoal.Web.Services;

public sealed class TenantData
{
    public List<Categoria> Categorias { get; } = new() { new() { Id = 1, Nome = "Geral" } };
    public List<Conta> Contas { get; } = new() { new() { Id = 1, Nome = "Principal", Tipo = "Conta corrente" } };
    public List<Pessoa> Pessoas { get; } = new() { new() { Id = 1, Nome = "Padrão" } };
    public List<Lancamento> Lancamentos { get; } = new();
    public int NextLancamentoId { get; set; } = 1;
}

public static class MultiTenantDataStore
{
    private static readonly Dictionary<string, TenantData> Store = new(StringComparer.OrdinalIgnoreCase);

    public static TenantData GetOrCreate(string email)
    {
        if (!Store.TryGetValue(email, out var data))
        {
            data = new TenantData();
            Store[email] = data;
        }

        return data;
    }

    public static void InitializeTenant(string email) => _ = GetOrCreate(email);
}
