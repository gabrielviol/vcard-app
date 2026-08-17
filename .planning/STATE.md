---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: executing
stopped_at: 02-05 completo (backend no Render + Neon + keep-alive); pronto para 02-06
last_updated: "2026-08-17T19:34:21.051Z"
last_activity: 2026-08-17 -- 02-05 Task 3 completed (backend live on Render, cron-job.org keep-alive active)
progress:
  total_phases: 4
  completed_phases: 1
  total_plans: 13
  completed_plans: 12
  percent: 92
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-08-13)

**Core value:** Alguém recebe o cartão (por QR ou link) e consegue te chamar ou te pagar em um toque — sem sair da página, sem digitar nada, sem etapa intermediária.
**Current focus:** Phase 02 — cart-o-p-blico-no-ar

## Current Position

Phase: 02 (cart-o-p-blico-no-ar) — EXECUTING
Plan: 6 of 6
Status: 02-05 complete (Render + Neon + keep-alive live). Ready to execute 02-06 (Vercel deploy).
Last activity: 2026-08-17 -- 02-05 Task 3 completed (backend live on Render, cron-job.org keep-alive active)

Progress: [█████████░] 92%

## Performance Metrics

**Velocity:**

- Total plans completed: 7
- Average duration: - min
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01 | 7 | - | - |

**Recent Trend:**

- Last 5 plans: -
- Trend: -

*Updated after each plan completion*
| Phase 02 P05 | 30min | 3 tasks | 4 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Roadmap: cartão público + ISR + pré-aquecimento + QR ficam num único bloco (Fase 2) — separar criaria falsa sensação de "pronto" antes de resolver cold start.
- Roadmap: BRAND-01 (domínio) entra na Fase 2, não na fase final — evita bloquear o rodapé de marca/upgrade (Fase 4) e reconhece que registrar domínio é ação com lead time.
- Roadmap: validações de Pix (CARD-06/07) e normalização de WhatsApp (CARD-08) ficam na mesma fase que cria esses campos (Fase 1) — barato ali, caro depois.
- [Phase 02]: Backend .NET em producao no Render (vcard-app-tihd.onrender.com), containerizado, respondendo /health com banco Neon acordado; keep-alive cron-job.org ativo a cada 5 min. PUB-04 concluido; BRAND-01 permanece deferido (dominio nao registrado).

### Pending Todos

[From .planning/todos/pending/ — ideas captured during sessions]

None yet.

### Blockers/Concerns

[Issues that affect future work]

- **RESOLVIDO 2026-08-15 — precedência de rotas estáticas vs. dinâmicas (plano 02-03, Task 2):** verificado contra build de produção local. `curl -I http://localhost:3000/slug-que-nao-existe` retornou `404 Not Found` (não rebaixado a 200 por streaming); `curl -I http://localhost:3000/login` retornou `200 OK` (rota estática vence `/[slug]`, confirmado empiricamente). PUB-06 fechado. Os demais passos do checklist (1/2/3/5/7/8, inspeção visual/DOM) foram aprovados por sign-off do usuário sem evidência individualmente capturada — registrado honestamente no SUMMARY, não fabricado.
- Fase 3 (WhatsApp/Pix/.vcf em WebView) exige teste manual real dentro do app do Instagram/WhatsApp — não há documentação oficial da Meta sobre esse comportamento.
- **BRAND-01 (domínio) DEFERIDO por escolha do usuário, 2026-08-15/17:** usuário optou por adiar o registro de domínio (candidatos: vizzo.com.br primário, cartaum/pixtao/umtoque.com.br fallback, per D-14 em 02-CONTEXT.md) em vez de registrar agora, para evitar fricção/custo nesse momento. Não bloqueia o deploy: `Cors__WebOrigin` e `NEXT_PUBLIC_APP_URL` suportam valores provisórios `*.onrender.com`/`*.vercel.app` (D-15 proíbe URL hardcoded, então a troca pelo domínio real no plano 02-06 tem custo zero de código). Revisitar quando o usuário decidir registrar — não perguntar de novo proativamente.
- **Override registrado (Fase 1, planejamento):** o gate mecânico `check.decision-coverage-plan` reportou 11/13 decisões D-NN de `01-CONTEXT.md` como não cobertas, mesmo após citações explícitas `D-NN:` serem adicionadas ao objective de cada plan — o resultado do comando ficou idêntico byte-a-byte antes e depois da edição, e não foi possível localizar a implementação de `decision-coverage-plan` em nenhum arquivo instalado do `gsd-sdk`/`get-shit-done` (indício de bug/cache stale na ferramenta, não de lacuna real). O gate semântico `gsd-plan-checker` (revisão completa dos 7 planos, rodada 2x) confirmou explicitamente as 13 decisões cobertas com plan/task específico. Prosseguido com override (opção 3 do step 13a) dado o modo yolo.
- **RESOLVIDO 2026-08-14 — verificação manual do Walking Skeleton (plano 01-02, Task 4):** executada e aprovada. Os 10 passos do checklist (`01-02-PLAN.md` `<how-to-verify>`) passaram, incluindo a confirmação de que `password_hash` está em BCrypt (`$2a$...`) no Postgres. Walking Skeleton fechado; Wave 4 (01-04) liberada.
- **Override registrado (Fase 2, planejamento):** o gate mecânico `check.decision-coverage-plan` reportou 4/10 decisões D-NN de `02-CONTEXT.md` (D-15, D-17, D-20, D-21) como não cobertas — mesmo padrão observado na Fase 1 (indício de bug/cache stale na ferramenta, não de lacuna real). Confirmado por grep que as 4 decisões estão citadas explicitamente por ID nos planos (D-15 em 02-02/02-05/02-06, D-17 em 02-02/02-04/02-06, D-20/D-21 em 02-01/02-03/02-06). O gate semântico `gsd-plan-checker` também confirmou explicitamente as 10 decisões cobertas, dimensão por dimensão. Prosseguido com override (opção 3 do step 13a) dado o modo yolo.

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| *(none)* | | | |

## Session Continuity

Last session: 2026-08-17T19:34:21.033Z
Stopped at: 02-05 completo (backend no Render + Neon + keep-alive); pronto para 02-06
Resume file: .planning/phases/02-cart-o-p-blico-no-ar/02-06-PLAN.md
