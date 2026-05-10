namespace FinanceiroPessoal.Web.Models;

public sealed class PluggyOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.pluggy.ai";
    public bool UseMock { get; set; }
}

public sealed record ConnectTokenResponse(string ConnectToken);
public sealed record SaveItemRequest(string ItemId, string? ConnectorName, string? InstitutionName, string? Status);
public sealed record SyncTransactionsRequest(DateTime DataInicial, DateTime DataFinal);
public sealed record ConvertTransactionRequest(int? ContaId, int? CategoriaId, bool IgnorarDuplicidade = false, int? LancamentoIdVinculo = null);

public sealed class ConexaoBancaria
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Provider { get; set; } = "Pluggy";
    public string ItemId { get; set; } = string.Empty;
    public string Status { get; set; } = "UPDATED";
    public string? NomeInstituicao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    public DateTime? UltimaSincronizacao { get; set; }
    public bool Ativa { get; set; } = true;
}

public sealed class ContaBancariaExterna
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int ConexaoBancariaId { get; set; }
    public string ExternalAccountId { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Subtipo { get; set; }
    public string? Banco { get; set; }
    public string? Numero { get; set; }
    public decimal SaldoAtual { get; set; }
    public string Moeda { get; set; } = "BRL";
    public bool Ativa { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
}

public sealed class TransacaoBancaria
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int ContaBancariaExternaId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataTransacao { get; set; }
    public string Tipo { get; set; } = "DEBIT";
    public string Status { get; set; } = "POSTED";
    public string? CategoriaSugerida { get; set; }
    public bool Conciliado { get; set; }
    public int? LancamentoId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
}
