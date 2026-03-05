using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Tunora.API.DTOs.Billing;
using Tunora.Core.Domain.Enums;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/billing")]
public class BillingController(IBillingService billingService) : ApiControllerBase
{
    private static readonly Dictionary<SubscriptionTier, (int MaxInstances, int MaxChannels, bool CanSchedule)> TierInfo = new()
    {
        [SubscriptionTier.Starter]      = (1,          3, false),
        [SubscriptionTier.Professional] = (5,          5, true),
        [SubscriptionTier.Business]     = (20,         5, true),
        [SubscriptionTier.Enterprise]   = (int.MaxValue, 5, true),
    };

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await billingService.GetStatusAsync(CompanyId, ct);
        var (maxInstances, maxChannels, canSchedule) = TierInfo[status.Tier];

        return Ok(new SubscriptionStatusDto(
            status.Tier.ToString(),
            status.Status.ToString(),
            status.Status == SubscriptionStatus.Trialing,
            status.PeriodEnd,
            maxInstances == int.MaxValue ? -1 : maxInstances,
            maxChannels,
            canSchedule));
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutDto dto, CancellationToken ct)
    {
        var url = await billingService.CreateCheckoutSessionAsync(CompanyId, dto.Tier, dto.SuccessUrl, dto.CancelUrl, ct);
        return Ok(new CheckoutSessionDto(url));
    }

    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortal([FromBody] CreatePortalDto dto, CancellationToken ct)
    {
        var url = await billingService.CreatePortalSessionAsync(CompanyId, dto.ReturnUrl, ct);
        return Ok(new BillingPortalDto(url));
    }

    /// <summary>Stripe webhook — must be anonymous and receive raw body.</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        string payload;
        using (var reader = new System.IO.StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync();

        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        try
        {
            await billingService.HandleWebhookAsync(payload, signature);
            return Ok();
        }
        catch (StripeException ex) when (ex.StripeError?.Type == "invalid_request_error")
        {
            return BadRequest(new { error = "Invalid webhook signature." });
        }
    }
}
