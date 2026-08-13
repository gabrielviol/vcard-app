# Phase 1: Conta e Cartão - Context

**Gathered:** 2026-08-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Dono cria conta (email/senha) e monta seu cartão completo — identidade, canais de contato, chave Pix e links sociais — com todas as validações de segurança/formato aplicadas no momento em que os campos nascem, não depois. Cobre: `ACCT-01..05` (cadastro, login, sessão JWT, proteção de rotas) e `CARD-01..10` (slug, identidade, contato, Pix, WhatsApp, foto, links sociais). Esta é a primeira fase — não há código existente ainda (`apps/web` e `apps/api` serão criados do zero seguindo `docs/specs/01-setup.md`).

Não inclui: cartão público (`/[slug]`), QR code, .vcf, analytics de visualização, ou qualquer coisa de monetização — isso é Fase 2/3/4.

</domain>

<decisions>
## Implementation Decisions

### Onboarding (conta → cartão)
- **D-01:** Depois do cadastro (email+senha), o usuário cai direto no formulário de criar cartão — sem dashboard vazio intermediário. Consistente com "1 cartão por usuário" na v1.
- **D-02:** O slug é o primeiro campo do formulário de criação, com checagem de disponibilidade em tempo real (debounced) antes de preencher o resto.
- **D-03:** O formulário de criar/editar cartão é uma tela única dividida em seções visuais (identidade, contato, Pix, links sociais) — não um wizard multi-etapa. Mesma tela serve para criação e edição posterior.
- **D-04:** Salvar cartão incompleto é permitido. Só slug + nome completo são obrigatórios para criar o registro; Pix, WhatsApp, foto e links sociais podem ficar vazios e ser preenchidos depois.

### Persistência de sessão
- **D-05:** O accessToken JWT é guardado em `localStorage` (não em memória/contexto React) — sobrevive a reload sem lógica extra de revalidação. Aceito para o MVP dado que o token tem vida curta (15-30min) e claims mínimas.
- **D-06:** Sem refresh token (já fora de escopo pela spec 02). Quando o token expira durante navegação, nada acontece proativamente — a próxima chamada de API que retornar 401 é que dispara o redirect para `/login`.
- **D-07:** No reload do dashboard (F5), a UI assume logado com base no token presente no `localStorage` e renderiza direto — sem chamar `GET /auth/me` antes nem mostrar loading state. Se o token for inválido/expirado, o 401 da primeira chamada de API real redireciona.

### Fricção do aviso de exposição pública do Pix
- **D-08:** Para tipos de chave Pix de risco baixo (email, telefone, aleatória): aviso é um texto inline abaixo do campo, sem bloquear o salvamento.
- **D-09:** Para tipo CPF especificamente (CARD-07 exige "aviso reforçado"): modal ou checkbox de confirmação bloqueante ao selecionar esse tipo — usuário precisa confirmar explicitamente antes de conseguir salvar. É o único tipo que expõe um documento pessoal completo, justificando a fricção extra.
- **D-10:** A prévia formatada da chave Pix (CARD-06) aparece em tempo real, conforme o usuário digita — formatação e validação de dígito verificador (CPF/CNPJ) a cada mudança, não só ao tentar salvar.

### UX de WhatsApp e foto de perfil
- **D-11:** O campo de WhatsApp aplica máscara brasileira em tempo real ao digitar, e mostra abaixo o formato final normalizado (`+55 DDD XXXXX-XXXX`) antes de salvar. A normalização real para dígitos puros com DDI 55 (CARD-08) continua acontecendo no momento de salvar, como já especificado.
- **D-12:** Foto de perfil é opcional. Quando ausente, o cartão mostra um placeholder com as iniciais do nome num círculo colorido — consistente com D-04 (cartão incompleto é permitido).
- **D-13:** Upload de foto é direto, sem editor de crop/recorte. A imagem enviada é ajustada via CSS (`object-fit: cover`) no layout do cartão. Nenhuma lib de crop entra no escopo desta fase.

### Claude's Discretion
- Lista exata de slugs reservados além dos exemplos já citados em REQUIREMENTS.md (`login`, `dashboard`, `api`, `_next`, `admin`) — pesquisar convenção antes de implementar CARD-02.
- Componente visual exato do placeholder de iniciais (paleta de cores por hash do nome, etc.) — decisão de UI, não de produto.
- Mensagens de erro exatas de validação de Pix por tipo — desde que cubram os casos exigidos (dígito verificador CPF/CNPJ, UUID v4 aleatória).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Setup e schema
- `docs/specs/01-setup.md` — Estrutura de monorepo (`apps/web`, `apps/api`), rotas do frontend (`/[slug]`, `/(dashboard)/login`, `/(dashboard)/dashboard`, `/(dashboard)/dashboard/cards/[id]/edit`), schema das 4 tabelas (User, Card, SocialLink, CardView) — tratado como decisão tomada, não sugestão.

### Autenticação
- `docs/specs/02-autentication.md` — Endpoints (`POST /auth/register`, `POST /auth/login`, `GET /auth/me`), configuração JWT (vida curta, claims mínimas, header `Authorization: Bearer`), BCrypt para hash, middleware de proteção de rotas — tratado como decisão tomada, não sugestão. Deixa aberto (resolvido nesta discussão): onde guardar o token no frontend → ver D-05.

### Produto e requisitos
- `.planning/PROJECT.md` — Tese do produto (WhatsApp/Pix como campos de primeira classe), constraints (2 semanas, solo, free tier, mobile-first), decisões-chave já tomadas.
- `.planning/REQUIREMENTS.md` — Requisitos v1 numerados (`ACCT-*`, `CARD-*`) com critérios de aceite específicos (ex: CARD-06 dígito verificador, CARD-07 aviso reforçado CPF, CARD-08 normalização DDI 55).
- `.planning/ROADMAP.md` — Goal e success criteria formais da Fase 1.

</canonical_refs>

<code_context>
## Existing Code Insights

Projeto greenfield — nenhum código existe ainda em `apps/web` ou `apps/api`. Não há maps em `.planning/codebase/` (esperado, primeira fase). O `docs/specs/01-setup.md` define a estrutura de pastas e schema que a Fase 1 deve criar do zero.

### Reusable Assets
Nenhum — tudo será construído nesta fase.

### Established Patterns
Nenhum ainda estabelecido no código; padrões vêm das specs 01/02 (camadas Endpoints/Services/Data no backend, App Router no frontend).

### Integration Points
- Frontend chama backend via `NEXT_PUBLIC_API_URL` (variável de ambiente já definida na spec 01).
- Dashboard envia `Authorization: Bearer {token}` em toda chamada protegida.

</code_context>

<specifics>
## Specific Ideas

Nenhuma referência visual ou de produto específica além do que já está documentado em PROJECT.md/REQUIREMENTS.md e nas duas specs. As decisões desta fase priorizam consistentemente: menor fricção no primeiro uso (cartão incompleto permitido, foto opcional, sem crop) e reaproveitar o que a spec 02 já definiu sem reabrir decisões já tomadas.

</specifics>

<deferred>
## Deferred Ideas

Nenhuma — discussão ficou dentro do escopo da fase. Nenhum todo pendente encontrado em `.planning/todos/pending/` para cruzar com esta fase.

</deferred>

---

*Phase: 1-Conta e Cartão*
*Context gathered: 2026-08-13*
