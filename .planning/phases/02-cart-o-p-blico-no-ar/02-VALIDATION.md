---
phase: 02
slug: cart-o-p-blico-no-ar
status: approved
nyquist_compliant: true
wave_0_complete: false  # criado dentro dos planos 02-01-T1, 02-02-T2/T3 e 02-04-T1
created: 2026-08-14
---

# Phase 02 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + `Microsoft.AspNetCore.Mvc.Testing` (backend) / Vitest 4.1.10 (frontend) |
| **Config file** | `apps/api/Api.Tests/Api.Tests.csproj`, `apps/web/vitest.config.ts` |
| **Quick run command (backend)** | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCard` |
| **Quick run command (frontend)** | `cd apps/web && npx vitest run <changed-file>.test.ts` |
| **Full suite command (backend)** | `dotnet test apps/api/Api.Tests` |
| **Full suite command (frontend)** | `cd apps/web && npx vitest run` |
| **Estimated runtime** | ~30-60s combined |

---

## Sampling Rate

- **After every task commit:** Run the targeted quick command for whatever file was just touched (backend or frontend, matching the change).
- **After every plan wave:** Run both full suites — `dotnet test apps/api/Api.Tests` and `cd apps/web && npx vitest run`.
- **Before `/gsd:verify-work`:** Full suite must be green, plus the manual verification steps below (no e2e framework exists in this repo to automate ISR/404-status/domain checks).
- **Max feedback latency:** ~60 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-01-T1 | 02-01 | 1 | PUB-01/PUB-05 | T-02-01, T-02-02 | Testes RED: GET público 200 sem Bearer, DTO sem campos privados, 404 em slug inexistente | integration (xUnit) | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCardTests` | ✅ criado nesta task | ⬜ pending |
| 02-01-T2 | 02-01 | 1 | PUB-01/PUB-05 | T-02-01, T-02-02, T-02-03, T-02-06 | Endpoint fora do grupo autorizado, AsNoTracking, SlugService.Normalize | integration (xUnit) | `dotnet test apps/api/Api.Tests` | ✅ (02-01-T1) | ⬜ pending |
| 02-01-T3 | 02-01 | 1 | PUB-01/PUB-02/PUB-05 | — | revalidate=60, sem loading.tsx, fetch sem Authorization/localStorage | static gate + tsc/eslint | `cd apps/web && npx tsc --noEmit && npx eslint "app/[slug]" components/public-card lib/public-card.ts` | N/A (gate estático) | ⬜ pending |
| 02-02-T1 | 02-02 | 1 | SHARE-01/SHARE-02 | T-02-SC | Gate humano bloqueante de legitimidade de `qrcode`/`@types/qrcode` ([ASSUMED]) | checkpoint | — | N/A | ⬜ pending |
| 02-02-T2 | 02-02 | 1 | SHARE-01/SHARE-02 | T-02-07 | buildCardUrl usa env var, headers de download gated, QR preto no branco | unit (vitest) | `cd apps/web && npx vitest run lib/qr.test.ts` | ✅ criado nesta task | ⬜ pending |
| 02-02-T3 | 02-02 | 1 | SHARE-01/SHARE-02 | T-02-08 | Content-Disposition só com ?download=1; PNG só com ?format=png | integration (vitest, handler real) | `cd apps/web && npx vitest run lib/qr-route.test.ts` | ✅ criado nesta task | ⬜ pending |
| 02-03-T1 | 02-03 | 2 | PUB-06 | T-02-10, T-02-11 | 404 não ecoa o slug; error.tsx não renderiza error.message/stack | static gate + tsc/eslint/vitest | `cd apps/web && npx tsc --noEmit && npx eslint "app/[slug]" lib/brand.ts && npx vitest run` | N/A (gate estático) | ⬜ pending |
| 02-03-T2 | 02-03 | 2 | PUB-06 | T-02-12 | `curl -I` de slug inexistente retorna 404 real; /login não é capturado por /[slug] | manual (`curl -I`) | — | N/A | ⬜ pending |
| 02-04-T1 | 02-04 | 2 | PUB-03 | T-02-14, T-02-15 | Pré-aquecimento dispara sem await e engole a própria falha | unit (vitest) | `cd apps/web && npx vitest run lib/prewarm.test.ts` | ✅ criado nesta task | ⬜ pending |
| 02-04-T2 | 02-04 | 2 | SHARE-01/SHARE-02 | T-02-16 | QR 240px sem tint; hrefs de download corretos; estado "salve primeiro" | static gate + tsc/eslint/vitest | `cd apps/web && npx tsc --noEmit && npx eslint components/public-card components/card-form && npx vitest run` | N/A (gate estático) | ⬜ pending |
| 02-04-T3 | 02-04 | 2 | PUB-03/SHARE-01 | T-02-15 | Dois disparos de prewarm (create+edit), cadeia de ApiError intacta | static gate + vitest | `cd apps/web && npx vitest run && grep -c 'prewarmPublicCard(payload.slug)' components/card-form/card-form.tsx` | ✅ (02-04-T1) | ⬜ pending |
| 02-05-T1 | 02-05 | 2 | BRAND-01 | — | Provisionamento externo + registro do domínio (lead time) | checkpoint | — | N/A | ⬜ pending |
| 02-05-T2 | 02-05 | 2 | PUB-04/BRAND-01 | T-02-19, T-02-21 | Imagem constrói sem SDK/testes/segredos; runbook sem credenciais | build gate | `docker build -f apps/api/Dockerfile -t vcard-api:local apps/api` | N/A (gate de build) | ⬜ pending |
| 02-05-T3 | 02-05 | 2 | PUB-04 | T-02-18, T-02-22, T-02-24 | /health 200 com banco up; rota pública 404 e não 401; cron a 5 min | manual (`curl -i` + histórico do cron) | — | N/A | ⬜ pending |
| 02-06-T1 | 02-06 | 3 | BRAND-01 | T-02-25, T-02-26 | Domínio com TLS válido; Cors__WebOrigin é a origem exata, nunca `*` | checkpoint | — | N/A | ⬜ pending |
| 02-06-T2 | 02-06 | 3 | BRAND-01 | T-02-29 | Runbook cobre os 9 requisitos da fase e não contém segredo | static gate (grep) | `grep -q "Checklist de go-live" docs/DEPLOY.md` + grep das 9 IDs | N/A (gate estático) | ⬜ pending |
| 02-06-T3 | 02-06 | 3 | PUB-01..06, SHARE-01/02, BRAND-01 | T-02-26, T-02-28 | Verificação end-to-end em produção: 404 real, cold start <2s, QR no domínio próprio | manual (`curl -I` + leitura de QR) | — | N/A | ⬜ pending |

*Preenchido pelo planner em 2026-08-14, após a criação dos 6 PLAN.md da fase.*

---

## Wave 0 Requirements

- [ ] `apps/api/Api.Tests/PublicCardTests.cs` — covers PUB-01, PUB-05, PUB-06, and the no-auth-required regression guard
- [ ] `apps/web/lib/qr.test.ts` + `apps/web/lib/qr-route.test.ts` — cobrem SHARE-01/SHARE-02 (formato + gating de Content-Disposition). **Decisão de planejamento:** os testes moram em `lib/` e não em `app/[slug]/qr/` porque `vitest.config.ts` restringe `include` a `lib/**/*.test.ts`; o handler é importado via alias `@/app/[slug]/qr/route`. Evita alterar a config e evita instalar biblioteca de teste de componente.
- [ ] `apps/web/lib/prewarm.test.ts` — cobre PUB-03 (mock de `fetch`, asserção do disparo e de que uma promise rejeitada não escapa). **Decisão de planejamento:** a lógica foi extraída para `lib/prewarm.ts` em vez de ficar inline em `card-form.tsx`, pelo mesmo motivo de config acima; nenhuma biblioteca de teste de componente é instalada.
- [ ] No new test framework/config install needed — both xUnit and Vitest infra already exist and are wired into both apps

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ISR revalidate window serves stale-then-fresh content within 60s | PUB-02 | No e2e framework in this repo to drive real HTTP timing against a deployed instance | Edit a card in the dashboard, hit `/[slug]` immediately (expect old data or fast refresh), wait 60s, hit again (expect new data) |
| 404 page has product identity + CTA, real HTTP 404 status | PUB-06 | Streaming/status-code behavior only observable via real HTTP response inspection | `curl -I https://<domain>/nonexistent-slug` — assert `HTTP/2 404`; visually confirm branded copy + CTA in browser |
| External keep-alive cron actually prevents cold start in practice | PUB-04 | Depends on third-party service execution history over real time, not reproducible in a test run | Check cron-job.org execution log after 24h; separately time a cold GET to `/[slug]` at a random hour and confirm sub-2s response |
| Domain resolves end-to-end after DNS propagation | BRAND-01 | External DNS/infra state, not part of the codebase | `curl -I https://vizzo.com.br` after registro.br + Vercel DNS setup; confirm 200 and valid TLS cert |
| QR downloaded from dashboard actually scans to the correct public URL | SHARE-01/SHARE-02 | Requires a physical/simulated camera scan, not automatable | Download SVG and PNG from the dashboard, scan both with a phone camera, confirm they open `/[slug]` |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies — as únicas exceções são tasks `checkpoint:*`, que por definição são verificadas por humano
- [x] Sampling continuity: no 3 consecutive tasks without automated verify — verificado plano a plano (o pior caso é 02-05/02-06, onde checkpoint → gate automatizado → checkpoint)
- [x] Wave 0 covers all MISSING references — os 3 arquivos de teste ausentes são criados dentro dos próprios planos (02-01-T1, 02-02-T2, 02-02-T3, 02-04-T1), antes da implementação que eles cobrem
- [x] No watch-mode flags — todos os comandos usam `vitest run` e `dotnet test`, nenhum modo watch
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved (planner, 2026-08-14)
