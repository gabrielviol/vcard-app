---
phase: 02-cart-o-p-blico-no-ar
plan: 05
subsystem: api
tags: [docker, deploy, neon, render, migrations, keep-alive, infra]

# Dependency graph
requires:
  - phase: 02-cart-o-p-blico-no-ar
    plan: 01
    provides: "apps/api Program.cs eager config reads, /health endpoint, EF Core InitialSchema migration"
provides:
  - "apps/api/Dockerfile -- multi-stage build (sdk:10.0 -> aspnet:10.0), publish targets Api.csproj explicitly"
  - "apps/api/.dockerignore -- excludes bin/obj/Api.Tests/appsettings.Development.json from build context"
  - "docs/DEPLOY.md -- production runbook (topology, env vars, Render Web Service setup, migrations, keep-alive, cold start, secret rotation)"
  - "Neon production Postgres with InitialSchema migration applied (users, cards, social_links, card_views)"
affects: [02-06 (consumes NEXT_PUBLIC_API_URL from the Render URL produced in Task 3, and the domain decision once BRAND-01 unblocks)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Multi-stage Dockerfile: build stage restores/publishes Api.csproj explicitly (never Api.sln, keeps Api.Tests and its versioned TestJwtSecret out of the runtime image)"
    - "dotnet ef database update invoked with --connection pointing at Neon, all 5 of Program.cs's eagerly-read env vars set in the shell session only (never written to a file) -- required even for design-time/migration tooling because Program.cs throws before builder.Build()"

key-files:
  created:
    - apps/api/Dockerfile
    - apps/api/.dockerignore
    - docs/DEPLOY.md
  modified: []

key-decisions:
  - "Task 1 (external account provisioning) treated as already satisfied from a prior session -- Neon/Render/Vercel/cron-job.org accounts confirmed, domain (BRAND-01) explicitly deferred by user choice and logged in STATE.md; not re-asked in this run"
  - "Migration applied directly via dotnet ef database update from the dev machine against Neon, with placeholder (non-secret) values for JWT_SECRET/Jwt__Issuer/Jwt__Audience/Cors__WebOrigin passed only as session env vars -- these 4 are required by dotnet ef's design-time host build (which goes through Program.cs) even though they have no bearing on the migration itself"

requirements-completed: []
# PUB-04 and BRAND-01 are NOT marked complete: PUB-04 requires the live Render deploy +
# cron-job.org keep-alive confirmation from Task 3 (not yet executed, human-dashboard-only);
# BRAND-01 remains explicitly deferred per STATE.md.

# Metrics
duration: ~25min (Task 2 execution: Dockerfile authoring, Docker Desktop cold start, build, migration, runbook)
completed: 2026-08-17
---

# Phase 2 Plan 5: Backend no Ar (Render + Neon + Keep-alive) Summary

**apps/api containerizado (multi-stage .NET 10 Docker build) e o schema InitialSchema aplicado no Postgres de produção (Neon); runbook docs/DEPLOY.md versionado sem segredos. Backend ainda não está publicamente no ar -- isso depende de Task 3 (checkpoint humano no dashboard do Render + cron-job.org).**

## Performance

- **Tasks:** 2/3 completed this run (Task 1 já estava satisfeito de sessão anterior; Task 2 completo; Task 3 é checkpoint bloqueante para o usuário)
- **Files created:** 3 (`apps/api/Dockerfile`, `apps/api/.dockerignore`, `docs/DEPLOY.md`)
- **Verification:** `docker build` exit 0; runtime image confirmed to contain `Microsoft.AspNetCore.App 10.0.11`; all Dockerfile/`.dockerignore`/`docs/DEPLOY.md` acceptance-criteria greps pass; `grep -rE "postgres://|npg_|password="` across `docs/` and `apps/api/Dockerfile` returns no matches; Neon migration confirmed idempotent (`dotnet ef database update` re-run reports "already up to date")

## Accomplishments

### Task 1 (satisfied from prior session -- not re-executed)

Per explicit instruction from the resuming session, Task 1's checkpoint outputs were already collected and are treated as done:

- **Neon:** production project created, connection string received (`ep-cold-heart-ay809kr8.c-5.us-east-2.aws.neon.tech` / database `neondb`), converted to the .NET/Npgsql format with `SSL Mode=Require;Trust Server Certificate=true` explicit, used only via session env var (never written to a file).
- **Render:** account created, connected to GitHub repo `gabrielviol/vcard-app` (default branch renamed `master` -> `main`, pushed). No Web Service created yet (that's Task 3).
- **Vercel:** confirmed accessible (same account as the Phase 1 Vercel Blob token).
- **cron-job.org:** account created. No cronjob configured yet (that's Task 3).
- **BRAND-01 (domain):** explicitly deferred by user choice, already logged in `.planning/STATE.md` under "BRAND-01 (domínio) DEFERIDO" -- not re-raised in this run per instruction.

### Task 2 (this run)

- `apps/api/.dockerignore` -- excludes `bin/`, `obj/`, `Api.Tests/`, `**/*.user`, `appsettings.Development.json`, `.vs/` from the build context.
- `apps/api/Dockerfile` -- multi-stage build:
  - Build stage `mcr.microsoft.com/dotnet/sdk:10.0`: copies `Api.csproj` alone first (cacheable restore layer), runs `dotnet restore Api.csproj`, then copies the rest of the context and runs `dotnet publish Api.csproj -c Release -o /app/publish --no-restore` -- targets the `.csproj` explicitly, never `Api.sln` (which would drag in `Api.Tests` and its versioned `TestJwtSecret`).
  - Runtime stage `mcr.microsoft.com/dotnet/aspnet:10.0`: copies `/app/publish`, sets `ENV ASPNETCORE_HTTP_PORTS=8080`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet", "Api.dll"]`. No dev config, no `ASPNETCORE_ENVIRONMENT=Development`.
  - Verified locally: `docker build -f apps/api/Dockerfile -t vcard-api:local apps/api` exits 0; `docker run --rm --entrypoint dotnet vcard-api:local --list-runtimes` (entrypoint override, since the image's fixed `ENTRYPOINT` swallows plain trailing args) shows `Microsoft.AspNetCore.App 10.0.11`; `docker run --rm --entrypoint ls vcard-api:local /app` confirms only publish output made it into the image (no `Api.Tests`, no `appsettings.Development.json`, no test project DLLs).
- Applied the `20260814015302_InitialSchema` migration to the Neon production database: `dotnet restore Api.csproj` (fresh worktree had no `obj/project.assets.json`), then `dotnet ef database update --project Api.csproj --connection "<Neon connection string, session env var only>"`. Confirmed idempotent by re-running the same command, which reported "No migrations were applied. The database is already up to date." The full `CREATE TABLE` output during the first run showed all 4 tables (`users`, `cards`, `card_views`, `social_links`) plus their indexes and foreign keys created exactly as in the migration file.
- `docs/DEPLOY.md` (136 lines) -- production runbook in Portuguese: Topologia, tabela de variáveis de ambiente do Render (todas as 5 chaves lidas eagerly por `Program.cs` + `ASPNETCORE_HTTP_PORTS`), tabela de variáveis da Vercel, passo a passo de criação do Web Service, seção de Migrations (comando exato, nota sobre as 4 env vars extras exigidas pelo `dotnet ef` design-time host), seção de Keep-alive (justificativa do intervalo de 5 min, alternativas descartadas, fallback GitHub Actions), tabela de Cold start esperado, e procedimento de Rotação de `JWT_SECRET`.

## Task Commits

1. **Task 2: Dockerfile, .dockerignore, DEPLOY.md, Neon migration**
   - `efdfc63` feat(02-05): containerize apps/api and migrate Neon production schema

Task 1 produced no code changes (external provisioning only) -- its outputs are the Neon connection string (session-only, never committed) and account confirmations, already logged in `.planning/STATE.md` from the prior session (`b264b7c`).

**Plan metadata:** committed alongside this SUMMARY (worktree mode -- orchestrator handles STATE.md/ROADMAP.md after merge).

## Files Created/Modified

- `apps/api/Dockerfile` -- production container build, verified locally with `docker build`
- `apps/api/.dockerignore` -- build-context exclusions, critical for keeping dev config and test artifacts out of the image
- `docs/DEPLOY.md` -- the reproducible runbook Task 3 (and the human operator) follows to actually put the backend on Render

## Decisions Made

- Treated Task 1 as already satisfied per the resuming session's explicit instruction -- did not re-run its checkpoint or re-ask the user for any of the 4 accounts or the domain decision.
- Passed 4 non-secret placeholder values (`JWT_SECRET`, `Jwt__Issuer`, `Jwt__Audience`, `Cors__WebOrigin`) as session-only env vars to satisfy `dotnet ef`'s design-time host build (which constructs the app via `Program.cs`, hitting the same eager `?? throw` reads as the real app) -- these placeholders never touch a file and have no effect on the migration itself, which only cares about `ConnectionStrings__Default`.
- Verified the Dockerfile's runtime image with `docker run --entrypoint dotnet ... --list-runtimes` (entrypoint override) rather than the plan's literal `docker run vcard-api:local dotnet --list-runtimes` -- with a fixed `ENTRYPOINT ["dotnet", "Api.dll"]`, trailing `docker run` arguments are appended to (not substituted into) the entrypoint command, so the literal form in the plan's `<verify>` block would not actually invoke `dotnet --list-runtimes`. This is documented as a Rule 1 auto-fix below.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Plan's literal Docker runtime-verification command does not exercise `--list-runtimes` against a fixed `ENTRYPOINT`**
- **Found during:** Task 2 verification
- **Issue:** The plan's `<verify><automated>` block specifies `docker run --rm vcard-api:local dotnet --list-runtimes | grep -q "Microsoft.AspNetCore.App 10"`. With `ENTRYPOINT ["dotnet", "Api.dll"]` set (as the plan's own `<action>` requires), Docker appends any trailing `docker run` arguments to the entrypoint rather than replacing it -- the actual command executed becomes `dotnet Api.dll dotnet --list-runtimes`, which just starts the app (and fails on missing env vars) instead of listing runtimes.
- **Fix:** Verified with `docker run --rm --entrypoint dotnet vcard-api:local --list-runtimes` instead (explicit entrypoint override), which correctly printed `Microsoft.AspNetCore.App 10.0.11` and `Microsoft.NETCore.App 10.0.11`. The Dockerfile itself is unchanged -- this was purely a correction to how the verification command was invoked, not a change to the built artifact.
- **Files modified:** none (verification-only)
- **Commit:** N/A (no file change)

**2. [Rule 3 - Blocking] Fresh worktree had no `obj/project.assets.json`, blocking `dotnet ef database update`**
- **Found during:** Task 2, first `dotnet ef database update` attempt (`NETSDK1004: Arquivo de ativos ... não encontrado`)
- **Issue:** This worktree had never run `dotnet restore` for `apps/api`, so EF Tools could not resolve project metadata.
- **Fix:** Ran `dotnet restore Api.csproj` once before retrying the migration.
- **Files modified:** none tracked (`obj/` is gitignored)
- **Verification:** subsequent `dotnet ef database update` succeeded

**3. [Rule 3 - Blocking] Docker Desktop was not running at task start**
- **Found during:** Task 2, first `docker build` attempt (`error during connect ... dockerDesktopLinuxEngine`)
- **Issue:** The local Docker Desktop daemon was not started, so the Docker CLI could not connect.
- **Fix:** Launched `Docker Desktop.exe` and polled `docker info` until it responded (a few minutes for the daemon to come up), then proceeded with the build.
- **Files modified:** none
- **Verification:** `docker build` succeeded immediately after the daemon reported ready

**4. [Rule 3 - Blocking] `dotnet ef database update`'s design-time host build requires all 5 of `Program.cs`'s eagerly-read config keys, not just `ConnectionStrings__Default`**
- **Found during:** Task 2, second `dotnet ef database update` attempt (`Unable to create a 'DbContext' ... JWT_SECRET not set`)
- **Issue:** EF Tools builds the app host via `Program.cs` to discover the `DbContext`, which hits the same eager `?? throw` reads as the real app -- `JWT_SECRET`, `Jwt:Issuer`, `Jwt:Audience`, and `Cors:WebOrigin` all needed to be set in the session, even though none of them affect the migration.
- **Fix:** Set 4 non-secret placeholder values (`Jwt__Issuer=vcard-api`, `Jwt__Audience=vcard-web`, `Cors__WebOrigin=http://localhost:3000`, and a clearly-labeled placeholder `JWT_SECRET`) as session env vars alongside `ConnectionStrings__Default`, matching the values `TestAppFactory.cs` already uses for issuer/audience.
- **Files modified:** none (session env vars only, documented in `docs/DEPLOY.md`'s Migrations section so future migration runs aren't surprised by this)
- **Verification:** migration applied successfully; re-run confirmed idempotent

---

**Total deviations:** 4 auto-fixed (1 verification-command correction, 3 environment/blocking). No architectural changes, no scope creep.
**Impact on plan:** None on the delivered artifacts (Dockerfile, `.dockerignore`, `docs/DEPLOY.md` match the plan's `<action>` exactly). All 4 deviations were either local-environment setup gaps or a verification-command correction.

## Issues Encountered

None beyond the deviations documented above.

## User Setup Required (Task 3 -- NOT executed, checkpoint reached)

Task 3 (`type="checkpoint:human-verify"`, `gate="blocking"`) requires the human to click through the Render and cron-job.org dashboards -- no API token is available to automate this. See the parent session's response for the full step-by-step instructions relayed to the user, built directly from `docs/DEPLOY.md`'s "Criação do Web Service no Render" and "Keep-alive" sections:

1. Create the Render Web Service (Docker runtime, root directory `apps/api`, Dockerfile `apps/api/Dockerfile`, Free plan, health check path `/health`).
2. Fill in the 6 env vars from `docs/DEPLOY.md`'s Render table (`ConnectionStrings__Default`, `JWT_SECRET` -- a **new** random 32+ byte value, never the dev/test one --, `Jwt__Issuer=vcard-api`, `Jwt__Audience=vcard-web`, `Cors__WebOrigin` -- provisional `*.vercel.app` value acceptable now --, `ASPNETCORE_HTTP_PORTS=8080`).
3. After first deploy, run `curl -i https://<render-url>/health` (expect `200` + `"database":"up"`) and `curl -i https://<render-url>/public/cards/slug-que-nao-existe` (expect `404`, not `401`).
4. Create a cron-job.org cronjob: `GET https://<render-url>/health`, 5-minute interval.
5. After ~30 minutes, confirm ≥5 consecutive 200s in the cron-job.org execution history with no 30-60s latency spikes.
6. Record the Render public URL for plan `02-06`'s `NEXT_PUBLIC_API_URL`.

PUB-04 and the "backend publicly reachable" truth are not yet satisfied -- both depend on this checkpoint's outputs.

## Next Phase Readiness

- `apps/api/Dockerfile` is validated and ready for Render to build directly from the repo -- no further code changes needed for Task 3.
- Neon schema is live and confirmed idempotent against re-migration.
- `docs/DEPLOY.md` is the single source of truth Task 3 (and any future re-deploy) should follow.
- Plan `02-06` is blocked on Task 3's Render URL (`NEXT_PUBLIC_API_URL`) and on the still-deferred domain decision (`NEXT_PUBLIC_APP_URL` provisional value acceptable per D-15).

---
*Phase: 02-cart-o-p-blico-no-ar*
*Completed: Task 2 only -- 2026-08-17 (Task 3 checkpoint pending human dashboard actions)*

## Self-Check: PASSED

All claimed files verified present: `apps/api/Dockerfile`, `apps/api/.dockerignore`, `docs/DEPLOY.md`, this SUMMARY.md.
All claimed commit hashes verified present in git log: `efdfc63`.
Neon migration state verified via `dotnet ef migrations list --connection` showing `20260814015302_InitialSchema` applied, and a repeat `dotnet ef database update` reporting no pending migrations.
