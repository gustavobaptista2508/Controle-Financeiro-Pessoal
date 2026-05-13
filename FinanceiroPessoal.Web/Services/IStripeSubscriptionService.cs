using Stripe;

namespace FinanceiroPessoal.Web.Services;

public interface IStripeSubscriptionService
{
    Task<string> CriarCheckoutSessionAsync(int usuarioId, int planoId);
    Task<string> CriarPortalSessionAsync(int usuarioId);
    Task ProcessarWebhookAsync(string json, string stripeSignature);
    Task AtualizarAssinaturaPorSubscriptionAsync(Stripe.Subscription subscription);
    Task AtualizarAssinaturaPorInvoiceAsync(Stripe.Invoice invoice);
}
