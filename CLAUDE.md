# Tunora — Claude Project Instructions

## Current Phase: Polish
The core product is complete and live. All 6 phases done. Focus now is:
- UX refinement (better error messages, loading states, empty states, edge cases)
- Marketing / landing website (not yet built)
- Hardening (input validation, security review, graceful degradation)
- Performance (EF query review, SignalR efficiency, Jamendo caching)

When implementing anything new, ask: "Does this belong in polish, or is it scope creep?"

---

## What Is Tunora
SaaS for in-store background music. Companies manage **Instances** (locations) from a React dashboard. In-store players run in Chrome Kiosk mode. Real-time control via SignalR. Music from Jamendo. Payments via Stripe.

## Project Structure
```
Tunora.Core           → Entities, interfaces, enums (no external deps)
Tunora.Infrastructure → EF Core, Quartz.NET, JamendoClient, services
Tunora.API            → ASP.NET Core 10, SignalR hub, JWT, rate limiting
Tunora.Web            → React 18 + TS dashboard (Tailwind v4, Spotify dark theme)
Tunora.Player         → React 18 Chrome kiosk (minimal UI)
```
Local DB: `Server=localhost\SQLEXPRESS;Database=TunoraDb;Trusted_Connection=True;TrustServerCertificate=True;`

## Deployment
- API: Azure App Service — `tunora-api-atc3gnd2e9fvcsa4.centralus-01.azurewebsites.net` (Free tier — cold starts expected)
- Dashboard: Netlify — `https://graceful-crepe-638445.netlify.app`
- Player: Netlify — `https://vermillion-tartufo-7d748b.netlify.app`
- CI/CD: GitHub Actions → push to `main` auto-deploys (api-deploy.yml, web-deploy.yml)
- CORS: `CORS_ORIGINS` env var on Azure App Service (comma-separated)
- Azure SWA abandoned (MIME issues) — Netlify used for both React apps

---

## C# Rules
- `net10.0`, nullable refs enabled. No domain logic in controllers — services only.
- No Repository pattern — use `ApplicationDbContext` directly in services.
- All EF queries async. `CompanyId` always from JWT claims, never request body.
- Use `record` for DTOs and SignalR messages. Log via `ILogger<T>` only.
- Passwords: BCrypt cost 12. ConnectionKey: `Convert.ToHexString(RandomNumberGenerator.GetBytes(32))`.
- Custom exceptions in `Tunora.Core.Exceptions` — handled by `ExceptionHandlingMiddleware`.

## EF Migrations
```bash
dotnet ef migrations add <Name> --project src/Tunora.Infrastructure --startup-project src/Tunora.API
dotnet ef database update        --project src/Tunora.Infrastructure --startup-project src/Tunora.API
```

## Key Technical Decisions
- `MapInboundClaims = false` — JWT claims stay as short names (`sub`, `role`, `companyId`)
- `ApiControllerBase.UserId` reads `"sub"`; `CompanyId` reads `"companyId"`
- Hub auth: Kiosk checks JWT `instanceId` claim; Dashboard does async DB lookup
- Quartz: RAMJobStore, single `ChannelSwitchJob` (durable), reloaded on startup via `ScheduleLoaderService`
- DaysOfWeek stored as JSON nvarchar(100) — always filter in memory after `ToListAsync` (EF can't translate)
- Stripe.net 50.x: use `CancelAt ?? TrialEnd` (not `CurrentPeriodEnd` — removed)
- Refresh tokens: SHA-256 hashed in DB, raw token returned to client
- `AuthResult` factory: `.Ok(access, refresh)` and `.Fail(error)`
- Zustand storage key: `tunora-auth`

## TypeScript / React Rules
- Strict mode. Functional components only (except `ErrorBoundary`).
- Global state: Zustand (`/src/store/`). Server state: TanStack Query. No manual fetch in components.
- All API calls via `/src/api/client.ts` (Axios + JWT interceptor).
- Tailwind v4 only — config in `@theme` block in `index.css`.

## SignalR Groups
| Group | Members | Purpose |
|-------|---------|---------|
| `instance-{id}` | Dashboard + Player | Commands → player |
| `dashboard-{id}` | Dashboard | Player state → dashboard |

## Subscription Tiers
| Tier | Instances | Channels/Instance | Scheduling |
|------|-----------|------------------|------------|
| Starter | 1 | 3 | No |
| Professional | 5 | 5 | Yes |
| Business | 20 | 5 | Yes |
| Enterprise | Unlimited | 5 | Yes |

Enforce server-side via `TierLimitService`. Never trust the client.

## Stripe
- Verify webhook signature. Store event IDs in `StripeEvents` (idempotency).
- `Subscription.CurrentPeriodEnd` removed in API 2024-10-28 — use `CancelAt ?? TrialEnd`.

## Jamendo
- `JamendoOptions:ClientId` in User Secrets (dev). Never hardcode.
- Always `audioformat=mp3`, `featured=1`. Handle missing `album_image` gracefully.

---

## Recommended Workflow for Every Feature

1. **Plan first** — use `/plan` (plan mode) or write a spec before asking Claude to code
2. **Implement** — Claude makes all changes, builds must be clean before moving on
3. **Self-review** — ask Claude: *"Review what you just built for bugs, edge cases, and security issues"*
4. **Adversarial review** — paste files into a new conversation using the Code Review Agent below
5. **Simplify** — run `/simplify` on changed files

---

## Code Review Agent
After any significant feature, open a **new Claude conversation** and paste:

```
You are a senior .NET/React reviewer with NO prior context. Review the files below and return:
## Security Issues (CRITICAL/HIGH/MEDIUM/LOW)
## Performance Issues
## Code Quality / Logic Issues
## Summary

Focus on: cross-tenant data leaks (CompanyId scoping), async correctness, input validation,
null checks on claims, EF over-fetching, missing error handling.
Reference exact file:line for every finding.
```
Include the controller, service, and any hub/job touched.

---

## Path Fixes (bash shell)
```bash
export PATH="$PATH:/c/Program Files/nodejs"         # npm
export PATH="$PATH:/c/Users/xande/.dotnet/tools"    # dotnet ef
```
