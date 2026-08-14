---
phase: 01-conta-e-cart-o
plan: 04
subsystem: web+api
tags: [whatsapp, contact-form, react-imask, zod, vitest, xunit, brazilian-phone-normalization]

# Dependency graph
requires:
  - phase: 01-conta-e-cart-o (plan 03)
    provides: "apps/api real card endpoints (POST/PUT /cards), CardWriteDto/CardResponseDto contract already including Phone/Email/WhatsappNumber fields verbatim (unvalidated); apps/web card-form.tsx single-screen shell with reserved <section> title-only blocks for Contato/Pix/Links sociais"
provides:
  - "apps/web/lib/whatsapp-normalize.ts: normalizeWhatsapp/formatWhatsappPreview/isValidWhatsapp pure functions, NINTH_DIGIT_DDDS whitelist (CARD-08)"
  - "apps/api/Services/WhatsappNormalizer.cs: 1:1 authoritative server-side mirror -- Normalize()/IsValid() -- called before SaveChangesAsync on both POST /cards and PUT /cards/{id}, 400 whatsapp_invalid on invalid input"
  - "apps/web/components/card-form/contact-section.tsx + whatsapp-input.tsx: Contato section (phone/email/WhatsApp) mounted in card-form.tsx, live BR mask + D-11 normalized preview"
affects: [01-05, 01-06, 01-07]

# Tech tracking
tech-stack:
  added:
    - "react-imask@7.6.1 (apps/web) -- dynamic BR phone mask via dispatch(), pre-approved in plan 01's gate"
  patterns:
    - "Mirror-file discipline: whatsapp-normalize.ts and WhatsappNormalizer.cs carry an explicit top-of-file comment that they must change together -- same pattern to reuse for CARD-06 Pix validation in plan 05"
    - "Client never pre-normalizes before submit: the form field keeps the masked/typed text as-is; the server's Normalize()/IsValid() call is the only place the persisted value is decided (T-01-22) -- client-side isValidWhatsapp in card-schema.ts is UX-only, a mirror of the same rule, not the source of truth"
    - "IMaskInput dispatch() for dynamic (10-vs-11-digit) masks: dispatch receives a typed MaskedDynamic from the `imask` package (not react-imask) and must return one of its own compiledMasks[] entries -- returning a plain {mask: string} object fails tsc since it's not a real Masked instance"

key-files:
  created:
    - apps/web/lib/whatsapp-normalize.ts
    - apps/web/lib/whatsapp-normalize.test.ts
    - apps/api/Services/WhatsappNormalizer.cs
    - apps/api/Api.Tests/WhatsappNormalizeTests.cs
    - apps/web/components/card-form/whatsapp-input.tsx
    - apps/web/components/card-form/contact-section.tsx
  modified:
    - apps/api/Endpoints/CardEndpoints.cs
    - apps/web/lib/card-schema.ts
    - apps/web/components/card-form/card-form.tsx
    - apps/web/package.json
    - apps/web/package-lock.json

key-decisions:
  - "apps/api/Program.cs left unmodified -- WhatsappNormalizer is a static class (same pattern as plan 03's SlugService), called directly from CardEndpoints.cs with no DI registration needed. The plan's files_modified list included Program.cs speculatively; no change was required there."
  - "The whitelist of 9th-digit DDDs (Assumptions Log A2 in 01-RESEARCH.md) is community-sourced, not from an official ANATEL document. Reproducing that flag here per the plan's own instruction (action step 6): recommend a manual spot-check against a few real, known numbers from different DDDs before the wa.me deep link goes live in Phase 3."

patterns-established:
  - "Mirror-file discipline for cross-runtime validation/normalization logic (TS <-> C#), with an explicit file-header comment stating the pairing -- reusable for CARD-06 Pix validation in plan 05"

requirements-completed: [CARD-04, CARD-08]

# Metrics
duration: 55min
completed: 2026-08-14
---

# Phase 1 Plan 4: Contact Channels + WhatsApp Normalization Summary

**WhatsApp/phone/email fields added to the card form with a live Brazilian phone mask and D-11 normalized preview on the client, while the server (`WhatsappNormalizer.Normalize`) is the sole authority for the persisted digit-only DDI-55 value -- proven both by 17 xUnit cases (including a DDD-85-does-NOT-get-a-spurious-9 integration test against live Postgres) and a manual curl+psql smoke test.**

## Performance

- **Duration:** ~55 min
- **Started:** 2026-08-14T11:00:00-03:00 (approx)
- **Completed:** 2026-08-14T11:55:00-03:00
- **Tasks:** 2 automated tasks executed
- **Files modified:** 6 created, 5 modified

## Accomplishments

- `apps/web/lib/whatsapp-normalize.ts`: `normalizeWhatsapp`/`formatWhatsappPreview`/`isValidWhatsapp` pure functions with `NINTH_DIGIT_DDDS` whitelist (SP 11-19, RJ 21/22/24, ES 27/28) -- strips DDI/trunk-zero/mask characters, adds the 9th digit only for whitelisted DDDs with an 8-digit local number, leaves every other DDD's digit count untouched (the exact anti-pattern the research flagged)
- `apps/web/lib/whatsapp-normalize.test.ts`: 16 Vitest cases, including the DDD-85-does-NOT-receive-a-9 proof, idempotency on already-masked/DDI input, trunk-zero stripping, and `isValidWhatsapp` boundary cases (5 digits rejected; 10/11 accepted)
- `apps/api/Services/WhatsappNormalizer.cs`: 1:1 C# port of the same logic, `NinthDigitDdds` `HashSet`, file-header comment declaring the TS file as its mirror
- `apps/api/Endpoints/CardEndpoints.cs`: both `POST /cards` and `PUT /cards/{id}` now call `WhatsappNormalizer.Normalize(dto.WhatsappNumber)` before `SaveChangesAsync`, and return `400 { error: "whatsapp_invalid" }` when a non-empty value fails `WhatsappNormalizer.IsValid` -- the client's own (possibly already-"normalized") value is never trusted (T-01-22)
- `apps/api/Api.Tests/WhatsappNormalizeTests.cs`: 13 unit cases mirroring the Vitest suite plus 4 integration tests against live Postgres (`TestAppFactory`) -- masked input persists as pure digits with DDI 55, DDD 85 with 8 digits does NOT get a spurious 9, invalid input returns 400, empty input persists as `null`
- `apps/web/components/card-form/whatsapp-input.tsx`: `IMaskInput` (react-imask) with a `dispatch()`-driven dynamic mask (`(00) 0000-0000` vs `(00) 00000-0000`, switching at >10 digits), "Vai ser salvo como: {preview}" hint line (Label 14px/600, sm/8px spacing per `FormItem`'s `grid gap-2`) built from `formatWhatsappPreview`, and a `red-600` "Número de WhatsApp inválido." message when the field is filled but `isValidWhatsapp` fails; empty field shows neither
- `apps/web/components/card-form/contact-section.tsx`: "Contato" heading (20px/600) with `phone` (simple masked-free `tel` input), `email` (zod `.email()`-validated when filled), and `WhatsappInput`, all wired through the shared `useFormContext`
- `apps/web/lib/card-schema.ts`: `email` refined with `z.email()` only when non-empty; `whatsappNumber` refined with `isValidWhatsapp` only when non-empty -- both optional per D-04, both client-side UX mirrors of the server's real validation
- `apps/web/components/card-form/card-form.tsx`: `ContactSection` replaces the plan-03 title-only reserved block, in the same stable position (Identidade -> Contato -> Pix -> Links sociais); submit payload logic untouched (still sends the raw typed string, `|| null` for empties)
- End-to-end verified against live services: `docker exec ... psql` confirmed the `cards`/`users`/`social_links`/`card_views` schema already existed in this worktree's `vcard` database; started `dotnet run` against it, registered a test user, `POST /cards`, then `PUT /cards/{id}` with `whatsappNumber: "11987654321"` -- response and a direct `psql` query both confirmed `whatsapp_number = '5511987654321'`; smoke-test row and user deleted afterward, background `dotnet.exe` processes killed

## Task Commits

Each task was committed atomically:

1. **Task 1: Normalização de WhatsApp testada no cliente e espelhada no servidor** - `8bb5b17` (feat)
2. **Task 2: Seção Contato com máscara ao vivo e prévia normalizada** - `d5661b7` (feat)

_No separate "plan metadata" commit prior to this one -- this SUMMARY.md's commit is the final commit for this worktree per parallel-executor protocol._

## Files Created/Modified

- `apps/web/lib/whatsapp-normalize.ts` - normalize/format/validate pure functions, DDD whitelist
- `apps/web/lib/whatsapp-normalize.test.ts` - 16 Vitest cases
- `apps/api/Services/WhatsappNormalizer.cs` - C# mirror, `NinthDigitDdds`, `Normalize`/`IsValid`
- `apps/api/Api.Tests/WhatsappNormalizeTests.cs` - 13 unit + 4 integration test cases
- `apps/api/Endpoints/CardEndpoints.cs` - server-side normalize/validate call on POST and PUT, before `SaveChangesAsync`
- `apps/web/components/card-form/whatsapp-input.tsx` - `IMaskInput` dynamic mask + D-11 preview/error
- `apps/web/components/card-form/contact-section.tsx` - phone/email/WhatsApp section
- `apps/web/lib/card-schema.ts` - `email`/`whatsappNumber` client-side refines
- `apps/web/components/card-form/card-form.tsx` - `ContactSection` mounted in place of the reserved block
- `apps/web/package.json`, `apps/web/package-lock.json` - `react-imask@7.6.1` added

## Decisions Made

- `apps/api/Program.cs` was listed in the plan's `files_modified` but did not need any change -- `WhatsappNormalizer` follows plan 03's already-established pattern of a stateless static class called directly from the endpoint handler, no DI registration required
- Kept the DDD-9th-digit-rule assumption flag (A2 from `01-RESEARCH.md`) visible here per the plan's own instruction: the whitelist is community-sourced, not an official ANATEL document -- recommend a manual spot-check with real numbers before the `wa.me` deep link (Phase 3) goes live

## Deviations from Plan

None -- plan executed exactly as written. `Program.cs` was read and confirmed to need no change rather than being edited to "match" the plan's file list (editing it with no functional purpose would have been noise, not a fix).

## Issues Encountered

None. The worktree's `apps/web/node_modules` and `.next/types` did not exist yet (fresh worktree) -- `npm install` and `npm run build` were run as normal environment setup (not a deviation, not a code change) before `npx tsc --noEmit` could pass, since `.next/types` (which resolves `LayoutProps` in `app/layout.tsx`, an unrelated pre-existing file) is generated by `next build`, not by `tsc` alone.

## User Setup Required

None for this plan's automated tasks. Same local Postgres (`docker compose up -d db`, already running and already migrated from prior work in this worktree) was reused for the xUnit integration suite and the manual curl+psql smoke test.

## Next Phase Readiness

- `CardWriteDto`/`CardResponseDto` contract unchanged in shape -- `WhatsappNumber` now flows through server-side normalization but the DTO fields themselves were already present since plan 03
- Plan 05 (Pix) can reuse the same "mirror-file + server-is-authority" pattern established here for `pixKey`/`pixKeyType` validation (`cpf-cnpj-validator` client-side, C# regex/digit-check mirror server-side)
- `ContactSection`'s position in `card-form.tsx` is stable; plan 05 replaces the next `ReservedSection title="Pix"` block the same way this plan replaced the Contato one
- No blockers remaining for `01-05`

---
*Phase: 01-conta-e-cart-o*
*Completed: 2026-08-14*

## Self-Check: PASSED

All 6 created files verified present via `git ls-files` (tracked, not just on disk):
`apps/web/lib/whatsapp-normalize.ts`, `apps/web/lib/whatsapp-normalize.test.ts`,
`apps/api/Services/WhatsappNormalizer.cs`, `apps/api/Api.Tests/WhatsappNormalizeTests.cs`,
`apps/web/components/card-form/whatsapp-input.tsx`, `apps/web/components/card-form/contact-section.tsx`.
Both commit hashes (`8bb5b17`, `d5661b7`) verified present in `git log --oneline --all`.
