using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Tunora.Core.Domain.Enums;
using Tunora.Core.Exceptions;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Options;

namespace Tunora.Infrastructure.Services;

public class BillingService(ApplicationDbContext db, IOptions<StripeOptions> options) : IBillingService
{
    private readonly StripeOptions _opts = options.Value;
    private StripeClient Client => new(_opts.SecretKey);

    // ── Status ───────────────────────────────────────────────────────────────

    public async Task<BillingStatus> GetStatusAsync(int companyId, CancellationToken ct = default)
    {
        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        return new BillingStatus(company.SubscriptionTier, company.SubscriptionStatus, company.SubscriptionEndDate);
    }

    // ── Checkout ─────────────────────────────────────────────────────────────

    public async Task<string> CreateCheckoutSessionAsync(
        int companyId, string tier, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        // Create Stripe customer on first checkout
        if (string.IsNullOrEmpty(company.StripeCustomerId))
        {
            var customerService = new CustomerService(Client);
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email    = company.ContactEmail,
                Metadata = new Dictionary<string, string> { ["companyId"] = companyId.ToString() },
            }, cancellationToken: ct);
            company.StripeCustomerId = customer.Id;
            await db.SaveChangesAsync(ct);
        }

        var priceId = ResolvePriceId(tier);

        var sessionService = new SessionService(Client);
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Customer           = company.StripeCustomerId,
            Mode               = "subscription",
            LineItems          = [new() { Price = priceId, Quantity = 1 }],
            SuccessUrl         = successUrl,
            CancelUrl          = cancelUrl,
            SubscriptionData   = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string> { ["companyId"] = companyId.ToString() },
            },
        }, cancellationToken: ct);

        return session.Url;
    }

    // ── Customer Portal ───────────────────────────────────────────────────────

    public async Task<string> CreatePortalSessionAsync(int companyId, string returnUrl, CancellationToken ct = default)
    {
        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        if (string.IsNullOrEmpty(company.StripeCustomerId))
            throw new InvalidOperationException("No Stripe customer associated with this account.");

        var portalService = new Stripe.BillingPortal.SessionService(Client);
        var portal = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer  = company.StripeCustomerId,
            ReturnUrl = returnUrl,
        }, cancellationToken: ct);

        return portal.Url;
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, _opts.WebhookSecret);

        // Idempotency — skip events already processed
        var alreadyProcessed = await db.StripeEvents
            .AnyAsync(e => e.StripeEventId == stripeEvent.Id, ct);
        if (alreadyProcessed) return;

        await (stripeEvent.Type switch
        {
            EventTypes.CheckoutSessionCompleted    => HandleCheckoutCompleted(stripeEvent, ct),
            EventTypes.CustomerSubscriptionUpdated => HandleSubscriptionUpdated(stripeEvent, ct),
            EventTypes.CustomerSubscriptionDeleted => HandleSubscriptionDeleted(stripeEvent, ct),
            _ => Task.CompletedTask
        });

        db.StripeEvents.Add(new Core.Domain.Entities.StripeEvent
        {
            StripeEventId = stripeEvent.Id,
            EventType     = stripeEvent.Type,
            ProcessedAt   = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    // ── Webhook event handlers ────────────────────────────────────────────────

    private async Task HandleCheckoutCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.SubscriptionId is null) return;

        var subscriptionService = new SubscriptionService(Client);
        var subscription = await subscriptionService.GetAsync(session.SubscriptionId, cancellationToken: ct);

        var companyId = int.Parse(subscription.Metadata.GetValueOrDefault("companyId") ?? "0");
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null) return;

        var priceId = subscription.Items.Data.FirstOrDefault()?.Price.Id ?? string.Empty;
        company.StripeSubscriptionId  = subscription.Id;
        company.SubscriptionTier      = MapPriceTier(priceId);
        company.SubscriptionStatus    = MapStatus(subscription.Status);
        company.SubscriptionStartDate = subscription.StartDate;
        // CurrentPeriodEnd removed in Stripe API 2024-10-28; use CancelAt or TrialEnd as period boundary
        company.SubscriptionEndDate   = subscription.CancelAt ?? subscription.TrialEnd;
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var company = await db.Companies
            .FirstOrDefaultAsync(c => c.StripeSubscriptionId == subscription.Id, ct);
        if (company is null) return;

        var priceId = subscription.Items.Data.FirstOrDefault()?.Price.Id ?? string.Empty;
        company.SubscriptionTier    = MapPriceTier(priceId);
        company.SubscriptionStatus  = MapStatus(subscription.Status);
        company.SubscriptionEndDate = subscription.CancelAt ?? subscription.TrialEnd;
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var company = await db.Companies
            .FirstOrDefaultAsync(c => c.StripeSubscriptionId == subscription.Id, ct);
        if (company is null) return;

        company.SubscriptionStatus  = SubscriptionStatus.Cancelled;
        company.SubscriptionEndDate = subscription.EndedAt ?? subscription.CanceledAt;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string ResolvePriceId(string tier) => tier switch
    {
        "Starter"      => _opts.StarterPriceId,
        "Professional" => _opts.ProfessionalPriceId,
        "Business"     => _opts.BusinessPriceId,
        _ => throw new ArgumentException($"Unknown tier: {tier}"),
    };

    private SubscriptionTier MapPriceTier(string priceId)
    {
        if (priceId == _opts.ProfessionalPriceId) return SubscriptionTier.Professional;
        if (priceId == _opts.BusinessPriceId)     return SubscriptionTier.Business;
        return SubscriptionTier.Starter;
    }

    private static SubscriptionStatus MapStatus(string status) => status switch
    {
        "active"   => SubscriptionStatus.Active,
        "trialing" => SubscriptionStatus.Trialing,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" or "cancelled" => SubscriptionStatus.Cancelled,
        _ => SubscriptionStatus.Active,
    };
}
