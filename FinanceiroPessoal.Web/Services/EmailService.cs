using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Web.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FinanceiroPessoal.Web.Services;

public class EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public Task EnviarBoasVindasAsync(Usuario usuario)
        => EnviarAsync(usuario.Email, "Bem-vindo ao GranaOK",
            $"Olá, {usuario.Nome}.\nSua conta foi criada com sucesso.\nVocê iniciou seu teste grátis de 14 dias.\nAcesse seu painel e comece a organizar seu financeiro.");

    public Task EnviarLembreteTrialAsync(Usuario usuario, int diasRestantes)
        => EnviarAsync(usuario.Email, "Seu teste grátis do GranaOK termina em breve",
            $"Olá, {usuario.Nome}.\nSeu período grátis termina em {diasRestantes} dias.\nPara continuar usando o GranaOK sem interrupção, escolha seu plano.");

    public Task EnviarTrialEncerradoAsync(Usuario usuario)
        => EnviarAsync(usuario.Email, "Seu teste grátis do GranaOK terminou",
            $"Olá, {usuario.Nome}.\nSeu teste grátis terminou.\nPara continuar acessando seu dashboard, lançamentos e relatórios, assine um plano.");

    private async Task EnviarAsync(string para, string assunto, string conteudo)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpPassword))
        {
            logger.LogWarning("E-mail não enviado porque Email:SmtpPassword não está configurado.");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(para));
        message.Subject = assunto;
        message.Body = new TextPart("plain") { Text = conteudo };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
