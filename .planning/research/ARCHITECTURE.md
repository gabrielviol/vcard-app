# Architecture Research

**Domain:** Cartão de visita digital (Next.js + .NET API separados, Postgres)
**Researched:** 2026-08-13
**Confidence:** MEDIUM-HIGH (estrutura macro já travada pelas specs 01/02; as decisões novas abaixo combinam documentação oficial verificada com práticas de mercado — níveis de confiança marcados por item)

Este documento não questiona a decisão já tomada (monorepo `/apps/web` + `/apps/api`, Postgres, Vercel + Render + Neon/Supabase). O foco é resolver as questões arquiteturais que a spec ainda deixa em aberto: como o caminho público sobrevive ao cold start do Render, onde cada responsabilidade mora, como registrar views sem poluir o dado, como as rotas convivem com o slug na raiz, e em que ordem construir.

## Visão Geral do Sistema

```
┌───────────────────────────── Navegador (celular) ─────────────────────────────┐
│  QR scan / link WhatsApp / Instagram bio                                      │
│       │                                    │                                  │
│       ▼                                    ▼                                  │
│  GET /{slug}  (HTML, cacheado)      Client Component (pós-hydration)          │
│                                       └─ sendBeacon POST /cards/{slug}/views  │
└───────┬─────────────────────────────────────────┬─────────────────────────────┘
        │                                          │
        ▼                                          ▼
┌───────────────────── apps/web (Vercel) ─────────────────────┐        (CORS aberto
│ /[slug]/page.tsx        → RSC, fetch com next.revalidate     │         para o domínio
│ /[slug]/vcard.vcf       → route handler, reusa fetch cacheado│         do frontend)
│ /[slug]/opengraph-image → next/og ImageResponse              │                │
│ /[slug]/qrcode          → route handler, gera PNG/SVG        │                │
│ (dashboard)/login, /dashboard, /dashboard/cards/[id]/edit    │                │
│   → Server Actions/fetch com Authorization: Bearer            │                │
│ upload de foto → token assinado do Vercel Blob                │                │
└───────────────────────────────┬───────────────────────────────┘                │
                                 │ fetch (server-side, cacheado por revalidate)   │
                                 ▼                                               │
┌───────────────────── apps/api (Render, .NET minimal API) ─────────────────────┴─┐
│ Endpoints (públicos): GET /cards/{slug}, POST /cards/{slug}/views               │
│ Endpoints (protegidos, JWT): /auth/*, CRUD Card/SocialLink, upload-token         │
│ Services: validação de slug reservado, hashing, regras de negócio               │
│ Data (EF Core): único componente que escreve no Postgres                        │
└───────────────────────────────┬──────────────────────────────────────────────────┘
                                 ▼
                    Postgres (Neon/Supabase, free tier)
```

## Responsabilidade por Componente

| Componente | Responsabilidade | Observação |
|-----------|----------------|------------------------|
| `apps/web` (Next.js, Vercel) | Renderização pública (SSR+ISR), dashboard autenticado, geração de QR/.vcf/OG image, upload de imagem (via Vercel Blob), disparo do beacon de view | Nunca fala com Postgres diretamente |
| `apps/api` (.NET, Render) | Autenticação, regras de negócio, validação de slug, único ponto de escrita no Postgres, endpoint de contagem de views | Fonte única da verdade do schema (EF Core migrations) |
| Postgres (Neon/Supabase) | Persistência das 4 tabelas | Um único writer (`apps/api`) evita duas ORMs (EF Core + algo em Node) mantendo o mesmo schema |
| Vercel Blob | Armazenamento de foto de perfil | Evita passar bytes de imagem pelo Render free tier (limite de payload/timeout, filesystem efêmero) |

---

## Questão 1 — Cold start do Render no caminho público (a decisão mais arriscada do projeto)

**Recomendação concreta: ISR (Incremental Static Regeneration) com `revalidate` longo + revalidação sob demanda no save + "self-warming" no editor + ping externo de keep-warm. Não ler Postgres direto do Next.js.**

### Por que ISR resolve o problema, e SSR puro não

O medo é: alguém escaneia o QR, o Render está "dormindo" (free tier hiberna após ~15 min de inatividade, cold start leva de 30 a 60s conforme relatos de 2026 — MEDIUM confidence, múltiplas fontes de terceiros, não há SLA oficial da Render para isso) e a pessoa vê uma tela em branco por meio minuto. Se `/[slug]/page.tsx` fizer um `fetch` simples (SSR puro, sem cache) em toda requisição, todo scan de QR paga esse custo sempre que o Render estiver frio.

Com `export const revalidate = 3600` (ou tempo similar) na página do App Router, o Next.js usa stale-while-revalidate: a primeira requisição gera e armazena o HTML; todas as requisições dentro da janela de revalidação recebem o HTML cacheado instantaneamente, direto da edge da Vercel, **sem tocar no Render**. Quando a janela expira, a *próxima* requisição ainda recebe o HTML cacheado (stale) imediatamente, e o Next.js dispara a regeneração em segundo plano — só essa regeneração, que não bloqueia ninguém, é que eventualmente acorda o Render. Isso desacopla completamente a latência percebida pelo visitante do estado de cold start da API. (HIGH confidence — mecanismo documentado oficialmente pela Next.js: [docs.nextjs.org — dynamic routes / caching](https://nextjs.org/docs/app/api-reference/file-conventions/dynamic-routes)).

### O ponto cego: a primeira visita a um slug novo

ISR clássico com `generateStaticParams` vazio ainda faz SSR síncrono na primeira requisição de cada slug (o HTML só é armazenado *depois* que alguém pede). Isso significa que o pior caso real não é "revisita a um cartão conhecido" — é "QR recém-impresso, primeiro scan de todos, Render frio". Mitigações concretas, em ordem de impacto:

1. **Pré-aquecer no momento do save, não no momento do scan.** Depois que o dono salva o cartão no dashboard, a Server Action/handler do Next.js dispara `revalidatePath('/{slug}')` e, em seguida, um `fetch` próprio para a própria URL pública (`https://seu-dominio/{slug}`). Isso paga o custo do cold start — se houver — no momento em que o *dono* está editando (ele já espera alguma demora ao salvar), nunca no momento em que um estranho escaneia o QR impresso.
2. **`generateStaticParams` incluindo o(s) slug(s) conhecidos no build.** Como a v1 é 1 cartão por usuário e o próprio dev é o primeiro usuário real, dá para popular `generateStaticParams` com os slugs existentes no momento do deploy, deixando a página pré-renderizada antes de qualquer scan.
3. **Keep-warm externo.** Um ping simples a cada 10–14 minutos no health check do Render (GitHub Actions com `schedule`, ou serviço gratuito tipo cron-job.org/UptimeRobot) reduz drasticamente a chance de o Render estar frio quando a regeneração em segundo plano (item do ISR) precisar rodar. Isso é um paliativo de infraestrutura, não uma solução arquitetural — mas é barato e reduz o pior caso residual do item 2.

Essas três táticas combinadas cobrem os dois cenários: cartão já existe e é revisitado (resolvido só pelo ISR) e cartão/slug novo (resolvido por pré-aquecimento no save + keep-warm).

### Por que rejeitar "ler Postgres direto do Next.js" para o caminho público

Foi cogitado como opção. Rejeito para este projeto, por três razões concretas, não apenas "por princípio":

- **Duplicaria o schema em dois lugares.** O EF Core já é a fonte da verdade das migrations. Um client Postgres separado no Next.js (`postgres.js`/Prisma introspectado) cria uma segunda definição do mesmo schema que pode divergir silenciosamente após a próxima migration.
- **Contenção de conexões no free tier.** Funções serverless da Vercel abrem conexões sob demanda; somado ao pool do EF Core no Render, isso aproxima o limite de conexões do plano free do Neon/Supabase mais rápido do que um único writer.
- **Não resolve o problema real.** O gargalo é o cold start do *processo* Render, não a latência do Postgres em si (Neon/Supabase respondem rápido). Ler o banco direto elimina uma etapa de rede, mas o ISR já elimina a etapa inteira (incluindo o banco) para a grande maioria das requisições. É complexidade adicional sem ganho proporcional — contraria a restrição de "sem abstração especulativa" do projeto.

Se, na prática, o ISR + pré-aquecimento não bastar (por exemplo, se o volume de slugs novos crescer rápido no plano pago futuro), a leitura direta do Postgres para o caminho público fica registrada aqui como via de escape documentada — não como algo a construir agora.

---

## Questão 2 — Onde cada responsabilidade mora

| Responsabilidade | Onde mora | Por quê |
|---|---|---|
| Geração de QR code | `apps/web` | O QR só codifica a URL pública (string); não depende de dado nenhum do banco. Biblioteca JS (`qrcode` no npm) gera PNG/SVG num route handler dedicado para download, e a mesma lib renderiza a versão on-screen. Zero motivo para envolver o .NET. |
| Geração de `.vcf` | `apps/web` (já definido na spec 01: `/[slug]/vcard.vcf/route.ts`) | Reaproveita o mesmo `fetch` cacheado que a página pública já faz para os dados do Card/SocialLink — sem chamada extra à API. Manter no web evita duplicar a leitura no `apps/api`. |
| OG image (preview de link) | `apps/web`, usando `next/og` (`opengraph-image.tsx` no App Router, roda em Edge Runtime) | É literalmente para isso que a convenção existe: gerar imagem a partir de JSX/dados dinâmicos. Fazer isso em .NET exigiria ImageSharp + layout manual — desproporcional para o prazo de 2 semanas. |
| Escrita de `CardView` (view tracking) | `apps/api` (endpoint público `POST /cards/{slug}/views`), **disparado a partir de um Client Component no web** | Ver Questão 3 — o *disparo* mora no web (client-side, pós-hydration), mas a *escrita* no Postgres continua exclusiva do `apps/api`, preservando o writer único. |
| Upload de imagem de perfil | `apps/web` orquestra (token assinado do Vercel Blob), `apps/api` só recebe a URL final | O Render free tier tem filesystem efêmero e limites de payload/timeout mais apertados que o necessário para upload de imagem. O padrão de client upload do Vercel Blob (`@vercel/blob/client`, `handleUpload`) deixa o navegador enviar o arquivo direto pro storage; o `apps/api` só grava `photo_url` como qualquer outro campo de texto no `PATCH /cards/{id}` (HIGH confidence — [Vercel Blob client upload docs](https://vercel.com/docs/vercel-blob/client-upload)). |

**Regra geral usada para decidir:** se a responsabilidade não precisa tocar no Postgres nem em regra de negócio protegida, ela fica no `apps/web` (mais rápido de iterar, roda perto do usuário). Se precisa validar, autorizar ou persistir, vai para o `apps/api`. Isso mantém o `apps/api` fino (Endpoints/Services/Data, como a spec pede) em vez de virar um "faz tudo".

---

## Questão 3 — View tracking sem estragar performance nem virar dado de mentira

### O problema estrutural: ISR e "toda visita gera uma view" são incompatíveis por natureza

Se a contagem de view for feita *dentro* do Server Component que renderiza `/[slug]/page.tsx`, ela só executa quando a página é (re)gerada — ou seja, uma vez por janela de `revalidate`, não uma vez por visitante real. Isso subestimaria views brutalmente (e de forma inconsistente, dependendo de quando a janela expira). A contagem de view **precisa viver fora do caminho cacheado**.

**Recomendação:** um Client Component pequeno na página pública que, no `useEffect` (ou seja, só depois que o HTML já chegou e foi hidratado no navegador), dispara `navigator.sendBeacon` (ou `fetch` com `keepalive: true` como fallback) direto para `POST {NEXT_PUBLIC_API_URL}/cards/{slug}/views` no `apps/api`. Como o CORS já está liberado para o domínio do frontend (spec 01) e a rota é pública (sem JWT), não precisa de proxy no Next.js — chamar direto simplifica.

Esse desenho já resolve, de graça, o maior ofensor do problema de bot/prefetch:

### Bot/prefetch — por que a abordagem client-side-only já filtra a maior parte, e o que falta

- **Crawlers de preview (WhatsApp, `facebookexternalhit`, Facebot, etc.) majoritariamente não executam JavaScript** — eles pegam o HTML/meta tags para montar o preview e não disparam `useEffect`. Um beacon que só roda depois da hidratação no navegador real, portanto, **não é acionado por esses crawlers só por eles buscarem a página** (MEDIUM confidence — comportamento documentado por múltiplas fontes de terceiros sobre como o WhatsApp/Meta crawler funciona; não há uma fonte oficial única da Meta afirmando "nunca executa JS", mas é o consenso técnico corrente).
- **Prefetch do `next/link`** (`Next-Router-Prefetch` header / `purpose: prefetch`) só importa se algo dentro do próprio app linkar para `/{slug}` via `<Link>` — no fluxo real deste produto (QR, link direto no WhatsApp, bio do Instagram), quase não há navegação interna via `<Link>` até o cartão público, então esse vetor é secundário aqui. Mesmo assim, como o beacon dispara só após hydration real da página de destino (não da página de origem que fez o prefetch), o prefetch em si nunca dispara o beacon — ele só pré-carrega o HTML, não executa o `useEffect` da página até haver navegação de fato. Isso já neutraliza esse vetor sem esforço extra.
- **Crawlers que executam JS de verdade** (alguns validadores de card do X/Twitter e do LinkedIn rodam headless Chrome em certos casos) passariam pelo filtro acima. Para esses, adicionar um filtro simples de User-Agent no endpoint `POST /cards/{slug}/views` do `apps/api` (lista de padrões conhecidos: `facebookexternalhit`, `WhatsApp`, `Slackbot`, `TelegramBot`, `Twitterbot`, `LinkedInBot`, `Googlebot`, `bingbot` — ou a lib `isbot` do npm portada/equivalente em C#) cobre o restante com esforço pequeno.
- **Deduplicação de revisitas do mesmo visitante:** gravar 1 view por card por visitante por dia é suficiente para o v1 (analytics aqui é só contagem, não é dado de auditoria). Implementação simples: cookie/localStorage local (`viewed:{cardId}:{yyyy-mm-dd}`) checado no próprio Client Component antes de disparar o beacon — evita inflar o contador a cada refresh do dono checando o próprio cartão, sem precisar de infraestrutura de rate limiting no servidor.

**O que fica deliberadamente fora de escopo:** rate limiting robusto por IP, detecção de bot com Redis/serviço externo, auditoria de fraude. O objetivo aqui é eliminar as duas fontes de ruído *conhecidas e previsíveis* (crawlers de preview e prefetch), não construir infraestrutura antifraude — coerente com "analytics é só contagem de views" no escopo do v1. Se o contador de views virar algo mais sério (plano pago com detalhamento), essa é a hora de revisitar.

---

## Questão 4 — Slug na raiz e palavras reservadas

O App Router resolve rotas em uma ordem de precedência clara: **segmento estático literal vence segmento dinâmico, que vence catch-all**, dentro do mesmo diretório (MEDIUM-HIGH confidence — comportamento confirmado pela documentação oficial de [Dynamic Segments](https://nextjs.org/docs/app/api-reference/file-conventions/dynamic-routes) combinada com múltiplas fontes da comunidade que descrevem essa ordem explicitamente para o App Router; a estrutura já planejada na spec 01 — `/[slug]`, `(dashboard)/login`, `(dashboard)/dashboard` — depende disso funcionar exatamente assim). **Recomendação prática: validar esse comportamento localmente com um teste de 5 minutos antes de confiar nele** (criar `/login/page.tsx` e `/[slug]/page.tsx` lado a lado e acessar `/login`), já que é uma suposição estrutural com custo de correção baixo agora e alto depois.

Como `(dashboard)` é um route group (parênteses não entram na URL), `/login` e `/dashboard` já são segmentos estáticos de primeiro nível convivendo com `/[slug]` — exatamente a estrutura da spec 01, sem mudança necessária.

**Palavras reservadas:** a lista de slugs proibidos deve morar como *validação no `apps/api`*, no momento de criar/editar o Card (é o único lugar que efetivamente grava o slug), não espalhada em middleware do Next.js. Fonte única da lista: todo nome de pasta estática de primeiro nível em `apps/web/app/` (`login`, `dashboard`, `api` se vier a existir, mais qualquer rota de marketing futura) mais reservas de segurança (`admin`, `_next`, `favicon.ico`, `robots.txt`, `sitemap.xml`, `www`). Manter a lista no backend evita que ela fique desatualizada quando o frontend ganhar novas rotas estáticas sem o dev lembrar de atualizar dois lugares — só é preciso lembrar de atualizar *um* lugar (a lista no `apps/api`) toda vez que uma pasta estática nova for criada no `apps/web`.

---

## Questão 5 — Fronteira de autenticação

Duas superfícies bem separadas, já implícitas na spec 02:

- **Superfície pública (sem JWT):** `GET /cards/{slug}`, `GET /cards/{slug}/vcard` (dado servido pelo web, não uma rota da API em si), `POST /cards/{slug}/views`. Tudo que um visitante anônimo do cartão toca.
- **Superfície protegida (JWT Bearer):** `/auth/*`, CRUD de Card/SocialLink do dono, emissão de token de upload do Vercel Blob. Tudo que o dashboard toca.

Como a autenticação é 100% Bearer token (sem cookie), o CORS no `apps/api` pode ficar simples: `AllowCredentials` não é necessário (não há cookie cross-domain), então a política pode restringir por origem (domínio da Vercel) sem lidar com `SameSite`/`Secure` de cookie entre domínios diferentes — exatamente a razão que a spec 02 já registra para essa escolha. Armazenamento do token no frontend (memória via contexto React, com fallback `localStorage`) já está definido na spec 02 e não muda nada aqui.

---

## Fluxo de Dados

### Caminho público de leitura (QR scan → tela no celular)

```
QR scan / link
    ↓
GET /{slug}  (Vercel edge)
    ↓
Cache HIT (dentro da janela de revalidate)?
    ├── SIM → serve HTML cacheado imediatamente (Render nem é tocado)
    └── NÃO → serve HTML stale imediatamente (se existir) e dispara, em paralelo,
              regeneração em segundo plano:
                  apps/web (RSC fetch) → apps/api GET /cards/{slug} → Postgres
                  (aqui é onde o cold start do Render pode acontecer —
                   mas quem está esperando é o processo de regeneração, não o visitante)
    ↓
Navegador recebe HTML + meta OG tags
    ↓
Hidratação → Client Component dispara sendBeacon POST /cards/{slug}/views
    ↓ (assíncrono, keepalive, não bloqueia nada visualmente)
apps/api valida (slug existe, UA não é crawler conhecido, dedupe por dia) → grava CardView
```

### Caminho autenticado de escrita (dono edita o cartão)

```
Login (POST /auth/login) → JWT armazenado no cliente
    ↓
Dashboard: editar Card/SocialLink → fetch com Authorization: Bearer {token}
    ↓
apps/api valida JWT → Services aplicam regra de negócio (ex: slug reservado) → EF Core grava no Postgres
    ↓
Server Action do Next.js dispara revalidatePath('/{slug}')
    ↓
Server Action também faz um fetch de "pré-aquecimento" para a própria URL pública
    (paga qualquer cold start do Render agora, no momento do save — não no scan)
    ↓
Cache público atualizado e já quente para o próximo visitante
```

### Upload de imagem

```
Dashboard pede token de upload (Next.js valida JWT antes de emitir)
    ↓
Navegador envia o arquivo direto pro Vercel Blob (bytes nunca passam pelo apps/api)
    ↓
Vercel Blob retorna URL pública do arquivo
    ↓
Dashboard chama PATCH /cards/{id} { photo_url } no apps/api (mesma rota de sempre)
```

---

## Ordem de Construção — caminho crítico até "meu cartão está no ar e compartilhável por QR"

Janela de ~2 semanas, solo. Abaixo, o que é **caminho crítico** (bloqueia o objetivo mínimo) versus o que **pode esperar**.

### Caminho crítico

1. **Setup (spec 01) já descrito** — monorepo, migrations, health check, Docker Compose local. Pré-requisito de tudo.
2. **Auth mínimo (spec 02)** — `register`/`login`/`me`. Sem isso não existe dono de cartão. Não precisa de UI bonita, só funcional.
3. **CRUD de Card + SocialLink no `apps/api`** — incluindo `GET /cards/{slug}` público e a validação de slug reservado (barata de adicionar já nesse momento, evita retrabalho depois).
4. **Dashboard mínimo** para criar/editar o próprio cartão (formulário simples, sem polimento visual).
5. **Página pública `/[slug]`** no Next.js, mobile-first, consumindo `GET /cards/{slug}` — este é o ponto em que "o cartão existe" passa a ser verdade.
6. **ISR + revalidação sob demanda + pré-aquecimento no save** (Questão 1) — não é opcional nem "depois". Um cartão que trava 30-60s na cara de quem escaneou o QR não atende à barra de "no ar e compartilhável" definida no PROJECT.md. Construir junto com o item 5, não depois.
7. **Geração de QR code** (tela + download) — está explicitamente marcado como corte prioritário no PROJECT.md ("QR code é o corte prioritário... fica acima até do botão de upgrade"). É o critério de "pronto" da janela de 2 semanas.
8. **Keep-warm externo** (ping a cada ~10-14 min) — tarefa pequena e independente, fazer em paralelo ao item 6.

### Pode esperar (não bloqueia "cartão no ar e compartilhável")

- **`.vcf` download** — o cartão funciona sem esse botão; adicionar depois que o caminho crítico estiver de pé.
- **OG image dinâmica** — melhora a primeira impressão no WhatsApp, mas o QR nem passa pelas OG tags (só compartilhamento de link passa). Pode nascer com uma OG image estática genérica e evoluir depois.
- **View tracking (`CardView`) e o beacon anti-bot** — é o gancho do plano pago, não faz o cartão existir. Construir depois que o cartão estiver publicado e recebendo tráfego de verdade.
- **Upload de foto via Vercel Blob** — o cartão funciona com um placeholder/avatar padrão; a integração de storage é isolável e pode entrar depois.
- **Rodapé de marca (`is_branded`) e botão de intenção de upgrade** — dependem do nome/domínio do produto (pendência bloqueante já registrada no PROJECT.md) e não afetam se o cartão está no ar.

**Nota sobre dependências dentro do caminho crítico:** os itens 5, 6 e 7 são efetivamente um só bloco de trabalho — não faz sentido dar como "pronta" a página pública sem a mitigação de cold start, porque a definição de "pronto" deste produto inclui explicitamente a experiência de quem escaneia o QR em pé, na hora, no celular.

---

## Anti-Padrões a Evitar

### Anti-Padrão 1: Contar view dentro do render cacheado (RSC)

**O que as pessoas fazem:** gravar `CardView` dentro do Server Component que renderiza `/[slug]/page.tsx`, achando que "toda requisição chama esse código".
**Por que é errado:** com ISR, esse código só roda na (re)geração do HTML — uma vez por janela de `revalidate`, não uma vez por visitante. O contador fica artificialmente baixo e inconsistente.
**Fazer assim em vez disso:** disparar a escrita a partir de um Client Component, pós-hydration, via `sendBeacon`/`fetch keepalive`, fora do caminho cacheado.

### Anti-Padrão 2: Ler o Postgres direto do Next.js para "resolver" o cold start

**O que as pessoas fazem:** dado o medo do cold start, adicionar um client Postgres no `apps/web` só para o caminho público, pulando o `apps/api` inteiramente nesse caso.
**Por que é errado:** duplica a definição do schema fora do EF Core, aumenta a pressão de conexões no free tier do Neon/Supabase, e resolve um sintoma (uma chamada de rede a mais) sem resolver a causa real (processo do Render dormindo) — que o ISR já resolve sozinho para a esmagadora maioria das requisições.
**Fazer assim em vez disso:** ISR + pré-aquecimento no save + keep-warm externo (Questão 1).

### Anti-Padrão 3: Construir infraestrutura antifraude de analytics no v1

**O que as pessoas fazem:** ao perceber o risco de bot/prefetch, sair construindo rate limiting distribuído, fingerprinting, Redis para deduplicação.
**Por que é errado:** o produto define analytics do v1 como "só contagem de views" — over-engineering aqui rouba tempo da janela de 2 semanas sem mover a métrica que importa (cartão no ar, QR funcionando).
**Fazer assim em vez disso:** as três táticas baratas da Questão 3 (client-side-only, filtro de User-Agent conhecido, dedupe por cookie/dia) cobrem os vetores de ruído previsíveis; o resto é overkill até o produto ter tração.

---

## Considerações de Escala

Fora de propósito para o v1 (1 cartão por usuário, poucas dezenas de usuários possíveis na validação inicial), mas registrado para quando o plano pago existir:

| Escala | Ajuste |
|-------|--------------------------|
| Validação inicial (dezenas de usuários) | Arquitetura acima é suficiente sem ajuste. Gargalo mais provável: cold start do Render em slugs muito novos — mitigado pelas táticas da Questão 1. |
| Centenas de usuários / múltiplos cartões (plano pago) | Considerar plano pago do Render (sem cold start) antes de considerar reescrever qualquer coisa — é o ajuste de infraestrutura mais barato disponível. Revisitar a granularidade do `revalidate` por card (cartões que mudam bastante podem precisar de janela menor). |
| Analytics detalhado (referrer, série temporal) | Aí sim justifica-se rate limiting mais sério e possivelmente mover a escrita de `CardView` para uma fila (mesmo que simples) em vez de escrita síncrona no endpoint público. |

---

## Sources

- [Next.js — Dynamic Segments (App Router, oficial)](https://nextjs.org/docs/app/api-reference/file-conventions/dynamic-routes) — HIGH confidence, mecanismo de `generateStaticParams`, caching e comportamento de rota
- [Next.js — Prefetching guide (oficial)](https://nextjs.org/docs/app/guides/prefetching) — MEDIUM-HIGH confidence, comportamento de prefetch do `next/link`
- [Vercel Blob — Client Uploads (oficial)](https://vercel.com/docs/vercel-blob/client-upload) — HIGH confidence
- [Vercel Blob — Server Uploads (oficial)](https://vercel.com/docs/vercel-blob/server-upload) — HIGH confidence
- Discussões da comunidade sobre precedência estática vs dinâmica no roteamento por arquivo do Next.js (ex.: [vercel/next.js discussion #37171](https://github.com/vercel/next.js/discussions/37171)) — MEDIUM confidence, corroborado por múltiplas fontes de terceiros, recomenda-se validação local
- Fontes agregadas de terceiros sobre tempo de spin-down/cold start do Render free tier em 2026 (ex.: agentdeals.dev, expresstech.io) — MEDIUM confidence, não há SLA oficial documentado pela Render para esse comportamento
- Documentação e discussões sobre comportamento de crawlers de preview (`facebookexternalhit`, WhatsApp) não executando JavaScript — MEDIUM confidence, consenso técnico de múltiplas fontes de terceiros, sem declaração oficial única da Meta
- `docs/specs/01-setup.md` e `docs/specs/02-autentication.md` deste repositório — decisões já travadas, tratadas como fato

---
*Architecture research for: cartão de visita digital (Next.js + .NET API)*
*Researched: 2026-08-13*
