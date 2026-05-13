using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinanceiroPessoal.Web.Services;

public class StripeSubscriptionService(
    FinanceiroDbContext db,
    IOptions<StripeOptions> options,
    ILogger<StripeSubscriptionService> logger) : IStripeSubscriptionService
{
    private readonly StripeOptions _opt = options.Value;

    private bool StripeConfigurado()
    {
        var configurado = !string.IsNullOrWhiteSpace(_opt.SecretKey);
        if (!configurado)
        {
            logger.LogWarning("Stripe não configurado.");
        }

        return configurado;
    }

    public async Task<string> CriarCheckoutSessionAsync(int usuarioId, int planoId)
    {
        if (!StripeConfigurado()) throw new Exception("Stripe não configurado.");

        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId)
            ?? throw new Exception("Usuário não encontrado");

        var plano = await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == planoId && x.Ativo)
            ?? throw new Exception("Plano não encontrado");

        if (string.IsNullOrWhiteSpace(plano.StripePriceId))
            throw new Exception("Plano sem Stripe Price ID configurado.");

        if (string.IsNullOrWhiteSpace(usuario.StripeCustomerId))
        {
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
            {
                Email = usuario.Email,
                Name = usuario.Nome,
                Metadata = new Dictionary<string, string> { ["usuarioId"] = usuarioId.ToString() }
            });
            usuario.StripeCustomerId = customer.Id;
            await db.SaveChangesAsync();
        }

        var checkoutService = new Stripe.Checkout.SessionService();
        var checkoutOptions = new Stripe.Checkout.SessionCreateOptions
        {
            Mode = "subscription",
            Customer = usuario.StripeCustomerId,
            SuccessUrl = $"{_opt.SuccessUrl}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = _opt.CancelUrl,
            ClientReferenceId = usuario.Id.ToString(),
            LineItems =
            [
                new Stripe.Checkout.SessionLineItemOptions
                {
                    Price = plano.StripePriceId,
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["usuarioId"] = usuario.Id.ToString(),
                ["planoId"] = plano.Id.ToString()
            },
            SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["usuarioId"] = usuario.Id.ToString(),
                    ["planoId"] = plano.Id.ToString()
                }
            }
        };

        var checkoutSession = await checkoutService.CreateAsync(checkoutOptions);

        logger.LogInformation("CHECKOUT STRIPE CRIADO {usuarioId} {planoId} {sessionId}", usuarioId, planoId, checkoutSession.Id);
        return checkoutSession.Url!;
    }

    public async Task<string> CriarPortalSessionAsync(int usuarioId)
    {
        if (!StripeConfigurado()) throw new Exception("Stripe não configurado.");

        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId)
            ?? throw new Exception("Usuário não encontrado");

        if (string.IsNullOrWhiteSpace(usuario.StripeCustomerId))
            throw new Exception("Usuário sem Stripe Customer ID");

        var portalService = new Stripe.BillingPortal.SessionService();
        var portalOptions = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = usuario.StripeCustomerId,
            ReturnUrl = _opt.PortalReturnUrl
        };

        var portalSession = await portalService.CreateAsync(portalOptions);
        return portalSession.Url;
    }

    public async Task ProcessarWebhookAsync(string json, string stripeSignature)
    {
        var evt = EventUtility.ConstructEvent(json, stripeSignature, _opt.WebhookSecret);
        logger.LogInformation("STRIPE WEBHOOK RECEBIDO evento {evento}", evt.Type);

        switch (evt.Type)
        {
            case "checkout.session.completed":
                var session = evt.Data.Object as Stripe.Checkout.Session;
                if (session is null)
                {
                    return;
                }

                var customerId = session.CustomerId;
                var subscriptionId = session.SubscriptionId;
                var usuarioId = session.Metadata != null && session.Metadata.ContainsKey("usuarioId")
                    ? int.Parse(session.Metadata["usuarioId"])
                    : int.Parse(session.ClientReferenceId);
                var planoId = session.Metadata != null && session.Metadata.ContainsKey("planoId")
                    ? int.Parse(session.Metadata["planoId"])
                    : 0;

                var usuario = await db.Usuarios.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == usuarioId || x.StripeCustomerId == customerId);
                if (usuario is not null)
                {
                    usuario.StripeCustomerId = customerId;
                    usuario.StripeSubscriptionId = subscriptionId;
                    if (planoId > 0)
                    {
                        usuario.PlanoId = planoId;
                    }

                    usuario.AssinaturaStatus = "PENDENTE";
                    await UpsertAssinatura(usuario.Id, planoId, usuario.StripeCustomerId, subscriptionId, "PENDENTE");
                    await db.SaveChangesAsync();
                }

                break;

            case "invoice.paid":
                await AtualizarAssinaturaPorInvoiceAsync((Stripe.Invoice)evt.Data.Object, "ATIVA");
                break;

            case "invoice.payment_failed":
                await AtualizarAssinaturaPorInvoiceAsync((Stripe.Invoice)evt.Data.Object, "ATRASADA");
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await AtualizarAssinaturaPorSubscriptionAsync((Stripe.Subscription)evt.Data.Object);
                break;
        }
    }

    public async Task AtualizarAssinaturaPorSubscriptionAsync(Stripe.Subscription subscription)
    {
        var subscriptionId = subscription.Id;
        var customerId = subscription.CustomerId;

        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x =>
            x.StripeSubscriptionId == subscriptionId ||
            x.StripeCustomerId == customerId);

        if (usuario == null)
            return;

        usuario.StripeCustomerId = customerId;
        usuario.StripeSubscriptionId = subscriptionId;
        usuario.AssinaturaStatus = MapearStatusStripe(subscription.Status);

        var fimPeriodo = ObterFimPeriodoDaSubscription(subscription);
        if (fimPeriodo.HasValue)
            usuario.AssinaturaExpiraEm = fimPeriodo.Value;

        if (subscription.Status == "canceled")
            usuario.AssinaturaExpiraEm = DateTime.Now;

        await UpsertAssinatura(
            usuario.Id,
            usuario.PlanoId,
            usuario.StripeCustomerId,
            usuario.StripeSubscriptionId,
            usuario.AssinaturaStatus,
            fimPeriodo: usuario.AssinaturaExpiraEm,
            canceladaEm: subscription.CanceledAt);

        await db.SaveChangesAsync();
    }

    public async Task AtualizarAssinaturaPorInvoiceAsync(Stripe.Invoice invoice)
    {
        await AtualizarAssinaturaPorInvoiceAsync(invoice, "ATIVA");
    }

    private async Task AtualizarAssinaturaPorInvoiceAsync(Stripe.Invoice invoice, string statusInterno)
    {
        var customerId = invoice.CustomerId;
        var subscriptionId = ObterSubscriptionIdDaInvoice(invoice);

        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x =>
            (!string.IsNullOrEmpty(subscriptionId) && x.StripeSubscriptionId == subscriptionId) ||
            x.StripeCustomerId == customerId);

        if (usuario == null)
            return;

        usuario.AssinaturaStatus = statusInterno;
        usuario.StripeCustomerId = customerId;

        if (!string.IsNullOrEmpty(subscriptionId))
            usuario.StripeSubscriptionId = subscriptionId;

        if (statusInterno == "ATIVA")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(usuario.StripeSubscriptionId))
                {
                    var subscriptionService = new SubscriptionService();
                    var subscription = await subscriptionService.GetAsync(usuario.StripeSubscriptionId);
                    var fimPeriodo = ObterFimPeriodoDaSubscription(subscription);

                    if (fimPeriodo.HasValue)
                        usuario.AssinaturaExpiraEm = fimPeriodo.Value;
                    else
                        usuario.AssinaturaExpiraEm ??= DateTime.Now.AddMonths(1);
                }
                else
                {
                    usuario.AssinaturaExpiraEm ??= DateTime.Now.AddMonths(1);
                }
            }
            catch
            {
                usuario.AssinaturaExpiraEm ??= DateTime.Now.AddMonths(1);
            }
        }

        await UpsertAssinatura(
            usuario.Id,
            usuario.PlanoId,
            usuario.StripeCustomerId,
            usuario.StripeSubscriptionId,
            statusInterno,
            fimPeriodo: usuario.AssinaturaExpiraEm);

        await db.SaveChangesAsync();
    }

    private static string? ObterSubscriptionIdDaInvoice(Invoice invoice)
    {
        var invoiceSubscriptionIdProperty = invoice.GetType().GetProperty("SubscriptionId");
        if (invoiceSubscriptionIdProperty?.GetValue(invoice) is string subId && !string.IsNullOrWhiteSpace(subId))
            return subId;

        if (invoice.Parent?.SubscriptionDetails?.SubscriptionId != null)
            return invoice.Parent.SubscriptionDetails.SubscriptionId;

        if (invoice.Lines?.Data != null)
        {
            var line = invoice.Lines.Data.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.SubscriptionId));
            if (line != null)
                return line.SubscriptionId;
        }

        return null;
    }

    private static DateTime? ObterFimPeriodoDaSubscription(Subscription subscription)
    {
        try
        {
            var subProp = subscription.GetType().GetProperty("CurrentPeriodEnd");
            if (subProp?.GetValue(subscription) is DateTime dt)
                return dt;

            var item = subscription.Items?.Data?.FirstOrDefault();
            if (item != null)
            {
                var itemProp = item.GetType().GetProperty("CurrentPeriodEnd");
                if (itemProp?.GetValue(item) is DateTime itemDt)
                    return itemDt;
            }
        }
        catch
        {
            // noop: fallback handled by caller
        }

        return null;
    }

    private static string MapearStatusStripe(string? status) => status switch
    {
        "active" => "ATIVA",
        "trialing" => "TRIAL",
        "past_due" => "ATRASADA",
        "unpaid" => "ATRASADA",
        "canceled" => "CANCELADA",
        "incomplete" => "PENDENTE",
        "incomplete_expired" => "EXPIRADA",
        "paused" => "CANCELADA",
        _ => "PENDENTE"
    };

    private async Task UpsertAssinatura(int usuarioId, int? planoId, string? customerId, string? subId, string status, DateTime? inicio = null, DateTime? fimPeriodo = null, DateTime? canceladaEm = null)
    {
        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Provider == "STRIPE" && x.ProviderSubscriptionId == subId && subId != null)
              ?? await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Provider == "STRIPE" && x.UsuarioId == usuarioId);
        if (a is null)
        {
            a = new Assinatura { UsuarioId = usuarioId, DataCriacao = DateTime.Now };
            db.Assinaturas.Add(a);
        }

        a.PlanoId = planoId;
        a.Provider = "STRIPE";
        a.ProviderCustomerId = customerId;
        a.ProviderSubscriptionId = subId;
        a.Status = status;
        a.Inicio = inicio ?? a.Inicio;
        a.FimPeriodo = fimPeriodo ?? a.FimPeriodo;
        a.CanceladaEm = canceladaEm ?? a.CanceladaEm;
        a.DataAtualizacao = DateTime.Now;
    }
}
