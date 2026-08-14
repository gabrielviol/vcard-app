---
phase: 01-conta-e-cart-o
verified: 2026-08-14T20:30:00Z
status: passed
score: 5/5 roadmap success criteria verified (plus 15/15 requirement IDs satisfied)
overrides_applied: 1
overrides:
  - must_have: "Abaixo do campo aparece o formato final normalizado antes de salvar (WhatsApp preview, D-11 / 01-04-PLAN.md)"
    reason: "Preview text removed per product feedback when unifying phone/WhatsApp inputs into a shared masked component (commit ba45983); underlying server-side normalization (CARD-08) is unaffected and fully tested."
    accepted_by: "Gabriel Oliveira"
    accepted_at: "2026-08-14"
---

# Phase 1: Conta e Cartão Verification Report

**Phase Goal:** Dono cria conta e monta seu cartão completo — identidade, canais de contato, Pix e links sociais — com todas as validações de segurança/formato aplicadas no momento em que os campos nascem
**Verified:** 2026-08-14
**Status:** passed
**Re-verification:** No — initial verification

## Process Note: MVP Mode Mismatch

ROADMAP.md marks Phase 1 `Mode: mvp`, which normally requires the phase goal to be a User Story (`As a ... I want to ... so that ...`). Running `gsd-sdk query user-story.validate` against the literal goal text returns `valid: false` — the goal is not in User Story format. This was already flagged by every plan in the phase (`01-01-PLAN.md` through `01-07-PLAN.md` each carry the note "o Goal da ROADMAP não está no formato de user story... nenhuma user story foi inventada"), so it is a pre-existing, self-documented process gap, not something this verification pass introduced. Per the MVP-mode instructions, the User Flow Coverage format is not applicable here; this report proceeds with standard goal-backward verification against the 5 ROADMAP Success Criteria instead. Recommend running `/gsd mvp-phase 1` retroactively if the mode label should be kept accurate for future phases, but this does not block Phase 1 itself.

## Goal Achievement

### Observable Truths (ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Visitante cria conta com e-mail/senha (hash BCrypt) e faz login recebendo um access token JWT | ✓ VERIFIED | `apps/api/Endpoints/AuthEndpoints.cs` registers/logs in with `BCrypt.Net.BCrypt` hash (`AuthService.cs`), issues JWT (`HmacSha256`). `dotnet test apps/api/Api.Tests` → **90/90 passed**, including `RegisterTests`/`LoginTests` (password hash `$2` prefix asserted). Email now normalized to lowercase (CR-01 fix confirmed live in code) so "email único" holds. |
| 2 | Usuário permanece autenticado ao navegar e recarregar (F5); redireciona para `/login` sem token/expirado; rotas de escrita de Card/SocialLink retornam 401 sem token | ✓ VERIFIED | `apps/web/lib/auth-storage.ts` (localStorage-based session, D-07 no loading/no pre-check). `apps/web/app/(dashboard)/layout.tsx` guard redirects when token absent (excludes `/login`,`/register`). `apps/web/lib/api-client.ts` clears token + hard-redirects to `/login?expired=1` on 401. Server side: `apps/api/Program.cs` line 94 — `app.MapGroup("/cards").RequireAuthorization()` covers both `MapCardEndpoints()` and `MapSocialLinkEndpoints()`. `AuthGuardTests`/`CardOwnershipTests`/`SocialLinkReorderTests` all assert 401 without token — part of the 90/90 green suite. |
| 3 | Usuário cria cartão com slug único (reservados + já-em-uso rejeitados) e edita nome, cargo, empresa e foto | ✓ VERIFIED | `apps/api/Services/SlugService.cs` reserved-word HashSet + format regex; `CardEndpoints.cs` catches `SqlState 23505` on both `IX_cards_slug`/`IX_cards_user_id` with correct differentiated error codes (WR-01 fix confirmed live). DB confirmed: `cards.slug` and `cards.user_id` both UNIQUE indexes (`psql \d cards`). Identity fields (`fullName`,`role`,`company`) editable via `identity-section.tsx`; photo upload wired through `PhotoSection` (mounted inside `IdentitySection`, confirmed by reading the file) → Vercel Blob → `PUT /cards/{id}` persists `photo_url` (host-allowlist validated server-side). `SlugTests`/`CardOwnershipTests`/`PhotoUrlTests` green. |
| 4 | Usuário cadastra telefone, e-mail e WhatsApp (normalizado DDI 55) e chave Pix com validação por tipo, prévia formatada e aviso reforçado para CPF | ✓ VERIFIED | WhatsApp: `apps/api/Services/WhatsappNormalizer.cs` mirrors `apps/web/lib/whatsapp-normalize.ts` (9th-digit DDD whitelist, DDD 85 correctly excluded); `CardEndpoints.cs` calls `WhatsappNormalizer.Normalize` server-side before persist (never trusts client value) — confirmed by grep and code read. Pix: `pix-section.tsx` runs `isValidPixKey`/`formatPixKey` on every keystroke, shows live "Prévia: ..." and the low-risk inline notice "Essa chave ficará visível publicamente no seu cartão."; CPF selection opens `PixCpfConfirmDialog` (blocking modal + checkbox), `pixConsentConfirmed` flows into the PUT payload via `card-form.tsx` (confirmed wired, contrary to the raw grep's false negative on the dialog file itself). Server mirrors all validation + `pix_consent_required` gate (`PixValidationService.cs`, `ValidatePix` in `CardEndpoints.cs`, WR-06 fix confirmed live). `WhatsappNormalizeTests`/`PixValidationTests` green as part of the 90/90 suite; `whatsapp-normalize.test.ts`/`pix-validation.test.ts` green as part of the 54/54 Vitest suite. **Caveat:** the WhatsApp-specific "Vai ser salvo como: +55 DDD XXXXX-XXXX" preview line (D-11, required by `01-04-PLAN.md`'s must-haves) was deliberately removed in commit `ba45983` ("per product feedback", at the user's explicit request) when the WhatsApp and phone inputs were unified into a shared `PhoneMaskInput`. This satisfies the ROADMAP-level criterion (which only requires "prévia formatada" for the **Pix** key, not WhatsApp) but is a real, intentional deviation from the plan-04-level must-have and D-11. See Gaps/Overrides note below. |
| 5 | Usuário adiciona, remove e reordena links sociais (Instagram, LinkedIn, Twitter, TikTok, YouTube, site) | ✓ VERIFIED | `apps/api/Endpoints/SocialLinkEndpoints.cs`: POST/DELETE/PUT-order all check `card.UserId == sub` (403 `not_owner`), `Resequence` keeps `display_order` gapless on delete, `SELECT ... FOR UPDATE` row lock on create closes the concurrent-duplicate-order race (WR-04 fix confirmed live). `apps/web/components/card-form/social-links-section.tsx`: `@dnd-kit` drag reorder with optimistic update + rollback on failure, empty state copy "Nenhum link ainda" / "Adicione Instagram, LinkedIn ou outro link para aparecer no seu cartão." confirmed verbatim. `SocialLinkReorderTests` green (part of 90/90 suite). |

**Score:** 5/5 ROADMAP success criteria verified (with one flagged, accepted-looking deviation under criterion 4 — see below).

### Required Artifacts (spot check across all 7 plans)

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `apps/api/Endpoints/AuthEndpoints.cs` | register/login/me, BCrypt, JWT | ✓ VERIFIED | Read in full; CR-01/WR-02/WR-03 fixes present and live |
| `apps/api/Endpoints/CardEndpoints.cs` | slug/identity/contact/pix/photo CRUD, ownership | ✓ VERIFIED | Read in full; WR-01/WR-06 fixes present and live |
| `apps/api/Endpoints/SocialLinkEndpoints.cs` | add/remove/reorder, ownership | ✓ VERIFIED | Read in full; WR-04 fix (row lock) present and live |
| `apps/api/Program.cs` | DI, JWT, CORS, rate limiter, `/cards` group auth | ✓ VERIFIED | Read in full; `RequireAuthorization()` wraps both card and social-link endpoints |
| `apps/web/lib/api-client.ts` | Bearer injection, 401 interceptor | ✓ VERIFIED | Read in full; WR-07 fix present and live |
| `apps/web/lib/card-schema.ts` | zod schema, Pix/WhatsApp refine | ✓ VERIFIED | Read in full |
| `apps/web/components/card-form/*.tsx` (8 section/dialog components) | tela única seccionada | ✓ VERIFIED | All sections mounted in `card-form.tsx` (Slug, Identity+Photo, Contact, Pix, SocialLinks) |
| `apps/web/components/card-form/whatsapp-input.tsx` | máscara + prévia WhatsApp (plan 04) | ⚠️ SUPERSEDED | Renamed/merged into `phone-mask-input.tsx` in commit `ba45983`; preview text removed. File no longer exists at the plan-04 path — intentional refactor, not an unnoticed regression |
| `apps/web/app/api/upload/route.ts` | authenticated Vercel Blob token issuance | ✓ VERIFIED | Read in full; `GET /auth/me` check before token issuance confirmed |
| Postgres schema (4 tables) | users/cards/social_links/card_views | ✓ VERIFIED | `psql \dt` confirms all 4 + EF migrations history; `\d cards` confirms unique indexes on `slug` and `user_id` |

### Key Link Verification

All `gsd-sdk query verify.key-links` runs across the 7 plans returned 17/18 automated matches; the 2 reported mismatches were manually verified as false negatives (grep-pattern limitations, not actual gaps):

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `apps/web/app/(dashboard)/dashboard/page.tsx` | `GET /auth/me` | plan-02 wiring | ⚠️ SUPERSEDED | Plan 03 (D-01) intentionally rewrote `dashboard/page.tsx` to call `GET /cards/me` and skip the empty-dashboard screen instead of showing the user's email via `/auth/me`. This was the documented plan-03 design (D-01, present already in plan 02's own objective text). `/auth/me` is still called from `app/api/upload/route.ts` for auth verification. No roadmap Success Criterion requires displaying the user's email on screen. |
| `apps/web/components/card-form/pix-cpf-confirm-dialog.tsx` | `cards.pix_consent_confirmed` | form field wiring | ✓ VERIFIED (manual) | `pixConsentConfirmed` state lives in the parent `pix-section.tsx` (not the dialog file itself) and flows into `card-form.tsx`'s PUT payload (line 104). Grep on the dialog file alone was a false negative; full data flow confirmed by reading the three files together. |
| All other 16 key links (plans 01, 03, 04, 06, 07 fully; plan 02 partially; plan 05 partially) | — | — | ✓ VERIFIED | Automated pattern match succeeded |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|--------------|--------|----------|
| ACCT-01 | 01-01 | Registro com hash BCrypt | ✓ SATISFIED | `AuthService.HashPassword`, `RegisterTests` |
| ACCT-02 | 01-01 | Login com JWT | ✓ SATISFIED | `AuthService.IssueToken`, `LoginTests` |
| ACCT-03 | 01-02, 01-07 | Sessão persiste ao navegar/F5 | ✓ SATISFIED | `auth-storage.ts` + D-07 layout guard; manual 13-step pass in `01-VALIDATION.md` sign-off |
| ACCT-04 | 01-02 | Redirect para `/login` sem token/expirado | ✓ SATISFIED | `(dashboard)/layout.tsx` guard + `api-client.ts` 401 interceptor |
| ACCT-05 | 01-01, 01-03, 01-07 | Rotas de escrita de Card/SocialLink retornam 401 sem token | ✓ SATISFIED | `.RequireAuthorization()` on `/cards` group; `AuthGuardTests`, `CardOwnershipTests`, `SocialLinkReorderTests` |
| CARD-01 | 01-03 | Slug único na criação | ✓ SATISFIED | `SlugService`, unique index, `SlugTests` |
| CARD-02 | 01-03 | Slugs reservados/duplicados rejeitados | ✓ SATISFIED | `SlugService.ReservedSlugs`, `SlugTests` |
| CARD-03 | 01-03 | Nome, cargo, empresa editáveis | ✓ SATISFIED | `identity-section.tsx`, `CardOwnershipTests` |
| CARD-04 | 01-04 | Telefone/e-mail/WhatsApp editáveis | ✓ SATISFIED | `contact-section.tsx` (now via shared `PhoneMaskInput`) |
| CARD-05 | 01-05 | Tipo de chave Pix selecionável (5 valores) | ✓ SATISFIED | `pix-section.tsx` select, `pix-validation.ts` |
| CARD-06 | 01-05 | Validação por tipo + dígito verificador + UUID v4 + prévia | ✓ SATISFIED | `PixValidationService.cs`, `pix-validation.ts`, live preview in `pix-section.tsx` |
| CARD-07 | 01-05 | Aviso de exposição pública + confirmação bloqueante CPF | ✓ SATISFIED | `PixCpfConfirmDialog`, server-side `pix_consent_required` gate |
| CARD-08 | 01-04 | Normalização WhatsApp DDI 55, server-authoritative | ✓ SATISFIED | `WhatsappNormalizer.cs` called before `SaveChangesAsync`; DDD 85 exclusion tested both sides |
| CARD-09 | 01-06 | Upload de foto direto do browser | ✓ SATISFIED | `app/api/upload/route.ts` (auth-gated), `photo-section.tsx`, human-verified in plan-06 Task 2 + re-confirmed in plan-07 Task 3 |
| CARD-10 | 01-07 | Adicionar/remover/reordenar links sociais | ✓ SATISFIED | `SocialLinkEndpoints.cs`, `social-links-section.tsx`, `SocialLinkReorderTests` |

**Orphaned requirements:** None — all 15 requirement IDs declared in ROADMAP.md for Phase 1 (`ACCT-01..05`, `CARD-01..10`) are claimed by at least one plan's frontmatter and have verified supporting evidence.

**Documentation gap (non-blocking):** `.planning/REQUIREMENTS.md` still shows `ACCT-05` and all `CARD-*` rows as `[ ]` / "Pending" in the traceability table, even though the code evidence above confirms they are implemented and tested. Only `ACCT-01..04` are checked `[x]`. This appears to be a tracking-document lag (the SUMMARY/tracking commits updated ROADMAP.md's phase checkbox and `01-VALIDATION.md` but not `REQUIREMENTS.md`), not a code gap. Recommend updating `REQUIREMENTS.md` checkboxes before starting Phase 2 so the traceability table stays trustworthy.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `apps/web/app/api/upload/route.ts` | 36 | `console.log` debug statement (IN-01 from 01-REVIEW.md) | ℹ️ Info | Explicitly excluded from the fix pass scope (`--fix`, not `--all`); fires in production logs but leaks no secret, low impact |
| `apps/web/components/card-form/card-form.tsx` | 117 | `initialCard!` non-null assertion (IN-02 from 01-REVIEW.md) | ℹ️ Info | Same — excluded from fix scope; not statically enforced but no current call site can trigger it |

No TBD/FIXME/XXX/TODO/HACK/PLACEHOLDER debt markers found in any phase-modified file (`apps/api/Endpoints`, `apps/api/Services`, `apps/api/Contracts`, `apps/api/Data/Entities`, `apps/web/lib`, `apps/web/components/card-form`, `apps/web/app`). No stub returns, no empty handlers, no hardcoded-empty data flowing to rendering.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Backend integration suite | `dotnet test apps/api/Api.Tests` | 90/90 passed | ✓ PASS |
| Frontend unit suite | `cd apps/web && npx vitest run` | 54/54 passed (5 files) | ✓ PASS |
| Backend build | `dotnet build apps/api/Api.csproj` | 0 errors, 0 warnings | ✓ PASS |
| Frontend typecheck + build | `cd apps/web && npx tsc --noEmit && npm run build` | Clean, all 8 routes generated | ✓ PASS |
| Postgres schema | `docker compose exec db psql -c "\dt"` / `"\d cards"` | 4 domain tables + migrations history; unique indexes on `slug`, `user_id` confirmed | ✓ PASS |
| `/cards` auth gate | Code read: `app.MapGroup("/cards").RequireAuthorization()` wraps both endpoint groups | Confirmed at source | ✓ PASS |

### Probe Execution

No `scripts/*/tests/probe-*.sh` files or probe references found in this phase's PLAN/SUMMARY files. SKIPPED — no probes declared for this phase (standard xUnit/Vitest suites serve this role and were executed directly above, not merely trusted from SUMMARY claims).

### Human Verification Required

None outstanding. `01-VALIDATION.md` frontmatter shows `status: approved`, `wave_0_complete: true`, `nyquist_compliant: true`, and documents a signed-off 13-step manual end-to-end walkthrough (plan 01-07, Task 3) plus two earlier blocking human checkpoints (Walking Skeleton in 01-02, photo upload in 01-06) — all recorded as "aprovado" with no unresolved app-level bugs. This verification pass independently re-ran both automated suites (90/90 backend, 54/54 frontend) rather than trusting the recorded pass counts, and both are green as of this report.

### Gaps Summary

No blocking gaps found. Two items are worth the developer's attention but do not block the phase:

1. **D-11 WhatsApp preview text removed (commit `ba45983`, at user's explicit request).** The literal plan-04 must-have "Abaixo do campo aparece o formato final normalizado antes de salvar" and the corresponding acceptance-criteria grep (`"Vai ser salvo como"`) no longer hold, because the WhatsApp-specific preview line was intentionally dropped when unifying the phone/WhatsApp inputs into a shared masked component. The underlying CARD-08 behavior (server-authoritative normalization to DDI 55, correct 9th-digit-DDD handling) is fully intact and tested — this is a UI-copy simplification, not a functional regression, and it does not violate the ROADMAP-level Success Criterion (which only requires a "prévia formatada" for the Pix key). Since this was a deliberate, already-committed product decision by the developer (not something the executor silently skipped), it reads as accepted rather than an oversight — no plan update was made to formally record the D-11 supersession in `01-CONTEXT.md`, though.

   **This looks intentional.** To formally record this deviation, add to this file's frontmatter:
   ```yaml
   overrides:
     - must_have: "Abaixo do campo aparece o formato final normalizado antes de salvar (WhatsApp preview, D-11 / 01-04-PLAN.md)"
       reason: "Preview text removed per product feedback when unifying phone/WhatsApp inputs into a shared masked component (commit ba45983); underlying server-side normalization (CARD-08) is unaffected and fully tested."
       accepted_by: "Gabriel Oliveira"
       accepted_at: "2026-08-14"
   ```

2. **`.planning/REQUIREMENTS.md` traceability checkboxes are stale** (`ACCT-05` and all `CARD-*` still `[ ]`/"Pending" despite verified implementation). Recommend a housekeeping update before Phase 2 planning begins, so the requirements table remains a reliable source of truth.

Both items are informational/process notes, not code defects. All 5 ROADMAP Success Criteria are observably true in the current codebase, both automated test suites pass in full (90/90 + 54/54, independently re-executed by this verification), the build is clean on both sides, and the manual end-to-end sign-off in `01-VALIDATION.md` is documented and approved.

---

_Verified: 2026-08-14_
_Verifier: Claude (gsd-verifier)_
