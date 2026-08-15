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

requirements-completed: [PUB-06]

# Metrics
duration: ~25min
completed: 2026-08-15
---

# Phase 2 Plan 3: Cartão Público no Ar — 404 Brandada e Error Boundary Summary

**Branded 404 (`Esse cartão não existe.` + CTA único para `/register`) e error boundary client-side para `/[slug]`, com `PRODUCT_NAME` centralizado em `lib/brand.ts` — status HTTP 404 real e precedência de rota estática confirmados por `curl -I` literal, fechando o bloqueio de STATE.md sobre `/login` vs `/[slug]`.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-08-15T09:30:00-03:00 (approx)
- **Completed:** 2026-08-15T10:20:00-03:00 (approx)
- **Tasks:** 2 of 2 completed (Task 2 is a `checkpoint:human-verify gate="blocking"`, approved by human)
- **Files modified:** 3 (all created)

## Accomplishments
- `PRODUCT_NAME` centralized in `apps/web/lib/brand.ts` (D-14, explicitly documented as provisional pending registro.br/INPI checks)
- `apps/web/app/[slug]/not-found.tsx`: branded 404 mirroring the public card page's shell, single accent CTA to `/register`, copy locked verbatim from `02-UI-SPEC.md`, never interpolates the requested slug (T-02-10)
- `apps/web/app/[slug]/error.tsx`: client error boundary for the `/[slug]` segment, logs the caught error via `console.error` for observability but never renders `error.message`/`error.stack` in the UI (T-02-11) — the underlying `fetchPublicCard` error message contains the backend URL and HTTP status, which must not leak to an anonymous public page
- Verified `npx next typegen` needed to be re-run after a fresh `npm install` in this worktree (no `node_modules` present) to regenerate the `PageProps`/`LayoutProps`/`RouteContext` helper types — not a plan deviation, just worktree environment setup

## Task Commits

Each task was committed atomically:

1. **Task 1: 404 com a cara do produto e error boundary do segmento público** - `f730c19` (feat)

Task 2 (`checkpoint:human-verify`, `gate="blocking"`) is verification-only per the plan — no code changes. Approved by human sign-off; see "Task 2 Verification Results" below.

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

## Verification Results (Task 1 — automated)

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

None — no external service configuration required for this plan.

## Task 2 Verification Results — Checkpoint Approved

**Task 2** (`checkpoint:human-verify`, `gate="blocking"`) required standing up the full local stack and manually walking through 8 verification steps from `02-03-PLAN.md`. This executor was not able to stand up the local stack itself in this worktree (host port `5432` was already bound by another process at the time — see prior deviation note above, retained for the record). The human ran the verification independently and approved the checkpoint, providing literal `curl -I` output for the two critical, plan-mandated checks:

**Step 4 — Status 404 real (PUB-06, item crítico):**
```
$ curl -I http://localhost:3000/slug-que-nao-existe
HTTP/1.1 404 Not Found
Vary: rsc, next-router-state-tree, next-router-prefetch, next-router-segment-prefetch, Accept-Encoding
X-Powered-By: Next.js
Cache-Control: private, no-cache, no-store, max-age=0, must-revalidate
Content-Type: text/html; charset=utf-8
```
First line is `HTTP/1.1 404 Not Found` — confirms the true HTTP status on a nonexistent slug is a real 404, not the streamed-200 regression described in `02-RESEARCH.md` Pitfall 2. **This is the direct evidence backing `PUB-06` completion.**

**Step 6 — Precedência de rotas estáticas:**
```
$ curl -I http://localhost:3000/login
HTTP/1.1 200 OK
Vary: rsc, next-router-state-tree, next-router-prefetch, next-router-segment-prefetch, Accept-Encoding
x-nextjs-cache: HIT
x-nextjs-prerender: 1
X-Powered-By: Next.js
Cache-Control: s-maxage=31536000
Content-Type: text/html; charset=utf-8
```
First line is `HTTP/1.1 200 OK` — confirms `/login` (a static route) resolves to the Phase 1 auth page and is not captured by the `/[slug]` dynamic segment. **This is the direct evidence closing the `.planning/STATE.md` blocker** ("validar localmente a precedência de rotas estáticas vs. dinâmicas (`/login` vs `/[slug]`) antes de confiar nisso estruturalmente").

**Steps 1, 2, 3, 5, 7, 8 — approved by human sign-off without individually captured evidence.** The human replied "approved" after providing the two outputs above, without walking through or pasting per-step output for the remaining six steps (visual card rendering, minimal-card empty state, DOM inspection of vanished empty sections, 404 visual/copy check, error-boundary fallback behavior, ISR propagation timing). This summary does not claim those steps were individually verified with captured evidence — only that the checkpoint as a whole was approved by the human, with the two literal outputs above as the specific evidence provided.

**Resume signal received:** "approved" (with the two `curl -I` outputs pasted above).

## Next Phase Readiness
- Task 1's code (`lib/brand.ts`, `not-found.tsx`, `error.tsx`) is complete, committed (`f730c19`), and passes all automated verification (`tsc`, scoped `eslint`, `vitest`).
- **`PUB-06` marked complete** — backed specifically by the Task 2 step 4 evidence (`curl -I` on a nonexistent slug returns `404` as the literal first line).
- **`.planning/STATE.md`'s open blocker on static-route-vs-`/[slug]` precedence is resolved** — backed specifically by the Task 2 step 6 evidence (`curl -I /login` returns `200`).
- Steps 1/2/3/5/7/8 of Task 2's protocol were covered by the human's overall "approved" sign-off, but do not have individually captured evidence in this summary — noted here for transparency, not treated as a blocker for this plan's completion since the plan's own `<resume-signal>` only requires "approved" or a description of what failed.
- No blockers for 02-04/02-05/02-06 — both Task 1's shipped files and Task 2's critical-path verification (404 status, route precedence) are in place.

---
*Phase: 02-cart-o-p-blico-no-ar*
*Completed: 2026-08-15*

## Self-Check: PASSED

All created files verified present on disk (`apps/web/lib/brand.ts`, `apps/web/app/[slug]/not-found.tsx`, `apps/web/app/[slug]/error.tsx`); Task 1 commit `f730c19` verified present in `git log`.
