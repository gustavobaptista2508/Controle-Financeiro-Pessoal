using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Services;
using FinanceiroPessoal.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FinanceiroPessoal.Web.Services;

public interface IPluggyAuthService { Task<string> GetApiKeyAsync(); }
public interface IPluggyService
{
    Task<string> CreateConnectTokenAsync(int usuarioId);
    Task<List<ContaBancariaExterna>> SyncAccountsAsync(int usuarioId, string itemId);
    Task<List<TransacaoBancaria>> SyncTransactionsAsync(int usuarioId, string externalAccountId, DateTime ini, DateTime fim);
}

public sealed class PluggyStore { public List<ConexaoBancaria> Conexoes { get; } = []; public List<ContaBancariaExterna> Contas { get; } = []; public List<TransacaoBancaria> Transacoes { get; } = []; }

public sealed class PluggyAuthService(HttpClient http, IOptions<PluggyOptions> options, IMemoryCache cache) : IPluggyAuthService
{
    public async Task<string> GetApiKeyAsync()
    {
        if (cache.TryGetValue<string>("pluggy_api_key", out var key) && !string.IsNullOrWhiteSpace(key)) return key;
        var opt = options.Value;
        var resp = await http.PostAsJsonAsync($"{opt.BaseUrl.TrimEnd('/')}/auth", new { clientId = opt.ClientId, clientSecret = opt.ClientSecret });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>() ?? [];
        key = body.GetValueOrDefault("apiKey") ?? throw new InvalidOperationException("apiKey não retornada.");
        cache.Set("pluggy_api_key", key, TimeSpan.FromMinutes(100));
        return key;
    }
}

public sealed class PluggyService(HttpClient http, IOptions<PluggyOptions> options, IPluggyAuthService auth, PluggyStore store) : IPluggyService
{
    public async Task<string> CreateConnectTokenAsync(int usuarioId)
    {
        if (options.Value.UseMock) return $"mock-token-{usuarioId}-{Guid.NewGuid():N}";
        var req = new HttpRequestMessage(HttpMethod.Post, $"{options.Value.BaseUrl.TrimEnd('/')}/connect_token");
        req.Headers.Add("X-API-KEY", await auth.GetApiKeyAsync());
        req.Content = JsonContent.Create(new { options = new { clientUserId = usuarioId.ToString(), avoidDuplicates = true } });
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>() ?? [];
        return body.GetValueOrDefault("accessToken") ?? body.GetValueOrDefault("connectToken") ?? string.Empty;
    }

    public async Task<List<ContaBancariaExterna>> SyncAccountsAsync(int usuarioId, string itemId)
    {
        if (options.Value.UseMock)
        {
            var cx = store.Conexoes.First(x => x.UsuarioId == usuarioId && x.ItemId == itemId);
            var acc = new ContaBancariaExterna { UsuarioId = usuarioId, ConexaoBancariaId = cx.Id, ExternalAccountId = $"mock-{itemId}-1", Nome = "Conta Corrente", Tipo = "BANK", Banco = cx.NomeInstituicao ?? "Banco Mock", SaldoAtual = 1250.90m };
            if (!store.Contas.Any(x => x.UsuarioId == usuarioId && x.ExternalAccountId == acc.ExternalAccountId)) store.Contas.Add(acc);
            return store.Contas.Where(x => x.UsuarioId == usuarioId && x.ConexaoBancariaId == cx.Id).ToList();
        }
        var req = new HttpRequestMessage(HttpMethod.Get, $"{options.Value.BaseUrl.TrimEnd('/')}/accounts?itemId={itemId}");
        req.Headers.Add("X-API-KEY", await auth.GetApiKeyAsync());
        var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return store.Contas.Where(x => x.UsuarioId == usuarioId).ToList();
    }

    public Task<List<TransacaoBancaria>> SyncTransactionsAsync(int usuarioId, string externalAccountId, DateTime ini, DateTime fim)
    {
        var conta = store.Contas.First(x => x.UsuarioId == usuarioId && x.ExternalAccountId == externalAccountId);
        if (options.Value.UseMock)
        {
            var id = $"mock-tx-{externalAccountId}-{ini:yyyyMMdd}";
            if (!store.Transacoes.Any(x => x.UsuarioId == usuarioId && x.ExternalTransactionId == id))
                store.Transacoes.Add(new TransacaoBancaria { UsuarioId = usuarioId, ContaBancariaExternaId = conta.Id, ExternalTransactionId = id, Descricao = "Supermercado", Valor = -89.90m, DataTransacao = ini.Date.AddDays(1) });
            return Task.FromResult(store.Transacoes.Where(x => x.UsuarioId == usuarioId && x.ContaBancariaExternaId == conta.Id && x.DataTransacao >= ini && x.DataTransacao <= fim).ToList());
        }
        return Task.FromResult(store.Transacoes.Where(x => x.UsuarioId == usuarioId && x.ContaBancariaExternaId == conta.Id).ToList());
    }
}

public static class PluggyUserResolver
{
    public static int GetUsuarioId(ClaimsPrincipal user) => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
