# Tunora

A commercial music streaming SaaS platform for retail stores. Companies manage multiple store locations from a web dashboard. Each location runs a browser-based kiosk player. Music channels switch in real time via SignalR.

Built as a portfolio project demonstrating AI-assisted development with [Claude Code](https://claude.ai/code) by Anthropic.

---

## What It Does

- **Company dashboard** — Create and manage music channels, schedule automatic channel switches by time/day, view playback analytics
- **In-store player** — Runs in Chrome kiosk mode, streams licensed music from [Jamendo](https://www.jamendo.com/), updates in real time when the dashboard changes channels
- **Subscription tiers** — Starter / Professional / Business plans enforced server-side, powered by Stripe
- **Multi-location** — Each subscription can manage multiple store instances

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 10, EF Core 10, SignalR, Quartz.NET |
| Database | SQL Server |
| Auth | JWT (access + refresh tokens), BCrypt |
| Payments | Stripe |
| Music | Jamendo API |
| Dashboard | React 19, TypeScript, Vite, Tailwind CSS v4, Zustand, TanStack Query |
| Player | React 19, TypeScript, Vite, Zustand |
| Hosting | Azure App Service (API) + Azure Static Web Apps (frontend) |
| CI/CD | GitHub Actions |

---

## Running Locally

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server Express
- Node.js 20+
- A [Jamendo developer account](https://devportal.jamendo.com/) (free) for the `ClientId`

### API

```bash
cd src/Tunora.API

# Set user secrets
dotnet user-secrets set "JamendoOptions:ClientId" "your_client_id"
dotnet user-secrets set "StripeOptions:SecretKey" "sk_test_..."

# Create database and apply migrations
dotnet ef database update --project ../Tunora.Infrastructure

# Run
dotnet run
```

### Dashboard

```bash
cd src/Tunora.Web
npm install
npm run dev   # http://localhost:5173
```

### Player

```bash
cd src/Tunora.Player
npm install
npm run dev   # http://localhost:5174
```

---

## Project Structure

```
Tunora/
├── src/
│   ├── Tunora.Core/            # Domain entities, interfaces, enums
│   ├── Tunora.Infrastructure/  # EF Core, services, Jamendo/Stripe integrations
│   ├── Tunora.API/             # ASP.NET Core REST API + SignalR hub
│   ├── Tunora.Web/             # React dashboard (company admin)
│   └── Tunora.Player/          # React kiosk player (in-store)
├── tests/
│   ├── Tunora.API.Tests/
│   └── Tunora.Core.Tests/
└── .github/workflows/          # CI/CD — auto-deploy to Azure on push to main
```

---

## AI-Assisted Development

This project was built iteratively using Claude Code, Anthropic's AI development tool. The workflow used:

- **Parallel agents** for codebase exploration and design
- **Iterative review** — implement → self-review → adversarial review → simplify
- **CLAUDE.md** for project-wide instructions and conventions
- **Skills and hooks** for automated quality checks

This demonstrates how a solo developer can build a production-grade SaaS platform with AI assistance, covering architecture, authentication, real-time features, billing, and cloud deployment.
