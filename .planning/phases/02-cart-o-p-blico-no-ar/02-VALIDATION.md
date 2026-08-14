---
phase: 02
slug: cart-o-p-blico-no-ar
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-08-14
---

# Phase 02 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + `Microsoft.AspNetCore.Mvc.Testing` (backend) / Vitest 4.1.10 (frontend) |
| **Config file** | `apps/api/Api.Tests/Api.Tests.csproj`, `apps/web/vitest.config.ts` |
| **Quick run command (backend)** | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCard` |
| **Quick run command (frontend)** | `cd apps/web && npx vitest run <changed-file>.test.ts` |
| **Full suite command (backend)** | `dotnet test apps/api/Api.Tests` |
| **Full suite command (frontend)** | `cd apps/web && npx vitest run` |
| **Estimated runtime** | ~30-60s combined |

---

## Sampling Rate

- **After every task commit:** Run the targeted quick command for whatever file was just touched (backend or frontend, matching the change).
- **After every plan wave:** Run both full suites — `dotnet test apps/api/Api.Tests` and `cd apps/web && npx vitest run`.
- **Before `/gsd:verify-work`:** Full suite must be green, plus the manual verification steps below (no e2e framework exists in this repo to automate ISR/404-status/domain checks).
- **Max feedback latency:** ~60 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD-01 | TBD | 0 | PUB-01/PUB-05 | V4 | Public GET returns safe DTO (no id/userId/pix/phone leak) without auth header | integration | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCardTests` | ❌ W0 | ⬜ pending |
| TBD-02 | TBD | 0 | PUB-06 | V4 | Public GET returns 404 for nonexistent slug; unreachable via authorized `cards` group | integration | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCardTests` | ❌ W0 | ⬜ pending |
| TBD-03 | TBD | 0 | — (regression guard) | V4 | New public route does NOT require Bearer token | integration | same file, explicit no-auth-header case | ❌ W0 | ⬜ pending |
| TBD-04 | TBD | 1 | PUB-02/PUB-05 | — | `revalidate = 60` exported from `app/[slug]/page.tsx`; fetch has no Authorization header | manual / code-review | — | N/A | ⬜ pending |
| TBD-05 | TBD | 1 | PUB-06 | — | `curl -I` against nonexistent slug in production returns literal HTTP 404 | manual (`curl -I`) | — | N/A | ⬜ pending |
| TBD-06 | TBD | 1 | PUB-03 | — | Save success triggers fire-and-forget pre-warm fetch to public URL | manual devtools / vitest mock | `cd apps/web && npx vitest run components/card-form/card-form.test.tsx` | ❌ W0 | ⬜ pending |
| TBD-07 | TBD | 1 | PUB-04 | — | External cron successfully pings `/health` on schedule | manual (cron-job.org history + Render logs, 24h) | — | N/A | ⬜ pending |
| TBD-08 | TBD | 1 | SHARE-01/SHARE-02 | — | QR route returns valid SVG inline and PNG with `Content-Disposition` only on `?download=1` | integration/unit | `cd apps/web && npx vitest run app/\[slug\]/qr/route.test.ts` | ❌ W0 | ⬜ pending |
| TBD-09 | TBD | 1 | BRAND-01 | — | Domain resolves and serves the Vercel deployment over HTTPS | manual (`curl -I https://vizzo.com.br`) | — | N/A | ⬜ pending |

*Task IDs and wave numbers are placeholders — the planner fills these in with real plan/task IDs once PLAN.md files exist.*

---

## Wave 0 Requirements

- [ ] `apps/api/Api.Tests/PublicCardTests.cs` — covers PUB-01, PUB-05, PUB-06, and the no-auth-required regression guard
- [ ] `apps/web/app/[slug]/qr/route.test.ts` (or equivalent) — covers SHARE-01/SHARE-02 format + Content-Disposition gating
- [ ] `apps/web/components/card-form/card-form.test.tsx` — covers PUB-03's fire-and-forget pre-warm call (mock `fetch`, assert call + assert a rejected pre-warm promise does not surface an error toast)
- [ ] No new test framework/config install needed — both xUnit and Vitest infra already exist and are wired into both apps

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ISR revalidate window serves stale-then-fresh content within 60s | PUB-02 | No e2e framework in this repo to drive real HTTP timing against a deployed instance | Edit a card in the dashboard, hit `/[slug]` immediately (expect old data or fast refresh), wait 60s, hit again (expect new data) |
| 404 page has product identity + CTA, real HTTP 404 status | PUB-06 | Streaming/status-code behavior only observable via real HTTP response inspection | `curl -I https://<domain>/nonexistent-slug` — assert `HTTP/2 404`; visually confirm branded copy + CTA in browser |
| External keep-alive cron actually prevents cold start in practice | PUB-04 | Depends on third-party service execution history over real time, not reproducible in a test run | Check cron-job.org execution log after 24h; separately time a cold GET to `/[slug]` at a random hour and confirm sub-2s response |
| Domain resolves end-to-end after DNS propagation | BRAND-01 | External DNS/infra state, not part of the codebase | `curl -I https://vizzo.com.br` after registro.br + Vercel DNS setup; confirm 200 and valid TLS cert |
| QR downloaded from dashboard actually scans to the correct public URL | SHARE-01/SHARE-02 | Requires a physical/simulated camera scan, not automatable | Download SVG and PNG from the dashboard, scan both with a phone camera, confirm they open `/[slug]` |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
