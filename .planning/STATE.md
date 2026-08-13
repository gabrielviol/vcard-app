# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-08-13)

**Core value:** Alguém recebe o cartão (por QR ou link) e consegue te chamar ou te pagar em um toque — sem sair da página, sem digitar nada, sem etapa intermediária.
**Current focus:** Phase 1 — Conta e Cartão

## Current Position

Phase: 1 of 4 (Conta e Cartão)
Plan: 0 of TBD in current phase
Status: Ready to plan
Last activity: 2026-08-13 — Roadmap criado (4 fases, 43/43 requisitos v1 mapeados)

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: - min
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: -
- Trend: -

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Roadmap: cartão público + ISR + pré-aquecimento + QR ficam num único bloco (Fase 2) — separar criaria falsa sensação de "pronto" antes de resolver cold start.
- Roadmap: BRAND-01 (domínio) entra na Fase 2, não na fase final — evita bloquear o rodapé de marca/upgrade (Fase 4) e reconhece que registrar domínio é ação com lead time.
- Roadmap: validações de Pix (CARD-06/07) e normalização de WhatsApp (CARD-08) ficam na mesma fase que cria esses campos (Fase 1) — barato ali, caro depois.

### Pending Todos

[From .planning/todos/pending/ — ideas captured during sessions]

None yet.

### Blockers/Concerns

[Issues that affect future work]

- Fase 2 (cartão público + ISR + cold start) é a decisão mais arriscada do projeto segundo research/ARCHITECTURE.md — validar localmente a precedência de rotas estáticas vs. dinâmicas (`/login` vs `/[slug]`) antes de confiar nisso estruturalmente.
- Fase 3 (WhatsApp/Pix/.vcf em WebView) exige teste manual real dentro do app do Instagram/WhatsApp — não há documentação oficial da Meta sobre esse comportamento.
- BRAND-01 depende de ação externa do usuário (registrar domínio) — pode ter lead time fora do controle do dev; iniciar cedo.

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| *(none)* | | | |

## Session Continuity

Last session: 2026-08-13
Stopped at: ROADMAP.md e STATE.md criados; aguardando aprovação do usuário para seguir com `/gsd:plan-phase 1`
Resume file: None
