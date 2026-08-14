---
phase: 01-conta-e-cart-o
plan: 06
subsystem: web+api
tags: [vercel-blob, upload, avatar, initials-placeholder, photo-url-validation, vitest, xunit]

# Dependency graph
requires:
  - phase: 01-conta-e-cart-o (plan 05)
    provides: "apps/web card-form.tsx single-screen shell with IdentitySection/ContactSection/PixSection mounted; apps/api CardEndpoints.cs ValidatePix pattern (shared 400-return helper called from both POST and PUT before persisting); mirror-file discipline precedent"
provides:
  - "apps/web/app/api/upload/route.ts: authenticated Vercel Blob client-upload token issuance (handleUpload), Bearer validated against GET /auth/me before any token is issued"
  - "apps/web/lib/initials.ts: getInitials/getAvatarColor -- reusable initials-in-circle placeholder logic, no dependency on any card-specific type"
  - "apps/web/components/avatar-placeholder.tsx: AvatarPlaceholder component (shadcn Avatar based)"
  - "apps/web/components/card-form/photo-section.tsx: direct-to-blob upload UI (progress, preview, remove) mounted inside IdentitySection"
  - "apps/api/Endpoints/CardEndpoints.cs IsValidPhotoUrl: server-side allow-list of *.public.blob.vercel-storage.com hosts, https-only, wired into POST/PUT before SaveChangesAsync"
affects: [01-07]

# Tech tracking
tech-stack:
  added:
    - "@vercel/blob@2.8.0 (apps/web) -- confirmed current on npm (matches CLAUDE.md's stale '1.x' note being outdated, as flagged in 01-RESEARCH.md State of the Art); handleUpload/upload() signatures match the research's code example with one addition: upload() now requires an explicit access: 'public' client-side (GenerateClientTokenOptions.access is optional server-side, but ClientCommonCreateBlobOptions.access is required client-side in v2.8.0's type defs)"
  patterns:
    - "Auth-gated token-issuance route: apps/web/app/api/upload/route.ts extracts the Bearer header and calls GET {NEXT_PUBLIC_API_URL}/auth/me server-side before calling handleUpload's onBeforeGenerateToken -- closes the gap Vercel's own docs leave open (flagged [ASSUMED] in 01-RESEARCH.md/01-PATTERNS.md)"
    - "Server allow-list for externally-hosted URLs stored in a DB column that later becomes an <img src> on a public page: CardEndpoints.IsValidPhotoUrl checks both scheme (https only) and host suffix (.public.blob.vercel-storage.com) -- same shape reusable for any future field that stores a third-party URL"

key-files:
  created:
    - apps/web/app/api/upload/route.ts
    - apps/web/lib/initials.ts
    - apps/web/lib/initials.test.ts
    - apps/web/components/avatar-placeholder.tsx
    - apps/web/components/card-form/photo-section.tsx
    - apps/api/Api.Tests/PhotoUrlTests.cs
  modified:
    - apps/web/components/card-form/identity-section.tsx
    - apps/web/components/card-form/card-form.tsx
    - apps/web/lib/card-schema.ts
    - apps/web/.env.example
    - apps/web/package.json
    - apps/web/package-lock.json
    - apps/api/Endpoints/CardEndpoints.cs

key-decisions:
  - "PhotoSection mounted inside IdentitySection (apps/web/components/card-form/identity-section.tsx modified) rather than as a standalone top-level section in card-form.tsx -- the plan's action text explicitly says the photo belongs 'dentro da seção Identidade', even though identity-section.tsx was not listed in the plan's files_modified frontmatter (treated as a minor plan-frontmatter omission, not a scope decision to override)"
  - "upload() client call includes access: 'public' explicitly -- required by @vercel/blob@2.8.0's client-side type signature (ClientCommonCreateBlobOptions.access is non-optional), even though the server-side onBeforeGenerateToken return type treats access as optional. No functional ambiguity: the card's photo is always meant to be publicly viewable (product is a public-facing digital card)."

patterns-established:
  - "Third-party-hosted-URL validation on the server (scheme + host-suffix allow-list) as the standard shape for any future DB column that will render as a public <img src>/<a href> pointing off-platform"

requirements-completed: [CARD-09]

# Metrics
duration: ~50min (Task 1 automated work) + human-verification checkpoint wait
completed: 2026-08-14
---

# Phase 1 Plan 6: Photo Upload + Initials Placeholder Summary

**Direct browser-to-Vercel-Blob photo upload (`@vercel/blob@2.8.0` client upload, auth-gated token issuance validated against `GET /auth/me`) with live circular preview (`object-fit: cover`, no crop lib per D-13), a 44px borderless "Remover foto" action, and a deterministic initials-in-circle placeholder when no photo exists — server enforces `photo_url` must be `https` on a `*.public.blob.vercel-storage.com` host before persisting.**

## Performance

- **Duration:** ~50 min for Task 1's automated implementation; Task 2 (human-verify checkpoint) added additional wall-clock time while the human tested manually, including recreating the Vercel Blob store mid-verification (see Issues Encountered)
- **Started:** 2026-08-14T12:00:00-03:00 (approx)
- **Task 1 committed:** 2026-08-14T12:48:32-03:00
- **Tasks:** 1 automated task + 1 human-verify checkpoint (approved)
- **Files modified:** 6 created, 7 modified

## Accomplishments

- `apps/web/app/api/upload/route.ts`: Route Handler (Node runtime, default — not Edge, required by `handleUpload`) implementing the plan's exact interface contract. `onBeforeGenerateToken` extracts the `Authorization: Bearer` header, calls `GET {NEXT_PUBLIC_API_URL}/auth/me` with it, and throws (mapped to `401 {"error":"unauthorized"}`) if that call doesn't return 200 — closing the auth gap Vercel's own official docs leave unaddressed (T-01-32, flagged `[ASSUMED]` in `01-RESEARCH.md`/`01-PATTERNS.md`). `allowedContentTypes` restricted to `image/jpeg`/`image/png`/`image/webp`, `maximumSizeInBytes` 4MB, `addRandomSuffix: true` (T-01-33). `onUploadCompleted` only logs — persistence of `photoUrl` happens via the existing `PUT /cards/{id}`, since the callback can't reach `localhost` without a tunnel.
- `apps/web/lib/initials.ts` + `initials.test.ts`: `getInitials` (first+last word initial, single-word names return one letter, accent-tolerant via native `toUpperCase()`, empty/null-safe) and `getAvatarColor` (simple deterministic string hash → fixed 8-color Tailwind palette). 10 Vitest cases, all passing, covering the exact examples from the plan's action text ("Gabriel Oliveira"→"GO", "Conceição"→"C", "João da Silva"→"JS", empty string, null/undefined, extra whitespace) plus determinism/format checks for `getAvatarColor`.
- `apps/web/components/avatar-placeholder.tsx`: circle built on the shadcn `Avatar`/`AvatarFallback` primitives, colored via `getAvatarColor`, sized via `className` override (default `size-24`, larger than shadcn's built-in `sm`/`default`/`lg` presets which are too small for a profile-photo slot).
- `apps/web/components/card-form/photo-section.tsx`: mounted inside `IdentitySection` (see key-decisions). Hidden `<input type="file">` triggered by an "Enviar foto" button; on select, calls `upload()` from `@vercel/blob/client` against `/api/upload` with the user's Bearer token forwarded via `headers`, `access: "public"`, and live `onUploadProgress` reflected in the button label (`"Enviando... N%"`, button disabled meanwhile). On success, sets `photoUrl` in the form (triggering validation + dirty state) so the preview updates immediately, using `object-cover` for the circular crop-free preview (D-13). A 44×44px icon-only "Remover foto" button (`red-600`, no confirmation modal per `01-UI-SPEC.md`) clears `photoUrl`. Upload errors show `toast.error("Não foi possível enviar a foto. Tente novamente.")`.
- `apps/web/lib/card-schema.ts`: `photoUrl` optional; when filled, must parse as a valid URL (`z.url()`) and start with `https://` — client-side mirror of the server's host-allow-list intent (exact host-suffix check only happens server-side, since that's authoritative).
- `apps/api/Endpoints/CardEndpoints.cs`: new `IsValidPhotoUrl` helper (empty/null always valid per D-04/D-12; otherwise requires `Uri.TryCreate` to succeed as absolute, scheme `https`, and host ending in `.public.blob.vercel-storage.com` case-insensitively) — wired into both `CreateCardHandler` and `UpdateCardHandler` right after the existing Pix validation, before `SaveChangesAsync`. Returns `400 {"error":"photo_url_invalid"}` on failure (T-01-34: blocks a public page in a future phase from rendering `<img src>` pointed at an arbitrary/tracking host).
- `apps/api/Api.Tests/PhotoUrlTests.cs`: 6 integration tests against live Postgres (`TestAppFactory`) — allowed host via PUT returns 200 and persists the column; `http://` on the same allowed host returns 400; an entirely external host returns 400; a `javascript:` scheme returns 400; omitting `photoUrl` returns 200 with a null column; POST with an allowed host returns 201 with `photoUrl` echoed in the response. All 6 pass; full suite (`dotnet test apps/api/Api.Tests`) passes 81/81 with no regressions.
- `apps/web/components/card-form/card-form.tsx`: added server-error mapping for `photo_url_invalid` → `form.setError("photoUrl", ...)`, following the same pattern already used for `slug_taken`/`pix_key_invalid`/etc.
- Package legitimacy: `@vercel/blob@2.8.0` verified via `npm view` before installing (matches the plan's requirement to re-check the changelog before implementing) — confirmed current, and its `dist/client.d.ts` type definitions confirmed the `handleUpload`/`upload()` shapes still match the research's v1-era code example, with the one addition that `upload()`'s `access` option is required client-side in this version.
- Verification commands run: `npx tsc --noEmit` (0 errors, after `npm run build` generated Next.js 16's route-type declarations), `npm run build` (succeeded, `/api/upload` listed as a dynamic Node route), `npx vitest run` (54/54 passing across all files, including the new 10 `initials` cases), `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PhotoUrlTests` (6/6), full `dotnet test apps/api/Api.Tests` (81/81). All acceptance-criteria grep checks passed (`auth/me` count 2, `allowedContentTypes` count 1 with exactly the 3 allowed types, `NEXT_PUBLIC_BLOB` count 0 in `.env.example`, `object-cover` count 1, no crop libs in `package.json`).
- **Task 2 (human-verify checkpoint): approved.** All 8 steps confirmed by the developer: circle-with-initials placeholder before upload; upload with progress indicator and circular `object-cover` preview after; card save + reload preserves the photo; `photo_url` confirmed in Postgres pointing to `https://5ozcdcge7evm07s4.public.blob.vercel-storage.com/foto-perfil-....png` (matches the required `*.public.blob.vercel-storage.com` host pattern); "Remover foto" reverts to the placeholder and zeroes `photo_url` in the DB; `POST /api/upload` without `Authorization` confirmed returning 401 (also independently re-verified live by this executor via `curl`, both before and after the blob-store swap described below).

## Task Commits

Each task was committed atomically:

1. **Task 1: Upload autenticado da foto, prévia, remoção e placeholder de iniciais** - `4c93819` (feat)
2. **Task 2: Verificação humana do upload de foto (CARD-09)** - checkpoint, no code change; approved by the developer, no separate commit (verification-only task per `01-VALIDATION.md`'s manual-only classification)

_No separate "plan metadata" commit prior to this one — this SUMMARY.md's commit is the final commit for this worktree per parallel-executor protocol._

## Files Created/Modified

- `apps/web/app/api/upload/route.ts` - authenticated Vercel Blob token-issuance Route Handler
- `apps/web/lib/initials.ts`, `apps/web/lib/initials.test.ts` - getInitials/getAvatarColor + 10 Vitest cases
- `apps/web/components/avatar-placeholder.tsx` - circle-with-initials fallback component
- `apps/web/components/card-form/photo-section.tsx` - upload/preview/remove UI
- `apps/web/components/card-form/identity-section.tsx` - mounts `PhotoSection` at the top of the Identidade section
- `apps/web/components/card-form/card-form.tsx` - `photo_url_invalid` server-error mapping
- `apps/web/lib/card-schema.ts` - `photoUrl` optional-https validation
- `apps/web/.env.example` - `BLOB_READ_WRITE_TOKEN` documented (server-only, no `NEXT_PUBLIC_` prefix)
- `apps/web/package.json`, `apps/web/package-lock.json` - `@vercel/blob@2.8.0` added
- `apps/api/Endpoints/CardEndpoints.cs` - `IsValidPhotoUrl` helper wired into POST/PUT
- `apps/api/Api.Tests/PhotoUrlTests.cs` - 6 integration test cases

## Decisions Made

- Mounted `PhotoSection` inside `IdentitySection` (modifying `identity-section.tsx`, which was not in the plan's `files_modified` frontmatter list) because the plan's action text explicitly states "dentro da seção 'Identidade' (a foto pertence à identidade visual)" — treated the frontmatter list omission as incomplete bookkeeping rather than a directive to keep the photo outside that section.
- `upload()`'s client call passes `access: "public"` explicitly, required by `@vercel/blob@2.8.0`'s client-side types (not shown as required in the plan's/research's example, which predates this exact version's stricter typing) — no product ambiguity here since a card's photo is always meant to be publicly visible.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] `identity-section.tsx` needed modification even though absent from `files_modified`**
- **Found during:** Task 1, implementing the photo-section mount point
- **Issue:** The plan's action text placed the photo "dentro da seção Identidade," but `apps/web/components/card-form/identity-section.tsx` was not listed in the plan's `files_modified` frontmatter, and `PhotoSection` cannot render inside another component's JSX without modifying that component.
- **Fix:** Added an `import` and a single `<PhotoSection />` line at the top of `IdentitySection`'s returned JSX, above the "Nome completo" field.
- **Files modified:** `apps/web/components/card-form/identity-section.tsx`
- **Verification:** `npm run build` succeeds; manually confirmed in the human-verify checkpoint that the photo/placeholder renders inside the "Identidade" section, above the name field.
- **Committed in:** `4c93819` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking, file-list omission)
**Impact on plan:** No scope creep — the fix only made the plan's own action text executable; it did not add functionality beyond what Task 1 specified.

## Issues Encountered

- **Vercel Blob store recreated mid-verification (not a code bug).** During Task 2's manual testing, the first Blob store the developer created was configured for private access, causing uploads to fail with 400s against a working, correctly-authenticated route. The developer recreated the store as public-access on the Vercel dashboard and updated `apps/web/.env.local` in the main working tree with the new `BLOB_READ_WRITE_TOKEN`; that file was then re-copied into this worktree and the `npm run dev` process was restarted to pick up the new token. No code in this plan needed to change — `access: "public"` was already being requested client-side in `photo-section.tsx`; the issue was purely the store's dashboard configuration matching (or not) that request. **Flagged for local dev setup docs:** when creating the Vercel Blob store for local development, make sure it's configured for public access, not private, or client uploads will fail with a store-level 400 regardless of the app code being correct.
- A background-task wrapper twice reported the dev server processes as "failed" (exit code 127) during this session even though the underlying `next dev`/`dotnet run` processes were still alive and serving requests correctly when independently re-verified via `curl` — cosmetic/wrapper-level noise, not a real server crash, and not investigated further since it didn't block verification.

## User Setup Required

None beyond what was already documented in the plan's `user_setup` frontmatter (Vercel Blob Store creation + `BLOB_READ_WRITE_TOKEN`) — already completed by the developer prior to this plan's execution, with the one addendum noted above (ensure the store is public-access).

## Next Phase Readiness

- `CardResponseDto.PhotoUrl` flows through full server-side host validation now; the public card page (Phase 2) can safely render it as `<img src>` without an additional trust check, since the server already guarantees it's `https` and hosted on `*.public.blob.vercel-storage.com`.
- `AvatarPlaceholder`/`getInitials`/`getAvatarColor` are generic (accept a plain `fullName` string), reusable as-is by the Phase 2 public card page for the same no-photo fallback, without needing to duplicate the palette/initials logic.
- Plan 07 (Links sociais) is the last reserved section in `card-form.tsx` — no blockers from this plan.

---
*Phase: 01-conta-e-cart-o*
*Completed: 2026-08-14*

## Self-Check: PASSED

All 11 created/modified files verified present on disk: `apps/web/app/api/upload/route.ts`,
`apps/web/lib/initials.ts`, `apps/web/lib/initials.test.ts`, `apps/web/components/avatar-placeholder.tsx`,
`apps/web/components/card-form/photo-section.tsx`, `apps/api/Api.Tests/PhotoUrlTests.cs`,
`apps/web/components/card-form/identity-section.tsx`, `apps/web/components/card-form/card-form.tsx`,
`apps/web/lib/card-schema.ts`, `apps/web/.env.example`, `apps/api/Endpoints/CardEndpoints.cs`.
Commit hash `4c93819` verified present in `git log --oneline --all`.
