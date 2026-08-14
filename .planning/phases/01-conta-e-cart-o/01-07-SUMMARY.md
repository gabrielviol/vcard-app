---
phase: 01-conta-e-cart-o
plan: 07
subsystem: api+web
tags: [dnd-kit, social-links, ownership-check, display-order, xunit, vitest, phase-gate]

# Dependency graph
requires:
  - phase: 01-conta-e-cart-o (plan 06)
    provides: "apps/web card-form.tsx single-screen shell with IdentitySection/ContactSection/PixSection mounted (last reserved slot: Links sociais); apps/api CardEndpoints.cs ownership-check pattern (card.UserId vs sub claim, 403 not_owner) and CardResponseDto.SocialLinks nested DTO shape"
provides:
  - "apps/api/Endpoints/SocialLinkEndpoints.cs: POST/DELETE/PUT-order for social links, every handler loading the card by cardId and comparing card.UserId to the sub claim before any write (closes the BOLA gap a bare linkId leaves open)"
  - "apps/api/Services/SocialLinkService.cs: 6-platform allow-list (case-insensitive), https-only absolute-URL validation (500 char max), and Resequence -- reassigns display_order as a gapless 0..n-1 sequence after delete/reorder"
  - "apps/web/components/card-form/social-link-row.tsx: SocialLinkRow with two modes -- persisted (drag handle, read-only platform/url, 44px red Remover) and draft (editable platform Select + URL Input, confirms via POST) -- split because the API has no single-link edit endpoint"
  - "apps/web/components/card-form/social-links-section.tsx: SocialLinksSection -- DndContext/SortableContext list, optimistic reorder on drop with revert-on-failure, empty state and create-mode note per 01-UI-SPEC.md"
  - "Phase 1 closed: full dotnet test (90/90) + npx vitest run (54/54) green, plus the 13-step manual end-to-end walkthrough approved by the developer"
affects: []

# Tech tracking
tech-stack:
  added:
    - "@dnd-kit/core@6.3.1, @dnd-kit/sortable@10.0.0 (apps/web) -- approved at the plan 01 gate; verified current on npm before install"
    - "@dnd-kit/utilities@3.2.2 (apps/web) -- added as an explicit direct dependency (was already present transitively via @dnd-kit/sortable) because social-link-row.tsx imports CSS.Transform.toString directly from it; declaring it explicitly avoids a phantom-dependency break if the transitive tree ever changes shape"
  patterns:
    - "Two-mode row component (persisted vs draft) for list items backed by create/delete/reorder-only endpoints with no single-item update endpoint -- persisted rows render read-only + structural actions (drag, remove); a separate draft row owns the only editable fields, confirmed via POST"
    - "Section-local state fed by initialCard, independent of the surrounding react-hook-form -- used whenever a sub-resource persists through its own endpoints (not the parent PUT), same shape as this plan's SocialLinksSection"

key-files:
  created:
    - apps/api/Contracts/SocialLinkDtos.cs
    - apps/api/Services/SocialLinkService.cs
    - apps/api/Endpoints/SocialLinkEndpoints.cs
    - apps/api/Api.Tests/SocialLinkReorderTests.cs
    - apps/web/components/card-form/social-link-row.tsx
    - apps/web/components/card-form/social-links-section.tsx
  modified:
    - apps/api/Program.cs
    - apps/web/components/card-form/card-form.tsx
    - apps/web/package.json
    - apps/web/package-lock.json
    - .planning/phases/01-conta-e-cart-o/01-VALIDATION.md

key-decisions:
  - "SocialLinkService kept as a static utility class (AllowedPlatforms/IsValidPlatform/IsValidUrl/Resequence), NOT registered in the DI container -- follows the established codebase convention (SlugService, WhatsappNormalizer, PixValidationService are all static, none registered in Program.cs DI), even though the plan's action text literally says to register it. Registering a stateless static-field class in DI would have been inconsistent with every other Services/*.cs file in the project."
  - "SocialLink UI type reused from its existing export in card-form.tsx (added in an earlier plan as part of CardResponseDto) instead of duplicating a second declaration in card-schema.ts as the plan's action step 5 literally requested -- avoids two conflicting type definitions for the same shape."
  - "Row split into persisted/draft modes (see patterns-established) because the interfaces contract only exposes POST/DELETE/PUT-order, no PUT for an individual link's platform/url -- an always-editable select+input on a committed row would have had nowhere to send an update."

patterns-established:
  - "Two-mode row component (persisted read-only+structural vs draft editable+confirm) for any future sub-resource list with create/delete/reorder endpoints but no per-item update endpoint"

requirements-completed: [CARD-10, ACCT-03, ACCT-04, ACCT-05]

# Metrics
duration: ~15min (Tasks 1-2 automated implementation, back-to-back commits 13:33:50-13:39:20) + Task 3 human-verify checkpoint wall-clock (registration, 13-step walkthrough, two environment restarts)
completed: 2026-08-14
---

# Phase 1 Plan 7: Social Links + Phase Gate Summary

**Social link endpoints (POST/DELETE/PUT-order) with per-handler ownership checks and gapless `display_order` resequencing, a `@dnd-kit` drag-to-reorder section closing out the single-screen card editor, and the Phase 1 closing gate: full automated suite (90 xUnit + 54 Vitest) plus a 13-step manual end-to-end walkthrough approved by the developer.**

## Performance

- **Duration:** ~15 min for Tasks 1-2's automated implementation (commits 13:33:50 → 13:39:20 -03:00); Task 3 (human-verify checkpoint) added the remaining wall-clock time for the developer's 13-step manual pass, including two unrelated local dev-environment restarts (see Issues Encountered)
- **Started:** 2026-08-14T13:28:38-03:00 (approx, base commit)
- **Task 1 committed:** 2026-08-14T13:33:50-03:00
- **Task 2 committed:** 2026-08-14T13:39:20-03:00
- **Completed:** 2026-08-14 (Task 3 approved, "aprovado")
- **Tasks:** 2 automated tasks + 1 human-verify checkpoint (approved) = phase gate closed
- **Files modified:** 6 created, 5 modified (Tasks 1-2) + 1 modified (`01-VALIDATION.md`, this commit)

## Accomplishments

- `apps/api/Contracts/SocialLinkDtos.cs`: `SocialLinkWriteDto(Platform, Url)`, `SocialLinkResponseDto(Id, Platform, Url, DisplayOrder)`, `SocialLinkOrderDto(OrderedIds)` — exact shapes from the plan's `<interfaces>` block.
- `apps/api/Services/SocialLinkService.cs`: `AllowedPlatforms` (6-value, case-insensitive `HashSet`), `IsValidUrl` (absolute URI, scheme exactly `https`, ≤500 chars — rejects `javascript:`/`data:`/`http:`), `Resequence` (reassigns `DisplayOrder` as a gapless 0-based index over whatever order the caller enumerates).
- `apps/api/Endpoints/SocialLinkEndpoints.cs`: `POST /cards/{cardId}/social-links` (201, or 400 `platform_invalid`/`url_invalid`, or 403 `not_owner`, or 404 `not_found`), `DELETE /cards/{cardId}/social-links/{linkId}` (204, verifies the link belongs to *this* `cardId` before removing — not just that it exists anywhere — then resequences the remainder), `PUT /cards/{cardId}/social-links/order` (200 with the reordered list, or 400 `order_mismatch` when the submitted id set doesn't exactly match the card's links). Every handler loads the card first and compares `card.UserId` to the `sub` claim before touching anything (T-01-38/BOLA). Wired into `Program.cs` via `cards.MapSocialLinkEndpoints()` on the already-`RequireAuthorization()`'d `/cards` group.
- `apps/api/Api.Tests/SocialLinkReorderTests.cs`: 9 integration tests against live Postgres — sequential `display_order` 0/1/2 on add, reorder persists in DB and in `GET /cards/me`, removing the middle link resequences the remainder to 0/1 without a gap, unknown platform → 400, `http://`/`javascript:alert(1)` → 400 `url_invalid`, another user's POST/DELETE/PUT-order all return 403 `not_owner`, reordering with a foreign card's link id returns 400 `order_mismatch`, and all three endpoints return 401 without a token. All 9 pass; full `dotnet test` 90/90, no regressions.
- `apps/web/components/card-form/social-link-row.tsx`: `SocialLinkRow` in two modes — `persisted` (drag handle via `useSortable`, read-only platform label + URL text, 44×44px red "Remover" icon button, no confirmation modal per `01-UI-SPEC.md`) and `draft` (platform `Select` with the 6 options + URL `Input`, "Adicionar"/cancel buttons) — see key-decisions for why the split exists.
- `apps/web/components/card-form/social-links-section.tsx`: `SocialLinksSection` — manages its own `links` state seeded from `initialCard.socialLinks` (outside the card's `useForm`, since links persist through their own endpoints). "Adicionar link" opens a draft row; confirming POSTs and appends the created link. Removing calls `DELETE` and locally resequences `displayOrder` to mirror the server. `onDragEnd` reorders optimistically and `PUT`s the new order, reverting with a toast on failure. Empty state ("Nenhum link ainda" / "Adicione Instagram, LinkedIn ou outro link para aparecer no seu cartão.") and create-mode note ("Salve o cartão para começar a adicionar links.") match the UI-SPEC copy contract exactly.
- `apps/web/components/card-form/card-form.tsx`: mounted `SocialLinksSection` as the final section, replacing the `ReservedSection` placeholder (which was then deleted as dead code, no longer needed by any section).
- Package legitimacy: `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` all verified via `npm view` before installing.
- Verification commands run: `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~SocialLinkReorderTests` (9/9), full `dotnet test` (90/90), `npx tsc --noEmit` (0 errors, after `npm run build`), `npm run build` (succeeds), `npx vitest run` (54/54), `npx eslint` on the new/modified frontend files (clean). All acceptance-criteria grep checks passed (`not_owner` count 3, no raw-SQL, `Nenhum link ainda` count 1, empty-state body count 1, `social-links/order` count ≥1, `@dnd-kit/sortable` in `package.json`, `SocialLinksSection` in `card-form.tsx`).
- **Task 3 (human-verify checkpoint, phase gate): approved ("aprovado").** All 13 steps of the end-to-end walkthrough passed: register → straight into card form (no empty dashboard, D-01); slug-taken and reserved-slug (`login`) rejection with correct copy; name-only save; role/company persist across reload; WhatsApp mask → `+55 11 98765-4321` preview → `5511987654321` persisted; Pix e-mail type shows the non-blocking public-visibility notice; Pix CPF type opens the blocking confirm modal (checkbox-gated); invalid/valid CPF live feedback with formatted preview; photo upload → circular preview → remove → initials placeholder; add 3 social links, drag the third to the top, reload (order persisted), remove the middle one (no gap); clearing `accessToken` from Local Storage and triggering an API call redirects to `/login` with the session-expired copy; re-login shows the full card intact, including the social links in the correct order. No app-level bugs found. No browser console errors during the pass.

## Task Commits

Each task was committed atomically:

1. **Task 1: Endpoints de links sociais com posse verificada e ordem sem buracos** - `a26cc7c` (feat)
2. **Task 2: Seção de links sociais com arraste para reordenar** - `3720ffd` (feat)
3. **Task 3: Passagem manual end-to-end da Fase 1 (gate da fase)** - checkpoint, no code change; approved by the developer ("aprovado", 13/13 steps) — `01-VALIDATION.md` updated in this SUMMARY's commit per the task's `<action>`

## Files Created/Modified

- `apps/api/Contracts/SocialLinkDtos.cs` - SocialLinkWriteDto/SocialLinkResponseDto/SocialLinkOrderDto
- `apps/api/Services/SocialLinkService.cs` - allow-list, URL validation, gapless Resequence
- `apps/api/Endpoints/SocialLinkEndpoints.cs` - POST/DELETE/PUT-order with ownership checks
- `apps/api/Api.Tests/SocialLinkReorderTests.cs` - 9 integration tests
- `apps/api/Program.cs` - `cards.MapSocialLinkEndpoints()` wired in
- `apps/web/components/card-form/social-link-row.tsx` - persisted/draft row component
- `apps/web/components/card-form/social-links-section.tsx` - drag-to-reorder section, own state
- `apps/web/components/card-form/card-form.tsx` - mounts SocialLinksSection, removes dead ReservedSection helper
- `apps/web/package.json`, `apps/web/package-lock.json` - `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`
- `.planning/phases/01-conta-e-cart-o/01-VALIDATION.md` - Per-Task Verification Map rows flipped to ✅ green for automated-covered tasks, CARD-10 row's File Exists flipped to ✅, Validation Sign-Off fully checked, frontmatter `status: approved`, Approval line filled in

## Decisions Made

- Kept `SocialLinkService` as a static utility class, not registered in DI, matching every other `Services/*.cs` file in the codebase (`SlugService`, `WhatsappNormalizer`, `PixValidationService`) — the plan's action text asked to "register the SocialLinkService in Program.cs DI," but doing so would have been the only inconsistent service registration in the project for a stateless, dependency-free utility.
- Reused the `SocialLink` type already exported from `card-form.tsx` (established in an earlier plan as part of `CardResponseDto`) instead of adding a second, duplicate declaration to `card-schema.ts` as the plan's action step 5 literally asked — one canonical type, no drift risk between two declarations of the same shape.
- Split `SocialLinkRow` into `persisted` (read-only + drag + remove) and `draft` (editable select/URL + confirm) modes, because the API's `<interfaces>` contract only exposes create/delete/reorder for links, no per-link update endpoint — an always-editable row on a committed link would have had no server-side destination for a changed value.
- `@dnd-kit/utilities` promoted from a transitive dependency (already resolvable via `@dnd-kit/sortable`) to an explicit direct dependency, since `social-link-row.tsx` imports `CSS.Transform.toString` from it directly — avoids relying on an undeclared transitive resolution for a directly-imported module.

## Deviations from Plan

### Auto-fixed Issues

None — the two static-class/type-reuse choices above are design decisions responding to plan-text ambiguity (a static utility asked to register in DI; a type asked to be duplicated where one already existed), not bugs, missing functionality, or blockers, so none of Rules 1-3 strictly apply. Documented above under Decisions Made instead, per the same "document, don't silently diverge" principle.

---

**Total deviations:** 0 auto-fixed. 3 documented design decisions where the plan's literal action text conflicted with either established codebase convention or an already-existing declaration; no scope creep, no functionality added beyond the plan's `<interfaces>`/`<action>` intent.

## Issues Encountered

Environment-only, not app bugs, all during the Task 3 manual verification session:

- **Stale `.next` build cache caused a "Jest worker encountered 2 child process exceptions" error at step 4 (save with name only).** Root cause: earlier process restarts in this worktree left a corrupted Turbopack/webpack cache. Resolved by killing the dev server, deleting `.next`, and restarting cleanly — no code change needed.
- **`apps/api` process died mid-session (port 5153 not listening), causing a generic "Não foi possível salvar. Verifique sua conexão e tente novamente." at step 13 (re-login).** The background dev-server wrapper reported the process as unexpectedly stopped (same cosmetic-vs-real distinction documented in `01-06-SUMMARY.md`'s Issues Encountered, but this time the process actually was down, confirmed via `curl` returning connection-refused). Restarted with the same env vars documented in `apps/api/.env.example` (`ConnectionStrings__Default`, `JWT_SECRET`, `Jwt__Issuer`/`Jwt__Audience`/`Jwt__ExpiresMinutes`, `Cors__WebOrigin`), confirmed `GET /health` returned `{"status":"ok","database":"up"}`, developer retried successfully.
- **Confirmed (not a bug) during verification:** adding a social link does not require clicking the card form's main "Salvar alterações" button — this is intended behavior, since social links persist immediately through their own POST/PUT/DELETE endpoints (plan 07's `<interfaces>` contract), independent of the card form's `PUT /cards/{id}` save flow.
- Both dev servers (`apps/api` port 5153, `apps/web` port 3000) were killed after Task 3's approval, per the coordinator's post-verification instructions — confirmed no process left listening on either port before writing this summary.

## User Setup Required

None - no new external service configuration required (`@dnd-kit` is a plain npm dependency, no account/token setup).

## Next Phase Readiness

- Phase 1 (`conta-e-cartao`) is fully closed: `ACCT-01..05` and `CARD-01..10` all delivered and verified (automated + manual), `.planning/phases/01-conta-e-cart-o/01-VALIDATION.md` fully signed off.
- `CardResponseDto.SocialLinks` (ordered by `display_order`) is the exact shape Phase 2's public card page will consume for rendering the social link list — no contract change needed.
- `SocialLinkEndpoints.cs`'s ownership-check pattern (load by id, compare `UserId` to `sub` claim, verify child-resource membership before mutating) is now established across `CardEndpoints.cs` and `SocialLinkEndpoints.cs` — reusable template for any future per-user sub-resource.
- No blockers for Phase 2. `REQUIREMENTS.md` was intentionally left untouched by this worktree agent (consistent with plans 01-04/01-05/01-06, which also left it as `Pending` for their completed requirements) — the orchestrator owns the centralized `requirements mark-complete` pass after merge, per the established phase convention (STATE.md/ROADMAP.md/REQUIREMENTS.md are all shared, orchestrator-owned artifacts across parallel worktree plans).

---
*Phase: 01-conta-e-cart-o*
*Completed: 2026-08-14*

## Self-Check: PASSED

All 8 created/modified files verified present on disk: `apps/api/Contracts/SocialLinkDtos.cs`,
`apps/api/Services/SocialLinkService.cs`, `apps/api/Endpoints/SocialLinkEndpoints.cs`,
`apps/api/Api.Tests/SocialLinkReorderTests.cs`, `apps/web/components/card-form/social-link-row.tsx`,
`apps/web/components/card-form/social-links-section.tsx`, `.planning/phases/01-conta-e-cart-o/01-VALIDATION.md`,
`.planning/phases/01-conta-e-cart-o/01-07-SUMMARY.md`.
Commit hashes `a26cc7c` and `3720ffd` verified present in `git log --oneline --all`.
