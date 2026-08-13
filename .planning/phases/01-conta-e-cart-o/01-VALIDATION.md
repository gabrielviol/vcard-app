---
phase: 1
slug: conta-e-cartao
status: draft
nyquist_compliant: false
wave_0_complete: false
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

## Sampling Rate

- **After every task commit:** `npx vitest run <affected-file-pattern>` for frontend pure functions (normalization, validation); `dotnet test --filter <affected-class>` for backend changes touching auth/slug/card endpoints.
- **After every plan wave:** Full `dotnet test` + `npx vitest run`.
- **Before `/gsd:verify-work`:** Full suite must be green, plus one manual end-to-end pass (register → create card with slug/pix/whatsapp/photo/social-links → reload → confirm session persists → logout/expire → confirm redirect).
- **Max feedback latency:** 60 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-xx-xx | TBD | 0 | ACCT-01 | — | Register hashes password with BCrypt, never stores plaintext | integration | `dotnet test --filter FullyQualifiedName~RegisterTests` | ❌ W0 | ⬜ pending |
| 01-xx-xx | TBD | 0 | ACCT-02 | — | Login returns valid JWT for correct credentials | integration | `dotnet test --filter FullyQualifiedName~LoginTests` | ❌ W0 | ⬜ pending |
| 01-xx-xx | TBD | 0 | ACCT-04/05 | — | Protected endpoints return 401 without/with-expired token | integration | `dotnet test --filter FullyQualifiedName~AuthGuardTests` | ❌ W0 | ⬜ pending |
| 01-xx-xx | TBD | 0 | CARD-01/02 | — | Slug uniqueness + reserved-word rejection | integration | `dotnet test --filter FullyQualifiedName~SlugTests` | ❌ W0 | ⬜ pending |
| 01-xx-xx | TBD | 0 | CARD-06 | — | Pix validation per type (CPF check digit, CNPJ alphanumeric, UUID v4) | unit | `npx vitest run pix-validation` | ❌ W0 | ⬜ pending |
| 01-xx-xx | TBD | 0 | CARD-08 | — | WhatsApp normalization (9th-digit DDD rule, +55 stripping) | unit | `npx vitest run whatsapp-normalize` | ❌ W0 | ⬜ pending |
| 01-xx-xx | TBD | 0 | CARD-09 | — | Photo upload flow | manual | — (smoke test via dashboard UI) | N/A | ⬜ pending |
| 01-xx-xx | TBD | 0 | CARD-10 | — | Social link reorder persists `display_order` | integration | `dotnet test --filter FullyQualifiedName~SocialLinkReorderTests` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `apps/api/Api.Tests/Api.Tests.csproj` — xUnit + `Microsoft.AspNetCore.Mvc.Testing` project, none exists yet
- [ ] `apps/web/vitest.config.ts` — Vitest config, none exists yet
- [ ] `apps/web/lib/whatsapp-normalize.test.ts`, `apps/web/lib/pix-validation.test.ts` — cover CARD-06/CARD-08
- [ ] `apps/api/Api.Tests/AuthTests.cs`, `SlugTests.cs` — cover ACCT-01/02/04/05, CARD-01/02
- [ ] Framework install: `dotnet add apps/api/Api.Tests package Microsoft.AspNetCore.Mvc.Testing`, `npm install -D vitest` in `apps/web`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Photo upload via Vercel Blob | CARD-09 | External service, low value to mock/unit test | Upload a photo in the dashboard form, confirm it renders on the card preview with `object-fit: cover` |
| Full onboarding flow (register → card creation → session persistence → expiry redirect) | ACCT-01..05, CARD-01..10 | Cross-cutting end-to-end UX, best verified by hand before phase gate | Register, create card with all fields, reload dashboard (session persists), let token expire or clear it, confirm redirect to `/login` on next protected call |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
