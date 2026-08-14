---
phase: 01-conta-e-cart-o
reviewed: 2026-08-14T00:00:00Z
depth: standard
files_reviewed: 97
files_reviewed_list:
  - .gitignore
  - apps/api/.env.example
  - apps/api/Api.Tests/Api.Tests.csproj
  - apps/api/Api.Tests/AssemblyInfo.cs
  - apps/api/Api.Tests/AuthTests.cs
  - apps/api/Api.Tests/CardOwnershipTests.cs
  - apps/api/Api.Tests/HealthTests.cs
  - apps/api/Api.Tests/PhotoUrlTests.cs
  - apps/api/Api.Tests/PixValidationTests.cs
  - apps/api/Api.Tests/SlugTests.cs
  - apps/api/Api.Tests/SocialLinkReorderTests.cs
  - apps/api/Api.Tests/TestAppFactory.cs
  - apps/api/Api.Tests/WhatsappNormalizeTests.cs
  - apps/api/Api.csproj
  - apps/api/Api.sln
  - apps/api/Contracts/AuthDtos.cs
  - apps/api/Contracts/CardDtos.cs
  - apps/api/Contracts/SocialLinkDtos.cs
  - apps/api/Data/AppDbContext.cs
  - apps/api/Data/Entities/Card.cs
  - apps/api/Data/Entities/CardView.cs
  - apps/api/Data/Entities/SocialLink.cs
  - apps/api/Data/Entities/User.cs
  - apps/api/Data/Migrations/20260814015302_InitialSchema.cs
  - apps/api/Endpoints/AuthEndpoints.cs
  - apps/api/Endpoints/CardEndpoints.cs
  - apps/api/Endpoints/SocialLinkEndpoints.cs
  - apps/api/Program.cs
  - apps/api/Properties/launchSettings.json
  - apps/api/Services/AuthService.cs
  - apps/api/Services/PixValidationService.cs
  - apps/api/Services/SlugService.cs
  - apps/api/Services/SocialLinkService.cs
  - apps/api/Services/WhatsappNormalizer.cs
  - apps/api/appsettings.Development.json
  - apps/api/appsettings.json
  - apps/web/.env.example
  - apps/web/.gitignore
  - apps/web/AGENTS.md
  - apps/web/CLAUDE.md
  - apps/web/README.md
  - apps/web/app/(dashboard)/dashboard/cards/[id]/edit/page.tsx
  - apps/web/app/(dashboard)/dashboard/cards/new/page.tsx
  - apps/web/app/(dashboard)/dashboard/page.tsx
  - apps/web/app/(dashboard)/layout.tsx
  - apps/web/app/(dashboard)/login/page.tsx
  - apps/web/app/(dashboard)/register/page.tsx
  - apps/web/app/api/upload/route.ts
  - apps/web/app/globals.css
  - apps/web/app/layout.tsx
  - apps/web/app/page.tsx
  - apps/web/components.json
  - apps/web/components/avatar-placeholder.tsx
  - apps/web/components/card-form/card-form.tsx
  - apps/web/components/card-form/contact-section.tsx
  - apps/web/components/card-form/identity-section.tsx
  - apps/web/components/card-form/photo-section.tsx
  - apps/web/components/card-form/pix-cpf-confirm-dialog.tsx
  - apps/web/components/card-form/pix-section.tsx
  - apps/web/components/card-form/slug-field.tsx
  - apps/web/components/card-form/social-link-row.tsx
  - apps/web/components/card-form/social-links-section.tsx
  - apps/web/components/card-form/whatsapp-input.tsx
  - apps/web/components/ui/avatar.tsx
  - apps/web/components/ui/badge.tsx
  - apps/web/components/ui/button.tsx
  - apps/web/components/ui/card.tsx
  - apps/web/components/ui/checkbox.tsx
  - apps/web/components/ui/dialog.tsx
  - apps/web/components/ui/form.tsx
  - apps/web/components/ui/input.tsx
  - apps/web/components/ui/label.tsx
  - apps/web/components/ui/select.tsx
  - apps/web/components/ui/separator.tsx
  - apps/web/components/ui/sonner.tsx
  - apps/web/eslint.config.mjs
  - apps/web/lib/api-client.ts
  - apps/web/lib/auth-schema.ts
  - apps/web/lib/auth-storage.test.ts
  - apps/web/lib/auth-storage.ts
  - apps/web/lib/card-schema.ts
  - apps/web/lib/initials.test.ts
  - apps/web/lib/initials.ts
  - apps/web/lib/pix-validation.test.ts
  - apps/web/lib/pix-validation.ts
  - apps/web/lib/use-debounced-value.test.ts
  - apps/web/lib/use-debounced-value.ts
  - apps/web/lib/utils.ts
  - apps/web/lib/whatsapp-normalize.test.ts
  - apps/web/lib/whatsapp-normalize.ts
  - apps/web/next.config.ts
  - apps/web/package.json
  - apps/web/postcss.config.mjs
  - apps/web/tsconfig.json
  - apps/web/vitest.config.ts
  - docker-compose.yml
  - docker/init-test-db.sql
findings:
  critical: 1
  warning: 7
  info: 2
  total: 10
status: issues_found
---

# Phase 1: Code Review Report

**Reviewed:** 2026-08-14
**Depth:** standard
**Files Reviewed:** 97
**Status:** issues_found

## Summary

Reviewed the full Phase 1 scope: account creation/JWT auth, card CRUD, slug reservation, WhatsApp/Pix/photo/social-links slices, across both `apps/api` (.NET minimal API) and `apps/web` (Next.js). The security-sensitive server-side patterns are largely well executed: ownership checks (BOLA) are present on every card/social-link write, Pix/WhatsApp/photo-URL validation is authoritative server-side (never trusting client-normalized values), the JWT bearer setup explicitly pins the signing algorithm and validates issuer/audience/lifetime, and the slug/social-link uniqueness races are closed via catching Postgres `23505` rather than relying solely on a pre-check.

The most significant defect found is a genuine account-integrity bug: email uniqueness and lookup are case-sensitive end-to-end (DB index, register duplicate check, and login query), which both breaks the "e-mail único" invariant the register endpoint is supposed to enforce and can lock real users out of login if they type their email with different casing than at registration. Beyond that, review surfaced several narrower correctness/robustness gaps (a race condition that reports a misleading error code, no server-side password length ceiling before bcrypt hashing, a benign but real duplicate-`display_order` race on social-link creation) and a couple of code-quality nits (misplaced import statements, a debug `console.log`, and an error-message field that leaks internal error codes to any future consumer that reads it).

## Critical Issues

### CR-01: Email uniqueness and login lookup are case-sensitive, violating the "email único" invariant

**File:** `apps/api/Endpoints/AuthEndpoints.cs:33` (register duplicate check), `apps/api/Endpoints/AuthEndpoints.cs:60` (login lookup), `apps/api/Data/AppDbContext.cs:24` (unique index), `apps/api/Data/Migrations/20260814015302_InitialSchema.cs:124-127`

**Issue:** `email` is stored as plain `text` with a case-sensitive unique index (`IX_users_email`), and both the register duplicate check (`db.Users.AnyAsync(u => u.Email == request.Email)`) and the login lookup (`db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)`) do an exact, case-sensitive comparison. No normalization (`ToLowerInvariant()`/citext) is applied anywhere on the write or read path — not in `AuthEndpoints.cs`, not in the `User` entity, not on the frontend (`apps/web/lib/auth-schema.ts` only validates format via `z.email()`, it does not lowercase).

Concretely:
1. A user can register `Joao@Example.com` and then register again as `joao@example.com` — two distinct accounts, defeating the endpoint's own stated contract ("Valida email único").
2. A user who registers as `Joao@Example.com` and later types `joao@example.com` at login (the common case — most people don't consistently reproduce the exact casing they used at signup) gets `401 invalid_credentials`, indistinguishable from a wrong password, even though their credentials are otherwise correct.

This is a data-integrity and account-access defect in the core account-creation flow, not a style nit — it will produce real support tickets and duplicate accounts in production.

**Fix:** Normalize email to lower-case (or trim + lower-case) at the boundary before every comparison and before persisting, on both register and login:
```csharp
var normalizedEmail = request.Email.Trim().ToLowerInvariant();
var emailExists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
...
var user = new User { Email = normalizedEmail, PasswordHash = ... };
```
```csharp
// LoginHandler
var normalizedEmail = request.Email.Trim().ToLowerInvariant();
var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
```
Add a migration to backfill any existing rows and consider a citext column or a case-insensitive index if duplicate-prevention should also be enforced at the DB layer (defense in depth against a future code path that forgets to normalize).

## Warnings

### WR-01: `CreateCardHandler` reports the wrong error code under a rare uniqueness race

**File:** `apps/api/Endpoints/CardEndpoints.cs:44-95`
**Issue:** `cards.UserId` and `cards.Slug` both have unique DB indexes. `CreateCardHandler` pre-checks `alreadyHasCard` (TOCTOU-safe intent) but the actual insert's catch block unconditionally maps *any* `SqlState 23505` violation to `{ error = "slug_taken" }`:
```csharp
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
{
    return Results.Conflict(new { error = "slug_taken" });
}
```
If two concurrent `POST /cards` requests race for the *same user* (e.g. duplicate form submissions from two tabs) rather than colliding on slug, the real violated constraint is `IX_cards_user_id`, but the client is told `slug_taken` instead of `card_exists` — misleading error handling on the frontend (`CardForm.onSubmit` sets a slug-field error even though the slug was fine).
**Fix:** Inspect which constraint was violated (e.g. via `ex.InnerException.ConstraintName` from `PostgresException`) and return the correct error code:
```csharp
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pg)
{
    return pg.ConstraintName == "IX_cards_user_id"
        ? Results.Conflict(new { error = "card_exists" })
        : Results.Conflict(new { error = "slug_taken" });
}
```

### WR-02: No upper bound on password length before bcrypt hashing

**File:** `apps/api/Endpoints/AuthEndpoints.cs:30-31`, `apps/api/Services/AuthService.cs:11`
**Issue:** `RegisterHandler` only enforces `request.Password.Length < 8` as a lower bound; there is no maximum length check. BCrypt has a well-known 72-byte input limit — most implementations (including BCrypt.Net-Next by default) silently truncate the input beyond 72 bytes rather than hashing the full string. Without a max-length guard, two different passwords sharing the same first 72 bytes will hash identically and both will authenticate, which is a subtle correctness/security weakening that's easy to miss in review.
**Fix:** Reject passwords over a sane byte length (e.g. 72) at the same validation point as the minimum-length check:
```csharp
if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 || Encoding.UTF8.GetByteCount(request.Password) > 72)
    return Results.BadRequest(new { error = "invalid_password" });
```
Mirror the same cap in `apps/web/lib/auth-schema.ts`'s `registerSchema` so the client gives feedback before submit.

### WR-03: No rate limiting / lockout on `/auth/login`

**File:** `apps/api/Endpoints/AuthEndpoints.cs:58-67`, `apps/api/Program.cs`
**Issue:** `LoginHandler` has no throttling, attempt counting, or lockout — an attacker can send unlimited login attempts per email/IP. bcrypt verification is intentionally slow, which helps somewhat, but there's no defense against distributed/low-and-slow credential stuffing.
**Fix:** Add ASP.NET Core's built-in rate limiting middleware (`AddRateLimiter`) scoped to `/auth/login` and `/auth/register`, or a lightweight fixed-window limiter keyed by IP/email, even if generous for the MVP window.

### WR-04: Concurrent social-link creation can produce duplicate `display_order` values

**File:** `apps/api/Endpoints/SocialLinkEndpoints.cs:19-55`
**Issue:** `CreateSocialLinkHandler` assigns `DisplayOrder = card.SocialLinks.Count` based on an `Include`d snapshot read at request start. Two concurrent `POST /cards/{cardId}/social-links` requests for the same card (e.g. a double-click, or two open tabs) can both read the same count and both persist the same `DisplayOrder`, since there's no unique constraint on `(CardId, DisplayOrder)` (only a non-unique index, `AppDbContext.cs:67`). The result isn't a crash, but subsequent `ORDER BY display_order` reads become order-ambiguous between the two new rows.
**Fix:** Either wrap the count-read + insert in a serializable transaction / `SELECT ... FOR UPDATE`-equivalent, or compute `DisplayOrder` via a DB-side `COALESCE(MAX(display_order), -1) + 1` subquery at insert time to avoid the read-then-write race.

### WR-05: Import statements placed after other top-level statements in `card-form.tsx`

**File:** `apps/web/components/card-form/card-form.tsx:1-27`
**Issue:** Lines 20-27 (`import { ApiError, apiFetch } from "@/lib/api-client";` etc.) appear *after* a `const` declaration and a function declaration (lines 11-19), rather than being grouped with the rest of the imports at the top of the file. ES module imports are hoisted so this doesn't break at runtime, but it violates standard import-ordering conventions (the project's own `eslint-config-next` typically flags `import/first`), reads as a merge artifact, and makes the file harder to scan.
**Fix:** Move all `import` statements to the top of the file, above `KNOWN_PIX_TYPES`/`toPixKeyType`:
```ts
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { cardSchema, type CardFormValues } from "@/lib/card-schema";
import { PIX_ERROR_MESSAGES, type PixKeyType } from "@/lib/pix-validation";
import { ApiError, apiFetch } from "@/lib/api-client";
import { clearToken } from "@/lib/auth-storage";
// ...component imports...

const KNOWN_PIX_TYPES: readonly PixKeyType[] = [...];
function toPixKeyType(...) { ... }
```

### WR-06: `CardWriteDto` allows a `pixKeyType` to be persisted with no `pixKey`, silently accepting a stray `pixConsentConfirmed`

**File:** `apps/api/Endpoints/CardEndpoints.cs:184-202`
**Issue:** `ValidatePix` only runs its format/consent checks `if (!string.IsNullOrWhiteSpace(dto.PixKey))`. If a client sends `pixKeyType: "cpf"` with an empty/null `pixKey` (e.g. a partially-filled form, or a buggy client), validation is skipped entirely and the card is persisted with `PixKeyType = "cpf"`, `PixKey = null`, and — because `PixConsentConfirmed = dto.PixKeyType == "cpf" && dto.PixConsentConfirmed` only checks the type, not the presence of a key — `PixConsentConfirmed` can be persisted as `true` with no actual CPF stored. This is an inconsistent, half-filled Pix state that the public card page (Phase 2) will need to defensively handle.
**Fix:** Treat `pixKeyType` set without a matching `pixKey` as invalid input, or clear `pixKeyType`/`PixConsentConfirmed` server-side whenever `pixKey` is blank:
```csharp
if (string.IsNullOrWhiteSpace(dto.PixKey) && !string.IsNullOrWhiteSpace(dto.PixKeyType))
    return Results.BadRequest(new { error = "pix_key_required" });
```

### WR-07: `ApiError.message` is overwritten with the raw server error code instead of a human-readable string

**File:** `apps/web/lib/api-client.ts:56-68`
**Issue:**
```ts
let message = "Não foi possível completar a operação.";
try {
  const body = await res.json();
  if (body?.error) {
    code = body.error;
    message = body.error;   // <-- overwrites the friendly default with e.g. "slug_taken"
  }
} catch { ... }
throw new ApiError(res.status, code, message);
```
`message` is set to the same raw snake_case error code as `code` (e.g. `"pix_key_invalid"`) whenever the server returns a JSON body with an `error` field — which is effectively always, given every API error handler in this phase returns `{ error: "..." }`. Every current caller in this codebase reads `.code` and maps it to its own copy, so this is currently latent, but any future call site that renders `error.message` directly (a very natural thing to do with a class literally named `message`) will show raw internal error codes to end users in Portuguese-language UI.
**Fix:** Keep `message` as the friendly default unless the server ever sends a genuinely human-readable message field (e.g. a distinct `body.message`), and only use `body.error` to populate `code`:
```ts
if (body?.error) {
  code = body.error;
}
```

## Info

### IN-01: Debug `console.log` left in production upload route

**File:** `apps/web/app/api/upload/route.ts:36`
**Issue:** `console.log("[upload] blob concluído:", blob.url);` runs on every completed upload in the deployed Route Handler. It's intentionally documented as the only signal available locally, but it will also fire in production and write to server logs indefinitely.
**Fix:** Gate behind `process.env.NODE_ENV !== "production"`, or replace with a structured/conditional logger if this needs to persist past local development.

### IN-02: `CardForm`'s edit-mode submit relies on a non-null assertion that isn't statically guaranteed

**File:** `apps/web/components/card-form/card-form.tsx:117`
**Issue:** `await apiFetch<CardResponseDto>(`/cards/${initialCard!.id}`, ...)` uses `initialCard!` even though `CardFormProps.initialCard` is typed as optional (`initialCard?: CardResponseDto`) and `mode` and `initialCard` aren't linked by a discriminated union. Current call sites always pass both together in `edit` mode, so this doesn't fail today, but the type system doesn't protect against a future regression (e.g. someone rendering `<CardForm mode="edit" />` without `initialCard`), which would throw at runtime instead of being caught at compile time.
**Fix:** Model `CardFormProps` as a discriminated union so the compiler enforces the pairing:
```ts
type CardFormProps =
  | { mode: "create"; initialCard?: undefined }
  | { mode: "edit"; initialCard: CardResponseDto };
```

---

_Reviewed: 2026-08-14_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
