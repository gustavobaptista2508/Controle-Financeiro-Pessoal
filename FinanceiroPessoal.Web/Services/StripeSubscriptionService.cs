using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;

namespace FinanceiroPessoal.Web.Services;

public class StripeSubscriptionService(FinanceiroDbContext db, IOptions<StripeOptions> options, ILogger<StripeSubscriptionService> logger) : IStripeSubscriptionService
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
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId) ?? throw new Exception("Usuário não encontrado");
        var plano = await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == planoId && x.Ativo) ?? throw new Exception("Plano não encontrado");
        if (string.IsNullOrWhiteSpace(plano.StripePriceId)) throw new Exception("Plano sem Stripe Price ID configurado.");
        if (string.IsNullOrWhiteSpace(usuario.StripeCustomerId))
        {
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions { Email = usuario.Email, Name = usuario.Nome, Metadata = new() { ["usuarioId"] = usuarioId.ToString() } });
            usuario.StripeCustomerId = customer.Id;
            await db.SaveChangesAsync();
        }
        var session = await new SessionService().CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = usuario.StripeCustomerId,
            SuccessUrl = $"{_opt.SuccessUrl}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = _opt.CancelUrl,
            ClientReferenceId = usuarioId.ToString(),
            Metadata = new() { ["usuarioId"] = usuarioId.ToString(), ["planoId"] = planoId.ToString() },
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = new() { ["usuarioId"] = usuarioId.ToString(), ["planoId"] = planoId.ToString() } },
            LineItems = [new SessionLineItemOptions { Price = plano.StripePriceId, Quantity = 1 }]
        });
        logger.LogInformation("CHECKOUT STRIPE CRIADO {usuarioId} {planoId} {sessionId}", usuarioId, planoId, session.Id);
        return session.Url!;
    }

    public async Task ProcessarWebhookAsync(string json, string stripeSignature)
    {
        var evt = EventUtility.ConstructEvent(json, stripeSignature, _opt.WebhookSecret);
        logger.LogInformation("STRIPE WEBHOOK RECEBIDO evento {evento}", evt.Type);
        switch (evt.Type)
        {
            case "checkout.session.completed":
                var s = evt.Data.Object as Session; if (s is null) break;
                int.TryParse(s.Metadata.GetValueOrDefault("usuarioId") ?? s.ClientReferenceId, out var usuarioId);
                int.TryParse(s.Metadata.GetValueOrDefault("planoId"), out var planoId);
                var u = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId || x.StripeCustomerId == s.CustomerId);
                if (u is not null){ u.StripeCustomerId=s.CustomerId; u.StripeSubscriptionId=s.SubscriptionId; if(planoId>0) u.PlanoId=planoId; u.AssinaturaStatus="PENDENTE"; await UpsertAssinatura(u.Id,planoId,u.StripeCustomerId,s.SubscriptionId,"PENDENTE"); await db.SaveChangesAsync(); }
                break;
            case "invoice.paid": await AtualizarAssinaturaPorInvoiceAsync((Invoice)evt.Data.Object); break;
            case "invoice.payment_failed":
                var invf=(Invoice)evt.Data.Object; var uf=await LocalizarUsuario(invf.SubscriptionId, invf.CustomerId); if(uf!=null){uf.AssinaturaStatus="ATRASADA"; await UpsertAssinatura(uf.Id,uf.PlanoId,uf.StripeCustomerId,uf.StripeSubscriptionId,"ATRASADA"); await db.SaveChangesAsync();}
                break;
            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted": await AtualizarAssinaturaPorSubscriptionAsync((Subscription)evt.Data.Object); break;
        }
    }
    public async Task AtualizarAssinaturaPorSubscriptionAsync(Subscription sub)
    {
        var u = await LocalizarUsuario(sub.Id, sub.CustomerId); if (u is null) return;
        var status = Map(sub.Status); u.AssinaturaStatus = status; u.StripeSubscriptionId = sub.Id; u.StripeCustomerId = sub.CustomerId; u.AssinaturaExpiraEm = sub.CurrentPeriodEnd;
        if (sub.Status == "canceled") u.AssinaturaExpiraEm = DateTime.Now;
        await UpsertAssinatura(u.Id,u.PlanoId,u.StripeCustomerId,u.StripeSubscriptionId,status,sub.CurrentPeriodStart,sub.CurrentPeriodEnd,sub.CanceledAt);
        logger.LogInformation("ASSINATURA ATUALIZADA {usuarioId} {status}", u.Id, status);
        await db.SaveChangesAsync();
    }
    public async Task AtualizarAssinaturaPorInvoiceAsync(Invoice inv)
    {
        var u=await LocalizarUsuario(inv.SubscriptionId,inv.CustomerId); if(u is null)return;
        u.AssinaturaStatus="ATIVA"; u.StripeCustomerId=inv.CustomerId; u.StripeSubscriptionId=inv.SubscriptionId;
        DateTime? fim=null; if(!string.IsNullOrWhiteSpace(inv.SubscriptionId)){ var sub=await new SubscriptionService().GetAsync(inv.SubscriptionId); fim=sub.CurrentPeriodEnd; }
        u.AssinaturaExpiraEm=fim??u.AssinaturaExpiraEm;
        await UpsertAssinatura(u.Id,u.PlanoId,u.StripeCustomerId,u.StripeSubscriptionId,"ATIVA",fimPeriodo:u.AssinaturaExpiraEm);
        logger.LogInformation("ASSINATURA ATUALIZADA {usuarioId} {status}", u.Id, "ATIVA");
        await db.SaveChangesAsync();
    }
    private async Task<FinanceiroPessoal.Core.Models.Usuario?> LocalizarUsuario(string? subscriptionId, string? customerId) =>
        await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => (!string.IsNullOrWhiteSpace(subscriptionId) && x.StripeSubscriptionId == subscriptionId) || (!string.IsNullOrWhiteSpace(customerId) && x.StripeCustomerId == customerId));
    private static string Map(string s)=>s switch{"active"=>"ATIVA","trialing"=>"TRIAL","past_due"=>"ATRASADA","unpaid"=>"ATRASADA","canceled"=>"CANCELADA","incomplete"=>"PENDENTE","incomplete_expired"=>"EXPIRADA","paused"=>"CANCELADA",_=>"PENDENTE"};
    private async Task UpsertAssinatura(int usuarioId,int? planoId,string? customerId,string? subId,string status,DateTime? inicio=null,DateTime? fimPeriodo=null,DateTime? canceladaEm=null){
        var a=await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x=>x.Provider=="STRIPE" && x.ProviderSubscriptionId==subId && subId!=null)
              ?? await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x=>x.Provider=="STRIPE" && x.UsuarioId==usuarioId);
        if(a is null){a=new Assinatura{UsuarioId=usuarioId,DataCriacao=DateTime.Now}; db.Assinaturas.Add(a);} a.PlanoId=planoId;a.Provider="STRIPE";a.ProviderCustomerId=customerId;a.ProviderSubscriptionId=subId;a.Status=status;a.Inicio=inicio??a.Inicio;a.FimPeriodo=fimPeriodo??a.FimPeriodo;a.CanceladaEm=canceladaEm??a.CanceladaEm;a.DataAtualizacao=DateTime.Now;
    }
}
