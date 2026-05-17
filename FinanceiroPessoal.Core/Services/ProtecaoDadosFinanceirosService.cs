namespace FinanceiroPessoal.Core.Services;

/// <summary>
/// Valores financeiros não são hash porque precisam ser calculados, agregados e exibidos.
/// A proteção principal atual é isolamento por UsuarioId, controle de acesso,
/// conexão segura, logs seguros e secrets fora do repositório.
/// Criptografia seletiva pode ser implementada em etapa futura sem quebrar cálculos.
/// </summary>
public class ProtecaoDadosFinanceirosService
{
    public string Protect(string valor)
    {
        return valor;
    }

    public string Unprotect(string valorProtegido)
    {
        return valorProtegido;
    }
}
