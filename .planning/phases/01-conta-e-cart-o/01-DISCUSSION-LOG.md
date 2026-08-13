# Phase 1: Conta e Cartão - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-13
**Phase:** 1-Conta e Cartão
**Areas discussed:** Onboarding (conta → cartão), Persistência de sessão, Fricção do aviso de Pix público, UX de WhatsApp e foto de perfil

---

## Onboarding: conta → cartão

| Option | Description | Selected |
|--------|-------------|----------|
| Direto pro form de criar cartão | Cadastro → cai direto no formulário de criar cartão, sem passos extras | ✓ |
| Dashboard vazio com CTA | Cadastro → dashboard vazio com botão "Criar cartão" | |

**User's choice:** Direto pro form de criar cartão
**Notes:** Combina com "1 cartão por usuário" na v1 — dashboard vazio intermediário não agrega nada hoje.

| Option | Description | Selected |
|--------|-------------|----------|
| Primeiro campo do form de criação | Slug escolhido no início, com checagem de disponibilidade em tempo real | ✓ |
| Último passo, ao salvar | Slug só escolhido ao salvar o cartão pela primeira vez | |

**User's choice:** Primeiro campo do form de criação

| Option | Description | Selected |
|--------|-------------|----------|
| Tela única com seções | Formulário único dividido em seções visuais (identidade/contato/Pix/links) | ✓ |
| Wizard multi-etapa | Passos sequenciais, só avança com o passo atual válido | |

**User's choice:** Tela única com seções
**Notes:** Mesma tela serve para criação e edição posterior — importante pro uso recorrente.

| Option | Description | Selected |
|--------|-------------|----------|
| Salvar incompleto é permitido | Só slug + nome completo obrigatórios; resto pode ficar vazio | ✓ |
| Bloqueia até preencher tudo | Só salva com identidade, contato e Pix completos | |

**User's choice:** Salvar incompleto é permitido

---

## Persistência de sessão

| Option | Description | Selected |
|--------|-------------|----------|
| localStorage | Sobrevive a reload sem lógica extra de revalidação | ✓ |
| Em memória (contexto React) | Mais seguro contra XSS persistente, mas perde token a cada F5 | |

**User's choice:** localStorage

| Option | Description | Selected |
|--------|-------------|----------|
| Silencioso até próxima ação | Token expira, próxima chamada de API retorna 401 e redireciona | ✓ |
| Checagem ativa (polling/timer) | Timer detecta expiração e redireciona proativamente | |

**User's choice:** Silencioso até próxima ação
**Notes:** Sem refresh token na v1 (já fora de escopo pela spec 02), então não haveria como renovar silenciosamente mesmo com polling.

| Option | Description | Selected |
|--------|-------------|----------|
| Assume logado, renderiza direto | Lê token do localStorage antes do render, sem chamar /auth/me | ✓ |
| Tela de loading até validar via /auth/me | Spinner até confirmar token válido via chamada de API | |

**User's choice:** Assume logado, renderiza direto

---

## Fricção do aviso de Pix público

| Option | Description | Selected |
|--------|-------------|----------|
| Texto inline sob o campo | Aviso leve, não bloqueia salvamento — pro caso geral (email/telefone/aleatória) | ✓ |
| Checkbox de confirmação obrigatório | Confirmação obrigatória pra qualquer tipo de chave | |

**User's choice:** Texto inline sob o campo

| Option | Description | Selected |
|--------|-------------|----------|
| Modal/checkbox de confirmação bloqueante | Aviso destacado + confirmação obrigatória, específico pro tipo CPF | ✓ |
| Mesmo texto inline com destaque visual maior | Mantém fricção baixa, só muda cor/ícone | |

**User's choice:** Modal/checkbox de confirmação bloqueante
**Notes:** CPF é o único tipo que expõe um documento pessoal completo — justifica a fricção extra exigida por CARD-07.

| Option | Description | Selected |
|--------|-------------|----------|
| Em tempo real, ao digitar | Formata e valida dígito verificador a cada mudança | ✓ |
| Só depois de tentar salvar | Formatação e validação só aparecem ao salvar | |

**User's choice:** Em tempo real, ao digitar

---

## UX de WhatsApp e foto de perfil

| Option | Description | Selected |
|--------|-------------|----------|
| Máscara em tempo real | Input aplica máscara BR ao digitar, mostra preview normalizado (+55) | ✓ |
| Aceita livre, só normaliza ao salvar | Sem máscara ativa, resultado só aparece após salvar | |

**User's choice:** Máscara em tempo real

| Option | Description | Selected |
|--------|-------------|----------|
| Opcional, com placeholder de iniciais | Cartão funciona sem foto — mostra iniciais num círculo colorido | ✓ |
| Obrigatória pra publicar | Cartão só fica público com foto enviada | |

**User's choice:** Opcional, com placeholder de iniciais
**Notes:** Consistente com a decisão de permitir cartão incompleto no onboarding.

| Option | Description | Selected |
|--------|-------------|----------|
| Upload direto, sem crop | Imagem enviada como está, ajustada via CSS object-fit:cover | ✓ |
| Crop simples antes de enviar | Editor de recorte antes do upload pro Vercel Blob | |

**User's choice:** Upload direto, sem crop

---

## Claude's Discretion

- Lista exata de slugs reservados além dos exemplos citados em REQUIREMENTS.md.
- Componente visual exato do placeholder de iniciais (paleta de cores, etc.).
- Mensagens de erro exatas de validação de Pix por tipo.

## Deferred Ideas

Nenhuma — discussão ficou dentro do escopo da fase.
