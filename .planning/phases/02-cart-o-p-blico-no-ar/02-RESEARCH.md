# Phase 2: Cartão Público no Ar - Research

**Researched:** 2026-08-14
**Domain:** Next.js 16 App Router ISR/caching, .NET 10 minimal API public endpoint design, free-tier cold-start mitigation (Render + Neon), QR code generation, domain/DNS mechanics
**Confidence:** HIGH (Next.js caching model, .NET endpoint design, QR API — verified via bundled docs / codebase) / MEDIUM (keep-alive service choice, domain registration mechanics — WebSearch-verified, not officially documented for this exact use case)

## Summary

This phase has two genuinely new technical problems not already decided in the project's root `CLAUDE.md`: (1) making a page that must survive a cold Render dyno and a cold Neon compute without the visitor noticing, and (2) exposing exactly one new *unauthenticated* read path in a backend that today requires a bearer token on every route under `/cards`. Both are solvable with patterns already latent in the codebase — `SlugService.Normalize`, the existing `/health` endpoint, and Next.js's classic (non-cache-components) ISR model that this project has already locked in via `CLAUDE.md`.

The critical non-obvious finding: the public GET-by-slug endpoint **must not** be added inside the existing `cards` route group in `Program.cs`, because that group has `.RequireAuthorization()` applied at the group level (line 94). It needs its own top-level route (e.g. `app.MapGet("/public/cards/{slug}", ...)`). The second critical finding: Vercel's Hobby cron jobs are capped at **once per day** — completely unusable for PUB-04's keep-alive requirement, which needs a ping every 5–14 minutes. An external free service (cron-job.org, no commercial-use restriction found) or a scheduled GitHub Actions workflow is required instead. The third finding: Neon's 5-minute autosuspend window is *shorter* than any keep-alive interval that satisfies Render's 15-minute window — meaning Neon will still nap between pings, but that's fine, because Neon's own wake time (~500ms) is negligible; only Render's 30–60s wake needs active prevention.

**Primary recommendation:** One external cron (cron-job.org, 5–10 min interval) hitting the existing `/health` endpoint on Render satisfies PUB-04 in full. A new `app.MapGet("/public/cards/{slug}", ...)` outside the authorized `cards` group, returning a purpose-built `PublicCardDto`, satisfies PUB-01/05/06. The QR route (`app/[slug]/qr/route.ts`, already reserved as a slug) needs zero backend dependency — it encodes `NEXT_PUBLIC_APP_URL/{slug}` as a string, so it can never cold-start-fail.

## User Constraints (from CONTEXT.md)

<user_constraints>

### Locked Decisions

- **D-14:** Nome de trabalho definido como "Vizzo", domínio alvo `vizzo.com.br` — **provisório**, usuário ainda precisa confirmar disponibilidade real (registro.br) e ausência de conflito de marca (INPI) antes de travar de vez.
- **D-15:** TLD preferido: `.com.br`. Até confirmação final, planner/executor devem usar `NEXT_PUBLIC_APP_URL` (ou equivalente) para a URL base do produto — nunca hardcode.
- **D-16:** QR do cartão fica **sempre visível na tela de edição** — não é ação separada atrás de clique.
- **D-17:** Download do QR é **só o código puro, sem legenda/texto embutido**.
- **D-18:** Cor do QR na tela e no download é **preto no branco** (padrão), não cor de marca.
- Formato de arquivo (SVG padrão + PNG fallback) e nível de correção de erro (M) já travados na pesquisa de stack do projeto (`CLAUDE.md`) — não reabertos.
- **D-19:** Campos/seções vazios (sem WhatsApp, sem Pix, sem foto, sem links sociais) **somem inteiramente** da página pública — não aparecem como seção vazia ou botão desabilitado.
- **D-20:** Cartão salvo com o mínimo da Fase 1 (só slug + nome) fica **imediatamente acessível publicamente** em `/[slug]` — sem limiar de "preenchimento mínimo" adicional.
- **D-21:** Estado extremo (só nome + placeholder de iniciais) é **estado visual válido por si só** — sem mensagem de "cartão incompleto".
- **D-22:** 404 de slug inexistente (PUB-06) tem **a cara do produto** — não é a 404 genérica do framework.
- **D-23:** 404 inclui **CTA de cadastro** ("Crie seu próprio cartão" ou similar).

### Claude's Discretion

- Layout exato da tela de edição pra acomodar o QR sempre visível (posição, tamanho na tela vs. tamanho de download).
- Mecanismo técnico exato de pré-aquecimento (PUB-03) e keep-alive (PUB-04) — fetch disparado no save + cron/serviço externo de ping, a escolher na pesquisa/planejamento.
- Estrutura exata da rota `/[slug]` (Server Component + `revalidate`, já documentado na pesquisa de stack do projeto) — decisão técnica já pesquisada, não reaberta aqui.
- Copy exata da 404 (D-22/D-23 travam a intenção, não o texto literal).

### Deferred Ideas (OUT OF SCOPE)

- Botão de WhatsApp/copiar Pix/.vcf na página pública (Fase 3).
- Preview de link OG image (Fase 3, `SHARE-03`/`SHARE-04`).
- Analytics de visualização (Fase 4).
- Remoção efetiva de marca / `is_branded` rendering (Fase 4, `BRAND-02`) — esta fase só resolve nome+domínio do produto em si.
- **Pendência cross-fase:** confirmação final e registro real do domínio (D-14/D-15) não bloqueia planejamento/execução técnica (usa env var), mas bloqueia deploy real em produção — recomendado resolver antes do fim da Fase 2.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PUB-01 | Qualquer pessoa acessa `/[slug]` sem auth, mobile-first | New unauthenticated `.NET` endpoint design (§2), Next.js Server Component fetch pattern (§3), no-CORS-needed clarification |
| PUB-02 | Página pública servida por ISR sem depender do backend acordado a cada visita | `export const revalidate = 60` classic ISR model (verified via bundled Next.js 16 docs), stale-while-revalidate behavior |
| PUB-03 | Cartão pré-aquecido no save, primeiro acesso não cai em cold start | Client-side fire-and-forget fetch to public URL right after save success (§1) |
| PUB-04 | Keep-alive externo mantém backend + banco acordados | cron-job.org (or GH Actions) pinging existing `/health` endpoint every 5–10 min (§1) — Vercel Hobby cron ruled out (once/day cap) |
| PUB-05 | Edição no dashboard reflete no público sem novo deploy | Same ISR revalidate window as PUB-02; no `revalidatePath` needed given `CLAUDE.md`'s already-locked "no on-demand revalidation in MVP" decision |
| PUB-06 | Slug inexistente retorna 404 própria, não erro de servidor | `not-found.tsx` + `notFound()` pattern (§3), streaming caveat that can silently downgrade the HTTP status to 200 (§ Pitfalls) |
| SHARE-01 | QR visível na tela em tamanho utilizável | `app/[slug]/qr/route.ts` reused as `<img src>` for on-screen preview — zero backend dependency (§4) |
| SHARE-02 | QR baixável em resolução para impressão | Same route, `?download=1` toggling `Content-Disposition`, PNG variant via `toBuffer` (§4) |
| BRAND-01 | Nome + domínio registrado, apontando pro frontend em produção | registro.br mechanics + Vercel A/CNAME record mechanics (§5) |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Public card lookup by slug | API / Backend | Database | New unauthenticated read endpoint owns the query + DTO shaping; DB owns the unique index already backing `SlugService` |
| Public page rendering (`/[slug]`) | Frontend Server (SSR/ISR) | — | Server Component fetch + `revalidate=60`; no client-side data fetching needed, no auth token exists for anonymous visitors anyway |
| ISR cache freshness | Frontend Server (SSR) | CDN/Static (Vercel Edge Network serves the cached HTML) | Vercel's infrastructure serves the cached page from edge without invoking the Next.js function on cache hits |
| QR code generation (screen + download) | Frontend Server (Route Handler, Node runtime) | — | Pure string-in/string-out transform of `NEXT_PUBLIC_APP_URL + slug` — no DB/API call, so it never inherits backend cold start |
| Pre-warm on save | Browser / Client | Frontend Server, API / Backend | Fire-and-forget request originates in the dashboard's client component right after a successful save; it fans out through Next.js's SSR layer into the API/DB, waking both |
| Keep-alive ping | External service (out of tier map) | API / Backend, Database | Lives entirely outside the app's own deployed tiers — an external cron caller; `/health` endpoint is the only in-tier surface it touches |
| Domain/DNS resolution | CDN / Static (Vercel edge routing) | — | DNS + TLS termination happens at Vercel's edge before any application code runs |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|---------------|
| `qrcode` (npm) | 1.5.4 | QR generation (SVG inline preview + PNG/SVG download) | Already locked in root `CLAUDE.md` (HIGH confidence, npm-verified 13/08/2026) — not reopened here |
| `@types/qrcode` | 1.5.6 | TypeScript types for `qrcode` | `qrcode` itself ships no types; this is the DefinitelyTyped companion package, needed for `strict` TS in `apps/web` |

No new backend (.NET/NuGet) packages are needed for this phase — the public endpoint reuses `AppDbContext`, `SlugService`, and the existing `Card`/`SocialLink` entities. No new frontend data-fetching libraries needed — plain Server Component `fetch`.

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| — | — | — | No supporting libraries beyond `qrcode`/`@types/qrcode` were identified as necessary for this phase's scope |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| External cron (cron-job.org) for keep-alive | UptimeRobot free plan | UptimeRobot's Dec 2024 ToS update restricts the free plan to "personal, non-commercial use" — this product intends eventual monetization (UPG-01..03 in Phase 4), so it is a **worse fit despite being the more commonly cited option in tutorials** [MEDIUM confidence — WebSearch, ToS not independently re-verified against primary source this session]. cron-job.org's ToS only requires you own/have permission for the pinged URL, with no commercial-use carve-out found. |
| External cron for keep-alive | GitHub Actions scheduled workflow (`on: schedule`) | Free, but GitHub's own docs state scheduled workflows "may be delayed during periods of high loads" and minimum interval is officially 5 minutes — viable as a fallback, but a dedicated cron service is more purpose-built and has no minimum-interval ambiguity |
| Client-side `qrcode.toString()` for on-screen preview | `<img src="/[slug]/qr">` pointing at the same Route Handler used for download | Recommended: reusing the Route Handler avoids maintaining two QR-generation code paths (client bundle + server route) and guarantees the on-screen preview and the downloaded file are byte-identical |

**Installation:**
```bash
cd apps/web
npm install qrcode
npm install -D @types/qrcode
```

**Version verification:** Confirmed via `npm view qrcode version` → `1.5.4` (matches `CLAUDE.md`, last published 2025-11-13) and `npm view @types/qrcode version` → `1.5.6`, both checked 2026-08-14.

## Package Legitimacy Audit

> slopcheck could not be installed this session (`pip` not found in the execution environment — `pip: command not found`). Per the graceful-degradation protocol, **both packages below are tagged `[ASSUMED]`** and must be gated behind a `checkpoint:human-verify` task before install, despite the manual registry checks below looking clean.

| Package | Registry | Age | Downloads | Source Repo | slopcheck | Disposition |
|---------|----------|-----|-----------|-------------|-----------|-------------|
| `qrcode` | npm | Long-established (first published pre-2016 lineage under `soldair/node-qrcode`; version 1.5.4 published 2025-11-13) | Not queryable without slopcheck/npm-stat this session | `github.com/soldair/node-qrcode` | N/A (unavailable) | Approved — `[ASSUMED]`, already independently locked in root `CLAUDE.md` with its own npm-view verification (13/08/2026) |
| `@types/qrcode` | npm | Created 2016-10-26 (DefinitelyTyped) | Not queryable without slopcheck/npm-stat this session | `github.com/DefinitelyTyped/DefinitelyTyped` | N/A (unavailable) | Approved — `[ASSUMED]`, no postinstall script found via `npm view @types/qrcode scripts.postinstall` (empty) |

**Packages removed due to slopcheck [SLOP] verdict:** none (slopcheck unavailable — see above)
**Packages flagged as suspicious [SUS]:** none found via manual check, but both remain `[ASSUMED]` per protocol — planner must add `checkpoint:human-verify` before `npm install qrcode` / `npm install -D @types/qrcode`.

## Architecture Patterns

### System Architecture Diagram

```
Visitor's phone (QR scan or link tap)
        │
        ▼
  GET https://vizzo.com.br/{slug}   ──────────────────────────┐
        │                                                      │
        ▼                                                      │
  Vercel Edge Network                                          │
   ├─ Cache HIT (within revalidate=60 window) ──► serve cached HTML, done
   └─ Cache MISS / STALE                                       │
        │                                                      │
        ▼                                                      │
  Next.js Server Component (app/[slug]/page.tsx, Node runtime) │
        │  fetch(`${API_URL}/public/cards/${slug}`)            │
        ▼                                                      │
  Render (.NET minimal API) ── may be cold (15 min idle) ──────┘
        │  GET /public/cards/{slug}   (NO auth required)
        ▼
  EF Core → Neon Postgres ── may be cold (5 min idle, wakes ~500ms)
        │
        ├─ found  → 200 PublicCardDto → page renders → cached for 60s
        └─ not found → 404 → notFound() thrown → not-found.tsx renders (true 404 status, non-streamed)

Parallel, out-of-band paths:
  Dashboard save (PUT /cards/{id}) ──success──► fire-and-forget GET /{slug}
                                                  (pre-warms the exact path above, PUB-03)

  External cron (cron-job.org, every 5-10 min) ──► GET /health (Render)
                                                     (keeps Render warm, incidentally
                                                      wakes Neon too — PUB-04)

  Dashboard "always visible" QR:
    <img src="/{slug}/qr">  ──► Route Handler (Node runtime, apps/web only)
                                  no backend/DB call — pure string→QR transform
```

### Recommended Project Structure

```
apps/web/
├── app/
│   ├── [slug]/
│   │   ├── page.tsx          # PUB-01/02/05: Server Component, revalidate=60, calls notFound()
│   │   ├── not-found.tsx     # PUB-06: branded 404, segment-scoped (NOT global-not-found.js)
│   │   └── qr/
│   │       └── route.ts      # SHARE-01/02: SVG/PNG QR, zero backend dependency
│   └── (dashboard)/
│       └── dashboard/cards/[id]/edit/page.tsx   # existing — add pre-warm fetch after save
├── components/
│   └── public-card/           # new: presentational components for the public page
│       ├── public-card-view.tsx
│       └── qr-preview.tsx     # <img src={`/${slug}/qr`}> used both here and in edit screen
└── lib/
    └── public-card.ts         # fetch wrapper for the unauthenticated public endpoint (NOT api-client.ts)

apps/api/
├── Endpoints/
│   └── PublicCardEndpoints.cs  # new file — GetBySlugHandler, mapped OUTSIDE the `cards` group
├── Contracts/
│   └── PublicCardDtos.cs       # new file — PublicCardDto, PublicSocialLinkDto (no Id/UserId leak)
└── Program.cs                  # add: app.MapGet("/public/cards/{slug}", ...) — top-level, no .RequireAuthorization()
```

### Pattern 1: Public unauthenticated endpoint MUST live outside the `cards` route group

**What:** `Program.cs` currently does `var cards = app.MapGroup("/cards").RequireAuthorization(); cards.MapCardEndpoints(); cards.MapSocialLinkEndpoints();`. `RequireAuthorization()` is applied at the **group** level, so anything mapped onto `cards` (including inside `CardEndpoints.MapCardEndpoints`) inherits the auth requirement.

**When to use:** Any time a new public route is added to this codebase going forward — check whether it's mapped onto an authorized group before assuming it's reachable without a token.

**Example:**
```csharp
// Program.cs — DO NOT add this inside MapCardEndpoints() or onto the `cards` group.
// Source: apps/api/Program.cs line 94 (read this session)
app.MapGet("/public/cards/{slug}", PublicCardEndpoints.GetBySlugHandler);

var cards = app.MapGroup("/cards").RequireAuthorization();
cards.MapCardEndpoints();
cards.MapSocialLinkEndpoints();
```

```csharp
// apps/api/Endpoints/PublicCardEndpoints.cs (new)
public static class PublicCardEndpoints
{
    public static async Task<IResult> GetBySlugHandler(string slug, AppDbContext db)
    {
        var normalized = SlugService.Normalize(slug); // reuse Fase 1's normalizer — CONTEXT.md code_context
        var card = await db.Cards
            .Include(c => c.SocialLinks.OrderBy(l => l.DisplayOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == normalized);

        if (card is null)
            return Results.NotFound(); // maps to Next.js's notFound() on the frontend

        return Results.Ok(ToPublicDto(card));
    }

    private static PublicCardDto ToPublicDto(Card card) => new(
        card.Slug, card.FullName, card.Role, card.Company, card.PhotoUrl,
        card.SocialLinks.OrderBy(l => l.DisplayOrder)
            .Select(l => new PublicSocialLinkDto(l.Platform, l.Url, l.DisplayOrder))
            .ToList());
    // Deliberately excludes: Id, UserId, Phone, Email, WhatsappNumber, PixKey, PixKeyType,
    // PixConsentConfirmed, IsBranded, CreatedAt/UpdatedAt — none are rendered by Phase 2's
    // public page (D-19/deferred CONT/PAY features). See "Open Questions" for whether Phase 3
    // should widen this DTO now vs. later.
}
```

### Pattern 2: ISR without `generateStaticParams` (fallback-style on-demand generation)

**What:** The public route is a dynamic segment (`[slug]`) whose full set of values is unknown at build time (new cards are created continuously). `export const revalidate = 60` alone — with **no** `generateStaticParams` — is sufficient: `dynamicParams` defaults to `true`, so any slug not statically known is rendered on first request and then cached for the `revalidate` window. This matches `CLAUDE.md`'s already-locked decision (§4, "não há generateStaticParams necessário" is implied but not stated explicitly there — confirmed here via Next.js docs).

**When to use:** Any route where the full param space can't be enumerated at build time but should still benefit from ISR caching after first render.

**Example:**
```tsx
// Source: node_modules/next/dist/docs/01-app/02-guides/incremental-static-regeneration.md
// (read this session, Next.js 16.3.1 bundled docs) — combined with
// node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/02-route-segment-config/dynamicParams.md
// app/[slug]/page.tsx
export const revalidate = 60; // CLAUDE.md-locked: ISR by time, not on-demand revalidation

export default async function CardPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/public/cards/${slug}`);

  if (res.status === 404) {
    notFound(); // triggers app/[slug]/not-found.tsx — PUB-06
  }
  if (!res.ok) {
    throw new Error(`Unexpected status ${res.status}`); // triggers error.tsx / default error UI
  }

  const card = (await res.json()) as PublicCardDto;
  return <PublicCardView card={card} />;
}
```

### Pattern 3: Fetch from a Server Component never hits CORS

**What:** `CORS` is a *browser* enforcement mechanism — it is checked by the browser before allowing a cross-origin `fetch`/`XHR` response to be read by page JavaScript. A `fetch()` call made inside a Next.js Server Component runs in Node.js on Vercel's server, not in a browser context, so no CORS preflight or origin check applies regardless of whether `apps/api`'s `Cors:WebOrigin` allowlist includes the Next.js origin.

**When to use:** This resolves the "how to avoid CORS issues" question raised in the phase brief — no code changes to `Program.cs`'s existing `AddCors`/`UseCors("web")` config are needed for the public page's server-side fetch. The existing CORS policy remains scoped to protecting the authenticated dashboard's browser-side calls (`api-client.ts`), which is unrelated to this new server-to-server call.

**Anti-pattern to avoid:** Do not build the public page as a Client Component that calls the public endpoint via `fetch` in `useEffect` (as `EditCardPage` does for the authenticated dashboard) — that would (a) require adding the Vercel production origin to `Cors:WebOrigin`, (b) forfeit ISR entirely (client-fetched data can't be cached by Next.js's page-level cache), and (c) show a loading flash on every visit, which directly contradicts PUB-02's "carregamento rápido" goal.

### Anti-Patterns to Avoid

- **Adding a `loading.tsx` to the `app/[slug]/` segment:** Introduces a Suspense boundary, which per Next.js's own docs makes the `not-found.js` response "streamed" — and streamed not-found responses return HTTP **200**, not 404. This would silently break PUB-06's "retorna 404 própria, não erro de servidor" success criterion for any consumer checking the status code (search engines, link preview crawlers in Phase 3, monitoring tools) even though the page visually looks like a 404. Keep `/[slug]/page.tsx` a single `await`-then-render/notFound() Server Component with no segment-level `loading.tsx`.
- **Setting `Content-Disposition: attachment` unconditionally on the QR route:** Breaks the "always visible on screen" requirement (D-16) if the same route is used for both the `<img src>` preview and the download link — some browsers refuse to render an `attachment`-disposed resource inline. Gate the header behind an explicit `?download=1` (or separate route) so the default response is inline-renderable.
- **Reusing `api-client.ts` (`apiFetch`) for the public page's data fetch:** It unconditionally attaches an `Authorization: Bearer` header when a token exists in `localStorage` and — critically — `localStorage` doesn't exist in a Server Component at all (it's a browser-only API), so this would crash. A separate, deliberately auth-free fetch helper is needed for the public path (already flagged in `CONTEXT.md`'s `code_context`).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Slug normalization for the public lookup | A second regex/lowercasing helper in the new public endpoint | `SlugService.Normalize` (existing, `apps/api/Services/SlugService.cs`) | Already handles trim + lowercase consistently with how slugs are persisted; a second implementation risks drifting (e.g. different Unicode handling) and would let a case-mismatched slug 404 incorrectly |
| Cold-start keep-alive scheduling | A custom always-on Node.js process, VPS cron, or self-hosted scheduler | Free external HTTP cron service (cron-job.org) pinging the existing `/health` endpoint | Zero infrastructure to maintain, zero cost, and the project's own constraint is "free tier em tudo" — a self-run scheduler would itself need somewhere free-and-always-on to run, which doesn't exist in this stack |
| QR code image generation | Canvas-based drawing of QR modules by hand | `qrcode` npm package (already locked in `CLAUDE.md`) | QR encoding (Reed-Solomon error correction, version/mask selection) is a nontrivial spec (ISO/IEC 18004) — a hand-rolled encoder is a correctness minefield for zero benefit |

**Key insight:** Nothing in this phase's *product logic* is complex enough to justify a library beyond `qrcode` — the actual difficulty is entirely in *infrastructure sequencing* (which tier owns which cold-start risk, and in what order things wake up), which is a design problem, not a code-complexity problem.

## Common Pitfalls

### Pitfall 1: Adding the public endpoint inside the authorized `cards` group
**What goes wrong:** A new `GET /cards/public/{slug}` (or similar) mapped via `cards.MapCardEndpoints()` or directly onto the `cards` `RouteGroupBuilder` silently requires a Bearer token, because `.RequireAuthorization()` is applied once at the group level in `Program.cs`.
**Why it happens:** The existing `CardEndpoints.MapCardEndpoints(this RouteGroupBuilder cards)` extension method signature makes it natural to add one more `cards.MapGet(...)` line inside it, following the file's own established pattern.
**How to avoid:** Map the public endpoint directly onto `app` (the top-level `WebApplication`), before or after the `cards` group is configured, as its own standalone route.
**Warning signs:** An integration test hitting the new endpoint without a Bearer token returns 401 instead of 200/404.

### Pitfall 2: Streamed not-found responses return HTTP 200
**What goes wrong:** If any Suspense boundary (`loading.tsx`, or a nested `<Suspense>`) exists above `app/[slug]/not-found.tsx` in the render tree, Next.js flushes the response headers (status 200) before it knows the segment will call `notFound()`, so the wire-level response is 200 even though the visible content is the "not found" UI.
**Why it happens:** Streaming SSR must commit to a status code before all async work resolves; this is documented Next.js behavior, not a bug.
**How to avoid:** Keep `app/[slug]/page.tsx` free of segment-level `loading.tsx`; do the `fetch` + `notFound()` decision synchronously within the single async Server Component so the whole response resolves before any bytes are sent.
**Warning signs:** `curl -I https://vizzo.com.br/nonexistent-slug` returns `200` instead of `404`.

### Pitfall 3: Keep-alive interval mismatched to Render's actual spin-down window
**What goes wrong:** Choosing a ping interval close to or above Render free tier's 15-minute inactivity threshold (e.g. every 14 minutes with a jittery/unreliable external scheduler) risks the service spinning down between pings, defeating PUB-04.
**Why it happens:** External cron services don't guarantee millisecond-precise firing; Vercel's own cron docs note jobs "may be invoked at any point within the specified hour" for coarse schedules — the same imprecision risk applies to any third-party scheduler under load.
**How to avoid:** Use a comfortable safety margin — 5–10 minutes, not 14 — so scheduler jitter can't push a gap past 15 minutes.
**Warning signs:** Intermittent slow first-loads (30–60s) that don't correlate with actual long-idle periods, suggesting the keep-alive occasionally missed its window.

### Pitfall 4: Assuming pre-warm-on-save alone satisfies PUB-04
**What goes wrong:** PUB-03 (pre-warm on save) only fires when the *owner* edits their card. If a card is never touched again after creation, PUB-03 provides zero protection against cold start for the next 15 minutes of visitor traffic — this is exactly what PUB-04 (independent, time-based keep-alive) exists to cover. Treating them as redundant and skipping PUB-04 leaves a real gap for any card that isn't actively being edited when a visitor scans its QR.
**Why it happens:** Both mechanisms "wake the backend," so it's tempting to conflate them.
**How to avoid:** Implement both as genuinely independent mechanisms — PUB-03 client-triggered on save success, PUB-04 external-cron-triggered regardless of any save activity.
**Warning signs:** Cold-start complaints on cards that haven't been edited recently.

## Code Examples

### QR Route Handler — inline preview + gated download (SHARE-01/02, D-16/D-17/D-18)

```ts
// Source: qrcode npm README (github.com/soldair/node-qrcode) + CLAUDE.md's already-locked
// API usage (toString for SVG, toBuffer for PNG, errorCorrectionLevel M)
// app/[slug]/qr/route.ts
import QRCode from "qrcode";

export async function GET(request: Request, { params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const url = new URL(request.url);
  const format = url.searchParams.get("format") === "png" ? "png" : "svg";
  const download = url.searchParams.get("download") === "1";
  const publicUrl = `${process.env.NEXT_PUBLIC_APP_URL}/${slug}`;

  if (format === "png") {
    const buffer = await QRCode.toBuffer(publicUrl, {
      type: "png",
      width: 1024,
      errorCorrectionLevel: "M", // D-18: default B/W, no logo — H not needed
    });
    return new Response(buffer, {
      headers: {
        "Content-Type": "image/png",
        ...(download ? { "Content-Disposition": `attachment; filename="${slug}-qr.png"` } : {}),
      },
    });
  }

  const svg = await QRCode.toString(publicUrl, { type: "svg", errorCorrectionLevel: "M" });
  return new Response(svg, {
    headers: {
      "Content-Type": "image/svg+xml",
      ...(download ? { "Content-Disposition": `attachment; filename="${slug}-qr.svg"` } : {}),
    },
  });
}
```

```tsx
// On-screen preview reused in both the edit dashboard (D-16) and any future share UI
// components/public-card/qr-preview.tsx
export function QrPreview({ slug }: { slug: string }) {
  return (
    <img
      src={`/${slug}/qr`}
      alt="QR code do cartão"
      width={240}
      height={240}
      className="rounded-lg border border-zinc-200 bg-white"
    />
  );
}
```

### Pre-warm on save (PUB-03)

```ts
// Fire-and-forget — must not block the save success toast, must not throw if it fails
// (a failed pre-warm ping is not a save failure). Add right after the existing
// toast.success("Alterações salvas.") in card-form.tsx's onSubmit.
function prewarmPublicCard(slug: string) {
  fetch(`${process.env.NEXT_PUBLIC_APP_URL}/${slug}`, { cache: "no-store" }).catch(() => {
    // Deliberately swallowed — pre-warm is best-effort, PUB-04's keep-alive is the
    // durable fallback. Do not surface this failure to the user.
  });
}
```

### Public data fetch helper (separate from `api-client.ts`)

```ts
// lib/public-card.ts — NOT api-client.ts: no Authorization header, no 401 interceptor,
// no localStorage access (must be safe to call from a Server Component).
export type PublicCardDto = {
  slug: string;
  fullName: string;
  role: string | null;
  company: string | null;
  photoUrl: string | null;
  socialLinks: { platform: string; url: string; displayOrder: number }[];
};

export async function fetchPublicCard(slug: string): Promise<PublicCardDto | null> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/public/cards/${slug}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`public card fetch failed: ${res.status}`);
  return res.json();
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|-------------------|---------------|--------|
| `revalidate`/`dynamic`/`fetchCache` as always-available route segment config | Same options, but now documented as "Previous Model" (`caching-without-cache-components.md`) and **removed entirely** once `cacheComponents` is opted into | Next.js 16.0.0 | Not a concern for this phase — `CLAUDE.md` already locks the decision to *not* enable `cacheComponents` yet, so the classic `revalidate = 60` pattern researched here remains valid and stable |
| `getStaticPaths`/`fallback: 'blocking'` (Pages Router) | `generateStaticParams` (optional) + `dynamicParams = true` default (App Router) | App Router GA (v13.0.0) | Confirms no `generateStaticParams` implementation is required for this phase — omitting it entirely is the correct, current pattern for an unbounded, dynamically-created param space like card slugs |
| Vercel Hobby functions capped at 10s | Vercel Hobby functions can run up to 60s (with Fluid Compute, default for new deployments) | Vercel changelog, dated within the last year per this session's fetch | Removes a plausible risk that a cold Render + cold Neon double-wake (worst case ~1-2s + up to 60s) could exceed the serverless function's own timeout on Vercel's side — 60s ceiling gives comfortable headroom over Render's documented ~30-60s wake |

**Deprecated/outdated:**
- Manually triggering `revalidatePath` from a proxy Route Handler to get eager on-demand revalidation: considered and explicitly rejected already in `CLAUDE.md` (§4) as unnecessary complexity for this MVP window — not reconsidered here.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | cron-job.org's free plan has no commercial-use restriction (unlike UptimeRobot's Dec 2024 ToS change) | Standard Stack / Alternatives Considered | If wrong, the recommended keep-alive service would need swapping for a paid tier or a GitHub Actions workflow before production launch — low implementation cost to change later, but should be confirmed by the user or executor before relying on it long-term |
| A2 | cron-job.org supports down-to-1-minute custom intervals on its free plan | Standard Stack / Common Pitfalls | If actual minimum interval is coarser than assumed, a 5-10 min interval (well within any plausible free-tier minimum) should still work regardless — low risk |
| A3 | GitHub Actions scheduled workflows are a viable free fallback with ~5 min minimum interval | Alternatives Considered | If GitHub tightens free-tier scheduled workflow minutes for private repos, this fallback may incur cost — recommend keeping the repo public or verifying Actions minutes budget before relying on this as primary |
| A4 | `@types/qrcode` version 1.5.6 is current and compatible with `qrcode@1.5.4` | Standard Stack | Low risk — type packages rarely break at this scope; worst case is a handful of `any`-typed QRCode calls until types are refreshed |
| A5 | No `render.yaml` / Infrastructure-as-Code exists yet for `apps/api` — this phase is the first to actually deploy the backend to Render | Recommended Project Structure / Environment Availability | If a deployment config already exists elsewhere not found by this session's search, the planner should re-verify before assuming greenfield deploy setup is needed |

## Open Questions

1. **Should the `PublicCardDto` already include Phone/Email/WhatsappNumber/PixKey now, even though Phase 2's public page doesn't render them?**
   - What we know: CARD-07 already captured explicit user consent for Pix key public visibility at save time (Phase 1); Phase 3 (`CONT-01..05`, `PAY-01..03`) will need these exact fields on the public page.
   - What's unclear: Whether widening the DTO now (single contract, unused fields ignored by Phase 2's rendering) is worth the larger public data-exposure surface before Phase 3's UI actually protects/renders it correctly (e.g., before WhatsApp deep-link and Pix copy-fallback logic exists).
   - Recommendation: Keep the DTO minimal in Phase 2 (identity + social links only, as designed above) and widen it in Phase 3 alongside the UI that consumes the new fields — smaller blast radius, and Phase 3's research/planning will have full context for exactly which fields are needed and how they're formatted (e.g. WhatsApp deep-link URL construction).

2. **Is a `render.yaml` (Render Blueint/IaC) worth authoring in this phase, or is manual dashboard setup acceptable?**
   - What we know: No deployment config exists in the repo today (verified via `find` for `render.yaml`/`vercel.json`); this phase is the first to actually stand up production infrastructure (Render + Neon + Vercel + DNS).
   - What's unclear: Given the ~2-week solo timeline constraint, whether the reproducibility benefit of IaC outweighs the time cost of learning/authoring it versus a one-time manual dashboard setup.
   - Recommendation: Manual dashboard setup is acceptable for this MVP window (matches the project's stated "validação rápida vale mais que robustez agora" constraint) — document the env var names required (`ConnectionStrings__Default`, `JWT_SECRET`, `Jwt__Issuer`, `Jwt__Audience`, `Cors__WebOrigin`, all already established in `TestAppFactory.cs`'s double-underscore ASP.NET Core convention) so they can be reproduced manually without IaC.

3. **Does ISR cache a 404 (`notFound()`) response the same way it caches a successful render, and for how long?**
   - What we know: The Next.js ISR guide states an unknown dynamic param "will be generated on-demand" and that a genuinely nonexistent post correctly returns 404; it does not explicitly document the cache TTL behavior of the negative (404) case under the classic (non-cache-components) model used here.
   - What's unclear: Whether a slug that 404s gets cached for the full `revalidate` window (60s) such that a card created moments later at that exact slug would still show 404 until the window expires, or whether 404 responses are always dynamically re-checked.
   - Recommendation: Treat as low-risk (60s worst-case staleness is well within the project's own stated tolerance for edit-to-public propagation delay, per `CLAUDE.md` §4) and verify empirically during implementation with a manual test: request a nonexistent slug, create a card at that slug, re-request within 60s and after 60s.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| Node.js / npm | `apps/web` build, `qrcode` install | ✓ | Next.js 16.3.1 / React 19.2.8 already running per `apps/web/package.json` | — |
| .NET SDK | `apps/api` build | ✓ | net10.0 per `apps/api/Api.csproj` | — |
| Docker / docker-compose | Local Postgres for dev + `Api.Tests` integration tests | ✓ (`docker-compose.yml` present at repo root) | — | — |
| Render account (production API host) | PUB-04 keep-alive target, production deploy | Not verified this session (no `render.yaml`, no evidence of existing deployment) | — | This phase is expected to create the Render deployment as part of execution — flagged in Open Question 2 |
| Neon account (production Postgres) | Production data store | Not verified this session | — | Same as above — expected to be created this phase |
| Vercel account (production frontend host) | BRAND-01 domain pointing | Not verified this session | — | Same as above |
| registro.br account / CPF-CNPJ | BRAND-01 domain registration | Not verified — external to codebase | — | User-driven action, tracked as a cross-phase pending item in `CONTEXT.md` |
| cron-job.org (or equivalent) account | PUB-04 keep-alive | Not verified — external to codebase, free signup | — | GitHub Actions scheduled workflow (see Alternatives Considered) |
| `pip`/slopcheck | Package legitimacy verification | ✗ (`pip: command not found` in this session's shell) | — | Manual `npm view` registry checks performed instead; both packages tagged `[ASSUMED]`, gated behind `checkpoint:human-verify` |

**Missing dependencies with no fallback:**
- None — every missing piece (Render/Neon/Vercel/registro.br accounts) is an expected, in-scope deliverable of this phase, not a blocker to planning it.

**Missing dependencies with fallback:**
- Keep-alive service: cron-job.org primary, GitHub Actions scheduled workflow fallback.
- Package legitimacy tooling: slopcheck primary (unavailable this session), manual `npm view` + `[ASSUMED]` tagging + human-verify checkpoint fallback (applied).

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Backend framework | xUnit 2.9.3 + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`), real Postgres via `TestAppFactory` (`docker-compose` local DB or `TEST_DATABASE_URL`) |
| Frontend framework | Vitest 4.1.10, config restricts `include` to `lib/**/*.test.ts` only (no component/e2e tests exist yet) |
| Config files | `apps/api/Api.Tests/Api.Tests.csproj`, `apps/web/vitest.config.ts` |
| Quick run command (backend) | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCard` (once the new test class exists) |
| Quick run command (frontend) | `cd apps/web && npx vitest run lib/qr.test.ts` (once a pure-function QR helper is extracted, if any logic beyond the Route Handler itself needs unit testing) |
| Full suite command (backend) | `dotnet test apps/api/Api.Tests` |
| Full suite command (frontend) | `cd apps/web && npx vitest run` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|---------------------|---------------|
| PUB-01/PUB-05 | Public GET returns safe DTO (no id/userId/pix/phone leak) for an existing slug, without auth header | integration (xUnit) | `dotnet test apps/api/Api.Tests --filter FullyQualifiedName~PublicCardTests` | ❌ Wave 0 |
| PUB-06 | Public GET returns 404 for nonexistent slug; endpoint is unreachable via the authorized `cards` group by construction | integration (xUnit) | same as above | ❌ Wave 0 |
| — (regression guard for Pitfall 1) | New public route does NOT require Bearer token | integration (xUnit) | same file, explicit no-auth-header test case | ❌ Wave 0 |
| PUB-02/PUB-05 | `revalidate = 60` is exported from `app/[slug]/page.tsx`; page fetches without an `Authorization` header | manual / code-review (no e2e framework in this repo) | — | N/A — flag as manual verification step |
| PUB-06 | `curl -I` against a nonexistent slug in production returns literal HTTP 404 (not 200 from a streamed not-found) | manual (`curl -I`) | — | N/A — flag as manual verification step, see Pitfall 2 |
| PUB-03 | Save success triggers a fire-and-forget fetch to the public URL | manual (browser devtools Network tab) or a Vitest test mocking `fetch` in `card-form.tsx`'s `onSubmit` | `cd apps/web && npx vitest run components/card-form/card-form.test.tsx` (new) | ❌ Wave 0 |
| PUB-04 | External cron successfully pings `/health` on schedule | manual (check cron-job.org execution history + Render logs after 24h) | — | N/A — inherently external, not automatable in this repo's test suite |
| SHARE-01/SHARE-02 | QR route returns valid SVG (`image/svg+xml`) inline and PNG (`image/png`) with `Content-Disposition` only when `?download=1` | integration/unit — can be tested with a plain `fetch` against the running Next.js dev server, or a small Vitest test hitting the route handler function directly | `cd apps/web && npx vitest run app/\[slug\]/qr/route.test.ts` (new) | ❌ Wave 0 |
| BRAND-01 | Domain resolves and serves the Vercel deployment over HTTPS | manual (`curl -I https://vizzo.com.br` after DNS propagation) | — | N/A — external infra, not automatable |

### Sampling Rate
- **Per task commit:** targeted `dotnet test apps/api/Api.Tests --filter ...` / `npx vitest run <file>` for whatever was just touched.
- **Per wave merge:** `dotnet test apps/api/Api.Tests` (full) + `cd apps/web && npx vitest run` (full).
- **Phase gate:** Full suite green before `/gsd:verify-work`, plus the manual verification steps listed above (no e2e framework exists in this repo to automate the ISR/404-status/domain checks).

### Wave 0 Gaps
- [ ] `apps/api/Api.Tests/PublicCardTests.cs` — covers PUB-01, PUB-05, PUB-06, and the no-auth-required regression guard
- [ ] `apps/web/app/[slug]/qr/route.test.ts` (or equivalent) — covers SHARE-01/SHARE-02 format + Content-Disposition gating
- [ ] `apps/web/components/card-form/card-form.test.tsx` — covers PUB-03's fire-and-forget pre-warm call (mock `fetch`, assert it was called with the right URL after save success, and assert a rejected pre-warm promise does not surface an error toast)
- [ ] No new test framework/config install needed — both xUnit and Vitest infra already exist and are wired into the two apps

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|---------------------|
| V2 Authentication | No | This phase's new endpoint is intentionally unauthenticated by design (PUB-01) |
| V3 Session Management | No | No session state involved in the public read path |
| V4 Access Control | Yes | The new endpoint must be reachable with **zero** credentials (verify it is NOT nested under the `cards` group's `.RequireAuthorization()` — Pitfall 1) while simultaneously never exposing `Id`/`UserId`/write capability — this is an intentional, narrow public-read exception to the existing all-authenticated posture, not a general access-control gap |
| V5 Input Validation | Yes | The `slug` route parameter must be normalized via the existing `SlugService.Normalize` before querying — untrusted input, but EF Core's parameterized queries already prevent injection; no additional validation library needed beyond what's reused from Phase 1 |
| V6 Cryptography | No | No new cryptographic operations in this phase |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|------------------------|
| Slug enumeration to discover unlisted cards | Information Disclosure | Slugs are already chosen and known-shareable by the owner (the entire point of the product); this is an accepted product-level exposure, not a defect — no additional mitigation needed beyond what already exists (no sequential/numeric IDs are exposed, only the owner-chosen slug string) |
| IDOR via guessable numeric/GUID ID instead of slug | Information Disclosure | `PublicCardDto` deliberately omits `Id`/`UserId` (see Pattern 1's code example) — the public lookup path never accepts or returns the database primary key |
| Unauthenticated endpoint accidentally allowing writes | Tampering | The new `PublicCardEndpoints.GetBySlugHandler` is GET-only, read-only (`AsNoTracking()`), and lives in its own file/route separate from any write handler — no shared code path with `CreateCardHandler`/`UpdateCardHandler` |
| Public endpoint used as a DB-hammering vector (no rate limit) | Denial of Service | The existing rate limiter in `Program.cs` is currently scoped to the `"login"` policy only (`/auth/login`) — the new public endpoint has **no rate limiting** in this phase's scope. Given the free-tier Render/Neon setup, this is a real (if MVP-acceptable) gap; flagged here rather than silently left out. Recommend the planner note this as an explicit scope decision (accept for MVP vs. add a lightweight IP-based limiter matching the `"login"` pattern already in the codebase) rather than an oversight |

## Sources

### Primary (HIGH confidence)
- `apps/api/Program.cs`, `apps/api/Endpoints/CardEndpoints.cs`, `apps/api/Contracts/CardDtos.cs`, `apps/api/Services/SlugService.cs`, `apps/api/Api.Tests/TestAppFactory.cs`, `apps/api/Api.Tests/SlugTests.cs` — read in full this session
- `apps/web/lib/api-client.ts`, `apps/web/lib/initials.ts`, `apps/web/components/card-form/card-form.tsx`, `apps/web/app/(dashboard)/dashboard/cards/[id]/edit/page.tsx`, `apps/web/vitest.config.ts`, `apps/web/package.json` — read in full this session
- `apps/web/node_modules/next/dist/docs/01-app/02-guides/incremental-static-regeneration.md` (Next.js 16.3.1 bundled docs, per `apps/web/AGENTS.md`'s instruction to check bundled docs over training data)
- `apps/web/node_modules/next/dist/docs/01-app/02-guides/caching-without-cache-components.md`
- `apps/web/node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/not-found.md`
- `apps/web/node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/02-route-segment-config/dynamicParams.md` and `index.md`
- `apps/web/node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/02-route-segment-config/maxDuration.md`
- `.planning/phases/02-cart-o-p-blico-no-ar/02-CONTEXT.md`, `.planning/REQUIREMENTS.md`, `.planning/ROADMAP.md`, `.planning/STATE.md`, `.planning/phases/01-conta-e-cart-o/01-VERIFICATION.md` — read in full this session
- `CLAUDE.md` (root) — Technology Stack section, already-locked QR/vCard/OG/ISR/Blob/Pix/.NET decisions

### Secondary (MEDIUM confidence)
- [Vercel — Adding & Configuring a Custom Domain](https://vercel.com/docs/domains/working-with-domains/add-a-domain) — apex domain → A record, subdomain → CNAME, verified via WebFetch this session
- [Vercel — Vercel Functions for Hobby can now run up to 60 seconds](https://vercel.com/changelog/vercel-functions-for-hobby-can-now-run-up-to-60-seconds) — WebFetch this session
- Vercel Hobby cron once-per-day limit — WebSearch, cross-referenced across multiple third-party sources (crontap.com, runhooks.app) describing consistent, specific deploy-time failure behavior — no single official Vercel doc URL was fetched directly this session, flagged MEDIUM not HIGH
- Render free tier 15-min spin-down / ~30-60s cold start — already cited HIGH confidence in root `CLAUDE.md`'s own sources (Render Docs — Deploy for Free), not re-verified independently this session but treated as trustworthy given it's the project's own prior research
- registro.br `.com.br` registration steps (CPF/CNPJ requirement, 1-10 year terms) — WebSearch, aggregated across several Brazilian hosting-industry blog sources, no single official registro.br page fetched directly this session

### Tertiary (LOW confidence)
- cron-job.org free-plan minimum interval (1 minute) and lack of commercial-use ToS restriction — WebSearch summary + one WebFetch of cron-job.org's ToS page (which did not explicitly confirm the interval figure); flagged as Assumption A1/A2, needs user/executor confirmation before hard-committing
- UptimeRobot free-plan commercial-use ToS restriction (Dec 2024 change) — WebSearch only, multiple 2026-dated blog sources agree but no primary UptimeRobot ToS page was fetched this session

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — `qrcode`/`@types/qrcode` versions independently confirmed via `npm view` this session, matches already-locked `CLAUDE.md` decision
- Architecture (public endpoint placement, ISR pattern, CORS non-issue): HIGH — verified directly against the actual `Program.cs` source and Next.js's own bundled documentation (not training data)
- Pitfalls (streaming/404 status, route-group auth inheritance): HIGH — both derived from reading actual framework docs/source this session, not inferred
- Keep-alive service choice (cron-job.org vs. UptimeRobot vs. GitHub Actions): MEDIUM — WebSearch-verified across multiple sources but no single primary-source confirmation of exact free-tier limits; safe either way given the generous safety margin recommended (5-10 min vs. 15 min threshold)
- Domain registration mechanics (registro.br + Vercel DNS): MEDIUM — Vercel side is HIGH (official docs fetched), registro.br side is MEDIUM (aggregated Brazilian blog sources, not the official registro.br site directly)

**Research date:** 2026-08-14
**Valid until:** 2026-09-13 (30 days — Next.js/Vercel/Render specifics are moderately fast-moving; re-verify keep-alive service ToS and Vercel cron/function limits if planning is delayed past this window)
