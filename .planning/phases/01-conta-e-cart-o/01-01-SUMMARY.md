---
phase: 01-conta-e-cart-o
plan: 01
subsystem: auth
tags: [jwt, bcrypt, ef-core, postgres, minimal-api, xunit, docker-compose]

# Dependency graph
requires: []
provides:
  - "apps/api .NET 10 minimal API scaffold, compiling, with Api.sln (classic .sln format, not the SDK's new default .slnx)"
  - "Postgres local via Docker Compose (db + vcard_test), 4 domain tables migrated (users, cards, social_links, card_views)"
  - "Self-issued HMAC-SHA256 JWT auth: POST /auth/register, POST /auth/login, GET /auth/me"
  - "BCrypt password hashing (AuthService.HashPassword/VerifyPassword)"
  - "Named CORS policy (no AllowAnyOrigin), JwtBearer wired with HS256-only ValidAlgorithms and MapInboundClaims=false"
  - "/cards route group with .RequireAuthorization() and a placeholder POST (501) anchoring ACCT-05 ahead of plan 01-03's real handlers"
  - "apps/api/Api.Tests xUnit + WebApplicationFactory<Program> integration suite (12 tests) against real Postgres"
affects: [01-02, 01-03, 01-04, 01-05, 01-06, 01-07]

# Tech tracking
tech-stack:
  added:
    - "Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11"
    - "BCrypt.Net-Next 4.2.1"
    - "Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3"
    - "Microsoft.EntityFrameworkCore.Design/.Tools 10.0.11"
    - "Microsoft.AspNetCore.Mvc.Testing 10.0.11 + xunit (Api.Tests)"
    - "dotnet-ef global tool 10.0.11"
  patterns:
    - "Explicit snake_case column mapping in AppDbContext.OnModelCreating (ToTable/HasColumnName), no naming-convention package"
    - "TOCTOU-safe uniqueness: DB unique index + catch PostgresException.SqlState==23505 -> 409"
    - "Test config injected via process env vars in TestAppFactory ctor (not ConfigureAppConfiguration) because Program.cs reads required config eagerly via `?? throw` before Build()"
    - "xUnit CollectionBehavior(DisableTestParallelization=true) because all test classes share one physical vcard_test database"

key-files:
  created:
    - docker-compose.yml
    - docker/init-test-db.sql
    - apps/api/Api.sln
    - apps/api/Api.csproj
    - apps/api/.env.example
    - apps/api/Data/Entities/User.cs
    - apps/api/Data/Entities/Card.cs
    - apps/api/Data/Entities/SocialLink.cs
    - apps/api/Data/Entities/CardView.cs
    - apps/api/Data/AppDbContext.cs
    - apps/api/Data/Migrations/20260814015302_InitialSchema.cs
    - apps/api/Contracts/AuthDtos.cs
    - apps/api/Services/AuthService.cs
    - apps/api/Endpoints/AuthEndpoints.cs
    - apps/api/Api.Tests/Api.Tests.csproj
    - apps/api/Api.Tests/TestAppFactory.cs
    - apps/api/Api.Tests/HealthTests.cs
    - apps/api/Api.Tests/AuthTests.cs
    - apps/api/Api.Tests/AssemblyInfo.cs
  modified:
    - apps/api/Program.cs
    - .planning/phases/01-conta-e-cart-o/SKELETON.md

key-decisions:
  - "Forced dotnet new sln --format sln (classic .sln) because the .NET 10 SDK now defaults to the new .slnx XML format, which would have violated the plan's files_modified contract (apps/api/Api.sln)"
  - "Added JwtBearerOptions.MapInboundClaims = false so sub/email claims stay exactly as issued instead of being remapped to long ClaimTypes.* URIs by the default inbound claim mapping"
  - "Added a placeholder POST /cards (501) on the RequireAuthorization group so the ACCT-05 test has an actual routable endpoint to guard — an empty MapGroup has zero matched routes, so unauthenticated requests 404 before the auth middleware even runs"

patterns-established:
  - "Pattern: env-var-driven WebApplicationFactory test config (see tech-stack.patterns) — future backend test projects in this repo should follow the same TestAppFactory shape, not ConfigureAppConfiguration, given Program.cs's eager `?? throw` config reads"
  - "Pattern: disable xUnit test-class parallelization whenever multiple test classes hit the same physical integration-test database"

requirements-completed: [ACCT-01, ACCT-02, ACCT-05]

# Metrics
duration: 30min
completed: 2026-08-14
---

# Phase 1 Plan 1: Walking Skeleton (Backend) Summary

**Self-issued HMAC-SHA256 JWT auth (register/login/me) over BCrypt-hashed passwords, backed by a real Postgres schema (4 tables via EF Core migrations) running in Docker Compose, proven by a 12-test xUnit integration suite against the actual database.**

## Performance

- **Duration:** ~30 min (from Docker Compose scaffold to green test suite)
- **Started:** 2026-08-13T22:48:00-03:00 (approx, worktree creation)
- **Completed:** 2026-08-14T02:03:00Z
- **Tasks:** 3 automated tasks executed (Task 1 was a pre-approved package-legitimacy checkpoint, no files changed)
- **Files modified:** 20 created, 2 modified

## Accomplishments
- Postgres running locally via Docker Compose (`db` + `vcard_test`, healthcheck passing), `apps/api` scaffolded on .NET 10 and compiling clean (0 warnings, 0 errors)
- 4-table domain schema (users, cards, social_links, card_views) migrated and verified via `psql \dt`/`\d cards` — unique indexes on `users.email`, `cards.slug`, `cards.user_id`; `Card.PixConsentConfirmed` deliberately added per 01-RESEARCH.md Security Domain
- `POST /auth/register`, `POST /auth/login`, `GET /auth/me` working end-to-end against real Postgres: BCrypt hash (`$2` prefix) never plaintext, generic `invalid_credentials` message for both wrong-password and unknown-email (no user-enumeration oracle), `GET /auth/me`/`POST /cards` both 401 without a valid token
- 12/12 xUnit integration tests green (`RegisterTests`, `LoginTests`, `AuthGuardTests`, `HealthTests`), including expired-token and wrong-signature-token 401 cases

## Task Commits

Each task was committed atomically:

1. **Task 1: Package legitimacy gate** — no commit (checkpoint only; developer pre-approved all 15 packages per orchestrator's `<prior_attempt_note>`, `use-debounce` explicitly excluded)
2. **Task 2: Scaffold apps/api and Postgres via Docker Compose** — `dce3811` (feat)
3. **Task 3: Domain schema, DbContext, Program.cs JWT/CORS wiring, migration applied** — `18751ae` (feat)
4. **Task 4: Auth endpoints/service, xUnit suite, SKELETON.md update** — `8fe0636` (feat)

_No separate "plan metadata" commit yet — this SUMMARY.md itself is the final commit for this worktree per parallel-executor protocol._

## Files Created/Modified
- `docker-compose.yml`, `docker/init-test-db.sql` - Postgres 17 service (`db`) + `vcard_test` init script
- `apps/api/Api.sln`, `apps/api/Api.csproj` - .NET 10 solution/project, `Compile/Content Remove="Api.Tests/**"` to keep the test project out of the API build
- `apps/api/.env.example` - documents `SSL Mode=Disable` (local) vs `SSL Mode=Require;Trust Server Certificate=true` (Neon deploy), never omits `SSL Mode`
- `apps/api/Data/Entities/*.cs`, `apps/api/Data/AppDbContext.cs` - 4 entities, explicit snake_case mapping, unique indexes, cascade deletes
- `apps/api/Data/Migrations/20260814015302_InitialSchema.cs` - initial schema migration, applied against local Postgres
- `apps/api/Contracts/AuthDtos.cs` - RegisterRequest/LoginRequest/UserDto/AuthResponse (never serializes PasswordHash)
- `apps/api/Services/AuthService.cs` - BCrypt hash/verify, JWT issuance (`sub`/`email` claims only, HS256, 20min expiry)
- `apps/api/Endpoints/AuthEndpoints.cs` - register/login/me handlers with TOCTOU-safe 23505 handling
- `apps/api/Program.cs` - DbContext, JwtBearer (HS256-only, `MapInboundClaims=false`, ClockSkew 30s), named CORS policy, `/health`, auth endpoints wired, `/cards` group with placeholder POST
- `apps/api/Api.Tests/*` - TestAppFactory (env-var config override + migrate + reset), HealthTests, AuthTests (RegisterTests/LoginTests/AuthGuardTests), AssemblyInfo (disables parallelization)
- `.planning/phases/01-conta-e-cart-o/SKELETON.md` - marked backend scaffold/routing/database items delivered by this plan; UI and full local-stack deployment remain pending for plan 01-02

## Decisions Made
- Used classic `.sln` format (`dotnet new sln --format sln`) instead of the SDK's new default `.slnx`, to match the plan's `files_modified` contract literally
- `MapInboundClaims = false` on JwtBearerOptions so downstream code can read `sub`/`email` claim types exactly as issued by AuthService, rather than ASP.NET Core's default WS-* URI remapping
- Test configuration overridden via process environment variables in `TestAppFactory`'s constructor rather than `WebApplicationFactory.ConfigureAppConfiguration`, because `Program.cs` reads `ConnectionStrings__Default`/`JWT_SECRET` eagerly (`?? throw`) before `builder.Build()` runs, and `ConfigureAppConfiguration` overrides are only injected at `Build()` time — too late for those eager reads. Environment variables are visible immediately since `AddEnvironmentVariables()` is one of `CreateBuilder`'s default sources, and this also matches the interfaces contract ("chaves de configuração lidas de variável de ambiente").

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] `dotnet new sln` produces `.slnx` by default on this SDK, not `.sln`**
- **Found during:** Task 2 (project scaffold)
- **Issue:** The installed .NET 10 SDK's `dotnet new sln` template defaults to the new XML solution format (`Api.slnx`), but the plan's `files_modified` list and acceptance criteria reference `Api.sln`
- **Fix:** Re-ran with `dotnet new sln -o apps/api -n Api --format sln` to force the classic format
- **Files modified:** `apps/api/Api.sln`
- **Verification:** `dotnet sln apps/api/Api.sln add ...` and subsequent builds succeeded
- **Committed in:** `dce3811`

**2. [Rule 1 - Bug] `/auth/me` returned 401 with a valid, correctly-signed token**
- **Found during:** Task 4 (manual smoke test after xUnit showed the same failure)
- **Issue:** JwtBearerHandler logged "Successfully validated the token" / "was successfully authenticated", but `MeHandler`'s `principal.FindFirstValue("sub")` still returned null — ASP.NET Core's default inbound claim mapping was remapping the JWT's literal `sub` claim to a different `ClaimTypes.*` URI
- **Fix:** Set `options.MapInboundClaims = false` in the JwtBearerOptions configuration delegate in `Program.cs`
- **Files modified:** `apps/api/Program.cs`
- **Verification:** Manual curl smoke test confirmed 200 OK with correct email; xUnit `AuthGuardTests.Me_WithValidToken_Returns200WithCorrectEmail` passes
- **Committed in:** `8fe0636`

**3. [Rule 1 - Bug] Test-suite flakiness: cross-class database truncation**
- **Found during:** Task 4 (xUnit run after fix #2, `Me_WithValidToken` still failed with `Unauthorized` despite passing manual smoke test)
- **Issue:** All four test classes (`RegisterTests`, `LoginTests`, `AuthGuardTests`, `HealthTests`) each have their own `TestAppFactory` instance, but all point at the same physical `vcard_test` Postgres database. xUnit runs different test classes in parallel by default, so one class's `ResetDatabaseAsync()` (`TRUNCATE ... CASCADE`) could wipe rows another class's in-flight test had just written between its register and me calls
- **Fix:** Added `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in a new `apps/api/Api.Tests/AssemblyInfo.cs`
- **Files modified:** `apps/api/Api.Tests/AssemblyInfo.cs` (new)
- **Verification:** `dotnet test apps/api/Api.Tests` now consistently green, 12/12, across multiple re-runs
- **Committed in:** `8fe0636`

**4. [Rule 2 - Missing Critical] `POST /cards` without a token returned 404, not 401**
- **Found during:** Task 4 (writing `AuthGuardTests.PostCards_WithoutToken_Returns401`, an explicit acceptance criterion of this task)
- **Issue:** The plan's Program.cs instructions only create an empty `app.MapGroup("/cards").RequireAuthorization()` with no registered route inside it (real handlers arrive in plan 01-03). ASP.NET Core's routing returns 404 for unmatched routes before authorization middleware runs, so an empty group cannot 401 anything
- **Fix:** Added a minimal placeholder `cards.MapPost("/", () => Results.StatusCode(501))` so the group has an actual routable, authorization-guarded endpoint. Commented as a placeholder to be replaced by plan 01-03's real handler
- **Files modified:** `apps/api/Program.cs`
- **Verification:** `AuthGuardTests.PostCards_WithoutToken_Returns401` passes
- **Committed in:** `8fe0636`

**5. [Rule 3 - Blocking] EF Core package version conflict in Api.Tests**
- **Found during:** Task 4 (`dotnet build apps/api/Api.Tests/Api.Tests.csproj`)
- **Issue:** `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 pulled a transitive `Microsoft.EntityFrameworkCore` 10.0.4 reference that conflicted with `Api.csproj`'s direct 10.0.11 reference (CS1705 assembly version mismatch), blocking the build
- **Fix:** Added explicit `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` 10.0.11 PackageReferences to `Api.Tests.csproj` to force version alignment
- **Files modified:** `apps/api/Api.Tests/Api.Tests.csproj`
- **Verification:** Clean build, 0 warnings, 0 errors
- **Committed in:** `8fe0636`

---

**Total deviations:** 5 auto-fixed (1 blocking/tooling-format, 1 bug, 1 test-isolation bug, 1 missing-critical/test-coverage, 1 blocking/dependency-conflict)
**Impact on plan:** All fixes were necessary for the plan's own stated acceptance criteria (Task 4 explicitly requires the `AuthGuardTests` suite, including the `POST /cards` case, to pass with 0 failures). No scope creep — no functionality was added beyond what Task 4 already specified; the placeholder `POST /cards` handler is explicitly marked for replacement in plan 01-03, consistent with the plan's own text ("os handlers entram no plano 03").

## Issues Encountered
None beyond the deviations documented above — all were investigated and resolved within the plan's own scope.

## User Setup Required
None - no external service configuration required. Local Postgres runs via `docker compose up -d db`; `dotnet-ef` global tool and all 5 approved NuGet packages were installed as part of Task 2.

## Next Phase Readiness
- Backend walking skeleton complete: `apps/api` compiles, migrates, and serves a working auth flow against real Postgres
- Plan `01-02` (frontend scaffold, session, login/register screens) can now consume `POST /auth/register`, `POST /auth/login`, `GET /auth/me` per the literal response shapes documented in this plan's `<interfaces>` block
- Plan `01-03` must replace the placeholder `POST /cards` (501) with the real card-creation handler (slug uniqueness, reserved words) — this placeholder is a known, intentional stub, not a gap
- No blockers. `docker compose up -d db` + `dotnet run --project apps/api` is the full local backend startup command, documented in `.env.example` and `SKELETON.md`

---
*Phase: 01-conta-e-cart-o*
*Completed: 2026-08-14*

## Self-Check: PASSED

All 20 created files verified present on disk. All 4 commit hashes (`dce3811`, `18751ae`, `8fe0636`, `9a5eb1f`) verified present in `git log --oneline --all`.
