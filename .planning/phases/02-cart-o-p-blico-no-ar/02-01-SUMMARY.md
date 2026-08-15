---
phase: 02-cart-o-p-blico-no-ar
plan: 01
subsystem: api
tags: [dotnet, minimal-api, ef-core, nextjs, isr, server-components, public-endpoint]

# Dependency graph
requires:
  - phase: 01-conta-e-cart-o
    provides: Card/SocialLink entities, SlugService.Normalize, AvatarPlaceholder component, authenticated CardEndpoints pattern
provides:
  - "GET /public/cards/{slug} — unauthenticated read-only endpoint returning PublicCardDto"
  - "PublicCardDto/PublicSocialLinkDto contracts (apps/api/Contracts/PublicCardDtos.cs)"
  - "app/[slug]/page.tsx — ISR Server Component (revalidate=60) rendering the public card"
  - "lib/public-card.ts — unauthenticated fetch helper safe for Server Component use"
  - "components/public-card/public-card-view.tsx — mobile-first presentational component with D-19/D-21 empty-state rules"
affects: [02-02, 02-03, 02-04, 02-05, 02-06, phase-3-whatsapp-pix]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Public unauthenticated routes registered top-level on `app`, never inside the `.RequireAuthorization()` cards group"
    - "Server Component fetch (no CORS, no Authorization header) as the pattern for anonymous public data reads"
    - "ISR via `export const revalidate = 60`, no generateStaticParams, no segment loading.tsx (preserves true 404 status)"

key-files:
  created:
    - apps/api/Contracts/PublicCardDtos.cs
    - apps/api/Endpoints/PublicCardEndpoints.cs
    - apps/api/Api.Tests/PublicCardTests.cs
    - apps/web/lib/public-card.ts
    - apps/web/app/[slug]/page.tsx
    - apps/web/components/public-card/public-card-view.tsx
  modified:
    - apps/api/Program.cs

key-decisions:
  - "PublicCardDto kept deliberately minimal (Slug/FullName/Role/Company/PhotoUrl/SocialLinks) — Phone/Email/WhatsappNumber/PixKey/PixKeyType/PixConsentConfirmed/IsBranded/timestamps all excluded per T-02-01, widened in Phase 3 alongside the UI that consumes them"
  - "Public route mapped as app.MapGet top-level, positioned between /health and MapAuthEndpoints(), per plan's exact placement instruction"

patterns-established:
  - "Pattern 1 (02-RESEARCH.md): public unauthenticated endpoint MUST live outside the cards route group"
  - "Pattern 2: ISR without generateStaticParams for unbounded dynamic param spaces"

requirements-completed: [PUB-01, PUB-02, PUB-05]

# Metrics
duration: 25min
completed: 2026-08-15
---

# Phase 2 Plan 1: Cartão Público no Ar Summary

**Unauthenticated GET /public/cards/{slug} backend endpoint plus ISR-rendered `/[slug]` public page in Next.js, both built TDD-first and verified to leak zero private fields.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-08-15T09:10:00-03:00 (approx)
- **Completed:** 2026-08-15T09:21:16-03:00
- **Tasks:** 3
- **Files modified:** 7 (6 created, 1 modified)

## Accomplishments
- First unauthenticated read path in the .NET backend, with a full RED→GREEN TDD cycle proving it does not leak `id`/`userId`/`phone`/`email`/`whatsappNumber`/`pixKey`/`pixKeyType`/`pixConsentConfirmed`/`isBranded`/timestamps
- First public Server Component data-fetch pattern in the Next.js app, deliberately separated from the authenticated `api-client.ts`
- Mobile-first public card rendering that implements D-19 (empty sections vanish entirely) and D-21 (name+initials is a complete, non-apologetic state) exactly as specified

## Task Commits

Each task was committed atomically:

1. **Task 1: Testes de integração RED para o endpoint público por slug** - `caedd0e` (test)
2. **Task 2: Endpoint público por slug com DTO mínimo, fora do grupo autorizado** - `dc0031e` (feat)
3. **Task 3: Página pública ISR em /[slug] com renderização esconde-se-vazio** - `55f2265` (feat)

_TDD gate compliance: Task 1 establishes RED (4/5 new tests fail as expected — the 5th, the nonexistent-slug 404 case, coincidentally passes because the endpoint doesn't exist yet, which is itself a 404); Task 2 establishes GREEN (95/95 full xUnit suite green, including all 5 PublicCardTests facts)._

## Files Created/Modified
- `apps/api/Api.Tests/PublicCardTests.cs` - 5 facts covering PUB-01/PUB-05/PUB-06 + no-auth-required regression guard + socialLinks ordering
- `apps/api/Contracts/PublicCardDtos.cs` - `PublicCardDto`/`PublicSocialLinkDto`, narrow-by-design public contract
- `apps/api/Endpoints/PublicCardEndpoints.cs` - `GetBySlugHandler`, AsNoTracking, reuses `SlugService.Normalize`
- `apps/api/Program.cs` - registers `app.MapGet("/public/cards/{slug}", ...)` top-level, outside the `cards` authorized group
- `apps/web/lib/public-card.ts` - `fetchPublicCard`, no Authorization/localStorage/window
- `apps/web/app/[slug]/page.tsx` - Server Component, `revalidate = 60`, calls `notFound()` on null
- `apps/web/components/public-card/public-card-view.tsx` - presentational component, reuses `AvatarPlaceholder`

## Decisions Made
- Followed the plan's DTO scope decision as-is (keep public contract minimal in Phase 2, widen in Phase 3) — no deviation.
- No new dependencies added; reused `SlugService.Normalize`, `AvatarPlaceholder`, and the existing shadcn `Avatar` primitives per the interfaces contract.

## Deviations from Plan

None — plan executed exactly as written. Two environment-setup actions were required but are not deviations from the plan's code/behavior:
- Docker Desktop was not running; started it and brought up the `db` Postgres container (`docker compose up -d db`) so `dotnet test` could connect — required by `TestAppFactory`, not a plan change.
- `apps/web` had no `node_modules` in this worktree; ran `npm install` (no `package.json`/lockfile changes) and `npx next typegen` to generate the `PageProps<"/[slug]">` helper type used by the plan's own interface guidance.

## Issues Encountered
- Running `npx eslint .` across the entire `apps/web` tree (the plan's phase-level `<verification>` command, broader than Task 3's own scoped `<verify>` command) surfaces 3 pre-existing errors and 2 warnings in `components/card-form/{pix-section,slug-field,photo-section}.tsx` (`react-hooks/set-state-in-effect`, `@next/next/no-img-element`). These files are untouched by this plan and the errors predate it — out of scope per the executor's scope-boundary rule (only auto-fix issues directly caused by this task's changes). Task 3's own scoped verify command (`npx eslint "app/[slug]" components/public-card lib/public-card.ts`) passes clean with zero errors/warnings. Not fixed here; flagged for a future cleanup plan if desired.

## User Setup Required

None - no external service configuration required for this plan. (Render/Neon/Vercel/cron-job.org production setup is scoped to later plans in this phase per `02-RESEARCH.md`.)

## Next Phase Readiness
- `GET /public/cards/{slug}` and `/[slug]` are both live and covered by automated tests (95/95 backend, 54/54 frontend, both full suites green) — ready for the QR code plan (02-02) to link against this exact URL shape.
- The manual `<human-check>` step in Task 3's verify block ("abrir http://localhost:3000/{slug} em viewport de celular") was not performed interactively in this autonomous run — all automated checks (tsc, scoped eslint, acceptance-criteria greps) passed; recommend a quick manual pass before this phase's overall `/gsd:verify-work` gate.
- No blockers for 02-02 (QR code) or 02-03 (branded 404), both of which build directly on `/[slug]`'s existence.

---
*Phase: 02-cart-o-p-blico-no-ar*
*Completed: 2026-08-15*
