using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Web.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FinanceiroPessoal.Web.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> EnviarBoasVindasAsync(Usuario usuario)
    {
        var assunto = "Bem-vindo ao GranaOK";

        var corpo = $"""
        Olá, {usuario.Nome}.

        Sua conta foi criada com sucesso no GranaOK.

        Você iniciou seu teste grátis de 14 dias.
        Acesse seu painel e comece a organizar seu financeiro.

        Atenciosamente,
        Equipe GranaOK
        """;

        return await EnviarAsync(usuario.Email, assunto, corpo);
    }

    public async Task<bool> EnviarLembreteTrialAsync(Usuario usuario, int diasRestantes)
    {
        var assunto = "Seu teste grátis do GranaOK termina em breve";

        var corpo = $"""
        Olá, {usuario.Nome}.

        Seu período grátis do GranaOK termina em {diasRestantes} dias.

        Para continuar usando o dashboard, lançamentos, categorias, contas e relatórios sem interrupção, escolha um plano.

        Atenciosamente,
        Equipe GranaOK
        """;

        return await EnviarAsync(usuario.Email, assunto, corpo);
    }

    public async Task<bool> EnviarTrialEncerradoAsync(Usuario usuario)
    {
        var assunto = "Seu teste grátis do GranaOK terminou";

        var corpo = $"""
        Olá, {usuario.Nome}.

        Seu teste grátis do GranaOK terminou.

        Para continuar acessando seu dashboard, lançamentos, relatórios e recursos financeiros, assine um plano.

        Atenciosamente,
        Equipe GranaOK
        """;

        return await EnviarAsync(usuario.Email, assunto, corpo);
    }

    private async Task<bool> EnviarAsync(string destinatario, string assunto, string corpo)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            _logger.LogWarning("E-mail não enviado: destinatário vazio.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.SmtpHost) ||
            string.IsNullOrWhiteSpace(_options.SmtpUser) ||
            string.IsNullOrWhiteSpace(_options.SmtpPassword) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning("E-mail não configurado. Mensagem não enviada para {Destinatario}.", destinatario);
            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(destinatario));
        message.Subject = assunto;
        message.Body = new TextPart("plain") { Text = corpo };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail para {Destinatario}.", destinatario);
            return false;
        }
    }
}
