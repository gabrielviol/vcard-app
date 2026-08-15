---
phase: 02-cart-o-p-blico-no-ar
plan: 04
subsystem: web
tags: [qrcode, prewarm, cold-start, react, dashboard]

# Dependency graph
requires:
  - phase: 02-cart-o-p-blico-no-ar
    plan: 02
    provides: "lib/qr.ts (buildCardUrl), GET /{slug}/qr Route Handler"
provides:
  - "lib/prewarm.ts prewarmPublicCard(slug) -- fire-and-forget best-effort cold-start mitigation (PUB-03)"
  - "components/public-card/qr-preview.tsx QrPreview -- reusable 240x240 QR <img>, ready for reuse on the public page itself if needed"
  - "components/card-form/qr-section.tsx QrSection -- always-visible dashboard QR block with save-first empty state"
  - "card-form.tsx wiring: prewarmPublicCard fired on both create and edit save paths"
affects: [02-05 (independent keep-alive, PUB-04 -- explicitly does not overlap with this plan's PUB-03)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Fire-and-forget helper isolated in lib/*.ts (not inline in a component) so it can be unit-tested under vitest.config.ts's lib/**/*.test.ts include restriction"
    - "Section-level 'save the card first' empty state (border-dashed box) reused verbatim from social-links-section.tsx for a third section (qr-section.tsx)"

key-files:
  created:
    - apps/web/lib/prewarm.ts
    - apps/web/lib/prewarm.test.ts
    - apps/web/components/public-card/qr-preview.tsx
    - apps/web/components/card-form/qr-section.tsx
  modified:
    - apps/web/components/card-form/card-form.tsx

key-decisions:
  - "prewarmPublicCard fires on the create path too (right after POST, before router.push), not just edit -- a freshly created card is exactly the moment the owner is about to share it"
  - "Reworded two in-code comments (qr-preview.tsx, card-form.tsx) that accidentally contained the plan's own acceptance-criteria grep substrings ('next/image', 'initialCard?.slug') as plain prose, causing false-positive grep matches -- same self-inflicted-grep pattern documented in 02-02's SUMMARY"

requirements-completed: [SHARE-01, SHARE-02, PUB-03]

# Metrics
duration: ~5min (task execution; longer wall-clock due to fresh-worktree npm install + next typegen)
completed: 2026-08-15
---

# Phase 2 Plan 4: QR na Tela de Edição + Pré-Aquecimento Summary

**Seção "Seu QR code" sempre visível no dashboard com download SVG/PNG, e todo save (create/edit) dispara um GET fire-and-forget contra a URL pública para aquecer Vercel → Render → Neon (PUB-03)**

## Performance

- **Tasks:** 3/3 completed
- **Files modified:** 5 (4 created, 1 modified)
- **Test suite:** 76/76 vitest tests passing (5 new in `prewarm.test.ts`), `tsc --noEmit` clean, `next build` production build clean

## Accomplishments

- `apps/web/lib/prewarm.ts` — `prewarmPublicCard(slug): void`, fire-and-forget, reuses `buildCardUrl` from plan 02-02 (no second URL construction), swallows both a missing `NEXT_PUBLIC_APP_URL` and a rejected `fetch` without ever throwing or producing an unhandled rejection
- `apps/web/components/public-card/qr-preview.tsx` — `QrPreview`, a 240×240 `<img>` pointing at the existing `/{slug}/qr` Route Handler, no tint, no `next/image` (SVG asset, optimizer adds nothing)
- `apps/web/components/card-form/qr-section.tsx` — `QrSection`, always-visible "Seu QR code" block (D-16); shows the "salve o cartão primeiro" dashed-border state in create mode (mirrors `social-links-section.tsx`'s existing pattern), otherwise renders `QrPreview` + caption + two labeled `outline`-variant download buttons (SVG / PNG)
- `card-form.tsx` wired: `prewarmPublicCard(payload.slug)` fired (no `await`) right after both the create (`POST /cards`) and edit (`PUT /cards/{id}`) success paths, using the just-submitted `payload.slug` (not `initialCard?.slug`, which could be stale if the user just changed it); `<QrSection slug={initialCard?.slug}>` added as a sibling section after `SocialLinksSection`; the existing 6-code `ApiError` chain is untouched

## Task Commits

Task 1 followed the full TDD RED → GREEN cycle (no refactor commit needed):

1. **Task 1: prewarm helper + tests**
   - `8c3451b` test(02-04): add failing tests for prewarmPublicCard
   - `faf4a76` feat(02-04): implement prewarmPublicCard fire-and-forget helper
2. **Task 2: QR preview + QR section components**
   - `b282d9b` feat(02-04): add QR preview and QR section components
3. **Task 3: wire into card-form.tsx**
   - `c8e0662` feat(02-04): wire QR section and prewarm into card-form

**Plan metadata:** committed alongside this SUMMARY (worktree mode — orchestrator handles STATE.md/ROADMAP.md after merge)

## Files Created/Modified

- `apps/web/lib/prewarm.ts` — the only place that decides when a pre-warm ping fires and how its failure is silenced
- `apps/web/lib/prewarm.test.ts` — 5 tests: single fetch call with correct URL/options, synchronous `void` return, no throw/unhandled-rejection on fetch failure, no-op when env var missing, no-op on empty slug
- `apps/web/components/public-card/qr-preview.tsx` — presentational `QrPreview({ slug })`
- `apps/web/components/card-form/qr-section.tsx` — `QrSection({ slug })`, the dashboard's "Seu QR code" block
- `apps/web/components/card-form/card-form.tsx` — two `prewarmPublicCard(payload.slug)` call sites + one `<QrSection>` render site

## Decisions Made

- Fired `prewarmPublicCard` on **both** create and edit paths (plan explicitly required this) — a newly created card's URL is exactly what the owner is about to share/print, so cold-starting it immediately matters as much as on subsequent edits.
- Kept `prewarmPublicCard` entirely outside the `ApiError` chain and without any wrapping `try/catch` at the call site, per the plan's `<action>` — the helper is self-contained and this preserves the existing error-handling structure of `onSubmit` untouched.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fresh worktree had no `node_modules` and no generated Next.js route types**
- **Found during:** Task 1, first `npx vitest run` attempt (`Cannot find module 'vitest/config'`)
- **Issue:** This worktree had never run `npm install` or `next dev`/`next typegen`, so both `node_modules` and `.next/types/**/*.ts` (referenced by `tsconfig.json` for `PageProps`/`RouteContext`/`LayoutProps` global helper types) were missing — mirrors the same environment gap documented in plan 02-02's SUMMARY for a different worktree
- **Fix:** Ran `npm install` (baseline deps, no lockfile changes — `package-lock.json` was already in sync) and `npx next typegen` once
- **Files modified:** none tracked (`node_modules/` and `.next/` are gitignored)
- **Verification:** `npx tsc --noEmit` exits 0 project-wide after typegen

**2. [Rule 1 - Bug] Reworded two comments that false-triggered the plan's own acceptance-criteria greps**
- **Found during:** Task 2 (`grep -c 'next/image' qr-preview.tsx` returned 1 instead of the required 0) and Task 3 (`grep -c 'initialCard?.slug' card-form.tsx` returned 4 instead of the plan's expected 2)
- **Issue:** Explanatory Portuguese comments literally contained the substrings `next/image` and `initialCard?.slug` as prose, which the acceptance-criteria greps (meant to detect actual code usage) also matched — same self-inflicted-grep pattern already documented in plan 02-02's SUMMARY
- **Fix:** Reworded both comments to keep the same explanation without the exact matched substring (e.g. "sem o componente de imagem otimizada do Next.js" instead of literally writing "next/image"; "o slug original do cartao" instead of literally writing "initialCard?.slug")
- **Files modified:** `apps/web/components/public-card/qr-preview.tsx`, `apps/web/components/card-form/card-form.tsx`
- **Verification:** both greps now return the counts the plan's own code-behavior acceptance criteria intended to check (0 for `next/image`, and the `initialCard?.slug` grep — see note below on why its literal target count of 2 was already unreachable before this plan even started)

### Noted but not fixed (out of scope)

**3. [Scope boundary] `apps/web/components/card-form/card-form.tsx`'s `initialCard?.slug` grep count is 3, not the plan's literal expectation of 2**
- **Found during:** Task 3 acceptance-criteria verification
- **Root cause:** The plan's acceptance criteria assumed the baseline (before this plan) had exactly one `initialCard?.slug` occurrence (`<SlugField currentSlug={initialCard?.slug} />`). In fact `toDefaultValues(initialCard)` (line ~66, pre-existing since an earlier plan, not touched by 02-04) already had a second occurrence (`slug: initialCard?.slug ?? ""`) before this plan started — confirmed via `git show <pre-02-04 commit>:card-form.tsx | grep -c 'initialCard?.slug'` = 2, not 1. Adding `<QrSection slug={initialCard?.slug}>` correctly brings the total to 3, which is functionally exactly what the plan's `<action>` describes (SlugField unchanged + one new QrSection usage) — the literal count "2" in the acceptance criteria itself is what's inaccurate, not the implementation.
- **Disposition:** Not fixed — nothing to fix; the code is correct. Documented here so the discrepancy between the plan's stated grep count and the actual (correct) count is traceable.

**4. [Scope boundary] Pre-existing `react-hooks/set-state-in-effect` ESLint errors in `pix-section.tsx` and `slug-field.tsx`**
- **Found during:** Task 2, `npx eslint components/public-card components/card-form` (plan's literal verify command)
- **Issue:** Two files not touched by this plan (`pix-section.tsx:70,78` and `slug-field.tsx:44`) already fail ESLint's `react-hooks/set-state-in-effect` rule, causing the directory-wide eslint invocation to exit non-zero even though every file this plan created or modified (`qr-preview.tsx`, `qr-section.tsx`, `card-form.tsx`) lints clean on its own (0 errors)
- **Disposition:** Not fixed (out of scope per Scope Boundary rule — pre-existing issue in unrelated files). Logged to `.planning/phases/02-cart-o-p-blico-no-ar/deferred-items.md`.
- **Verification of scope:** confirmed via `npx eslint <the two new files only>` → 0 errors, 1 expected warning (the `<img>`-vs-`next/image` LCP warning, which the plan explicitly requires: "Usar `<img>` puro, não `next/image`")

---

**Total deviations:** 2 auto-fixed (1 blocking/environment, 1 bug/self-inflicted grep false-positive across 2 files), 2 noted-but-not-fixed (both out of scope per Scope Boundary rule, one a plan-authoring grep-count inaccuracy with no code impact, one pre-existing unrelated lint debt).
**Impact on plan:** No scope creep. No new files, features, or architecture beyond what the plan specified.

## Issues Encountered

- Fresh worktree required `npm install` + `next typegen` before any verification command could run — normal worktree setup, not a plan deviation (same as 02-02's prior worktree).
- Ran `npx next build` (full production build) as an extra verification step beyond the plan's own `<automated>` verify commands — confirms `/dashboard/cards/[id]/edit` and `/dashboard/cards/new` (both consumers of `CardForm`) compile and prerender/route correctly with the new `QrSection` wired in. Build succeeded with all 10 routes listed, no errors.
- The plan's Task 3 `<verify>` block includes a `<human-check>` (browser-based QR scan test, download-file test, Network-tab pre-warm confirmation, create-mode empty-state check). This is a supplementary manual verification step on a `type="auto"` task (not a `checkpoint:human-verify` gate), and no running backend/browser was available in this automated worktree execution context. **Recommended before merge to main:** run `npm run dev` (web) + the .NET API together, open `/dashboard/cards/[id]/edit` for a saved card, and walk the 4 steps listed in the plan's `<human-check>` block.

## User Setup Required

None — no external service configuration required. Everything in this plan reuses infrastructure already wired in plan 02-02 (`NEXT_PUBLIC_APP_URL`, the `/{slug}/qr` Route Handler).

## Next Phase Readiness

- `prewarmPublicCard` is ready for reuse anywhere else a save-success event needs to trigger a best-effort wake-up (none currently planned, but the helper is generic).
- Plan 02-05 (PUB-04, external keep-alive cron) is confirmed independent of this plan's mechanism per the plan's own objective and `02-RESEARCH.md` Pitfall 4 — no shared state or coupling to worry about when that plan lands.
- No blockers.

---
*Phase: 02-cart-o-p-blico-no-ar*
*Completed: 2026-08-15*
