---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: ready_to_plan
stopped_at: Phase 02 complete (6/6) — ready to discuss Phase 3
last_updated: 2026-08-18T17:25:03.956Z
last_activity: 2026-08-18 -- 02-06 complete (frontend live on Vercel, phase 2 done; BRAND-01 domain remains deferred)
progress:
  total_phases: 4
  completed_phases: 2
  total_plans: 13
  completed_plans: 13
  percent: 50
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-08-13)

**Core value:** Alguém recebe o cartão (por QR ou link) e consegue te chamar ou te pagar em um toque — sem sair da página, sem digitar nada, sem etapa intermediária.
**Current focus:** Phase 3 — contato, pagamento e compartilhamento

## Current Position

Phase: 3
Plan: Not started
Status: Ready to plan
Last activity: 2026-08-18 -- Phase 2 complete (6/6 plans), BRAND-01 deferred

Progress: [█████░░░░░] 50%

## Performance Metrics

**Velocity:**

- Total plans completed: 13
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
- [Phase 02 completa, 2026-08-18]: Frontend em producao na Vercel (`vcard-app-one.vercel.app`), CORS do Render fechado para essa origem, `docs/DEPLOY.md` com runbook completo e checklist de go-live. 6/6 planos da fase concluidos. BRAND-01 (dominio proprio) segue deferido por escolha explicita do usuario — nao e lacuna tecnica, e decisao de produto a revisitar depois, sem custo de codigo (D-15).

### Pending Todos

[From .planning/todos/pending/ — ideas captured during sessions]

None yet.

### Blockers/Concerns

[Issues that affect future work]

- **RESOLVIDO 2026-08-15 — precedência de rotas estáticas vs. dinâmicas (plano 02-03, Task 2):** verificado contra build de produção local. `curl -I http://localhost:3000/slug-que-nao-existe` retornou `404 Not Found` (não rebaixado a 200 por streaming); `curl -I http://localhost:3000/login` retornou `200 OK` (rota estática vence `/[slug]`, confirmado empiricamente). PUB-06 fechado. Os demais passos do checklist (1/2/3/5/7/8, inspeção visual/DOM) foram aprovados por sign-off do usuário sem evidência individualmente capturada — registrado honestamente no SUMMARY, não fabricado.
- Fase 3 (WhatsApp/Pix/.vcf em WebView) exige teste manual real dentro do app do Instagram/WhatsApp — não há documentação oficial da Meta sobre esse comportamento.
- **BRAND-01 (domínio) DEFERIDO por escolha do usuário, 2026-08-15/17/18:** usuário optou por adiar o registro de domínio (candidatos: vizzo.com.br primário, cartaum/pixtao/umtoque.com.br fallback, per D-14 em 02-CONTEXT.md) em vez de registrar agora, para evitar fricção/custo nesse momento. Não bloqueou o deploy: plano 02-06 foi executado inteiro com `Cors__WebOrigin`/`NEXT_PUBLIC_APP_URL` apontando para `https://vcard-app-one.vercel.app` (D-15 proíbe URL hardcoded, então a troca pelo domínio real tem custo zero de código quando acontecer — procedimento documentado em `docs/DEPLOY.md`, seção "Trocar o domínio"). Fase 2 encerrada com esse item explicitamente deferido, não escondido — ver Deferred Items abaixo. Revisitar quando o usuário decidir registrar — não perguntar de novo proativamente.
- **Anomalia da ferramenta, 2026-08-18 (transição Fase 2 → 3):** `gsd-sdk query phase.complete` atualizou `total_plans`/`completed_plans` para 13/13 mas deixou `completed_phases` em 1 (não incrementou para 2) e recalculou `percent` para 25% (consistente com 1/4 fases, não com 13/13 planos) — mesma categoria de inconsistência já observada com `check.decision-coverage-plan` nas Fases 1 e 2 (ver entradas de "Override registrado" abaixo). Corrigido manualmente nesta sessão: `completed_phases: 2`, `percent: 50` (baseado em fases, não em planos, já que Fases 3/4 ainda têm contagem de planos "TBD"). Não é uma decisão de produto, é um registro de discrepância da ferramenta para não confundir sessões futuras.
- **Override registrado (Fase 1, planejamento):** o gate mecânico `check.decision-coverage-plan` reportou 11/13 decisões D-NN de `01-CONTEXT.md` como não cobertas, mesmo após citações explícitas `D-NN:` serem adicionadas ao objective de cada plan — o resultado do comando ficou idêntico byte-a-byte antes e depois da edição, e não foi possível localizar a implementação de `decision-coverage-plan` em nenhum arquivo instalado do `gsd-sdk`/`get-shit-done` (indício de bug/cache stale na ferramenta, não de lacuna real). O gate semântico `gsd-plan-checker` (revisão completa dos 7 planos, rodada 2x) confirmou explicitamente as 13 decisões cobertas com plan/task específico. Prosseguido com override (opção 3 do step 13a) dado o modo yolo.
- **RESOLVIDO 2026-08-14 — verificação manual do Walking Skeleton (plano 01-02, Task 4):** executada e aprovada. Os 10 passos do checklist (`01-02-PLAN.md` `<how-to-verify>`) passaram, incluindo a confirmação de que `password_hash` está em BCrypt (`$2a$...`) no Postgres. Walking Skeleton fechado; Wave 4 (01-04) liberada.
- **Override registrado (Fase 2, planejamento):** o gate mecânico `check.decision-coverage-plan` reportou 4/10 decisões D-NN de `02-CONTEXT.md` (D-15, D-17, D-20, D-21) como não cobertas — mesmo padrão observado na Fase 1 (indício de bug/cache stale na ferramenta, não de lacuna real). Confirmado por grep que as 4 decisões estão citadas explicitamente por ID nos planos (D-15 em 02-02/02-05/02-06, D-17 em 02-02/02-04/02-06, D-20/D-21 em 02-01/02-03/02-06). O gate semântico `gsd-plan-checker` também confirmou explicitamente as 10 decisões cobertas, dimensão por dimensão. Prosseguido com override (opção 3 do step 13a) dado o modo yolo.

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| Product/brand | BRAND-01 — registro de domínio próprio | Deferido por escolha do usuário | Fase 2 (planos 02-05/02-06), 2026-08-15/17/18 |

## Session Continuity

Last session: 2026-08-18T17:25:03.956Z
Stopped at: Phase 02 complete (6/6) — ready to discuss/plan Phase 3
Resume file: None
