# Walking Skeleton — vCard App (nome a definir)

**Phase:** 1 — Conta e Cartão
**Generated:** 2026-08-13

## Capability Proven End-to-End

Um visitante cria conta com e-mail e senha no navegador, o registro é gravado no Postgres com senha em hash BCrypt, a API devolve um JWT real, e o usuário vê o próprio e-mail numa tela autenticada que buscou o dado de `GET /auth/me` — atravessando browser → Next.js → .NET minimal API → EF Core → Postgres → JWT → browser.

O esqueleto é entregue pelos planos `01-01` (backend, banco, migration, auth) e `01-02` (frontend, sessão, telas), e é fechado pelo checkpoint humano da tarefa 3 do plano `01-02`. Os planos `01-03` a `01-07` empilham slices verticais de produto sobre esta base sem renegociar nenhuma decisão abaixo.

## Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Framework (web) | Next.js 16.3 App Router, TypeScript, Tailwind, shadcn (New York / Zinc / CSS variables) | Stack travado em CLAUDE.md e `docs/specs/01-setup.md`; preset shadcn travado em `01-UI-SPEC.md`. `cacheComponents` fica desligado nesta janela (custo de modelo mental novo sem necessidade concreta) |
| Framework (api) | .NET 10 minimal API, camadas Endpoints / Services / Data | Spec 01 define as camadas; sem MediatR, CQRS ou repositório genérico — "sem abstração especulativa" é constraint de projeto |
| Data layer | Postgres 17 + EF Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3) com Migrations | Migrations versionam o schema; `dotnet-ef` instalado como tool global no plano 01 (não estava presente na máquina) |
| Mapeamento de schema | `ToTable` / `HasColumnName` explícitos para snake_case em `AppDbContext` | Evita adicionar um pacote de naming convention não auditado; os nomes de coluna batem literalmente com `docs/specs/01-setup.md` |
| Banco local | Docker Compose com um serviço `db` e um segundo banco `vcard_test` criado no init | A suíte xUnit roda contra Postgres real via `WebApplicationFactory`, não contra provider in-memory (o índice único e o `23505` só existem no Postgres de verdade) |
| Auth | JWT auto-emitido HMAC-SHA256 (20 min, claims `sub` e `email`) + BCrypt.Net-Next para senha | `docs/specs/02-autentication.md` trava essa escolha como simplificação consciente de MVP de 2 semanas; a documentação atual da Microsoft recomenda OIDC — tradeoff informado, com mitigações: token curto, claims mínimas, `alg` restrito a HS256, `JWT_SECRET` só via ambiente |
| Sessão no cliente | `accessToken` em `localStorage`, guard client-side no layout do grupo `(dashboard)` | D-05/D-07. `middleware.ts` NÃO pode ser usado para isso: roda no runtime Edge/servidor, onde `localStorage` não existe |
| Expiração de sessão | Reativa: o interceptor de 401 do `lib/api-client.ts` limpa o token e redireciona para `/login?expired=1` | D-06 — sem refresh token, sem revalidação proativa, sem chamada preventiva a `/auth/me` |
| Unicidade de slug | Índice único no Postgres + catch de `PostgresException.SqlState == "23505"` traduzido em 409 | A checagem debounced de disponibilidade é apenas dica de UX; a garantia real é o banco (TOCTOU) |
| Consentimento de Pix/CPF | Coluna `cards.pix_consent_confirmed` persistida e exigida pelo servidor quando `pix_key_type = cpf` | Adição deliberada ao schema da spec 01, recomendada pela Security Domain do `01-RESEARCH.md`: um checkbox de UI não é prova de consentimento |
| Upload de foto | Vercel Blob com upload direto do browser, token emitido por Route Handler autenticado do Next.js | O binário nunca passa pelo `apps/api` (Render free tier); a rota de emissão de token valida o Bearer contra `GET /auth/me` — integração que a doc oficial da Vercel não cobre |
| CORS | Política nomeada com `WithOrigins(Cors__WebOrigin)`; `AllowAnyOrigin()` proibido | Spec 01 exige allow-list; header `Authorization` em jogo torna isso crítico |
| TLS até o banco | `SSL Mode=Disable` explícito no local, `SSL Mode=Require;Trust Server Certificate=true` no deploy | O default `Prefer` do Npgsql é vetado no CLAUDE.md; o container `postgres` local não tem SSL habilitado |
| Deployment target | Vercel (`apps/web`) + Render free tier (`apps/api`, cold start aceito) + Neon (Postgres, autosuspend com wake ~500ms) | Free tier obrigatório; Neon escolhido em vez de Supabase porque Supabase pausa o projeto após 7 dias e exige restauração manual |
| Directory layout | `apps/web` (`app/`, `lib/`, `components/`) e `apps/api` (`Program.cs`, `Endpoints/`, `Services/`, `Data/Entities/`, `Data/Migrations/`, `Contracts/`, `Api.Tests/`) | Monorepo simples sem ferramenta de workspace (linguagens diferentes), conforme spec 01 |
| Testes | xUnit + `Microsoft.AspNetCore.Mvc.Testing` no backend; Vitest (ambiente node, funções puras) no frontend | `01-VALIDATION.md`. Sem framework e2e nesta janela — a passagem manual é o gate de ponta a ponta |

## Stack Touched in Phase 1

- [x] Project scaffold — `apps/api` (.NET 10 + solution + Api.Tests) e `docker-compose.yml` entregues no plano `01-01`; `apps/web` (create-next-app + shadcn + Vitest) pendente no plano `01-02`
- [x] Routing — `/health`, `/auth/register`, `/auth/login`, `/auth/me` no backend entregues no plano `01-01`; grupo `/cards` criado com `.RequireAuthorization()` e um placeholder `POST /cards` (501, apenas para ancorar o teste de ACCT-05 — handler real entra no plano `01-03`); rotas do frontend (`/login`, `/register`, `/dashboard`, ...) pendentes no plano `01-02`
- [x] Database — leitura real (`GET /auth/me`) e escrita real (`POST /auth/register`) entregues no plano `01-01` contra Postgres migrado (4 tabelas); `GET /cards/me`, `POST /cards`, `PUT /cards/{id}` pendentes no plano `01-03`
- [ ] UI — formulário de criar conta e formulário de cartão em tela única, ambos ligados à API por `lib/api-client.ts` com header `Authorization` — pendente, plano `01-02`
- [ ] Deployment — comando local de stack completa documentado: `docker compose up -d db` + `dotnet run --project apps/api` operacionais desde o plano `01-01`; `cd apps/web && npm run dev` pendente no plano `01-02`

## Out of Scope (Deferred to Later Slices)

Explicitamente fora do esqueleto e fora da Fase 1 — não relitigar em fases seguintes:

- Cartão público em `/[slug]`, ISR, pré-aquecimento, keep-alive, página 404 própria (Fase 2 — PUB-01..06)
- QR code na tela e para impressão (Fase 2 — SHARE-01/02)
- Nome e domínio do produto (Fase 2 — BRAND-01; é a pendência bloqueante registrada em PROJECT.md)
- Botão de WhatsApp, `.vcf`, cópia de chave Pix, orientação para navegador embutido (Fase 3 — CONT-*, PAY-*)
- Preview de link / Open Graph image (Fase 3 — SHARE-03/04)
- Contagem de visualizações, marca do plano grátis no rodapé, sinal de intenção de upgrade (Fase 4 — VIEW-*, BRAND-02, UPG-*)
- Refresh token e renovação automática de sessão (v2 — ACCT-07)
- Recuperação de senha (v2 — ACCT-06), login social (v2 — ACCT-08), rate limiting no login (v2 — ACCT-09)
- Múltiplos cartões por usuário (v2 — UPG-06); o schema usa índice único em `cards.user_id`
- Temas e personalização visual do cartão (v2 — CARD-11)
- Editor de crop/recorte de imagem (D-13 — ajuste é por `object-fit: cover`)
- Geração de payload EMV / Pix BR Code (vetado no CLAUDE.md nesta v1; PAY-04 é v1.x)
- Deploy efetivo em Vercel/Render/Neon — a Fase 1 fecha com a stack rodando local; o deploy não bloqueia esta fase (spec 01)

## Subsequent Slice Plan

Cada fase seguinte acrescenta um slice vertical sobre este esqueleto sem alterar as decisões arquiteturais acima:

- **Fase 1 (slices internos):** `01-03` cartão com slug único + identidade · `01-04` contato + WhatsApp normalizado · `01-05` chave Pix validada e consentida · `01-06` foto no Blob · `01-07` links sociais reordenáveis
- **Fase 2:** o cartão existe no mundo — `/[slug]` público via ISR, resiliente a cold start, com QR pronto para circular e domínio próprio
- **Fase 3:** o cartão age — WhatsApp em um toque, Pix copiável, `.vcf` na agenda, links sociais, preview correto no compartilhamento
- **Fase 4:** o cartão ensina — contagem de visualizações, marca do plano grátis e registro de intenção de upgrade
