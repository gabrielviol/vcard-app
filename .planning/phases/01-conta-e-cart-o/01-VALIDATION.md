---
phase: 1
slug: conta-e-cartao
status: approved
nyquist_compliant: true
wave_0_infra_complete: true
wave_0_complete: true
created: 2026-08-13
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework (backend)** | xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) — not yet installed, greenfield |
| **Framework (frontend)** | Vitest — not yet installed, greenfield |
| **Config file** | none — Wave 0 installs |
| **Quick run command (backend)** | `dotnet test apps/api/Api.Tests` |
| **Quick run command (frontend)** | `npx vitest run` |
| **Full suite command** | `dotnet test` (backend) + `npx vitest run` (frontend) |
| **Estimated runtime** | ~30-60s (small greenfield suite) |

---

## Wave 0 Flag Semantics

Two distinct flags, because the test *infrastructure* lands several plans before the last Wave 0 *test file* does. Never set the second one early.

| Flag | Meaning | Set by |
|------|---------|--------|
| `wave_0_infra_complete` | Both test runners exist and execute: xUnit project (`apps/api/Api.Tests`) and Vitest config (`apps/web/vitest.config.ts`) | Plan `01-02`, Task 2 |
| `wave_0_complete` | All 5 items in "Wave 0 Requirements" below physically exist on disk and run green | Plan `01-05`, Task 1 (last Wave-0 test file: `pix-validation.test.ts`) |
| `nyquist_compliant` | Every task in the phase has an `<automated>` verify backed by a test file that now exists | Plan `01-05`, Task 1 (same moment as `wave_0_complete`) |

---

## Sampling Rate

- **After every task commit:** `npx vitest run <affected-file-pattern>` for frontend pure functions (normalization, validation); `dotnet test --filter <affected-class>` for backend changes touching auth/slug/card endpoints.
- **After every plan wave:** Full `dotnet test` + `npx vitest run`.
- **Before `/gsd:verify-work`:** Full suite must be green, plus one manual end-to-end pass (register → create card with slug/pix/whatsapp/photo/social-links → reload → confirm session persists → logout/expire → confirm redirect).
- **Max feedback latency:** 60 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-01-04 | 01-01 | 1 | ACCT-01 | T-01-03 | Register hashes password with BCrypt, never stores plaintext | integration | `dotnet test --filter FullyQualifiedName~RegisterTests` | ✅ | ✅ green |
| 01-01-04 | 01-01 | 1 | ACCT-02 | T-01-01 | Login returns valid JWT for correct credentials | integration | `dotnet test --filter FullyQualifiedName~LoginTests` | ✅ | ✅ green |
| 01-01-04 | 01-01 | 1 | ACCT-04/05 | T-01-02, T-01-07 | Protected endpoints return 401 without/with-expired token | integration | `dotnet test --filter FullyQualifiedName~AuthGuardTests` | ✅ | ✅ green |
| 01-03-01 | 01-03 | 3 | CARD-01/02 | T-01-19 | Slug uniqueness + reserved-word rejection | integration | `dotnet test --filter FullyQualifiedName~SlugTests` | ✅ | ✅ green |
| 01-05-01 | 01-05 | 5 | CARD-06 | T-01-27 | Pix validation per type (CPF check digit, CNPJ alphanumeric, UUID v4) | unit | `npx vitest run pix-validation` | ✅ | ✅ green |
| 01-04-01 | 01-04 | 4 | CARD-08 | — | WhatsApp normalization (9th-digit DDD rule, +55 stripping) | unit | `npx vitest run whatsapp-normalize` | ✅ | ✅ green |
| 01-06-02 | 01-06 | 6 | CARD-09 | — | Photo upload flow | manual | — (smoke test via dashboard UI) | N/A | ✅ green (manual, approved plan 01-06 Task 2 + re-confirmed 01-07 Task 3 step 10) |
| 01-07-01 | 01-07 | 7 | CARD-10 | — | Social link reorder persists `display_order` | integration | `dotnet test --filter FullyQualifiedName~SocialLinkReorderTests` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `apps/api/Api.Tests/Api.Tests.csproj` — xUnit + `Microsoft.AspNetCore.Mvc.Testing` project, none exists yet *(delivered by plan `01-01`, Task 4)*
- [x] `apps/web/vitest.config.ts` — Vitest config, none exists yet *(delivered by plan `01-02`, Task 1)*
- [x] `apps/web/lib/whatsapp-normalize.test.ts`, `apps/web/lib/pix-validation.test.ts` — cover CARD-06/CARD-08 *(delivered by plans `01-04` Task 1 and `01-05` Task 1)*
- [x] `apps/api/Api.Tests/AuthTests.cs`, `SlugTests.cs` — cover ACCT-01/02/04/05, CARD-01/02 *(delivered by plans `01-01` Task 4 and `01-03` Task 1)*
- [x] Framework install: `dotnet add apps/api/Api.Tests package Microsoft.AspNetCore.Mvc.Testing`, `npm install -D vitest` in `apps/web` *(delivered by plans `01-01` Task 4 and `01-02` Task 1)*

> Checklist closes in plan `01-05`, Task 1 — the plan that creates the last file above. Only then may the `wave_0_complete` and `nyquist_compliant` frontmatter flags be flipped to true.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Photo upload via Vercel Blob | CARD-09 | External service, low value to mock/unit test | Upload a photo in the dashboard form, confirm it renders on the card preview with `object-fit: cover` |
| Full onboarding flow (register → card creation → session persistence → expiry redirect) | ACCT-01..05, CARD-01..10 | Cross-cutting end-to-end UX, best verified by hand before phase gate | Register, create card with all fields, reload dashboard (session persists), let token expire or clear it, confirm redirect to `/login` on next protected call |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] Frontmatter flag `wave_0_infra_complete` flipped to true (plan `01-02`)
- [x] Frontmatter flag `wave_0_complete` flipped to true (plan `01-05`, only after all 5 checklist files exist)
- [x] Frontmatter flag `nyquist_compliant` flipped to true (plan `01-05`)

**Approval:** Approved — 2026-08-14, developer ran the full `dotnet test` (90/90) + `npx vitest run` (54/54) suites plus the 13-step manual end-to-end walkthrough (plan `01-07` Task 3) and responded "aprovado". No app-level bugs found; two local dev-environment hiccups during the session (stale `.next` build cache, `apps/api` process needing a restart) were unrelated to code and resolved by restarting the affected process — see `01-07-SUMMARY.md` for details.
