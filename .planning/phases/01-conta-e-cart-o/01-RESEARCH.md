# Phase 1: Conta e Cartão - Research

**Researched:** 2026-08-13
**Domain:** Greenfield auth (JWT + BCrypt in .NET 10 minimal API) + Postgres schema/uniqueness (EF Core 10 + Npgsql) + Brazilian-specific form validation (Pix, WhatsApp) on Next.js 16
**Confidence:** MEDIUM-HIGH (backend auth/EF patterns HIGH via official docs; Brazilian phone normalization and reserved-slug convention MEDIUM — no single canonical source, synthesized from multiple community sources)

## Summary

This phase is a walking-skeleton + full-feature build in one shot: stand up `apps/web` (Next.js 16 App Router) and `apps/api` (.NET 10 minimal API) from nothing, wire them to a real Postgres database (local Docker for dev, Neon for deploy), and implement the entire account + card-editing feature set with all validations live at field-creation time.

The riskiest unknowns are backend-shaped: (1) idiomatic JWT bearer wiring in a .NET 10 minimal API with a **self-issued** token (no external IdP/OIDC — official Microsoft guidance now actively discourages this pattern in favor of OIDC, but the project's canonical spec `docs/specs/02-autentication.md` locks in self-issued JWT as a deliberate 2-week-MVP simplification); (2) enforcing slug uniqueness at the database level while still giving the user a fast, friendly "disponível/indisponível" UX during typing, which requires treating the debounced availability check as advisory only and the DB unique constraint + `23505` catch as the actual source of truth; (3) Brazilian phone normalization has a well-documented "ninth digit" pitfall that is easy to get subtly wrong (it depends on the DDD, not a fixed rule).

**Primary recommendation:** Build the JWT flow with a symmetric HMAC-SHA256 key from `JWT_SECRET`, explicit `TokenValidationParameters` (no `Authority`/OIDC), BCrypt.Net-Next for hashing, a Postgres unique index on `Card.slug` backed by an app-level reserved-word blocklist, and a normalize-on-save function for WhatsApp that special-cases the DDDs that got the 9th digit added in 2012 (11–19, 21, 22, 24, 27, 28) versus the ones that didn't.

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Onboarding (conta → cartão)**
- **D-01:** Depois do cadastro (email+senha), o usuário cai direto no formulário de criar cartão — sem dashboard vazio intermediário. Consistente com "1 cartão por usuário" na v1.
- **D-02:** O slug é o primeiro campo do formulário de criação, com checagem de disponibilidade em tempo real (debounced) antes de preencher o resto.
- **D-03:** O formulário de criar/editar cartão é uma tela única dividida em seções visuais (identidade, contato, Pix, links sociais) — não um wizard multi-etapa. Mesma tela serve para criação e edição posterior.
- **D-04:** Salvar cartão incompleto é permitido. Só slug + nome completo são obrigatórios para criar o registro; Pix, WhatsApp, foto e links sociais podem ficar vazios e ser preenchidos depois.

**Persistência de sessão**
- **D-05:** O accessToken JWT é guardado em `localStorage` (não em memória/contexto React) — sobrevive a reload sem lógica extra de revalidação. Aceito para o MVP dado que o token tem vida curta (15-30min) e claims mínimas.
- **D-06:** Sem refresh token (já fora de escopo pela spec 02). Quando o token expira durante navegação, nada acontece proativamente — a próxima chamada de API que retornar 401 é que dispara o redirect para `/login`.
- **D-07:** No reload do dashboard (F5), a UI assume logado com base no token presente no `localStorage` e renderiza direto — sem chamar `GET /auth/me` antes nem mostrar loading state. Se o token for inválido/expirado, o 401 da primeira chamada de API real redireciona.

**Fricção do aviso de exposição pública do Pix**
- **D-08:** Para tipos de chave Pix de risco baixo (email, telefone, aleatória): aviso é um texto inline abaixo do campo, sem bloquear o salvamento.
- **D-09:** Para tipo CPF especificamente (CARD-07 exige "aviso reforçado"): modal ou checkbox de confirmação bloqueante ao selecionar esse tipo — usuário precisa confirmar explicitamente antes de conseguir salvar. É o único tipo que expõe um documento pessoal completo, justificando a fricção extra.
- **D-10:** A prévia formatada da chave Pix (CARD-06) aparece em tempo real, conforme o usuário digita — formatação e validação de dígito verificador (CPF/CNPJ) a cada mudança, não só ao tentar salvar.

**UX de WhatsApp e foto de perfil**
- **D-11:** O campo de WhatsApp aplica máscara brasileira em tempo real ao digitar, e mostra abaixo o formato final normalizado (`+55 DDD XXXXX-XXXX`) antes de salvar. A normalização real para dígitos puros com DDI 55 (CARD-08) continua acontecendo no momento de salvar, como já especificado.
- **D-12:** Foto de perfil é opcional. Quando ausente, o cartão mostra um placeholder com as iniciais do nome num círculo colorido — consistente com D-04 (cartão incompleto é permitido).
- **D-13:** Upload de foto é direto, sem editor de crop/recorte. A imagem enviada é ajustada via CSS (`object-fit: cover`) no layout do cartão. Nenhuma lib de crop entra no escopo desta fase.

### Claude's Discretion
- Lista exata de slugs reservados além dos exemplos já citados em REQUIREMENTS.md (`login`, `dashboard`, `api`, `_next`, `admin`) — pesquisar convenção antes de implementar CARD-02. **Resolved below in "Don't Hand-Roll" / reserved slugs section.**
- Componente visual exato do placeholder de iniciais (paleta de cores por hash do nome, etc.) — decisão de UI, não de produto.
- Mensagens de erro exatas de validação de Pix por tipo — desde que cubram os casos exigidos (dígito verificador CPF/CNPJ, UUID v4 aleatória).

### Deferred Ideas (OUT OF SCOPE)
Nenhuma — discussão ficou dentro do escopo da fase. Cartão público (`/[slug]`), QR code, `.vcf`, analytics de visualização e monetização são Fases 2/3/4, não esta fase.

## Project Constraints (from CLAUDE.md)

- Stack já decidido: Next.js (App Router, TypeScript, Tailwind) + .NET 10 minimal API + Postgres (Neon) — não reabrir essa decisão.
- Camadas simples no backend: Endpoints/Services/Data — sem abstração especulativa (evitar CQRS/MediatR/repository genérico neste MVP).
- Free tier obrigatório em tudo (Vercel, Render cold-start aceito, Neon).
- Mobile-first.
- `SSL Mode=Prefer` (default do Npgsql) é explicitamente proibido contra Neon/Supabase — usar `SSL Mode=Require;Trust Server Certificate=true`.
- `qr-code-styling`, geração de payload EMV/Pix BR Code, `next/font/google` dentro de `ImageResponse`, e Supabase como banco são explicitamente vetados (não relevantes a esta fase, mas documentado para não reabrir).
- Pacotes já pré-aprovados no CLAUDE.md para este domínio: `cpf-cnpj-validator@2.1.2`, `zod@4.4.3`, `@vercel/blob` (client SDK), `Npgsql.EntityFrameworkCore.PostgreSQL@10.0.3`, `Microsoft.EntityFrameworkCore.Design@10.0.11` — tratar como decisão tomada, apenas reverificar versão atual (feito abaixo).
- GSD workflow enforcement: mudanças de arquivo devem passar por `/gsd-execute-phase` ou equivalente — não editar fora do fluxo GSD.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ACCT-01 | Cria conta com e-mail/senha, hash BCrypt | BCrypt.Net-Next 4.2.1 usage pattern below; official pkg confirmed on NuGet |
| ACCT-02 | Login retorna access token JWT | JWT issuance code pattern below (symmetric key, minimal claims) |
| ACCT-03 | Permanece autenticado ao navegar/recarregar | D-05/D-07 (localStorage, no `/auth/me` preflight) — client-side only, see Pitfall 1 |
| ACCT-04 | Redireciona para `/login` quando token ausente/expirado | Client-side guard pattern (not Next.js middleware — see Pitfall 1) |
| ACCT-05 | Rotas de escrita Card/SocialLink retornam 401 sem token | `.RequireAuthorization()` on endpoint groups, see JWT section |
| CARD-01 | Cria cartão com slug único | Unique index + race-condition handling section |
| CARD-02 | Rejeita slugs reservados e em uso | Reserved slug list below (Don't Hand-Roll section) |
| CARD-03 | Edita nome, cargo, empresa | Standard EF Core CRUD — no special research needed |
| CARD-04 | Cadastra telefone, e-mail, WhatsApp | WhatsApp normalization section (Pitfall 3) |
| CARD-05 | Cadastra chave Pix por tipo | `cpf-cnpj-validator` + zod/regex by type, table below |
| CARD-06 | Valida formato Pix (dígito verificador, UUID v4) + prévia | `cpf-cnpj-validator` (`cpf.isValid`/`cnpj.isValid`), UUID v4 regex |
| CARD-07 | Aviso de exposição pública, reforçado para CPF | UX-only (D-08/D-09), no library needed |
| CARD-08 | Normaliza WhatsApp para DDI 55 no save | Normalization function + DDD whitelist, see Pitfall 3 |
| CARD-09 | Upload de foto direto do browser | `@vercel/blob` client upload flow (`handleUpload` route handler) below |
| CARD-10 | Adiciona/remove/reordena links sociais | `display_order` int column + reorder-on-drop persistence, standard EF Core pattern |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Registro/login (hash + emissão JWT) | API/Backend | Database (User row) | Senha nunca deve ser validada/hasheada no cliente; token é emitido pelo servidor que detém `JWT_SECRET` |
| Persistência de sessão (guardar/ler token) | Browser/Client | — | `localStorage` só existe no browser; nenhuma lógica de sessão no servidor (sem cookie, sem SSR de sessão) |
| Proteção de rotas do dashboard | Browser/Client | — | Next.js Middleware roda no Edge/servidor e **não tem acesso a `localStorage`** — ver Pitfall 1. A checagem tem que ser client-side (layout client component / hook) |
| Checagem de disponibilidade de slug (UX em tempo real) | Browser/Client | API/Backend | Debounce e chamada de rede vivem no cliente; a resposta "disponível" é só uma dica de UX, nunca a fonte da verdade |
| Unicidade de slug (garantia real) | Database | API/Backend | Unique index no Postgres é a única garantia contra race condition; API traduz a violação em erro 409 amigável |
| CRUD de Card/SocialLink | API/Backend | Database | Toda escrita passa por `.RequireAuthorization()` no .NET; EF Core traduz para SQL |
| Validação de formato Pix (dígito verificador, UUID) | Browser/Client | API/Backend | Prévia em tempo real (D-10) é client-side; validação espelho no backend antes de persistir (nunca confiar só no cliente) |
| Normalização de WhatsApp (DDI 55) | API/Backend | Browser/Client (máscara) | Máscara e preview são só exibição; a normalização que é persistida (CARD-08) acontece no momento de salvar, no servidor |
| Upload de foto (`photo_url`) | Browser/Client | CDN/Static (Vercel Blob) | Upload é direto do browser para o Blob Store via token assinado — o binário nunca passa pelo `apps/api` (.NET) |
| Reordenação de links sociais | Browser/Client | API/Backend | Drag-and-drop e novo array de ordem são client-side; persistência do `display_order` é uma chamada de API depois do drop |

## Standard Stack

### Core (Backend — `apps/api`)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.11 | Validação de JWT Bearer no pipeline ASP.NET Core | Pacote first-party da Microsoft para JWT Bearer, mesmo confirmado no NuGet e citado na doc oficial |
| `BCrypt.Net-Next` | 4.2.1 | Hash e verificação de senha | Fork ativamente mantido do BCrypt.Net original, zero dependências, alvo net10 confirmado a partir da 4.1.0 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Provider EF Core para Postgres | Já decidido em CLAUDE.md; reverificado nesta sessão via NuGet API |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | Ferramentas de design-time para migrations (`dotnet ef`) | Já decidido em CLAUDE.md; reverificado nesta sessão |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.11 | CLI `dotnet ef migrations add` / `database update` dentro do próprio projeto | Padrão junto com `.Design`; sem ele os comandos `dotnet ef` não funcionam de dentro do projeto de API |

### Core (Frontend — `apps/web`)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `zod` | 4.4.3 | Schema de validação de formulário (slug, pix_key por tipo, urls sociais) | Já decidido em CLAUDE.md; reverificado nesta sessão |
| `react-hook-form` | 7.85.0 | Estado de formulário do cartão (tela única, múltiplas seções) | Já decidido em CLAUDE.md; reverificado nesta sessão |
| `@hookform/resolvers` | 5.8.0 | Ponte entre `react-hook-form` e schemas `zod` (`zodResolver`) | Necessário para usar zod como resolver do RHF — sem ele a integração exige código manual de parsing de erro |
| `cpf-cnpj-validator` | 2.1.2 | Valida CPF/CNPJ com dígito verificador, inclui CNPJ alfanumérico (RFB, vigente jul/2026) | Já decidido em CLAUDE.md; README confirma suporte a `12.ABC.345/01DE-35` |
| `@vercel/blob` | 2.8.0 | Upload de `photo_url` direto do browser (CARD-09) | Já decidido em CLAUDE.md — **nota:** CLAUDE.md registrou "latest (SDK 1.x)"; a versão atual confirmada via `npm view` nesta sessão é 2.8.0, não 1.x — ver State of the Art |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `react-imask` | 7.6.1 | Máscara de WhatsApp em tempo real com padrão dinâmico (8 vs 9 dígitos conforme DDD) | D-11 exige máscara BR ao vivo; `react-imask` suporta `maskBuilder`/regras condicionais, diferente de libs de máscara numérica genérica |
| `use-debounce` | 10.1.1 | Debounce da checagem de disponibilidade de slug (D-02) | Alternativa a um hook de debounce escrito à mão (~10 linhas) — ver Alternatives Considered |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `react-imask` | Máscara manual com regex + `onChange` | Manual é viável (poucas linhas) mas replica lógica condicional de "8 vs 9 dígitos por DDD" que já existe testada na lib — maior risco de bug sutil escrito à mão |
| `use-debounce` | Hook de debounce próprio (`useEffect` + `setTimeout`) | Debounce é trivial o suficiente para não precisar de dependência — usar hook próprio é igualmente válido e evita mais uma dependência externa; **recomendação: preferir hook próprio** dado o princípio do projeto de "sem abstração especulativa" |
| Try/catch manual em `PostgresException.SqlState == "23505"` | `EntityFrameworkCore.Exceptions.PostgreSQL` (NuGet) | A lib dá exceções tipadas (`UniqueConstraintException`) — mas para 1 constraint (slug) o try/catch manual é mais simples e não adiciona dependência |

**Installation:**
```bash
# apps/api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.11
dotnet add package BCrypt.Net-Next --version 4.2.1
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.11
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.11
dotnet tool install --global dotnet-ef  # required — not installed on this machine yet

# apps/web
npm install zod react-hook-form @hookform/resolvers cpf-cnpj-validator @vercel/blob react-imask use-debounce
```

**Version verification:** All backend package versions confirmed 2026-08-13 via NuGet flat-container API (`api.nuget.org/v3-flatcontainer/{package}/index.json`). All frontend package versions confirmed 2026-08-13 via `npm view {package} version` against the live npm registry. `dotnet --version` on this machine reports `10.0.400` SDK (matches target); `dotnet-ef` global tool is **not installed** — must be added in Wave 0.

## Package Legitimacy Audit

> slopcheck could not be installed in this environment (`pip` is not available on the research machine — Python is not on PATH). Per the graceful-degradation rule, **every package below is tagged `[ASSUMED]`**, even where npm/NuGet registry metadata (age, repo, maintainers) looks legitimate. The planner MUST gate each install behind a `checkpoint:human-verify` task before `npm install` / `dotnet add package` runs.

| Package | Registry | Age | Downloads | Source Repo | slopcheck | Disposition |
|---------|----------|-----|-----------|-------------|-----------|-------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | NuGet | First-party Microsoft, versioned with .NET since inception | N/A (framework pkg) | github.com/dotnet/aspnetcore | N/A — not run | [ASSUMED] Approved pending human verify |
| `BCrypt.Net-Next` | NuGet | Registry shows versions back to 2.x era; net10 target added at 4.1.0 | Not queried | github.com/BcryptNet/bcrypt.net | N/A — not run | [ASSUMED] Approved pending human verify |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | NuGet | Long-established Npgsql project | Not queried | github.com/npgsql/efcore.pg | N/A — not run | [ASSUMED] Approved pending human verify (already in CLAUDE.md as decided) |
| `Microsoft.EntityFrameworkCore.Design` / `.Tools` | NuGet | First-party Microsoft | N/A | github.com/dotnet/efcore | N/A — not run | [ASSUMED] Approved pending human verify |
| `zod` | npm | Created 2020-03-07 (~6 yrs) | Not queried | github.com/colinhacks/zod | N/A — not run | [ASSUMED] Approved pending human verify (already in CLAUDE.md) |
| `react-hook-form` | npm | Created 2019-03-20 (~7 yrs) | Not queried | github.com/react-hook-form/react-hook-form | N/A — not run | [ASSUMED] Approved pending human verify (already in CLAUDE.md) |
| `@hookform/resolvers` | npm | Created 2020-05-20 (~6 yrs) | Not queried | github.com/react-hook-form/resolvers | N/A — not run | [ASSUMED] Approved pending human verify |
| `cpf-cnpj-validator` | npm | Created 2018-09-03 (~8 yrs), latest published ~2 months ago | Not queried | github.com/carvalhoviniciusluiz/cpf-cnpj-validator | N/A — not run | [ASSUMED] Approved pending human verify (already in CLAUDE.md) |
| `@vercel/blob` | npm | Created 2023-04-18 (~3 yrs), first-party Vercel | Not queried | github.com/vercel/storage | N/A — not run | [ASSUMED] Approved pending human verify (already in CLAUDE.md) |
| `react-imask` | npm | Created 2017-11-20 (~9 yrs) | Not queried | github.com/uNmAnNeR/imaskjs | N/A — not run | [ASSUMED] Approved pending human verify — **new recommendation this session**, not previously in CLAUDE.md |
| `use-debounce` | npm | Not individually checked | Not queried | Not checked | N/A — not run | [ASSUMED] — **recommend skipping this dependency entirely** in favor of a hand-rolled debounce hook (see Alternatives Considered) |

**Packages removed due to slopcheck [SLOP] verdict:** none (slopcheck did not run)
**Packages flagged as suspicious [SUS]:** none (slopcheck did not run) — treat all as requiring human verification before install

## Architecture Patterns

### System Architecture Diagram

```
[Browser: Dashboard SPA-ish pages]
   |
   |  1. POST /auth/register {email,password}
   |  2. POST /auth/login    {email,password}
   v
[apps/api .NET 10 minimal API]
   |  - BCrypt.HashPassword / BCrypt.Verify
   |  - Issues JWT (HMAC-SHA256, JWT_SECRET, 15-30min exp)
   |  <-- {accessToken, user}
   v
[Browser stores accessToken in localStorage] (D-05)
   |
   |  3. Every dashboard write: Authorization: Bearer {token}
   v
[apps/api: .RequireAuthorization() endpoints]
   |  - JwtBearer middleware validates signature/issuer/audience/lifetime
   |  - 401 if missing/invalid/expired  --> Browser catches 401, redirects to /login (D-06)
   v
[EF Core 10 DbContext] --(Npgsql, SSL Require)--> [Postgres: local Docker (dev) | Neon (deployed)]
   |
   |  Card writes: slug availability check (advisory, debounced) -> POST /cards
   |  DB unique index on Card.slug is the real gate; 23505 caught -> 409 "slug em uso"
   v
[4 tables: User, Card, SocialLink, CardView]

[Browser: photo upload widget] --(signed token via handleUpload route)--> [Vercel Blob storage]
   |                                                                            |
   +---- photo_url returned to browser --> PATCH /cards/{id} {photo_url} -------+
```

### Recommended Project Structure

```
apps/api/
├── Program.cs                 # DI wiring, JwtBearer config, middleware order, endpoint mapping
├── Endpoints/
│   ├── AuthEndpoints.cs        # POST /auth/register, /auth/login, GET /auth/me
│   ├── CardEndpoints.cs        # POST/PUT /cards, slug availability check
│   └── SocialLinkEndpoints.cs  # add/remove/reorder
├── Services/
│   ├── AuthService.cs          # BCrypt hash/verify, JWT issuance
│   ├── SlugService.cs          # reserved-word check, normalization
│   └── PixValidationService.cs # server-side mirror of client zod/cpf-cnpj-validator rules
├── Data/
│   ├── AppDbContext.cs
│   ├── Entities/ (User.cs, Card.cs, SocialLink.cs, CardView.cs)
│   └── Migrations/
└── appsettings.json            # non-secret config; JWT_SECRET etc. via env vars, not this file

apps/web/
├── app/
│   ├── (dashboard)/login/page.tsx
│   ├── (dashboard)/dashboard/page.tsx
│   └── (dashboard)/dashboard/cards/[id]/edit/page.tsx
├── lib/
│   ├── api-client.ts           # fetch wrapper injecting Authorization header, 401 -> redirect
│   ├── auth-guard.tsx          # client component: reads localStorage, redirects if absent
│   ├── whatsapp-normalize.ts   # pure function: mask input -> +55 digits, DDD 9th-digit rule
│   └── pix-validation.ts       # zod schemas per pix_key_type, wraps cpf-cnpj-validator
└── components/
    ├── card-form/ (identity, contact, pix, social-links sections)
    └── avatar-placeholder.tsx  # initials-in-circle fallback (D-12)
```

### Pattern 1: Self-issued symmetric JWT (no external IdP)
**What:** API generates and validates its own JWT using a shared HMAC-SHA256 secret (`JWT_SECRET`), rather than delegating to an OIDC authority.
**When to use:** Simple first-party API + first-party SPA-like frontend, no third-party clients, short MVP timeline — exactly this project's situation, and explicitly locked in by `docs/specs/02-autentication.md`.
**Example:**
```csharp
// Program.cs — Source: pattern synthesized from Microsoft Learn "Configure JWT bearer
// authentication in ASP.NET Core" (learn.microsoft.com/aspnet/core/security/authentication/
// configure-jwt-bearer-authentication) adapted for a self-issued (non-OIDC) symmetric key,
// which the official doc does not show directly (it assumes Authority/OIDC) — this shape is
// [ASSUMED] from general .NET JWT bearer conventions, not copy-pasted from an official sample.
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET not set");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30) // don't use the 5-min default for a 15-30min token
        };
    });

builder.Services.AddAuthorizationBuilder(); // AddAuthentication before AddAuthorization — order matters

var app = builder.Build();

// app.UseAuthentication()/UseAuthorization() are auto-registered by WebApplication when
// AddAuthentication/AddAuthorization services are present (confirmed in official docs for
// aspnetcore-10.0), but calling them explicitly in this order is still safe and clearer:
app.UseCors();          // must run before auth middleware so preflight isn't blocked
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/register", RegisterHandler);
app.MapPost("/auth/login", LoginHandler);
app.MapGet("/auth/me", MeHandler).RequireAuthorization();

var cards = app.MapGroup("/cards").RequireAuthorization(); // ACCT-05: all writes 401 without token
cards.MapPost("/", CreateCardHandler);
cards.MapPut("/{id}", UpdateCardHandler);
```

```csharp
// Token issuance — Services/AuthService.cs
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Email, user.Email),
};
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(
    issuer: issuer,
    audience: audience,
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(20), // within the 15-30min window from spec 02
    signingCredentials: creds);
var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
```

### Pattern 2: Advisory availability check + DB-enforced uniqueness
**What:** Two-layer slug uniqueness — a fast `GET /cards/slug-available?value=x` endpoint for real-time UX (D-02), and a Postgres unique index as the actual gate at save time.
**When to use:** Any "check then create" flow where two users could race on the same value (usernames, slugs, emails).
**Example:**
```csharp
// Data/AppDbContext.cs
modelBuilder.Entity<Card>()
    .HasIndex(c => c.Slug)
    .IsUnique();
```
```csharp
// Endpoints/CardEndpoints.cs — the availability check is a hint, not a guarantee
app.MapPost("/cards", async (CreateCardDto dto, AppDbContext db, ClaimsPrincipal user) =>
{
    if (ReservedSlugs.Contains(dto.Slug.ToLowerInvariant()))
        return Results.Conflict(new { error = "slug_reserved" });

    var card = new Card { Slug = dto.Slug, /* ... */ };
    db.Cards.Add(card);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
    {
        return Results.Conflict(new { error = "slug_taken" });
    }
    return Results.Created($"/cards/{card.Id}", card);
}).RequireAuthorization();
```
*Source: pattern synthesized from Npgsql EF Core docs (npgsql.org/efcore/modeling/indexes.html) + community writeups on catching `PostgresException.SqlState 23505` — [CITED: npgsql.org] for the index API, [ASSUMED] for the exact exception-matching syntax shape (`when (ex.InnerException is PostgresException { SqlState: "23505" })` uses C# property patterns, verified as valid syntax against .NET's pattern-matching docs from training knowledge, not re-verified against an official sample this session).*

### Pattern 3: Client-only auth guard (not Next.js Middleware)
**What:** Because the JWT lives in `localStorage` (D-05), route protection cannot be implemented in Next.js Middleware — middleware executes on the Edge/server before any page JS runs, and `localStorage` is a browser-only API. The guard must be a Client Component (e.g., a wrapper in the `(dashboard)` layout) that reads the token on mount and redirects with `next/navigation`'s `redirect()`/`useRouter().push()` if absent.
**When to use:** Any app that chose `localStorage` (vs. an httpOnly cookie) for token storage.
**Example:**
```tsx
// app/(dashboard)/layout.tsx — Client Component guard, not Middleware
"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  useEffect(() => {
    if (!localStorage.getItem("accessToken")) {
      router.replace("/login");
    }
  }, [router]);
  return <>{children}</>;
}
```

### Anti-Patterns to Avoid
- **Next.js `middleware.ts` checking `localStorage` for the token:** does not work — middleware runs server-side/Edge, `localStorage` is undefined there. This would silently never redirect (or throw at build/runtime depending on how it's referenced).
- **Trusting the debounced slug-availability response as the final answer:** two browser tabs (or two users) can both see "available" before either saves — the DB unique index + `23505` catch is the only real guarantee.
- **Skipping server-side Pix/CPF re-validation because the client already validated:** the client-side `cpf-cnpj-validator`/zod checks are for UX; a malicious or buggy client can still POST an invalid key directly to the API, so `apps/api` needs its own mirror validation (already anticipated in CLAUDE.md: "regex espelho em apps/api").
- **Applying a flat "+9 to every 8-digit number" rule for WhatsApp normalization:** wrong — only DDDs 11–19, 21, 22, 24, 27, 28 got the 9th digit added; blindly prepending 9 to, say, a DDD 85 (Fortaleza) number that's already correct at 8 digits would corrupt the number.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| CPF/CNPJ check-digit validation | Custom digit-verification algorithm | `cpf-cnpj-validator` | Check-digit math has edge cases (repeated-digit CPFs like `111.111.111-11` must be rejected even though they'd pass a naive weighted-sum check) — already solved and tested in the library |
| JWT encode/decode/verify | Hand-rolled base64+HMAC signing | `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt` | Constant-time signature comparison, clock-skew handling, and claim-parsing edge cases are exactly the kind of thing that becomes a silent security hole if hand-rolled |
| Password hashing | Custom salt+SHA256 | `BCrypt.Net-Next` | BCrypt's adaptive work factor and built-in per-hash salt are the whole point — a hand-rolled scheme is a guaranteed downgrade |
| Postgres unique-constraint-violation detection | String-matching the exception message | Catch `PostgresException` and check `.SqlState == "23505"` | Exception messages are locale-dependent and can change between Postgres versions; `SqlState` is the stable, documented contract |

**Key insight:** every item in this table maps to a security- or correctness-critical primitive (auth, personal-document validation, uniqueness). This is exactly the domain where "faster to write it myself" produces the most expensive bugs later — none of these are UI polish, they're the load-bearing walls of ACCT-* and CARD-06/07.

### Reserved slugs (CARD-02) — recommendation

No single canonical "reserved slugs for a Brazilian consumer web app" list exists; this is synthesized from (a) the routes this project's own spec defines or will define, (b) general SaaS reserved-username convention (community gists), and (c) Portuguese-language equivalents relevant to this specific product. **[ASSUMED — recommend validating this list with the user before locking it in, per Assumptions Log.]**

```
// System / framework (must-not-collide with Next.js internals or existing routes)
login, dashboard, api, _next, admin, favicon.ico, robots.txt, sitemap.xml, manifest.json,
sw.js, .well-known, static, public, assets

// Anticipated future routes (Phase 2/3 per docs/specs/01-setup.md and ROADMAP.md)
qr, vcard, og, share

// Product/marketing pages likely to exist even in v1.x
sobre, about, contato, contact, termos, terms, privacidade, privacy, preco, precos, pricing,
planos, plans, ajuda, help, suporte, support, blog, home, index

// Generic account/action words that would collide with future dashboard sub-routes
cadastro, cadastrar, registro, entrar, sair, logout, signup, perfil, conta, account,
configuracoes, settings, config, editar, edit, novo, new, criar, create, delete, excluir

// Brand/trademark self-protection (avoid a user claiming the product's own future name)
www, mail, app, apps
```

**Recommendation:** store this as a `const RESERVED_SLUGS: string[]` (or a Postgres check, but a simple app-level array is enough for this scale) checked case-insensitively before the DB write, in `apps/api/Services/SlugService.cs` — not duplicated in the frontend, since the backend is the source of truth (frontend can call the same availability endpoint for the debounced check).

## Common Pitfalls

### Pitfall 1: Next.js Middleware can't see `localStorage`
**What goes wrong:** A developer instinctively reaches for `middleware.ts` to protect `/dashboard/*` routes (it's the "official" way to gate routes in Next.js), but the token lives in `localStorage` per D-05, which Middleware (server/Edge-side) cannot read.
**Why it happens:** Next.js docs and most tutorials assume cookie-based auth, where Middleware works great. `localStorage`-based auth is a different model that requires client-side guarding.
**How to avoid:** Implement the guard as a Client Component in the `(dashboard)` route group layout (Pattern 3 above), not as `middleware.ts`. Rely on the API's 401 response + a fetch wrapper that redirects on 401, consistent with D-06/D-07.
**Warning signs:** If `middleware.ts` references `localStorage`, `window`, or `document`, it will fail (these APIs don't exist in the Middleware/Edge runtime).

### Pitfall 2: Brazilian mobile 9th-digit is DDD-dependent, not universal
**What goes wrong:** Applying "always add a 9 after DDI+DDD if the number is 8 digits" to every DDD. This is wrong for DDDs outside 11–19, 21, 22, 24, 27, 28 — ANATEL's 2012 rule only added the 9th digit in those regions; other regions' mobile numbers were already 9 digits (or the "9" convention worked differently).
**Why it happens:** Most write-ups about "Brazilian phone number has 9 digits" oversimplify; the actual ANATEL rollout was regional and phased between 2012–2016.
**How to avoid:** Maintain an explicit whitelist of the DDDs affected by the digit-9 insertion rule, and only apply the transformation to those; for all other valid DDDs, accept the number's digit count as-is (validate length is 8 or 9 after DDD, not force it to 9).
**Warning signs:** A WhatsApp number for a user outside the São Paulo/Rio/Espírito Santo area codes gets a spurious 9 that doesn't match what they'd actually dial.

### Pitfall 3: Availability-check race condition on slug creation
**What goes wrong:** Two browser tabs (or two different users) both call the debounced "is this slug free?" endpoint, both get "yes," and both submit the create form — the second `INSERT` throws.
**Why it happens:** Classic TOCTOU (time-of-check to time-of-use) gap between the advisory check and the actual write.
**How to avoid:** Treat the availability endpoint as UX only; always wrap the actual `SaveChangesAsync()` in a try/catch for the Postgres `23505` unique-violation code and surface a friendly "esse slug acabou de ser usado, escolha outro" message — never assume the earlier "available" response is still true at save time.
**Warning signs:** Intermittent, non-reproducible "slug already exists" 500 errors that only happen under concurrent load or rapid double-submission.

### Pitfall 4: Self-issued JWT without OIDC is against current Microsoft guidance
**What goes wrong:** Current official ASP.NET Core docs (`learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication`, updated 2026-08-07 per fetched metadata) explicitly say: *"You should NOT create an access token from a username/password request... Access tokens should only be created using an OpenID Connect flow or an OAuth standard flow."* This project's canonical spec (`docs/specs/02-autentication.md`) deliberately does exactly what the doc warns against, as a 2-week-MVP simplification.
**Why it happens:** Full OIDC (a separate identity provider, PKCE flows, etc.) is real engineering overhead that doesn't fit a solo 2-week build for a single first-party client.
**How to avoid:** This is a locked, informed tradeoff, not an oversight — but the mitigations matter: keep the token lifetime short (15-30min, per spec), keep claims minimal (id + email, no roles/permissions that could be tampered with client-side), never accept a JWT-alg-`none` token, and don't expand this pattern to a public/third-party API surface later without revisiting.
**Warning signs:** If a future phase adds a third-party integration or mobile app consuming this same `/auth/login`, that's the signal this simplification has been outgrown and OIDC should be reconsidered.

### Pitfall 5: `SSL Mode=Prefer` against Neon fails silently in a confusing way
**What goes wrong:** Npgsql's default `SSL Mode` is `Prefer`, which does not validate the server certificate — this technically "works" against Neon (connection succeeds) but provides no real transport security guarantee and can mask connection troubleshooting later (already flagged as vetoed in CLAUDE.md).
**Why it happens:** `Prefer` is the library default, so it's easy to omit `SSL Mode` from the connection string entirely and not notice.
**How to avoid:** Always set `SSL Mode=Require;Trust Server Certificate=true` explicitly in both local (Docker Postgres — SSL likely off, so this must be conditional per environment) and deployed (Neon) connection strings; keep this in `.env.example` with a comment.
**Warning signs:** Connection works locally with a bare `Host=...;Database=...` string but the same string against Neon in CI/prod either fails or "succeeds" without any way to confirm certificate validation happened.

## Code Examples

### Vercel Blob client upload (CARD-09)
```ts
// app/api/upload/route.ts — Source: Vercel official docs (vercel.com/docs/vercel-blob/client-upload)
import { handleUpload, type HandleUploadBody } from '@vercel/blob/client';
import { NextResponse } from 'next/server';

export async function POST(request: Request): Promise<NextResponse> {
  const body = (await request.json()) as HandleUploadBody;

  const jsonResponse = await handleUpload({
    body,
    request,
    onBeforeGenerateToken: async (pathname, clientPayload) => {
      // TODO: verify Authorization: Bearer {token} here before allowing upload —
      // this route handler is unauthenticated by default; CARD-09 is a dashboard-only
      // action, so this must check the JWT the same way the .NET API would.
      return {
        allowedContentTypes: ['image/jpeg', 'image/png', 'image/webp'],
        addRandomSuffix: true,
        tokenPayload: JSON.stringify({ userId: /* from validated token */ '' }),
      };
    },
    onUploadCompleted: async ({ blob, tokenPayload }) => {
      // Persist blob.url as photo_url via PATCH /cards/{id} — see PROJECT.md integration note
    },
  });

  return NextResponse.json(jsonResponse);
}
```
*[CITED: vercel.com/docs/vercel-blob/client-upload]* — the `onBeforeGenerateToken` auth check is **not** shown in Vercel's own example with this project's specific JWT; that integration line is [ASSUMED] and must be implemented against this project's actual token validation.

### Pix key validation by type (CARD-06)
```ts
// lib/pix-validation.ts
import { z } from "zod";
import { cpf, cnpj } from "cpf-cnpj-validator";

const uuidV4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const brPhone = /^(\+55\s?)?\(?\d{2}\)?\s?9?\d{4}-?\d{4}$/;

export const pixKeySchema = z.discriminatedUnion("pix_key_type", [
  z.object({ pix_key_type: z.literal("cpf"), pix_key: z.string().refine(cpf.isValid, "CPF inválido") }),
  z.object({ pix_key_type: z.literal("cnpj"), pix_key: z.string().refine(cnpj.isValid, "CNPJ inválido") }),
  z.object({ pix_key_type: z.literal("email"), pix_key: z.string().email("E-mail inválido") }),
  z.object({ pix_key_type: z.literal("telefone"), pix_key: z.string().regex(brPhone, "Telefone inválido") }),
  z.object({ pix_key_type: z.literal("aleatoria"), pix_key: z.string().regex(uuidV4, "Chave aleatória inválida") }),
]);
```
*[ASSUMED shape — `cpf-cnpj-validator`'s `cpf.isValid`/`cnpj.isValid` function names confirmed via README fetch this session; the zod `discriminatedUnion` composition is a standard zod pattern from training knowledge, not re-verified against zod 4.4.3's exact API surface this session.]*

### WhatsApp normalization (CARD-08)
```ts
// lib/whatsapp-normalize.ts
const NINTH_DIGIT_DDDS = new Set([
  "11","12","13","14","15","16","17","18","19", // São Paulo
  "21","22","24",                                 // Rio de Janeiro
  "27","28",                                       // Espírito Santo
]);

export function normalizeWhatsapp(raw: string): string {
  const digits = raw.replace(/\D/g, "");
  // Strip a leading 55 (already has DDI) or a leading 0 (trunk prefix) before re-adding DDI
  let national = digits.startsWith("55") && digits.length > 11 ? digits.slice(2) : digits;
  national = national.replace(/^0/, "");

  const ddd = national.slice(0, 2);
  let localNumber = national.slice(2);

  if (NINTH_DIGIT_DDDS.has(ddd) && localNumber.length === 8) {
    localNumber = "9" + localNumber; // add the 9th digit only where ANATEL's rule applies
  }

  return `55${ddd}${localNumber}`; // pure digits, DDI 55, no punctuation
}
```
*[ASSUMED — synthesized from multiple WebSearch sources on the ANATEL 9th-digit rule (openclaw issue tracker, zoko.io, wassenger.com blog); not verified against an official ANATEL or Bacen document this session. Recommend a manual smoke test with a few known real numbers from different DDDs before trusting in production.]*

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| `@vercel/blob` SDK "1.x" (per CLAUDE.md) | `@vercel/blob@2.8.0` is the current published version | Confirmed via `npm view` 2026-08-13 | CLAUDE.md's version note is stale — verify the v2 client-upload API shape still matches `handleUpload`/`upload()` signatures used in this research (the official docs page fetched this session is written against the current published version, so the code examples above should already reflect v2, but re-check the changelog for breaking changes between 1.x and 2.x before implementation) |
| Cookie-based session + Middleware route protection (Next.js's most-documented pattern) | `localStorage` + client-side guard (this project's locked D-05 decision) | N/A — deliberate project choice, not an industry shift | Middleware-based examples found everywhere in Next.js tutorials will NOT work for this project's auth model — see Pitfall 1 |
| Numeric-only CNPJ | Alphanumeric CNPJ format (`12.ABC.345/01DE-35`) per Receita Federal | Effective July 2026 (this project's "today" is 2026-08-13, so this is now **current**, not upcoming) | `cpf-cnpj-validator@2.1.2`'s `cnpj.isValid` must accept both legacy numeric and new alphanumeric formats — confirmed supported per README, but worth a specific test case for an alphanumeric CNPJ in Wave 0 tests |

**Deprecated/outdated:**
- Manually parsing/validating JWTs without `Microsoft.AspNetCore.Authentication.JwtBearer`'s `TokenValidationParameters` — the framework-managed validation (signature, issuer, audience, lifetime) supersedes any hand-rolled check.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Exact reserved-slugs list (system + product + future-route words) | Don't Hand-Roll → Reserved slugs | Low-medium — a missing reserved word means a user could claim a slug that later collides with a real route (e.g., someone registers `/qr` before Phase 2 builds a `/[slug]/qr` route conflict is actually fine since that's a sub-path, but `/blog` or `/sobre` as a *user's* slug would break a future marketing page at the top level) |
| A2 | Brazilian 9th-digit DDD whitelist (11-19, 21, 22, 24, 27, 28) is complete and correct | Common Pitfalls → Pitfall 2, Code Examples → WhatsApp normalization | Medium — an incomplete/wrong DDD list means WhatsApp numbers for some users are stored with an incorrect digit count, breaking the `wa.me` deep link in Phase 3 (CONT-01) |
| A3 | Self-issued JWT + `TokenValidationParameters` code shape (`SymmetricSecurityKey`, `SigningCredentials`, manual `JwtSecurityToken` construction) — not copied verbatim from an official Microsoft sample for this exact non-OIDC scenario | Architecture Patterns → Pattern 1 | Low — this is a very standard, widely-used .NET pattern from training knowledge; risk is a minor API surface change in `System.IdentityModel.Tokens.Jwt` for .NET 10, not a conceptual error |
| A4 | `react-imask` is the best-fit library for the dynamic BR phone mask (vs. `react-number-format` or a hand-rolled mask) | Standard Stack → Supporting | Low-medium — if `react-imask`'s conditional/dynamic mask API doesn't fit as cleanly as expected, falling back to a hand-rolled mask function (same logic as the normalization function) is a safe alternative with no data-correctness risk, just more UI code |
| A5 | `use-debounce` recommendation (though the research itself recommends the hand-rolled alternative) | Standard Stack → Supporting / Alternatives Considered | Very low — either choice works; no functional risk either way |
| A6 | All Package Legitimacy Audit entries — slopcheck did not run in this environment | Package Legitimacy Audit | Medium (procedural, not technical) — planner must insert `checkpoint:human-verify` before every package install per the graceful-degradation protocol; skipping that checkpoint would mean this phase's dependencies were never independently verified beyond registry existence |
| A7 | `dotnet-ef` global tool is not installed on the target dev machine | Environment Availability | Low — this blocks running `dotnet ef migrations add`/`database update` until `dotnet tool install --global dotnet-ef` is run; must be an explicit Wave 0 setup step, not assumed to already exist |

**If this table is empty:** N/A — see entries above; several claims need confirmation before being treated as locked fact, especially A1 (reserved slugs) and A2 (DDD whitelist), both of which affect user-visible correctness (CARD-02, CARD-08).

## Open Questions

1. **Does the alphanumeric CNPJ format need a specific test fixture in Wave 0?**
   - What we know: `cpf-cnpj-validator@2.1.2`'s README documents support for `12.ABC.345/01DE-35`-style values.
   - What's unclear: Whether `cnpj.isValid()` was exercised against a real, RFB-published alphanumeric CNPJ test vector, or only against the library author's own synthetic examples.
   - Recommendation: Add one alphanumeric CNPJ test case in Wave 0 tests (`tests/pix-validation.test.ts`) using whatever official example the RFB technical note publishes, if available; otherwise flag as best-effort.

2. **Should the availability-check endpoint (`GET /cards/slug-available`) require auth?**
   - What we know: D-02 requires the check to run before the rest of the form is filled, i.e., possibly before the user has an account/session in some flow variants — but per D-01, account creation happens first, then the card form, so the user IS authenticated by the time they reach the slug field.
   - What's unclear: Whether this endpoint should still require `.RequireAuthorization()` (consistent with ACCT-05's "rotas de escrita" being protected) even though it's a read, not a write, and reveals no sensitive data (just true/false).
   - Recommendation: Require auth on it anyway for consistency and to avoid it becoming an unauthenticated slug-enumeration oracle — cheap to add, no UX cost since the user is already logged in at that point per D-01.

3. **`SSL Mode=Require` against local Docker Postgres — does the local container even have SSL enabled?**
   - What we know: Spec 01 says Docker Compose is used purely for local Postgres, no Neon Local proxy.
   - What's unclear: Stock `postgres` Docker images ship with SSL off by default; forcing `SSL Mode=Require` in the connection string locally would break local dev unless the connection string differs per environment.
   - Recommendation: Use an environment-variable-driven connection string where local `.env` omits/sets `SSL Mode=Disable` (or the Docker Postgres image is configured with SSL on) and only the deployed/Neon connection string enforces `SSL Mode=Require;Trust Server Certificate=true`. Document this explicitly in `.env.example` per spec 01's own criterion.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | `apps/api` build/run | ✓ | 10.0.400 | — |
| Node.js | `apps/web` build/run | ✓ | v22.21.1 | — |
| npm | Frontend package install | ✓ | 11.11.0 | — |
| Docker / Docker Compose | Local Postgres (spec 01) | ✓ | Docker 28.2.2 / Compose v2.37.1 | — |
| Git | Version control | ✓ | 2.47.0.windows.1 | — |
| `dotnet-ef` (global tool) | EF Core migrations CLI | ✗ | — | Install via `dotnet tool install --global dotnet-ef` — must be a Wave 0 step, no viable fallback for running migrations from the CLI |
| `psql` (Postgres client CLI) | Manual DB inspection/debugging | ✗ | — | Not required for the app to run; use `docker exec -it <container> psql` against the local container, or a GUI client (TablePlus/DBeaver/pgAdmin) — no blocking impact |

**Missing dependencies with no fallback:**
- None — `dotnet-ef` has a trivial one-command fix, not a hard blocker.

**Missing dependencies with fallback:**
- `dotnet-ef` global tool — install command above, add as an explicit Wave 0 task.
- `psql` CLI — use `docker exec` into the Compose Postgres container, or any GUI Postgres client.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework (backend) | xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) — not yet installed, greenfield |
| Framework (frontend) | Vitest — not yet installed, greenfield; recommended for pure-function tests (normalization, validation) rather than full component/e2e tests given the 2-week timeline |
| Config file | none — see Wave 0 Gaps |
| Quick run command (backend) | `dotnet test apps/api/Api.Tests` |
| Quick run command (frontend) | `npx vitest run` |
| Full suite command | `dotnet test` (backend) + `npx vitest run` (frontend) — no e2e framework recommended for this phase given timeline; manual smoke test of the full register→login→edit-card flow is the practical "full suite" gate |

*[ASSUMED — no test framework exists yet in this greenfield project; xUnit and Vitest are recommended as the conventional default for .NET minimal APIs and Next.js/TypeScript respectively, not confirmed against any project-specific preference since none has been stated.]*

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ACCT-01 | Register hashes password with BCrypt, never stores plaintext | unit/integration | `dotnet test --filter FullyQualifiedName~RegisterTests` | ❌ Wave 0 |
| ACCT-02 | Login returns valid JWT for correct credentials | integration | `dotnet test --filter FullyQualifiedName~LoginTests` | ❌ Wave 0 |
| ACCT-04/05 | Protected endpoints return 401 without/with-expired token | integration | `dotnet test --filter FullyQualifiedName~AuthGuardTests` | ❌ Wave 0 |
| CARD-01/02 | Slug uniqueness + reserved-word rejection | integration | `dotnet test --filter FullyQualifiedName~SlugTests` | ❌ Wave 0 |
| CARD-06 | Pix validation per type (CPF check digit, CNPJ alphanumeric, UUID v4) | unit | `npx vitest run pix-validation` | ❌ Wave 0 |
| CARD-08 | WhatsApp normalization (9th-digit DDD rule, +55 stripping) | unit | `npx vitest run whatsapp-normalize` | ❌ Wave 0 |
| CARD-09 | Photo upload flow (manual — Vercel Blob is an external service, hard to unit test meaningfully) | manual-only | — (smoke test via dashboard UI) | N/A |
| CARD-10 | Social link reorder persists `display_order` | integration | `dotnet test --filter FullyQualifiedName~SocialLinkReorderTests` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `npx vitest run <affected-file-pattern>` for frontend pure functions; `dotnet test --filter <affected-class>` for backend changes touching auth/slug/card endpoints.
- **Per wave merge:** Full `dotnet test` + `npx vitest run`.
- **Phase gate:** Full suite green before `/gsd:verify-work`, plus one manual end-to-end pass (register → create card with slug/pix/whatsapp/photo/social-links → reload → confirm session persists → logout/expire → confirm redirect).

### Wave 0 Gaps
- [ ] `apps/api/Api.Tests/Api.Tests.csproj` — xUnit + `Microsoft.AspNetCore.Mvc.Testing` project, none exists yet
- [ ] `apps/web/vitest.config.ts` — Vitest config, none exists yet
- [ ] `apps/web/lib/whatsapp-normalize.test.ts`, `apps/web/lib/pix-validation.test.ts` — cover CARD-06/CARD-08
- [ ] `apps/api/Api.Tests/AuthTests.cs`, `SlugTests.cs` — cover ACCT-01/02/04/05, CARD-01/02
- [ ] Framework install: `dotnet add apps/api/Api.Tests package Microsoft.AspNetCore.Mvc.Testing`, `npm install -D vitest` in `apps/web`

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-------------------|
| V2 Authentication | yes | BCrypt.Net-Next for password storage (adaptive hashing, never plaintext); JWT bearer for session — see Pitfall 4 for the ASVS-adjacent tension with self-issued tokens |
| V3 Session Management | yes | Short-lived JWT (15-30min) as the session mechanism; no server-side session store (stateless) — acceptable for this ASVS level given no refresh-token/revocation requirement in scope |
| V4 Access Control | yes | `.RequireAuthorization()` on all Card/SocialLink write endpoints (ACCT-05); ownership check needed (a user can only edit their own Card — verify `card.UserId == currentUserId` server-side, not just "any authenticated user") |
| V5 Input Validation | yes | `zod` (frontend) + mirrored validation in `apps/api` (Pix format, slug reserved words, phone format) — never trust client-only validation for anything persisted |
| V6 Cryptography | yes | `BCrypt.Net-Next` for password hashing (never hand-rolled, per Don't Hand-Roll); `JWT_SECRET` via environment variable, never hardcoded or committed |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|----------------------|
| Broken object-level authorization — user A edits user B's Card by guessing/incrementing `card_id` | Elevation of Privilege | Every Card/SocialLink write endpoint must check `card.UserId == ClaimsPrincipal.userId`, not just that *some* valid token was presented |
| JWT `alg: none` or algorithm-confusion attack | Tampering | `TokenValidationParameters.ValidateIssuerSigningKey = true` + explicit `SecurityAlgorithms.HmacSha256` on issuance — never accept a token whose header specifies `alg: none` |
| Pix key / personal document (CPF) over-exposure | Information Disclosure | This is a product decision already handled via CARD-07/D-08/D-09 UX friction — from a pure security-control standpoint, ensure the public card page (Phase 2) only ever renders what `is_branded`/card visibility rules allow, and that CARD-07's consent is actually enforced server-side (don't just trust a client-side checkbox — store a boolean `pix_consent_confirmed` if the modal/checkbox in D-09 needs to be durable) |
| SQL injection via slug or other free-text fields | Tampering | EF Core's parameterized queries (LINQ) handle this by default — the only risk is if raw SQL (`FromSqlRaw`) is ever used for the slug lookup; avoid raw SQL entirely for this phase |
| CORS misconfiguration (`AllowAnyOrigin` "just to get it working") | Spoofing | Spec 01 already mandates CORS restricted to the frontend's exact origin — verify this is an explicit allow-list (`WithOrigins("https://...")`), not `AllowAnyOrigin()`, especially once `Authorization` headers are in play |

## Sources

### Primary (HIGH confidence)
- [Configure JWT bearer authentication in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — fetched in full this session; confirms `AddJwtBearer`, `TokenValidationParameters`, automatic middleware registration in minimal APIs, and explicitly warns against self-issued JWTs from username/password
- NuGet flat-container API (`api.nuget.org/v3-flatcontainer/{package}/index.json`) queried directly this session for: `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.11), `BCrypt.Net-Next` (4.2.1 latest stable, 5.0.0 is prerelease), `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.3), `Microsoft.EntityFrameworkCore.Design`/`.Tools`/core (10.0.11)
- npm registry (`npm view {pkg} version` / `time.created` / `repository.url`) queried directly this session for: `zod` (4.4.3), `react-hook-form` (7.85.0), `cpf-cnpj-validator` (2.1.2), `@vercel/blob` (2.8.0), `react-imask` (7.6.1), `use-debounce` (10.1.1), `@hookform/resolvers` (5.8.0)

### Secondary (MEDIUM confidence)
- [Client Uploads with Vercel Blob | Vercel Docs](https://vercel.com/docs/vercel-blob/client-upload) — verified via WebSearch summary of official docs (`handleUpload`, `onBeforeGenerateToken`, `onUploadCompleted`)
- [cpf-cnpj-validator GitHub README](https://raw.githubusercontent.com/carvalhoviniciusluiz/cpf-cnpj-validator/master/README.md) — fetched directly, confirms `cpf.isValid`/`cnpj.isValid` API and alphanumeric CNPJ support
- [Npgsql Documentation — Indexes](https://www.npgsql.org/efcore/modeling/indexes.html) and [Security and Encryption](https://www.npgsql.org/doc/security.html) — referenced via WebSearch summary for `HasIndex().IsUnique()` and SSL Mode guidance
- [Neon Docs — Connect to Neon securely](https://neon.com/docs/connect/connect-securely) — WebSearch summary confirming `sslmode=require` convention

### Tertiary (LOW confidence)
- Brazilian WhatsApp 9th-digit normalization rule and DDD whitelist — synthesized from multiple community sources (GitHub issue trackers, Zoko/Wassenger blog posts) via WebSearch, no single official ANATEL/Bacen document consulted directly this session. **Flagged in Assumptions Log (A2) — recommend manual spot-check before trusting in production.**
- Reserved-slugs convention — synthesized from a community GitHub Gist (`gist.github.com/caseyohara/1453705`) plus this project's own spec-derived routes. **Flagged in Assumptions Log (A1).**
- `react-imask` vs `react-number-format` comparison for dynamic BR phone masks — WebSearch-derived summary only, not independently verified against either library's actual docs this session.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions confirmed directly against NuGet/npm registries this session (though legitimacy audit is [ASSUMED] pending slopcheck, which was unavailable in this environment)
- Architecture (JWT, EF Core uniqueness, client-only auth guard): HIGH-MEDIUM — JWT bearer config confirmed via official MS docs fetched in full; self-issued-token code shape and EF Core race-condition pattern are well-established .NET conventions but not copy-pasted from an official sample for this exact combination
- Pitfalls: MEDIUM — the Middleware/localStorage pitfall is a well-understood Next.js architectural fact (HIGH); the Brazilian phone 9th-digit rule is MEDIUM-LOW (community-sourced, no official source consulted directly)

**Research date:** 2026-08-13
**Valid until:** ~2026-09-12 (30 days — stable domain overall, but re-check `@vercel/blob` version and `cpf-cnpj-validator` CNPJ-alfanumérico behavior sooner if implementation starts near the July 2026 RFB effective date boundary, since that regulatory change is very recent relative to this research date)
