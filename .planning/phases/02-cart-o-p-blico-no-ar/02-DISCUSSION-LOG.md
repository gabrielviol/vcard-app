# Phase 2: Cartão Público no Ar - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-14
**Phase:** 2-Cartão Público no Ar
**Areas discussed:** Nome e domínio do produto, QR code no dashboard, Cartão incompleto na página pública, Página 404 do slug

---

## Nome e domínio do produto

Usuário pediu ajuda pra gerar opções (não tinha nome em mente). Duas rodadas de brainstorming com checagem preliminar de colisão via busca na web.

**Rodada 1:**

| Opção | Descrição | Selecionada |
|-------|-----------|-------------|
| Cartaum | "Cartão" + "um", brasileiro, sem colisão | |
| Pixtão | Pix + cartão, comunica a tese no nome, sem colisão | |
| Umtoque | Literal ao "em um toque", sem colisão de produto | |
| Nenhum desses | Quer mais opções | ✓ |

**Rodada 2 (após mais brainstorming):**

| Opção | Descrição | Selecionada |
|-------|-----------|-------------|
| Cartaum | (repetida da rodada 1) | |
| Pixtão | (repetida da rodada 1) | |
| Umtoque | (repetida da rodada 1) | |
| Vizzo | Curto, moderno, estilo Nubank/Loft — Vizzano (calçados) é próximo foneticamente mas não concorrente | ✓ |

Nomes descartados por colisão direta durante a busca (não chegaram a virar opção formal): **Zapcard** (concorrente direto existente, zapcard.com.br), **Zappix** (empresa americana com esse nome exato), **MeuCard** (múltiplos concorrentes diretos), **Tapcard** (múltiplos apps NFC com esse nome), **Tokinho** (nome de comediante brasileira conhecida), **Cardly** (genérico, sem diferenciação clara).

**Escolha do usuário:** "Vamos de Vizzo por enquanto, se nada surgir melhor" — explicitamente provisório.

**TLD:**

| Opção | Descrição | Selecionada |
|-------|-----------|-------------|
| .com.br | Sinaliza produto BR, mais barato, combina com posicionamento | ✓ |
| .com | Mais internacional, mais caro/difícil de achar livre | |
| .app | Moderno, força HTTPS, menos familiar pro usuário leigo BR | |

**Notas:** Checagem foi só busca na web (não WHOIS/INPI oficial) — usuário precisa confirmar registro real antes de travar. Decisão de arquitetura acompanhante (não perguntada, adicionada por mim): usar variável de ambiente para a URL base, não hardcode, dado o caráter provisório do nome.

---

## QR code no dashboard

| Pergunta | Opção | Descrição | Selecionada |
|----------|-------|-----------|-------------|
| Onde aparece | Sempre visível na edição | Zero clique extra | ✓ |
| | Ação separada ("Ver QR") | Tela mais limpa | |
| Download p/ impressão | Só o código, sem texto | Máxima flexibilidade | ✓ |
| | QR + legenda embutida | Mais pronto pra usar | |
| Cor | Preto no branco (padrão) | Confiabilidade de leitura | ✓ |
| | Cor de marca | Mais bonito, mais arriscado | |

**Notas:** Formato de arquivo (SVG+PNG) e nível de correção de erro já eram decisões técnicas travadas na pesquisa de stack — não reabertas.

---

## Cartão incompleto na página pública

| Pergunta | Opção | Descrição | Selecionada |
|----------|-------|-----------|-------------|
| Seção vazia (ex: sem WhatsApp) | Esconde a seção inteira | Página limpa | ✓ |
| | Mostra a seção sem o campo | Estrutura sempre igual | |
| Cartão mínimo (só nome) | Sempre acessível publicamente | Consistente com D-04 da Fase 1 | ✓ |
| | Só com um mínimo a mais | Contradiria D-04 | |
| Caso extremo (só nome, tudo vazio) | Nome + iniciais é suficiente | Estado válido, sem pedir desculpa | ✓ |
| | Precisa de algo a mais | Evitar parecer quebrado | |

---

## Página 404 do slug

| Pergunta | Opção | Descrição | Selecionada |
|----------|-------|-----------|-------------|
| Estilo | Com a cara do produto | Reforça marca até no erro | ✓ |
| | Genérica/neutra | Sem investir em copy | |
| CTA | Sim, com CTA de cadastro | Transforma erro em aquisição | ✓ |
| | Não, só mensagem de erro | Mantém simples | |

---

## Claude's Discretion

- Layout exato da tela de edição pra acomodar o QR sempre visível.
- Mecanismo técnico de pré-aquecimento (PUB-03) e keep-alive (PUB-04).
- Estrutura exata da rota `/[slug]` (Server Component + revalidate) — já pesquisada na Fase 0.
- Copy literal da 404 — só a intenção (cara do produto + CTA) foi travada.

## Deferred Ideas

Nenhuma — discussão ficou dentro do escopo da fase.
