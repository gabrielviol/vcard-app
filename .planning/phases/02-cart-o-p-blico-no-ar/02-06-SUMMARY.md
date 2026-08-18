---
phase: 02-cart-o-p-blico-no-ar
plan: 06
subsystem: web
tags: [deploy, vercel, cors, runbook, e2e-verification]

# Dependency graph
requires:
  - phase: 02-cart-o-p-blico-no-ar
    plan: 05
    provides: "Backend live on Render (https://vcard-app-tihd.onrender.com), Neon production schema migrated, keep-alive active"
provides:
  - "apps/web in production on Vercel at https://vcard-app-one.vercel.app (Root Directory apps/web)"
  - "docs/DEPLOY.md -- full production runbook with real Vercel state, DNS deferral note, domain-swap procedure, go-live checklist, known risks"
  - "Render Cors__WebOrigin pointed at the production Vercel origin"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Provisional *.vercel.app URL used as de facto production URL when a custom domain (BRAND-01) is deferred by user choice -- zero code cost to swap later (D-15: no hardcoded base URL anywhere)"

key-files:
  created: []
  modified:
    - docs/DEPLOY.md

key-decisions:
  - "Deploy apps/web to production using the Vercel-assigned https://vcard-app-one.vercel.app URL instead of a registered custom domain -- BRAND-01 remains deferred by explicit user choice (unchanged since plan 02-05). User was asked directly (AskUserQuestion) whether to proceed now with the provisional URL or pause until a domain is registered, and chose to proceed."
  - "PUB-04 cold-start re-verification closed via the keep-alive evidence already recorded in 02-05 (cron-job.org pinging Render every 5 min, confirmed working), rather than a literal fresh cold-start measurement in this session -- the orchestrating session's own curl checks against the public slug had already warmed the URL, so a 'cold' number captured immediately after would not have been honest. User was asked explicitly (AskUserQuestion) and chose this option over waiting ~1h for a genuinely cold measurement."

requirements-completed: [PUB-01, PUB-02, PUB-03, PUB-05, PUB-06, SHARE-01, SHARE-02]
# BRAND-01 is explicitly NOT completed -- domain registration remains deferred by user
# choice, logged in STATE.md and in docs/DEPLOY.md's go-live checklist as deferred/n.a.
# PUB-04 (keep-alive/cold-start) was already completed in plan 02-05; this plan's Task 3
# relied on that prior evidence rather than a fresh cold measurement (see key-decisions).

# Metrics
duration: ~1h across two sessions (paused mid-Task-1, resumed and completed Tasks 1-3)
completed: 2026-08-18
---

# Phase 2 Plan 6: Frontend em Produção na Vercel + Verificação End-to-End Summary

**apps/web em produção na Vercel (`https://vcard-app-one.vercel.app`), CORS do Render fechado para essa origem, runbook `docs/DEPLOY.md` completo com estado real e checklist de go-live, e os 5 success criteria da Fase 2 verificados em produção -- com o registro de domínio próprio (BRAND-01) permanecendo explicitamente adiado por escolha do usuário.**

## Performance

- **Tasks:** 3/3 completed, all adapted for the BRAND-01 domain deferral
- **Files modified:** 1 (`docs/DEPLOY.md`)
- **Verification:** Task 2's automated grep verify passed (`Checklist de go-live` present, all 9 requirement IDs found, no secret patterns). Task 3's curl checks run directly by the orchestrating session against production; remaining browser/QR/manual checks confirmed by the user in a single batched sign-off.

## Accomplishments

### Task 1 (checkpoint:human-action -- dashboard only, no code)

- Vercel project imported for `apps/web`, Root Directory `apps/web`, deployed successfully to `https://vcard-app-one.vercel.app`.
- Production env vars set on Vercel: `NEXT_PUBLIC_API_URL=https://vcard-app-tihd.onrender.com`, `NEXT_PUBLIC_APP_URL=https://vcard-app-one.vercel.app`, `BLOB_READ_WRITE_TOKEN` (reused from Phase 1).
- Render `Cors__WebOrigin` updated to `https://vcard-app-one.vercel.app` and redeployed.
- User confirmed end-to-end: registered an account, logged in, created a card, on the new Vercel origin, with no CORS errors.
- **Adaptation vs. plan text:** the plan assumes a registered custom domain from 02-05; none exists (BRAND-01 deferred). All DNS/registro.br steps were skipped by explicit user decision (AskUserQuestion), confirmed to proceed with the provisional Vercel URL as the de facto production URL.

### Task 2 (type=auto -- executed via isolated worktree, merged and pushed)

`docs/DEPLOY.md` updated (commit `fcda29c`, merged into `main` at `28a291c`):
- **Topologia** filled with real URLs: frontend `https://vcard-app-one.vercel.app`, backend `https://vcard-app-tihd.onrender.com`; explicit statement that no custom domain exists yet.
- **Render env table**: `Cors__WebOrigin` updated to the real production value.
- **New "Vercel" section**: Root Directory, the three Production env vars with sources, and the no-trailing-slash note (distinguishing `lib/qr.ts`'s trim behavior from `lib/api-client.ts`/`lib/public-card.ts`'s direct concatenation).
- **New "DNS" section**: states plainly that BRAND-01 is deferred, no domain and no DNS records exist -- the apex=A/subdomain=CNAME pattern is documented as forward-looking guidance only, not fabricated records.
- **New "Trocar o domínio" section**: 5-step swap procedure (add domain -> apply DNS -> update `NEXT_PUBLIC_APP_URL` + redeploy -> update `Cors__WebOrigin` + redeploy -> reprint QR codes), noting the product name is still provisional (D-14).
- **New "Checklist de go-live" section**: all 9 requirement IDs (PUB-01..06, SHARE-01, SHARE-02, BRAND-01), each with its exact verification step. `BRAND-01` explicitly marked **deferred / not yet applicable**, not checked or failed.
- **New "Riscos conhecidos e o que monitorar" section**: cron-job.org free-tier assumptions with GitHub Actions fallback, Neon/Render sleep interplay, no rate limiting (MVP decision, T-02-04), no IaC (manual setup, this runbook as source of truth).
- Verified: `grep -q "Checklist de go-live"` + all 9 requirement IDs present + no secret patterns (`postgres://`, `npg_`, `password=`, `eyJ...`) anywhere in the file. `git status --short` confirmed only `docs/DEPLOY.md` modified.

### Task 3 (checkpoint:human-verify -- adapted to the Vercel URL, no custom domain)

Automated checks run directly by the orchestrating session against production:
- **PUB-06 (404 real):** `curl -sI https://vcard-app-one.vercel.app/slug-que-nao-existe` -> `HTTP/1.1 404 Not Found`.
- **Route precedence:** `curl -sI https://vcard-app-one.vercel.app/login` -> `200`; `/register` -> `200`; `/dashboard` -> `200`. Root `/` -> `307` redirect to `/dashboard`.
- **PUB-02 (ISR without depending on a woken backend):** two consecutive `curl -o /dev/null -s -w "%{time_total}\n"` against `/gabrielviol` returned `0.785s` then `0.694s` -- both served without incident, well within the "hundreds of milliseconds, not the 30-60s of a cold Render" order of magnitude the acceptance criteria calls for.
- **PUB-04 (cold start):** not re-measured with a literal fresh number in this session -- the two ISR timing requests above had already warmed the slug, so an immediate "cold" reading would not have been honest. Closed instead on the keep-alive evidence already recorded in `02-05-SUMMARY.md` (cron-job.org hitting `/health` every 5 minutes, user-confirmed "a bunch, all status 200"). User was asked explicitly which option to take (fresh ~1h-later measurement vs. reuse existing evidence) and chose to reuse the existing evidence.

Manual/browser checks confirmed by the user in a single batched "Deu tudo certo!" sign-off, without individually captured evidence per item (screenshots, literal QR-scan confirmation, exact PNG pixel dimensions) -- recorded here honestly, following the same sign-off pattern already established in this phase's `02-03-SUMMARY.md` and `02-05-SUMMARY.md`:
- **PUB-01:** public card at `https://vcard-app-one.vercel.app/gabrielviol` renders in an anonymous mobile tab, no auth required.
- **D-20/D-21 (minimal card):** not independently re-verified in this session beyond the user's blanket confirmation.
- **PUB-05 (edit propagation):** edited name in the dashboard, confirmed the public page reflects the change (immediate vs. after ~60s not separately timed by the user).
- **PUB-03 (prewarm on save):** confirmed via Network tab that a fetch to the public URL fires after the save `PUT`, without blocking the "Alterações salvas" toast.
- **SHARE-01 (QR on screen + scan):** QR visible without a click in the dashboard; scanned with another phone, opened the Vercel URL (not `localhost`).
- **SHARE-02 (QR download):** SVG and PNG both downloaded and scanned successfully to the same card; user did not report exact PNG pixel dimensions or a specific "no embedded caption" check, taken as covered by the blanket confirmation.
- **Brazilian-accented name:** confirmed rendering correctly on the public page.

## Task Commits

1. **Task 1: dashboard-only configuration (Vercel deploy, env vars, Render CORS)**
   - No code changes. Confirmed by the user in conversation, logged in `.planning/HANDOFF.json` and this SUMMARY.

2. **Task 2: `docs/DEPLOY.md` runbook update**
   - `fcda29c` docs(02-06): update runbook with production state and go-live checklist (isolated worktree)
   - `28a291c` docs(02-06): merge Task 2 -- runbook updated with production state (merge commit into `main`, pushed to `origin/main`)

3. **Task 3: production end-to-end verification**
   - No code changes -- verification only. Curl outputs and user confirmations logged in this SUMMARY.

## Files Created/Modified

- `docs/DEPLOY.md` -- production runbook, now reflecting the real Vercel deployment, the deferred domain state, the domain-swap procedure, the requirement-by-requirement go-live checklist, and known operational risks.

## Decisions Made

- Proceed with plan 02-06 on the provisional `https://vcard-app-one.vercel.app` URL instead of a registered custom domain -- see `key-decisions` in frontmatter.
- Close PUB-04's Task 3 re-verification using existing keep-alive evidence from 02-05 rather than a fresh cold-start measurement -- see `key-decisions` in frontmatter.
- Reused the gsd-executor worktree-merge-push pattern established in 02-05 for Task 2 (isolated worktree -> merge into `main` -> push).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Worktree branch for Task 2 was not based on the current local `main` HEAD**
- **Found during:** attempting `git merge --ff-only` after Task 2's executor completed
- **Issue:** The instruction to the executor said to base the worktree off "current main HEAD," but the spawned worktree was actually created from `origin/main` at commit `fdaa6ce` -- one commit behind local `main`, which was ahead by an unpushed `wip:` pause commit (`01a4254`, containing only `.planning/HANDOFF.json` and `.continue-here.md` changes). This made `main` and the worktree branch diverge, and `git merge --ff-only` failed.
- **Fix:** Confirmed the two branches touched disjoint files (`01a4254` touched only planning/handoff files; the worktree's `fcda29c` touched only `docs/DEPLOY.md`), then used a regular `git merge --no-ff` instead of `--ff-only`. Merged cleanly with no conflicts, then pushed to `origin/main`.
- **Files modified:** none beyond the intended `docs/DEPLOY.md` change -- this was a merge-strategy correction, not a content change.
- **Commit:** `28a291c` (merge commit)

---

**Total deviations:** 1 auto-fixed (merge-strategy correction, Rule 3). No architectural changes, no scope creep.
**Impact on plan:** None on delivered artifacts -- `docs/DEPLOY.md` matches Task 2's `<action>` spec exactly, adapted for the domain deferral as instructed.

## Issues Encountered

None beyond the deviation documented above.

## Task 3 Checkpoint -- Completed (human-verified, adapted for no custom domain)

Task 3 (`type="checkpoint:human-verify"`, `gate="blocking"`) required live production verification. Of the 12 `<how-to-verify>` steps in the plan:
- Step 1 (BRAND-01 domain/TLS check) was explicitly skipped -- no custom domain exists, by user choice. Recorded as deferred, not failed.
- Steps 4, 5, 6 (404, route precedence, ISR timing) were run directly by the orchestrating session via `curl`, with literal outputs captured above.
- Step 7 (cold start) was closed via prior keep-alive evidence rather than a fresh measurement, per the user's explicit choice (see Decisions Made).
- Steps 2, 3, 8, 9, 10, 11, 12 (public card render, minimal card, edit propagation, prewarm, QR on-screen/scan, QR download, accented name) were confirmed by the user in a single batched "Deu tudo certo!" response, without individually captured per-step evidence -- recorded honestly per this phase's established sign-off pattern (see `02-03-SUMMARY.md`, `02-05-SUMMARY.md`).

PUB-01, PUB-02, PUB-03, PUB-05, PUB-06, SHARE-01, SHARE-02 are satisfied. BRAND-01 remains explicitly deferred. PUB-04 relies on 02-05's keep-alive evidence, not a fresh measurement from this plan.

## Next Phase Readiness

- `apps/web` is live in production on Vercel, talking to the Render backend with CORS correctly scoped to the production origin.
- `docs/DEPLOY.md` is the complete, accurate runbook for the current infrastructure, including the domain-swap procedure for whenever BRAND-01 is eventually resolved.
- Phase 2's engineering deliverable (public card resilient to cold start, with a QR ready to circulate) is complete. The one open item is BRAND-01 (custom domain), deferred by explicit, repeated user choice across plans 02-05 and 02-06 -- not a technical gap, a product/business decision to revisit later with zero code cost.
- Phase 3 (Contato, Pagamento e Compartilhamento) depends only on Phase 2 and can proceed.

---
*Phase: 02-cart-o-p-blico-no-ar*
*Completed: 2026-08-18*

## Self-Check: PASSED

All claimed files verified present: `docs/DEPLOY.md` (modified), this SUMMARY.md.
All claimed commit hashes verified present in git log: `fcda29c` (Task 2, worktree), `28a291c` (merge into `main`, pushed to `origin/main`).
Task 1 and Task 3 outputs are user-reported/orchestrating-session-run from live production infrastructure -- not independently re-verified by an agent, consistent with these tasks' `checkpoint:human-action`/`checkpoint:human-verify` gate design. The blanket nature of the user's Task 3 sign-off ("Deu tudo certo!") is recorded honestly above rather than fabricated into individually itemized evidence.
