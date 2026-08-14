# vCard App (nome a definir)

## What This Is

Um cartão de visita digital (link único + QR code) feito para o mercado brasileiro, onde WhatsApp e Pix são campos de primeira classe em vez de "mais um link social". É para freelancers, autônomos e pequenos prestadores de serviço no Brasil que hoje usam cartão de papel ou uma bio de Instagram improvisada para passar contato e receber por um serviço.

Não é uma cópia do Linktree nem dos concorrentes internacionais (Popl, HiHello, Blinq) — esses assumem um fluxo de networking corporativo americano (e-mail como canal principal, cartão de crédito, integração com CRM) que não reflete como a transação de fato acontece no Brasil.

## Core Value

Alguém recebe o cartão (por QR ou link) e consegue **te chamar ou te pagar em um toque** — sem sair da página, sem digitar nada, sem etapa intermediária.

## Requirements

### Validated

- [x] Cadastro, login e sessão via JWT (specs 01/02 já escritas) — Phase 1
- [x] Dono cria e edita seu cartão pelo dashboard (nome, cargo, empresa, foto, contatos) — Phase 1
- [x] Links sociais ordenáveis (Instagram, LinkedIn, Twitter, TikTok, YouTube, site) — Phase 1

### Active

- [ ] Cartão público acessível por slug próprio (ex: `/gabriel`), renderizado rápido em celular
- [ ] Botão de WhatsApp que abre conversa direta com o dono do cartão
- [ ] Chave Pix copiável em um toque na página pública
- [ ] QR code gerado do cartão, exibível na tela e baixável para impressão
- [ ] Preview de link (OG image + título) correto quando o cartão é compartilhado no WhatsApp
- [ ] Botão "salvar contato" que baixa o `.vcf` com os dados do cartão
- [ ] Registro de visualizações do cartão (`CardView`) e contagem visível pro dono
- [ ] Marca do produto visível no rodapé do cartão gratuito (`is_branded`)
- [ ] Botão "remover marca" que registra intenção de upgrade sem cobrar — o sinal de validação

### Out of Scope

- **Checkout / assinatura recorrente** — o objetivo da v1 é medir intenção de pagar, não processar pagamento. Construir billing antes de saber se alguém quer pagar é a ordem errada.
- **Pix com valor definido ou cobrança dinâmica (BR Code / PSP)** — decisão explícita de começar com "copiar chave". Integração com PSP (Mercado Pago, Asaas, Efí) exige webhook, conciliação e conta configurada; não cabe na janela de 2 semanas e não é o que bloqueia a validação.
- **Refresh token, login social, recuperação de senha, rate limiting** — já marcados como fora de escopo na spec 02.
- **Múltiplos cartões por usuário** — é feature do plano pago; o schema suporta, mas a v1 entrega 1 cartão por usuário.
- **Analytics detalhado (referrer, série temporal, geografia)** — `CardView` grava `referrer`, mas a v1 mostra só contagem. O detalhamento é o gancho de upgrade do plano pago.
- **App nativo / NFC** — os concorrentes vendem hardware NFC; aqui o QR resolve o mesmo problema sem custo de produção.
- **Integração com CRM** — é justamente a premissa americana que o produto rejeita.

## Context

**Tese do produto.** O cartão não é "meu contato num link bonito" — ele existe para reduzir a distância entre a pessoa te descobrir e ela agir. Isso explica cada decisão de schema:

- `whatsapp_number` é campo próprio, não um `SocialLink` — o WhatsApp é o canal principal de contato no Brasil, não o e-mail.
- `pix_key` / `pix_key_type` existem porque cobrar deveria ser parte do cartão, não uma etapa separada depois do contato. Nenhum concorrente internacional grande tem isso nativo.
- `is_branded` é o motor do freemium (padrão Linktree): grátis mostra a marca, pago remove. A marca visível também é distribuição orgânica — cada cartão gratuito compartilhado expõe o produto a quem recebe.
- `CardView` é o gancho de upgrade — "veja de onde vêm suas visitas" é o que justifica o plano pago.

**Concorrência brasileira (achado da pesquisa, 2026-08-13).** O enquadramento inicial deste documento assumia que a lacuna era só contra os players americanos. A pesquisa encontrou concorrência brasileira direta e mais madura que o esperado:

- **Monocard** — se posiciona como "a plataforma #1 de cartão de visita digital no Brasil", já tem Pix nativo, cobra R$9,90/mês (o mesmo patamar planejado aqui) e declara freelancers/autônomos como público. A diferença que sobrevive: trata WhatsApp como mais um módulo entre outros, não como CTA central, e o modelo de negócio empurra para hardware NFC físico + CRM empresarial.
- **InfinitePay Link na Bio** — fintech grande, bio-link grátis com Pix nativo, mas sem identidade de cartão profissional (nome/cargo/foto). É vitrine de vendas, não cartão de visita.

**Consequência para o posicionamento:** o diferencial a comunicar não é "ter Pix no Brasil primeiro" — essa vantagem não existe mais. É **WhatsApp como CTA central + simplicidade radical** (sem hardware, sem CRM, sem múltiplos perfis), voltado ao autônomo solo. Nenhum concorrente pesquisado, brasileiro ou internacional, combina essas três coisas.

**Como o cartão chega em quem recebe.** Quatro caminhos, todos válidos: QR na tela do celular em encontro presencial, QR impresso (adesivo, cartão de papel, vidro da loja), link mandado no WhatsApp, e link na bio do Instagram. O canal do WhatsApp implica que o preview do link (OG image) é metade da primeira impressão — se vier quebrado, a pessoa não clica.

**Especificações já escritas.** `docs/specs/01-setup.md` (monorepo Next.js App Router + .NET minimal API + Postgres, schema de 4 tabelas, Docker Compose local, deploy Vercel/Render/Neon) e `docs/specs/02-autentication.md` (JWT Bearer, BCrypt, register/login/me). Ambas devem ser tratadas como decisões tomadas, não como sugestões.

**Quem está construindo.** Dev full-stack, trabalha com pagamentos e infraestrutura de checkout no dia a dia (React/TS + C#/.NET). Construindo sozinho durante as férias, com objetivo de gerar renda extra sem depender de site de freelancer. Usa o próprio cartão como primeiro teste real.

**Modelo de negócio (futuro).** Freemium: grátis (1 cartão, com marca, analytics básico) → pago em torno de R$9,90–19,90/mês (sem marca, múltiplos cartões, analytics detalhado).

## Constraints

- **Timeline**: ~2 semanas, solo, durante as férias — algo no ar e compartilhável até o fim da janela. Se algo tiver que cair, o QR fica; o resto negocia.
- **Tech stack**: Next.js (App Router, TypeScript, Tailwind) + .NET minimal API + Postgres — já decidido nas specs. Escolhido por ser o stack que o dev já domina, o que maximiza velocidade na janela curta.
- **Arquitetura**: validação rápida vale mais que robustez agora. Camadas simples (Endpoints/Services/Data), sem abstração especulativa.
- **Custo**: free tier em tudo (Vercel, Render com cold start aceito, Neon/Supabase) — o produto não pode custar dinheiro antes de gerar.
- **Mobile-first**: quem recebe o cartão abre no celular, quase sempre vindo de câmera de QR ou de app de mensagem. Desktop é secundário.
- **Pendência bloqueante**: o produto não tem nome nem domínio. Isso trava a marca do rodapé (`is_branded`) e a URL pública — precisa ser resolvido antes do cartão ir pro ar.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Pix por "copiar chave", não BR Code/PSP | Zero integração, cabe na janela de 2 semanas. Cobrança dinâmica exige webhook e conciliação sem provar nada sobre a tese. | — Pending |
| Sem checkout na v1; botão de upgrade só mede intenção | Construir billing antes de saber se alguém quer pagar é a ordem errada. O clique no botão é o sinal barato. | — Pending |
| Sinal de validação = intenção de pagar registrada | Sinal original era "primeiro pagante", mas a v1 não tem como cobrar. Medir intenção é o proxy honesto e possível. | — Pending |
| QR code é o corte prioritário | Sem QR não substitui o cartão de papel; sem cartão circulando não há o que medir. Fica acima até do botão de upgrade. | — Pending |
| WhatsApp e Pix como campos próprios, não `SocialLink` | São a tese do produto, não decoração. Tratá-los como links genéricos apagaria a diferenciação. | Confirmado — Phase 1 implementou `whatsapp_number`/`pix_key`/`pix_key_type` como colunas dedicadas em `cards`, separadas de `social_links` |
| Stack Next.js + .NET + Postgres | Stack que o dev já domina no dia a dia — velocidade importa mais que escolha ótima na janela curta. | Confirmado — Phase 1 (7 planos) construído e testado nessa stack sem atrito, 90/90 xUnit + 54/54 Vitest verdes |
| 1 cartão por usuário na v1 | Múltiplos cartões é feature do plano pago; o schema já suporta, então não há custo em adiar. | Confirmado — índice único em `cards.user_id`, erro `card_exists` distinto de `slug_taken` na criação (Phase 1, WR-01) |
| Monorepo sem ferramenta de workspace | Dois projetos de linguagens diferentes; Turborepo/Lerna não agregam. | Confirmado — `apps/web` e `apps/api` convivem sem fricção via 7 planos da Phase 1 |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-08-14 after Phase 1 (Conta e Cartão) completion*
