# Tunora — Claude Project Instructions

## What Is Tunora
SaaS platform for in-store background music. Companies manage store **Instances** (locations) from a React dashboard. In-store players run in Chrome Kiosk mode. Real-time control via SignalR. Music from Jamendo API. Payments via Stripe.

## Project Structure
```
Tunora.Core           → Entities, interfaces, enums. No external dependencies.
Tunora.Infrastructure → EF Core, Quartz.NET, JamendoClient, BillingService, AnalyticsService
Tunora.API            → ASP.NET Core 10 controllers, SignalR hubs, JWT auth, rate limiting
Tunora.Web            → React 18 + TypeScript dashboard (Tailwind v4, Spotify dark theme)
Tunora.Player         → React 18 Chrome kiosk player (minimal UI)
```
Local DB: `Server=localhost\SQLEXPRESS;Database=TunoraDb;Trusted_Connection=True;TrustServerCertificate=True;`

## C# Rules
- `net10.0`, nullable refs enabled everywhere
- **No domain logic in controllers** — call services, return results
- **No Repository pattern** — use `ApplicationDbContext` directly in services
- All EF queries async — never `.Result` or `.Wait()`
- `CompanyId` always from JWT claims, never from request body
- Use `record` for DTOs and SignalR message types
- Passwords: BCrypt cost 12. ConnectionKey: 32-byte crypto-random hex
- Logging: `ILogger<T>` only, never `Console.WriteLine`

## EF Migrations
```bash
dotnet ef migrations add <Name> --project src/Tunora.Infrastructure --startup-project src/Tunora.API
dotnet ef database update        --project src/Tunora.Infrastructure --startup-project src/Tunora.API
```

## TypeScript / React Rules
- Strict mode on. Functional components only. No class components (except `ErrorBoundary`).
- Global state: Zustand (`/src/store/`). Server state: TanStack Query. No manual fetch in components.
- All API calls through `/src/api/client.ts` (Axios + JWT interceptor).
- Tailwind v4 only — config in `@theme` block in `index.css`, plugin via `@tailwindcss/vite`.

## SignalR Groups
| Group | Members | Purpose |
|-------|---------|---------|
| `instance-{id}` | Dashboard + Player | Commands → player |
| `dashboard-{id}` | Dashboard | Player state → dashboard |

**Auth in hub:** Kiosk — check JWT `instanceId` claim matches. Dashboard — DB lookup: `db.Instances.AnyAsync(i => i.Id == instanceId && i.CompanyId == companyId)`.

## Subscription Tiers (enforce server-side, never trust client)
| Tier | Instances | Channels/Instance | Scheduling |
|------|-----------|------------------|------------|
| Starter | 1 | 3 | No |
| Professional | 5 | 5 | Yes |
| Business | 20 | 5 | Yes |
| Enterprise | Unlimited | 5 | Yes |

## Stripe
- Verify webhook signature before processing. Store event IDs in `StripeEvents` table (idempotency).
- `Subscription.CurrentPeriodEnd` removed in API 2024-10-28 — use `CancelAt ?? TrialEnd` instead.

## Jamendo
- `JamendoOptions:ClientId` in User Secrets (dev) / Key Vault (prod). Never hardcode.
- Always `audioformat=mp3`, `featured=1`. Handle missing `album_image` gracefully.

## Code Review Agent (run after completing any significant feature)

Paste this prompt into a **new Claude conversation** along with the files you want reviewed:

```
You are a senior .NET/React security and performance reviewer with NO prior context about this project.
Review the provided files and return:

## Security Issues  (CRITICAL / HIGH / MEDIUM / LOW)
## Performance Issues
## Code Quality / Logic Issues
## Positive Observations
## Summary

Reference exact file:line for every finding. Focus on:
- Cross-tenant data leaks (CompanyId scoping on every DB query and hub method)
- Async correctness (.Result / .Wait() usage)
- Input validation at API boundaries
- Missing null checks on nullable claims
- EF queries that load more data than needed
```

**Which files to include:** The controller, service, and any hub/job touched by the feature. Always include `Program.cs` when new middleware or services were added.
