---
phase: 02-cart-o-p-blico-no-ar
plan: 03
subsystem: ui
tags: [nextjs, app-router, error-boundary, not-found, branding]

# Dependency graph
requires:
  - phase: 02-cart-o-p-blico-no-ar
    provides: "app/[slug]/page.tsx ISR Server Component that calls notFound() on null card (02-01)"
provides:
  - "apps/web/lib/brand.ts — PRODUCT_NAME single source of truth (D-14, provisional)"
  - "apps/web/app/[slug]/not-found.tsx — branded 404 with single register CTA (D-22/D-23, PUB-06)"
  - "apps/web/app/[slug]/error.tsx — client error boundary for /[slug] infra failures (T-02-11)"
affects: [02-04, 02-05, 02-06, phase-3-whatsapp-pix]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Product name centralized in lib/brand.ts — any surface displaying the brand imports PRODUCT_NAME instead of hardcoding it"
    - "error.tsx logs via console.error(error) in useEffect but never renders error.message/error.stack in the UI (T-02-11)"

key-files:
  created:
    - apps/web/lib/brand.ts
    - apps/web/app/[slug]/not-found.tsx
    - apps/web/app/[slug]/error.tsx
  modified: []

key-decisions:
  - "error.tsx uses the `reset` prop (not the newly-stabilized `retry` prop from Next 16.3.0) — locked by the plan's <interfaces> contract; both are valid per node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/error.md"
  - "Comment prose in not-found.tsx/error.tsx deliberately avoids the literal substrings the automated verify greps check for (e.g. avoided writing the literal phrase in prose while still documenting the constraint) so acceptance-criteria greps don't false-positive on explanatory comments"

patterns-established:
  - "Pattern: not-found.tsx/error.tsx for a dynamic public segment mirror the segment's own page shell (min-h-screen bg-zinc-50 + mx-auto max-w-[480px] px-6 py-16) for visual family resemblance (D-22)"

requirements-completed: []  # PUB-06 NOT marked complete — Task 2 (human verification of the true-404 HTTP status) has not been executed. See "Next Phase Readiness" below.

# Metrics
duration: partial (Task 1 only; Task 2 is a pending checkpoint)
completed: 2026-08-15
---

# Phase 2 Plan 3: Cartão Público no Ar — 404 Brandada e Error Boundary Summary

**Branded 404 (`Esse cartão não existe.` + CTA único para `/register`) e error boundary client-side para `/[slug]`, com `PRODUCT_NAME` centralizado em `lib/brand.ts` — Task 1 completa e commitada; Task 2 (verificação humana do status HTTP 404 real) permanece como checkpoint pendente, não resolvido nesta execução.**

## Performance

- **Duration:** ~20 min (Task 1 only)
- **Started:** 2026-08-15T09:30:00-03:00 (approx)
- **Completed (Task 1):** 2026-08-15T09:50:00-03:00 (approx)
- **Tasks:** 1 of 2 completed (Task 2 is a `checkpoint:human-verify gate="blocking"`)
- **Files modified:** 3 (all created)

## Accomplishments
- `PRODUCT_NAME` centralized in `apps/web/lib/brand.ts` (D-14, explicitly documented as provisional pending registro.br/INPI checks)
- `apps/web/app/[slug]/not-found.tsx`: branded 404 mirroring the public card page's shell, single accent CTA to `/register`, copy locked verbatim from `02-UI-SPEC.md`, never interpolates the requested slug (T-02-10)
- `apps/web/app/[slug]/error.tsx`: client error boundary for the `/[slug]` segment, logs the caught error via `console.error` for observability but never renders `error.message`/`error.stack` in the UI (T-02-11) — the underlying `fetchPublicCard` error message contains the backend URL and HTTP status, which must not leak to an anonymous public page
- Verified `npx next typegen` needed to be re-run after a fresh `npm install` in this worktree (no `node_modules` present) to regenerate the `PageProps`/`LayoutProps`/`RouteContext` helper types — not a plan deviation, just worktree environment setup

## Task Commits

Each task was committed atomically:

1. **Task 1: 404 com a cara do produto e error boundary do segmento público** - `f730c19` (feat)

Task 2 (`checkpoint:human-verify`, `gate="blocking"`) was **not executed** — see "Next Phase Readiness" / "Checkpoint Pending" below.

## Files Created/Modified
- `apps/web/lib/brand.ts` - `export const PRODUCT_NAME = "Vizzo"` with D-14 provisional-name comment and the three clean alternatives from `02-CONTEXT.md` (Cartaum, Pixtão, Umtoque)
- `apps/web/app/[slug]/not-found.tsx` - Server Component, no props, mirrors the public card shell, wordmark + headline + body + single indigo CTA to `/register`
- `apps/web/app/[slug]/error.tsx` - `"use client"` error boundary with `{ error, reset }` props, logs via `console.error` in a `useEffect`, renders only the fixed copy + "Tentar de novo" outline button wired to `reset`

## Decisions Made
- Followed the plan's exact interface contract for props/copy/classNames — no deviation in code shape.
- Chose `console.error(error)` inside a `useEffect` (matching Next.js's own documented `error.js` example) instead of leaving the `error` prop completely unused, which would have triggered an `@typescript-eslint/no-unused-vars` lint warning. This satisfies T-02-11 (never rendered on screen) while still giving the caught error somewhere to go for local debugging.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Reworded explanatory comments that accidentally matched the automated verify greps**
- **Found during:** Task 1 verification (`<verify><automated>` block)
- **Issue:** The plan's own acceptance criteria run negative greps (`! grep -q '"use client"' not-found.tsx`, `! grep -qE 'error\.(message|stack)' error.tsx`) against the whole file, including comments. My first draft's Portuguese comments explained the constraints by literally quoting `"use client"` and `error.message`/`error.stack` in prose (e.g. "Server Component ... sem \"use client\"" and "NUNCA renderizar error.message nem error.stack"), which made the negative-match acceptance criteria fail even though no code violated the actual rule.
- **Fix:** Reworded both comments to describe the same constraints without using the literal matched substrings (e.g. "roda no servidor, sem props" instead of quoting `"use client"`; "detalhes internos do objeto de erro (mensagem ou stack trace)" instead of writing `error.message`/`error.stack` literally).
- **Files modified:** `apps/web/app/[slug]/not-found.tsx`, `apps/web/app/[slug]/error.tsx`
- **Verification:** Re-ran the full `<verify><automated>` command; all greps pass (see below).
- **Committed in:** `f730c19` (Task 1 commit — fixed before commit, not a separate commit)

**2. [Rule 3 - Blocking] Regenerated Next.js route helper types after fresh `npm install`**
- **Found during:** Task 1, running `npx tsc --noEmit`
- **Issue:** This worktree had no `apps/web/node_modules` (fresh worktree checkout). After `npm install`, `tsc --noEmit` failed with `Cannot find name 'PageProps'/'LayoutProps'/'RouteContext'` in three pre-existing files (`app/[slug]/page.tsx`, `app/[slug]/qr/route.ts`, `app/layout.tsx`) — these are Next.js-generated ambient types, not present until `next typegen` runs once.
- **Fix:** Ran `npx next typegen` (same step 02-01-SUMMARY.md already documented needing once before). Zero code changes — purely regenerates `.next/types/`.
- **Files modified:** none (generated types are gitignored, not committed)
- **Verification:** `npx tsc --noEmit` clean afterward.
- **Committed in:** N/A (no files to commit — build artifact only)

---

**Total deviations:** 2 auto-fixed (1 bug, 1 blocking/environment). No scope creep — both were required to make Task 1's own verify command pass.

## Verification Results (Task 1 only)

```
cd apps/web && npx tsc --noEmit           # exit 0, clean
npx eslint "app/[slug]" lib/brand.ts      # exit 0, 0 errors, 0 warnings
npx vitest run                            # 7 files, 71 tests passed
```

Acceptance criteria greps (all passed):
- `Esse cartão não existe.` / body copy / `Criar meu cartão` present verbatim in `not-found.tsx`
- `grep -c 'bg-indigo-600' not-found.tsx` → 1
- `grep -c 'from "@/lib/brand"' not-found.tsx` → 1
- No `app/[slug]/loading.tsx`, no `app/loading.tsx`, zero `Suspense` occurrences under `app/[slug]`
- `error.tsx` starts with `"use client"`, contains no rendered `error.message`/`error.stack`

## Issues Encountered
None beyond the two auto-fixes documented above.

## User Setup Required

None for Task 1 (no external services). **Task 2 requires the developer to run the local verification protocol described below.**

## Checkpoint Pending — Task 2 Not Executed

**Task 2** (`checkpoint:human-verify`, `gate="blocking"`) requires standing up the full local stack (`docker compose up -d`, `dotnet run --project apps/api`, `apps/web` production build + start) and manually walking through 8 verification steps in a real browser, including a `curl -I` check that the true HTTP status on a nonexistent slug is `404` (not `200`, which would indicate the Pitfall-2 streaming regression) and that `/login`/`/register`/`/dashboard` still resolve to `200` (route-precedence check, resolves the open `.planning/STATE.md` blocker).

**Why this executor did not attempt it:** at the time of this run, host port `5432` (Postgres) was already bound by another process — `netstat` showed an active listener on `0.0.0.0:5432` before any command in this session touched Docker. This worktree's `docker-compose.yml` maps `5432:5432` on the host; starting `docker compose up -d` here would either fail outright (port already allocated) or, if it somehow succeeded, risk interfering with a concurrent process (a sibling parallel-executor worktree, or the user's own dev environment) also depending on that port. Given Phase 2 wave 2 runs multiple plans in parallel worktrees, and the plan document itself frames this task as requiring live browser interaction and DOM inspection across 8 steps that are inherently outside CLI-only automation, this was deferred rather than risking a port collision with concurrent execution.

**What remains for Task 2 (verbatim from `02-03-PLAN.md`):**
1. Cartão publicado — visual check on `/{slug}`, mobile viewport
2. Cartão mínimo (D-20/D-21) — only initials+name render, no empty-state UI
3. Seções vazias somem (D-19) — DOM inspection confirms no empty social-links container
4. **Status 404 real (PUB-06, item crítico)** — `curl -I http://localhost:3000/slug-que-nao-existe` must return `HTTP/1.1 404 Not Found` as the first line
5. Visual da 404 (D-22/D-23) — wordmark, headline, body, single indigo CTA to `/register`, no secondary link
6. **Precedência de rotas estáticas** — `/login`, `/register`, `/dashboard` still resolve to the Phase 1 pages, `curl -I http://localhost:3000/login` returns `200` — resolves the `.planning/STATE.md` blocker
7. Error boundary — kill the backend, wait for the 60s ISR window, confirm the fixed error copy renders with no leaked stack/URL/status
8. ISR reflects edit (PUB-05) — edit name in dashboard, confirm propagation within 60s

**Resume signal (from the plan):** "Responda 'approved' ou descreva qual dos 8 passos falhou, com o output observado."

## Next Phase Readiness
- Task 1's code (`lib/brand.ts`, `not-found.tsx`, `error.tsx`) is complete, committed (`f730c19`), and passes all automated verification (`tsc`, scoped `eslint`, `vitest`).
- **`PUB-06` is NOT marked complete in this summary's frontmatter** — the requirement's acceptance criteria explicitly require the true-404 HTTP status check (Task 2 step 4), which was not executed.
- **`.planning/STATE.md`'s open blocker** ("validar localmente a precedência de rotas estáticas vs. dinâmicas antes de confiar nisso estruturalmente") remains **unresolved** — Task 2 step 6 is the evidence that would close it.
- No code-level blockers for 02-04/02-05/02-06 — they don't depend on Task 2's manual verification outcome, only on Task 1's shipped files existing, which they do.
- Recommend running Task 2's 8-step protocol in an environment where ports 5432/5153/3000 are free (e.g., after this worktree merges into the integration branch, or in the user's own local environment) before considering `PUB-06` and the STATE.md blocker closed.

---
*Phase: 02-cart-o-p-blico-no-ar*
*Completed (Task 1 only): 2026-08-15*
