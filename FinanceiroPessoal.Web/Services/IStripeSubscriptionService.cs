using Stripe;

namespace FinanceiroPessoal.Web.Services;

public interface IStripeSubscriptionService
{
    Task<string> CriarCheckoutSessionAsync(int usuarioId, int planoId);
    Task ProcessarWebhookAsync(string json, string stripeSignature);
    Task AtualizarAssinaturaPorSubscriptionAsync(Subscription subscription);
    Task AtualizarAssinaturaPorInvoiceAsync(Invoice invoice);
}
