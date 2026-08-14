---
phase: 01-conta-e-cart-o
fixed_at: 2026-08-14T19:50:00Z
review_path: .planning/phases/01-conta-e-cart-o/01-REVIEW.md
iteration: 1
findings_in_scope: 8
fixed: 7
skipped: 1
status: partial
---

# Phase 1: Code Review Fix Report

**Fixed at:** 2026-08-14
**Source review:** .planning/phases/01-conta-e-cart-o/01-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 8 (1 critical + 7 warnings; Info findings excluded per `--fix` scope, not `--all`)
- Fixed: 7
- Skipped: 1 (WR-05, already fixed manually prior to this run)

## Fixed Issues

### CR-01: Email uniqueness and login lookup are case-sensitive, violating the "email único" invariant

**Files modified:** `apps/api/Endpoints/AuthEndpoints.cs`
**Commit:** `23c0b6e`
**Applied fix:** `RegisterHandler` and `LoginHandler` now trim + lowercase (`ToLowerInvariant()`) the email before every DB comparison (existence check, uniqueness check, login lookup) and before persisting on register. No migration/backfill added — project is in early dev with no real user data, so app-layer normalization alone closes the invariant going forward per the constraints (avoided over-engineering with citext/case-insensitive collation).

### WR-01: `CreateCardHandler` reports the wrong error code under a rare uniqueness race

**Files modified:** `apps/api/Endpoints/CardEndpoints.cs`
**Commit:** `1705f35`
**Applied fix:** The `DbUpdateException` catch block for `CreateCardHandler` now inspects `PostgresException.ConstraintName` and returns `card_exists` when the violated constraint is `IX_cards_user_id`, falling back to `slug_taken` for any other 23505 violation (i.e. the actual slug index).

### WR-02: No upper bound on password length before bcrypt hashing

**Files modified:** `apps/api/Endpoints/AuthEndpoints.cs`, `apps/web/lib/auth-schema.ts`
**Commit:** `2cf4b4b`
**Applied fix:** `RegisterHandler` now rejects passwords whose UTF-8 byte length exceeds 72 (via `Encoding.UTF8.GetByteCount`), alongside the existing minimum-length check, returning `invalid_password`. Mirrored on the client with a `.max(72, ...)` constraint on `registerSchema.password` in `auth-schema.ts` for early feedback (character-length approximation; server remains the byte-length authority).

### WR-03: No rate limiting / lockout on `/auth/login`

**Files modified:** `apps/api/Program.cs`, `apps/api/Endpoints/AuthEndpoints.cs`
**Commit:** `e02eb10`
**Applied fix:** Added ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware with a fixed-window policy named `"login"` (10 requests/minute, partitioned by client IP), registered via `AddRateLimiter`/`UseRateLimiter`, and applied only to `POST /auth/login` via `.RequireRateLimiting("login")`. Scoped to login only (not register), matching the finding's stated threat (credential-stuffing/brute-force against an existing account) and keeping the change proportionate to the MVP window.

### WR-04: Concurrent social-link creation can produce duplicate `display_order` values

**Files modified:** `apps/api/Endpoints/SocialLinkEndpoints.cs`
**Commit:** `48aa71c`
**Applied fix:** `CreateSocialLinkHandler` now opens an explicit DB transaction and takes a `SELECT ... FOR UPDATE` row lock on the parent `cards` row before reading `SocialLinks.Count`, then commits after `SaveChangesAsync`. This serializes concurrent link-creation requests for the *same* card (the second request blocks until the first commits and then reads a fresh, post-insert count), closing the read-then-write race without a schema migration or retry loop.
**Note:** This fix touches transaction/locking semantics not exercised by any concurrency-specific test (no test spins up two truly concurrent requests against the same card). All 90 existing backend tests pass, but the concurrency-safety property itself is unverified by automated tests — **recommend a human sanity-check** of the locking logic, or a follow-up concurrent-load test, before treating this as fully verified.

### WR-06: `CardWriteDto` allows a `pixKeyType` to be persisted with no `pixKey`

**Files modified:** `apps/api/Endpoints/CardEndpoints.cs`
**Commit:** `2ddefca`
**Applied fix:** `ValidatePix` now returns `pix_key_required` (400) whenever `dto.PixKeyType` is set but `dto.PixKey` is blank, before falling through to the existing key-present validation branch. The reverse case (key present, type blank/invalid) was already correctly rejected by the pre-existing `PixValidationService.IsValid(null, key)` call returning `false` → `pix_key_invalid`.

### WR-07: `ApiError.message` is overwritten with the raw server error code

**Files modified:** `apps/web/lib/api-client.ts`
**Commit:** `809fcd3`
**Applied fix:** `message` now stays as the friendly default (`"Não foi possível completar a operação."`) unconditionally; only `code` is set from `body.error`. No caller in the codebase currently reads `.message` (all branch on `.code`), so this is purely a latent-footgun fix with no behavior change for existing call sites.

## Skipped Issues

### WR-05: Import statements placed after other top-level statements in `card-form.tsx`

**File:** `apps/web/components/card-form/card-form.tsx:1-27`
**Reason:** Explicitly excluded from this run per task instructions — already fixed manually in commit `ba45983` prior to this fix pass. Verified: all `import` statements in the current file are correctly grouped at the top (lines 1–17), above `KNOWN_PIX_TYPES`/`toPixKeyType`. No action taken.
**Original issue:** Imports for `ApiError`/`apiFetch`/`clearToken` appeared after `KNOWN_PIX_TYPES`/`toPixKeyType` declarations, violating import-ordering convention.

## Final Verification

Full test suites re-run after all fixes committed:
- `dotnet test apps/api/Api.Tests`: **90/90 passed**
- `cd apps/web && npx vitest run`: **54/54 passed**
- `cd apps/web && npx tsc --noEmit`: 1 pre-existing error unrelated to any fix (`app/layout.tsx(23,50): Cannot find name 'LayoutProps'` — a Next.js 16 generated-type reference that only resolves after `next build`/`next dev` populate `.next/types`; not present in any file touched by this run)
- `cd apps/web && npm run build`: **succeeded** (TypeScript pass included in the build also succeeded, confirming the standalone `tsc --noEmit` finding above is a tooling artifact, not a regression)

No regressions introduced by any of the 7 fixes.

---

_Fixed: 2026-08-14_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
