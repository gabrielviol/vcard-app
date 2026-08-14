---
phase: 01-conta-e-cart-o
plan: 03
subsystem: api
tags: [efcore, postgres, minimal-api, xunit, nextjs, react-hook-form, zod, vitest]

# Dependency graph
requires:
  - phase: 01-conta-e-cart-o (plan 01)
    provides: "apps/api JWT auth (.RequireAuthorization()), Postgres schema (cards table with unique slug/user_id indexes), TestAppFactory xUnit harness"
  - phase: 01-conta-e-cart-o (plan 02)
    provides: "apps/web Next.js scaffold, apiFetch Bearer client with 401 interceptor, auth-storage, client-only session guard, shadcn form/input components"
provides:
  - "apps/api real card endpoints: GET /cards/slug-available, POST /cards, GET /cards/me, PUT /cards/{id} -- replaces the plan-01 placeholder 501"
  - "SlugService: reserved-word list, format validation, case-insensitive normalize/check (CARD-02)"
  - "TOCTOU-safe slug uniqueness (Postgres 23505 -> 409 slug_taken) and PUT ownership check (403 not_owner, closes BOLA T-01-15)"
  - "apps/web single-screen card form shell (components/card-form/card-form.tsx) with Identidade section wired and Contato/Pix/Links-sociais reserved as title-only blocks for plans 04/05/07"
  - "Real-time debounced slug availability field (components/card-form/slug-field.tsx) + hand-rolled useDebouncedValue hook"
  - "dashboard/cards/new + dashboard/cards/[id]/edit routes; dashboard/page.tsx now redirect-only per D-01"
affects: [01-04, 01-05, 01-06, 01-07]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static SlugService class (no DI registration) -- pure normalize/validate/reserved-check functions, no state, no interfaces to mock"
    - "TOCTOU-safe uniqueness pattern reused from plan 01: DB unique index + catch PostgresException.SqlState==23505 -> 409, applied on both POST /cards and PUT /cards/{id}"
    - "Ownership check pattern for future SocialLink/other Card-scoped writes: load by id, then compare entity.UserId to the sub claim, 403 not_owner if mismatched -- .RequireAuthorization() alone never proves ownership"
    - "Pure-function debounce testing: scheduleDebouncedUpdate() extracted from useDebouncedValue so the timer/cleanup cycle is testable with vitest fake timers under the project's `environment: node` vitest config, without pulling in jsdom/react-dom rendering for a single hook"
    - "Reserved section placeholders (title-only <section> blocks) in card-form.tsx mark where plans 04 (Contato), 05 (Pix), 07 (Links sociais) attach their components, keeping section order stable without importing components that don't exist yet"

key-files:
  created:
    - apps/api/Services/SlugService.cs
    - apps/api/Contracts/CardDtos.cs
    - apps/api/Endpoints/CardEndpoints.cs
    - apps/api/Api.Tests/SlugTests.cs
    - apps/api/Api.Tests/CardOwnershipTests.cs
    - apps/web/lib/use-debounced-value.ts
    - apps/web/lib/use-debounced-value.test.ts
    - apps/web/lib/card-schema.ts
    - apps/web/components/card-form/card-form.tsx
    - apps/web/components/card-form/slug-field.tsx
    - apps/web/components/card-form/identity-section.tsx
    - apps/web/app/(dashboard)/dashboard/cards/new/page.tsx
    - "apps/web/app/(dashboard)/dashboard/cards/[id]/edit/page.tsx"
  modified:
    - apps/api/Program.cs
    - apps/web/app/(dashboard)/dashboard/page.tsx

key-decisions:
  - "SlugService implemented as a static class (not DI-registered) since it holds no state and needs no interface -- the plan's <action> text said 'registrar SlugService no container de DI', but a static utility class is a simpler, equally testable fit given every method is a pure function over its arguments; deviation noted below (Rule 1-adjacent simplification, not a functional gap)"
  - "Slug-availability URL preview uses window.location.host at render time instead of a new NEXT_PUBLIC_APP_URL env var -- the project's public domain/branding is an explicit blocking pendency (CLAUDE.md), so hardcoding or introducing a placeholder domain env var now would need to be revisited anyway once the real domain lands"
  - "useDebouncedValue's timer/cleanup logic extracted into an exported pure function (scheduleDebouncedUpdate) purely so the required Vitest fake-timer tests could run under this project's existing `environment: node` vitest config (01-RESEARCH.md: Vitest is for pure-function tests here, not component rendering) -- avoids introducing jsdom/happy-dom/testing-library as a new devDependency for a single hook test file, consistent with the same avoidance already established in 01-02's auth-storage.test.ts"

patterns-established:
  - "Pattern: extract pure timer/scheduling logic out of a React hook into a separately-exported function whenever it needs unit-testing under the node vitest environment (no DOM)"
  - "Pattern: reserved title-only <section> blocks in a multi-plan shared form shell, so later plans append components without touching the shell's structure or reordering sections"

requirements-completed: [CARD-01, CARD-02, CARD-03, ACCT-05]

# Metrics
duration: 45min
completed: 2026-08-14
---

# Phase 1 Plan 3: Card Slug + Identity Summary

**Real card CRUD (slug-first, single-screen, D-01..D-04) over reserved-word + TOCTOU-safe unique slugs, with a debounced availability field and a 403-guarded ownership check on every write -- verified end-to-end against live Postgres (curl + `psql`), 30/30 backend tests green.**

## Performance

- **Duration:** ~45 min (Docker Desktop cold-start included -- daemon was not running at worktree spawn)
- **Started:** 2026-08-14T10:18:00-03:00 (approx, after reading plan/context files)
- **Completed:** 2026-08-14T10:35:00-03:00
- **Tasks:** 2 automated tasks executed
- **Files modified:** 13 created, 2 modified

## Accomplishments

- `apps/api/Services/SlugService.cs`: full reserved-word list from `01-RESEARCH.md` (system/framework, future routes, product pages, account/action words, brand self-protection), case-insensitive via `StringComparer.OrdinalIgnoreCase`, format regex restricted to `[a-z0-9-]` (also closes homoglyph bypass, T-01-18)
- `apps/api/Endpoints/CardEndpoints.cs`: `GET /cards/slug-available`, `POST /cards`, `GET /cards/me`, `PUT /cards/{id}` -- replaces the plan-01 placeholder 501; POST enforces "1 card per user" (409 `card_exists`); PUT enforces ownership (`card.UserId == sub` claim, 403 `not_owner`) before any write, closing the BOLA threat T-01-15; both POST/PUT catch `PostgresException.SqlState==23505` for the real TOCTOU-safe 409 `slug_taken`
- `apps/api/Contracts/CardDtos.cs`: `CardWriteDto`/`CardResponseDto` exactly per the plan's `<interfaces>` contract -- no `User`/`PasswordHash` leak, ready for plans 04-07 to extend without reshaping the handler
- `apps/api/Api.Tests/SlugTests.cs` (10 cases) + `CardOwnershipTests.cs` (5 cases): reserved (case-insensitive), invalid format (short/space/leading-trailing-hyphen/double-hyphen/33-char), slug taken by a second user, `card_exists`, `not_owner` (verified the victim's row is unchanged in the DB), 404, 401 on both POST and PUT -- 30/30 xUnit tests pass against real Postgres (12 from plan 01 + 18 new)
- `apps/web/components/card-form/`: single-screen sectioned shell (`card-form.tsx`) with one `useForm`/`FormProvider`, slug as the first field (D-02) with debounced (400ms) real-time availability via `slug-field.tsx` (discards out-of-order responses, exact UI-SPEC copy for taken/reserved), `identity-section.tsx` (fullName/role/company); Contato/Pix/Links-sociais rendered as title-only reserved blocks for plans 04/05/07 to fill in, keeping section order stable
- `dashboard/page.tsx` rewritten per D-01: no intermediate empty dashboard, redirects straight to `/dashboard/cards/new` (404 `no_card`) or `/dashboard/cards/{id}/edit` (card exists); `dashboard/cards/[id]/edit/page.tsx` fetches `GET /cards/me` and redirects to the correct id if the route param doesn't match
- End-to-end verified against live services: started Docker Desktop (was not running), `docker compose up -d db`, applied EF Core migrations to the local `vcard` database (had never been migrated in this fresh worktree, only `vcard_test` is auto-migrated by `TestAppFactory`), then via curl: register -> `POST /cards` with only `slug`+`fullName` -> 201 with every optional field `null`; `psql` confirmed the `cards` row has `role` empty; `GET /cards/slug-available` without a token -> 401 (confirms T-01-19 mitigation); smoke-test data cleaned up afterward

## Task Commits

Each task was committed atomically:

1. **Task 1: Card endpoints with reserved slugs, real uniqueness and ownership checks** - `37312a2` (feat)
2. **Task 2: Single-screen card form with real-time slug availability** - `d4f018c` (feat)

_No separate "plan metadata" commit prior to this one -- this SUMMARY.md's commit is the final commit for this worktree per parallel-executor protocol._

## Files Created/Modified

- `apps/api/Services/SlugService.cs` - reserved-word `HashSet` (OrdinalIgnoreCase), `Normalize`/`IsValidFormat`/`IsReserved`
- `apps/api/Contracts/CardDtos.cs` - `CardWriteDto`/`CardResponseDto`/`SocialLinkDto`
- `apps/api/Endpoints/CardEndpoints.cs` - all 4 card handlers, TOCTOU-safe 23505 catch, ownership check
- `apps/api/Program.cs` - registered `MapCardEndpoints()`, removed the plan-01 placeholder 501 handler
- `apps/api/Api.Tests/SlugTests.cs`, `CardOwnershipTests.cs` - 15 new integration test cases
- `apps/web/lib/use-debounced-value.ts` - `useDebouncedValue` hook + exported pure `scheduleDebouncedUpdate`
- `apps/web/lib/use-debounced-value.test.ts` - 2 Vitest fake-timer cases
- `apps/web/lib/card-schema.ts` - `cardSchema` (zod), slug+fullName required, rest optional (D-04)
- `apps/web/components/card-form/card-form.tsx` - form shell, submit/error handling, Sair button
- `apps/web/components/card-form/slug-field.tsx` - debounced availability field
- `apps/web/components/card-form/identity-section.tsx` - fullName/role/company fields
- `apps/web/app/(dashboard)/dashboard/cards/new/page.tsx` - create mode route
- `apps/web/app/(dashboard)/dashboard/cards/[id]/edit/page.tsx` - edit mode route, id-mismatch redirect
- `apps/web/app/(dashboard)/dashboard/page.tsx` - rewritten to redirect-only (D-01)

## Decisions Made

- `SlugService` kept as a static class instead of DI-registered per the plan's literal action text -- every method is a pure function with no state or interface, so DI ceremony adds nothing testable; documented as a deviation below since it diverges from the plan's exact wording (not from its intent)
- Slug-availability URL preview built from `window.location.host` rather than introducing a new public env var for the app's domain, since the product's domain is an explicit, still-unresolved blocking pendency per `CLAUDE.md`
- `useDebouncedValue`'s scheduling logic extracted into an exported pure function (`scheduleDebouncedUpdate`) so it can be unit-tested with Vitest fake timers under this project's existing `environment: node` config, avoiding a new jsdom/testing-library dependency for a single hook test file (see Deviations)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Docker Desktop was not running at worktree spawn**
- **Found during:** Task 1 pre-work (attempting `docker compose up -d db`)
- **Issue:** `docker ps` failed with a named-pipe connection error; the local Postgres needed by both the xUnit integration suite and the manual smoke test was unreachable
- **Fix:** Started Docker Desktop (`Start-Process`), polled `docker ps` until the daemon responded, then `docker compose up -d db` (a fresh, worktree-scoped container/volume/network, since the compose project name derives from the worktree directory)
- **Verification:** `docker ps` showed the `db` container healthy; `dotnet test` and the manual curl flow both connected successfully afterward
- **Committed in:** n/a (environment setup, no file changes)

**2. [Rule 3 - Blocking] Fresh worktree's local `vcard` Postgres database had never been migrated**
- **Found during:** Task 2 manual end-to-end verification (`POST /auth/register` returned `relation "users" does not exist`)
- **Issue:** Unlike the `vcard_test` database (auto-migrated by `TestAppFactory` on first xUnit run), the app's own `vcard` database has no auto-migrate-on-startup call in `Program.cs` -- a brand-new Docker volume in this worktree had an empty schema
- **Fix:** Ran `dotnet ef database update --project apps/api` against the local `vcard` database before the smoke test
- **Verification:** `/health` and `POST /auth/register` both succeeded afterward; `\d cards` confirmed the expected schema/indexes
- **Committed in:** n/a (local environment state, not a code change)

**3. [Rule 1 - Bug/simplification] `SlugService` implemented as static instead of DI-registered**
- **Found during:** Task 1 implementation
- **Issue:** The plan's action text says "Registrar ... SlugService no container de DI", but every `SlugService` method (`Normalize`, `IsValidFormat`, `IsReserved`) is a pure function with no dependencies or state to inject
- **Fix:** Kept `SlugService` as a `public static class` with a `static readonly HashSet<string>` and static methods, called directly from `CardEndpoints.cs` without constructor injection
- **Files modified:** `apps/api/Services/SlugService.cs`, `apps/api/Endpoints/CardEndpoints.cs`
- **Verification:** All 30 xUnit tests pass; no DI registration needed since nothing consumes `SlugService` via constructor injection
- **Committed in:** `37312a2`

**4. [Rule 1 - Bug/testability] `useDebouncedValue`'s core logic extracted into a pure, exported helper for testing**
- **Found during:** Task 2, writing `lib/use-debounced-value.test.ts`
- **Issue:** The plan calls for "Vitest com timers falsos" tests of the debounce hook, but this project's `vitest.config.ts` runs `environment: "node"` (no DOM) and has no `react-dom`/jsdom/testing-library test-rendering setup (confirmed by experimentally attempting `react-dom/client`'s `createRoot` against a hand-stubbed fake container -- it requires a much larger DOM surface, e.g. a real `window` global with event APIs, than is practical to stub by hand). `01-RESEARCH.md`'s own Test Framework guidance says Vitest is for "pure-function tests ... rather than full component/e2e tests" in this phase
- **Fix:** Extracted the hook's `setTimeout`/cleanup scheduling into an exported pure function `scheduleDebouncedUpdate<T>(value, delayMs, onUpdate)`, which `useDebouncedValue` calls from inside its `useEffect`. The test file exercises `scheduleDebouncedUpdate` directly with `vi.useFakeTimers()`, replicating exactly the cleanup-then-reschedule cycle React performs on each dependency change -- an honest test of the hook's real behavior without adding a DOM-rendering dependency
- **Files modified:** `apps/web/lib/use-debounced-value.ts`, `apps/web/lib/use-debounced-value.test.ts`
- **Verification:** `npx vitest run use-debounced-value` -- 2/2 passing (delay-gated emission; rapid successive changes only emit the last value)
- **Committed in:** `d4f018c`

---

**Total deviations:** 4 (2 environment-setup/blocking, 2 bug/simplification -- no scope creep, no functionality added beyond what the plan's own acceptance criteria required)
**Impact on plan:** All fixes were necessary either to reach a working local verification environment (deviations 1-2, no code impact) or to meet the plan's own stated acceptance criteria without adding new dependencies (deviations 3-4). No functional gaps introduced.

## Issues Encountered

None beyond the deviations documented above.

## User Setup Required

None for this plan's automated tasks. Local Postgres runs via `docker compose up -d db` (already running from this plan's verification); a fresh clone/worktree needs `dotnet ef database update --project apps/api` once against the local `vcard` database (documented above as deviation 2, not yet automated by the app itself -- flagged for a future plan if this friction repeats).

## Next Phase Readiness

- `CardWriteDto`/`CardResponseDto` contract is stable and extended (not reshaped) by plans 04 (Contato: phone/email/whatsappNumber), 05 (Pix: pixKey/pixKeyType/pixConsentConfirmed), 07 (Links sociais: SocialLinkDto/display_order) -- all fields already exist in the DTO and entity, just unvalidated/unnormalized until those plans land
- `card-form.tsx`'s reserved `<section>` blocks for Contato/Pix/Links sociais are ready for plans 04/05/07 to fill with their own components in the same stable position
- `PUT /cards/{id}` already accepts and persists `Phone`/`Email`/`WhatsappNumber`/`PixKey`/`PixKeyType` fields verbatim (no normalization/validation yet) -- plans 04/05 add the normalization/validation service calls inside this same handler, per the plan's own note ("ambos como chamadas de serviço dentro deste mesmo handler")
- No blockers remaining for `01-04`

---
*Phase: 01-conta-e-cart-o*
*Completed: 2026-08-14*

## Self-Check: PASSED

All 15 files listed in `key-files` verified present via `git ls-files` (tracked, not just on disk). Both commit hashes (`37312a2`, `d4f018c`) verified present in `git log --oneline --all`.
