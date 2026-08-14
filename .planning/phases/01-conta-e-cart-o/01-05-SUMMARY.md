---
phase: 01-conta-e-cart-o
plan: 05
subsystem: web+api
tags: [pix, cpf-cnpj-validator, zod, vitest, xunit, discriminated-union, consent, wave-0-close]

# Dependency graph
requires:
  - phase: 01-conta-e-cart-o (plan 04)
    provides: "apps/api real card endpoints with WhatsApp normalization already wired; apps/web card-form.tsx single-screen shell with ContactSection mounted and a reserved <ReservedSection title=\"Pix\"> title-only block; mirror-file + server-is-authority pattern established by whatsapp-normalize.ts/WhatsappNormalizer.cs"
provides:
  - "apps/web/lib/pix-validation.ts: pixKeySchema (zod discriminatedUnion), isValidPixKey/formatPixKey pure functions, PIX_ERROR_MESSAGES, PixKeyType type -- used by card-schema.ts, pix-section.tsx, card-form.tsx"
  - "apps/api/Services/PixValidationService.cs: hand-rolled CPF/CNPJ Modulo-11 check-digit algorithm (incl. alphanumeric CNPJ ASCII-48 conversion), IsValid()/IsKnownType() -- called by CardEndpoints.ValidatePix before every POST/PUT /cards SaveChangesAsync"
  - "apps/web/components/card-form/pix-section.tsx + pix-cpf-confirm-dialog.tsx: Pix section mounted in card-form.tsx, live preview/validation per keystroke, inline low-risk warning, blocking CPF consent modal"
  - "01-VALIDATION.md Wave 0 checklist closed: wave_0_complete=true, nyquist_compliant=true"
affects: [01-06, 01-07]

# Tech tracking
tech-stack:
  added:
    - "cpf-cnpj-validator@2.1.2 (apps/web) -- CPF/CNPJ validation with check digit and alphanumeric-CNPJ (RFB, jul/2026) support, pre-approved in plan 01's gate"
  patterns:
    - "Mirror-file discipline extended to Pix: pix-validation.ts and PixValidationService.cs carry the same 'must change together' header comment as whatsapp-normalize.ts/WhatsappNormalizer.cs"
    - "Hand-rolled check-digit algorithm as the one legitimate Don't-Hand-Roll exception (no vetted NuGet package covers CPF/CNPJ Modulo-11 for .NET) -- manually verified against the RFB's own canonical alphanumeric CNPJ example (12.ABC.345/01DE-35 -> DV 35) by hand-computing the ASCII-48 weighted sum before trusting the implementation"
    - "dotnet test --filter FullyQualifiedName~<Name> requires the class name to literally CONTAIN that substring (not just start with related words) -- xUnit's ~ operator is a Contains on FullyQualifiedName, so 'PixValidationServiceUnitTests' would NOT match a '~PixValidationTests' filter (the same latent gap exists in 01-04's WhatsappNormalizeTests classes, confirmed by re-running that filter and getting 0 matched tests). Fixed here by naming both test classes with the literal 'PixValidationTests' prefix (PixValidationTests_Unit / PixValidationTests_Integration)."
    - "Cross-field zod validation (pixKey must be valid for the selected pixKeyType, and pixConsentConfirmed must be true when type=cpf and key is filled) done via object-level .superRefine with ctx.addIssue({ code: 'custom', path, message }) rather than a nested discriminatedUnion, because pixKeyType/pixKey coexist with every other card field in one flat cardSchema object"

key-files:
  created:
    - apps/web/lib/pix-validation.ts
    - apps/web/lib/pix-validation.test.ts
    - apps/api/Services/PixValidationService.cs
    - apps/api/Api.Tests/PixValidationTests.cs
    - apps/web/components/card-form/pix-section.tsx
    - apps/web/components/card-form/pix-cpf-confirm-dialog.tsx
  modified:
    - apps/api/Endpoints/CardEndpoints.cs
    - apps/web/lib/card-schema.ts
    - apps/web/components/card-form/card-form.tsx
    - apps/web/package.json
    - apps/web/package-lock.json
    - .planning/phases/01-conta-e-cart-o/01-VALIDATION.md

key-decisions:
  - "apps/api/Program.cs left unmodified, same as plan 01-04's precedent -- PixValidationService is a stateless static class called directly from CardEndpoints.cs, no DI registration needed. The plan's files_modified list included Program.cs speculatively (same wording as the WhatsApp plan); confirmed no change was required."
  - "xUnit test class names for PixValidationTests.cs use an explicit 'PixValidationTests' prefix (PixValidationTests_Unit / PixValidationTests_Integration) instead of the more idiomatic 'PixValidationServiceUnitTests' style, specifically so the plan's own verify command (`--filter FullyQualifiedName~PixValidationTests`) actually matches and runs them -- xUnit's `~` filter operator is Contains, not StartsWith/word-boundary, so a class name with 'Service' or 'Integration' inserted between 'Validation' and 'Tests' breaks the match. Flagged because the same latent bug exists in 01-04's WhatsappNormalizeTests classes (re-ran that filter here and confirmed 0 tests match it as-is)."

patterns-established:
  - "Mirror-file discipline (TS <-> C#) now covers 3 domains in this phase: slug (static-only, no client mirror), WhatsApp (01-04), and Pix (this plan) -- reusable template for any future phase field needing client-preview + server-authority validation"

requirements-completed: [CARD-05, CARD-06, CARD-07]

# Metrics
duration: 40min
completed: 2026-08-14
---

# Phase 1 Plan 5: Pix Key Validation + Blocking CPF Consent Summary

**Pix key section (5 types, discriminated zod schema mirrored by a hand-rolled C# Modulo-11 CPF/CNPJ check-digit service including RFB's alphanumeric CNPJ) with live per-keystroke preview, a non-blocking public-exposure notice for low-risk types, and a blocking confirmation modal for CPF that the server also enforces via a persisted `pix_consent_confirmed` column -- closing the phase's Wave 0 test checklist in the same commit.**

## Performance

- **Duration:** ~40 min
- **Started:** 2026-08-14T11:15:00-03:00 (approx)
- **Completed:** 2026-08-14T11:44:00-03:00
- **Tasks:** 2 automated tasks executed
- **Files modified:** 6 created, 6 modified

## Accomplishments

- `apps/web/lib/pix-validation.ts`: `pixKeySchema` (`z.discriminatedUnion("pixKeyType", ...)`), `isValidPixKey`/`formatPixKey` pure functions using `cpf-cnpj-validator@2.1.2`'s `cpf.isValid`/`cnpj.isValid`/`cpf.format`/`cnpj.format`, zod `.email()`, BR phone regex, and UUID v4 regex -- exact error-message copy from `01-UI-SPEC.md` locked in `PIX_ERROR_MESSAGES`
- `apps/web/lib/pix-validation.test.ts`: 22 Vitest cases -- known-valid/invalid CPF, repeated-digit CPF blacklist (`11111111111`, `00000000000`), numeric and **alphanumeric** CNPJ (`12.ABC.345/01DE-35`, the RFB's own canonical example from the PDF's question 14 -- confirmed `cnpj.isValid` accepts it, resolving 01-RESEARCH.md's Open Question 1 with a real result, not a loosened assertion), email, phone with/without `+55`, UUID v4/v1, unknown type, and `formatPixKey` mask output for all 5 types
- `apps/api/Services/PixValidationService.cs`: hand-rolled CPF/CNPJ check-digit calculation (the one legitimate Don't-Hand-Roll exception in this phase, no vetted NuGet package covers it) -- CNPJ's alphanumeric-character-to-ASCII-48 conversion was manually verified by hand against the RFB's canonical `12.ABC.345/01DE-35 -> DV1=3, DV2=5` example before trusting the implementation; `IsKnownType` restricts to the 5 known types
- `apps/api/Endpoints/CardEndpoints.cs`: new `ValidatePix` helper shared by `POST /cards` and `PUT /cards/{id}` -- 400 `pix_type_invalid` for an unknown type, 400 `pix_key_invalid` when the key fails `PixValidationService.IsValid`, 400 `pix_consent_required` when type is `cpf`, key is filled, and `pixConsentConfirmed` is not `true`; `PixConsentConfirmed` is force-computed server-side (`dto.PixKeyType == "cpf" && dto.PixConsentConfirmed`) so switching away from `cpf` always persists `false`, closing T-01-31
- `apps/api/Api.Tests/PixValidationTests.cs`: 16 unit cases mirroring the Vitest suite (including the same alphanumeric CNPJ vector) plus 7 integration tests against live Postgres (`TestAppFactory`) -- CPF without consent returns 400, CPF with consent returns 200 and persists `pix_consent_confirmed=true`, invalid CPF/random-key/unknown-type all return the correct 400 code, switching CPF to email zeroes `pix_consent_confirmed` in the DB even when the client sends a "stuck" `true`, and a card with no Pix fields at all still returns 201
- `apps/web/lib/card-schema.ts`: `pixKeyType` restricted to the 5 known literals (or `""`); `.superRefine` cross-validates `pixKey` against `pixKeyType` (using the same `isValidPixKey`/`PIX_ERROR_MESSAGES`) and requires `pixConsentConfirmed=true` whenever type is `cpf` and a key is present -- the client-side mirror of the server's `pix_consent_required` rule
- `apps/web/components/card-form/pix-section.tsx`: type `Select` (shadcn) + key `Input`, live `isValidPixKey`/`formatPixKey` preview on every keystroke (D-10) rendered via `FormItem`'s `grid gap-2` spacing (8px, no manual margin needed, same pattern as `whatsapp-input.tsx`), non-blocking "Essa chave ficará visível publicamente no seu cartão." notice for `email`/`telefone`/`aleatoria` (D-08), and a `revertTypeRef` that remembers the type selected right before switching to `cpf` so cancelling the confirm dialog restores it exactly (D-09)
- `apps/web/components/card-form/pix-cpf-confirm-dialog.tsx`: shadcn `Dialog` + `Checkbox`, exact UI-SPEC copy in `red-600` for the risk framing, "Confirmar" disabled until the checkbox is checked, closing/cancelling reverts the type selection and keeps consent `false`
- `apps/web/components/card-form/card-form.tsx`: `PixSection` mounted in the previously-reserved slot; server error codes `pix_key_invalid`/`pix_type_invalid` map to field-level `form.setError`, and `pix_consent_required` increments a `pixReopenSignal` prop that forces the modal back open (defensive path for a multi-tab desync -- the client-side schema already blocks this state from ever reaching submit in the single-tab case)
- Wave 0 checklist closed in `01-VALIDATION.md`: all 5 required files confirmed present on disk (`Api.Tests.csproj`, `vitest.config.ts`, `whatsapp-normalize.test.ts` + `pix-validation.test.ts`, `AuthTests.cs` + `SlugTests.cs`), `wave_0_complete: true` and `nyquist_compliant: true` set in frontmatter, all 5 checklist checkboxes and the two flag sign-off lines checked
- End-to-end verified against live services: started `dotnet run` against the shared local Postgres container, registered a test user, `POST /cards`, `PUT /cards/{id}` with `pixKeyType: "cpf"` and `pixConsentConfirmed: false` correctly returned 400 `pix_consent_required`; the same request with `pixConsentConfirmed: true` returned 200; `docker exec ... psql -c "select slug, pix_key_type, pix_consent_confirmed from cards..."` confirmed `cpf | t` in the database exactly as the plan's acceptance criterion specifies; smoke-test row and user deleted afterward, `dotnet.exe` processes killed

## Task Commits

Each task was committed atomically:

1. **Task 1: Validação de chave Pix por tipo, espelhada no servidor, com consentimento verificável** - `77f70d7` (feat)
2. **Task 2: Seção Pix com prévia ao vivo, aviso inline e modal bloqueante de CPF** - `e92ad70` (feat)

_No separate "plan metadata" commit prior to this one -- this SUMMARY.md's commit is the final commit for this worktree per parallel-executor protocol._

## Files Created/Modified

- `apps/web/lib/pix-validation.ts` - schema, isValidPixKey/formatPixKey, exact error copy
- `apps/web/lib/pix-validation.test.ts` - 22 Vitest cases
- `apps/api/Services/PixValidationService.cs` - hand-rolled CPF/CNPJ check digit, IsValid/IsKnownType
- `apps/api/Api.Tests/PixValidationTests.cs` - 16 unit + 7 integration test cases
- `apps/api/Endpoints/CardEndpoints.cs` - `ValidatePix` helper wired into POST/PUT, consent force-computed server-side
- `apps/web/lib/card-schema.ts` - `pixKeyType` enum, `superRefine` cross-field validation
- `apps/web/components/card-form/pix-section.tsx` - type select + key input, live preview/validation, inline notice
- `apps/web/components/card-form/pix-cpf-confirm-dialog.tsx` - blocking CPF consent modal
- `apps/web/components/card-form/card-form.tsx` - `PixSection` mounted, server-error mapping
- `apps/web/package.json`, `apps/web/package-lock.json` - `cpf-cnpj-validator@2.1.2` added
- `.planning/phases/01-conta-e-cart-o/01-VALIDATION.md` - Wave 0 checklist closed, `wave_0_complete`/`nyquist_compliant` set to true

## Decisions Made

- `apps/api/Program.cs` needed no change (same conclusion as plan 01-04 for `WhatsappNormalizer`) -- `PixValidationService` follows the established stateless-static-class pattern, no DI registration required despite the plan's action step 4 mentioning "Registrar o serviço no DI"; registering it would have added an unused service registration with no functional purpose
- Renamed the xUnit test classes to explicitly start with `PixValidationTests` (`PixValidationTests_Unit`, `PixValidationTests_Integration`) rather than the more conventional `PixValidationServiceUnitTests`/`PixValidationIntegrationTests` naming, because the plan's own verify command `dotnet test --filter FullyQualifiedName~PixValidationTests` uses a Contains match and would otherwise match zero tests -- confirmed this exact gap also exists in plan 01-04's `WhatsappNormalizeTests` classes by re-running `--filter FullyQualifiedName~WhatsappNormalizeTests` here (0 tests matched); not fixed retroactively since 01-04 is already merged and out of this plan's scope, but flagged here and in `tech-stack.patterns` for future plans to avoid the same trap
- CNPJ alphanumeric handling (Open Question 1 from `01-RESEARCH.md`) resolved with a real, non-loosened result: `cnpj.isValid('12.ABC.345/01DE-35')` returns `true`, matching the RFB's own published canonical example -- verified independently by hand-computing the Modulo-11 weighted sum with ASCII-48 character conversion before trusting the library and the C# port

## Deviations from Plan

**1. [Rule 1 - Bug] Renamed xUnit test classes so the plan's verify filter actually matches them**
- **Found during:** Task 1, running the plan's specified `dotnet test --filter FullyQualifiedName~PixValidationTests` command
- **Issue:** xUnit's `--filter FullyQualifiedName~X` operator is a substring Contains check, not a prefix/word-boundary match. A class named `PixValidationServiceUnitTests` does not contain `PixValidationTests` as a contiguous substring (`Service` breaks it), so the filter matched 0 tests even though the file and tests existed and passed when run unfiltered.
- **Fix:** Named both test classes with an explicit `PixValidationTests` prefix (`PixValidationTests_Unit`, `PixValidationTests_Integration`).
- **Files modified:** `apps/api/Api.Tests/PixValidationTests.cs`
- **Commit:** `77f70d7`
- **Note:** The same latent issue exists in plan 01-04's `WhatsappNormalizeTests` classes (`WhatsappNormalizerUnitTests`, `WhatsappNormalizeIntegrationTests`) -- confirmed by re-running `dotnet test --filter FullyQualifiedName~WhatsappNormalizeTests` in this worktree, which also matched 0 tests. Not fixed here (out of this plan's file scope, and 01-04 is already merged); flagged for awareness.

No other deviations -- the rest of the plan executed as written.

## Issues Encountered

None beyond the filter-naming issue documented above. Local Postgres was already running (shared container `vcard-app-db-1` from the host repo, reused across this worktree's test run and manual smoke test) with a `vcard_test` database already migrated from prior wave work.

## User Setup Required

None for this plan's automated tasks. `cpf-cnpj-validator@2.1.2` was pre-approved in plan 01's package-legitimacy gate per CLAUDE.md; re-verified its existence via `npm view cpf-cnpj-validator@2.1.2 version` before installing (no new checkpoint needed, matches the plan's own note "aprovado no gate do plano 01").

## Next Phase Readiness

- `CardWriteDto`/`CardResponseDto` contracts unchanged in shape -- `PixKey`/`PixKeyType`/`PixConsentConfirmed` now flow through full server-side validation but the DTO fields themselves were already present since plan 03
- `PixSection`'s position in `card-form.tsx` is stable; plan 07 replaces the final `ReservedSection title="Links sociais"` block the same way this plan replaced the Pix one
- Wave 0 is now fully closed (`wave_0_complete: true`, `nyquist_compliant: true` in `01-VALIDATION.md`) -- plan 07's `SocialLinkReorderTests.cs` is a normal (non-Wave-0) test file from this point forward
- No blockers remaining for `01-06`/`01-07`

---
*Phase: 01-conta-e-cart-o*
*Completed: 2026-08-14*

## Self-Check: PASSED

All 6 created files verified present via `git ls-files` (tracked, not just on disk):
`apps/web/lib/pix-validation.ts`, `apps/web/lib/pix-validation.test.ts`,
`apps/api/Services/PixValidationService.cs`, `apps/api/Api.Tests/PixValidationTests.cs`,
`apps/web/components/card-form/pix-section.tsx`, `apps/web/components/card-form/pix-cpf-confirm-dialog.tsx`.
Both commit hashes (`77f70d7`, `e92ad70`) verified present in `git log --oneline --all`.
