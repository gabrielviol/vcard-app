<!-- GSD:project-start source:PROJECT.md -->
## Project

**vCard App (nome a definir)**

Um cartão de visita digital (link único + QR code) feito para o mercado brasileiro, onde WhatsApp e Pix são campos de primeira classe em vez de "mais um link social". É para freelancers, autônomos e pequenos prestadores de serviço no Brasil que hoje usam cartão de papel ou uma bio de Instagram improvisada para passar contato e receber por um serviço.

Não é uma cópia do Linktree nem dos concorrentes internacionais (Popl, HiHello, Blinq) — esses assumem um fluxo de networking corporativo americano (e-mail como canal principal, cartão de crédito, integração com CRM) que não reflete como a transação de fato acontece no Brasil.

**Core Value:** Alguém recebe o cartão (por QR ou link) e consegue **te chamar ou te pagar em um toque** — sem sair da página, sem digitar nada, sem etapa intermediária.

### Constraints

- **Timeline**: ~2 semanas, solo, durante as férias — algo no ar e compartilhável até o fim da janela. Se algo tiver que cair, o QR fica; o resto negocia.
- **Tech stack**: Next.js (App Router, TypeScript, Tailwind) + .NET minimal API + Postgres — já decidido nas specs. Escolhido por ser o stack que o dev já domina, o que maximiza velocidade na janela curta.
- **Arquitetura**: validação rápida vale mais que robustez agora. Camadas simples (Endpoints/Services/Data), sem abstração especulativa.
- **Custo**: free tier em tudo (Vercel, Render com cold start aceito, Neon/Supabase) — o produto não pode custar dinheiro antes de gerar.
- **Mobile-first**: quem recebe o cartão abre no celular, quase sempre vindo de câmera de QR ou de app de mensagem. Desktop é secundário.
- **Pendência bloqueante**: o produto não tem nome nem domínio. Isso trava a marca do rodapé (`is_branded`) e a URL pública — precisa ser resolvido antes do cartão ir pro ar.
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

## Recommended Stack
### Novas dependências por área
| Área | Tecnologia | Versão | Onde vive | Confiança |
|------|-----------|--------|-----------|-----------|
| QR code | `qrcode` (npm, alias node-qrcode) | 1.5.4 | `apps/web` | HIGH |
| vCard (.vcf) | Nenhuma — string template própria | — | `apps/web` | HIGH |
| OG image | `next/og` (`ImageResponse`, embutido no Next.js) | Next.js 16.3.0 | `apps/web` | HIGH |
| Renderização da página pública | Next.js route segment config (`revalidate`) | Next.js 16.3.0 | `apps/web` | HIGH |
| Upload/host de foto | `@vercel/blob` | latest (SDK 1.x) | `apps/web` | HIGH |
| Validação de chave Pix (cpf/cnpj) | `cpf-cnpj-validator` (npm) | 2.1.2 | `apps/web` (+ regex espelho em `apps/api`) | MEDIUM-HIGH |
| ORM Postgres (.NET) | `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | `apps/api` | HIGH |
| EF Core (base) | `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | `apps/api` | HIGH |
### Supporting Libraries
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `qrcode` | 1.5.4 | Gera QR em SVG (vetor, print) e PNG (buffer, download rápido) | Sempre que precisar do QR do cartão — tela e download |
| `cpf-cnpj-validator` | 2.1.2 | Valida CPF/CNPJ com dígito verificador, já cobre o novo CNPJ alfanumérico (RFB, vigente jul/2026) | Form de edição do cartão, campo `pix_key` quando `pix_key_type = cpf` ou `cnpj` |
| `zod` | 4.4.3 | Schema de validação de formulário (pix_key por tipo, slug, urls sociais) | Se o form do dashboard ainda não tiver uma lib de validação — combina bem com React Hook Form |
| `@vercel/blob` | latest (client SDK) | Upload de `photo_url` direto do browser para blob storage, sem passar arquivo binário pelo .NET | Upload de foto do cartão no formulário de edição |
## As 7 áreas — decisão e justificativa
### 1. QR code — geração e onde mora
- **Exibição na tela** (dashboard, "veja seu QR"): `QRCode.toString(url, { type: 'svg' })` — gera SVG inline, renderiza nítido em qualquer tamanho de tela sem servir arquivo.
- **Download para impressão**: ofereça SVG como padrão (vetor puro, resolução infinita, ideal para adesivo/gráfica) via Route Handler `apps/web/app/[slug]/qr/route.ts` retornando `Content-Type: image/svg+xml` com `Content-Disposition: attachment`. Como fallback para quem precisa de PNG (algumas gráficas não aceitam SVG), gere também com `QRCode.toBuffer(url, { type: 'png', width: 1024, errorCorrectionLevel: 'M' })` — 1024px é suficiente para impressão em adesivo pequeno/médio a boa resolução.
- Use `errorCorrectionLevel: 'M'` (padrão da lib) — nível `H` só compensa se for embutir logo no centro do QR, o que não está no escopo da v1.
### 2. vCard (.vcf) — formato e biblioteca
- **Terminadores de linha `\r\n` (CRLF), não `\n`** — é o erro mais comum ao gerar vCard: parsers de iOS/Android esperam CRLF estrito; `\n` sozinho quebra silenciosamente em alguns clients.
- **Escapar `,` `;` `\` dentro de valores** (ex: se `role` ou `company` tiver vírgula).
- **Content-Type correto na Route Handler:** `text/vcard; charset=utf-8` e `Content-Disposition: attachment; filename="nome.vcf"` — sem isso, nomes com acento (ã, ç, é) corrompem no download em Safari/iOS.
- Não inclua `PHOTO;VALUE=uri:` apontando para o `photo_url` na v1 — muitos clients de contato baixam e embutem a imagem (base64), o que infla o arquivo e pode falhar silenciosamente se a URL não for acessível sem redirecionamento; se quiser foto no contato, é um approfundamento de fase futura, não do corte de 2 semanas.
- Links sociais (Instagram, LinkedIn etc.) não têm campo padronizado limpo em vCard 3.0 — jogue-os em `NOTE:` como texto simples, ou omita do .vcf (o objetivo do .vcf é nome + telefone + whatsapp + email, não replicar o cartão inteiro).
### 3. Open Graph image — `next/og` / `ImageResponse`
- `ImageResponse` só aceita `.ttf`, `.otf` ou `.woff` — **não** aceita `next/font/google` (isso é só para HTML normal, não funciona dentro do renderer do `ImageResponse`).
- Você precisa buscar o binário da fonte você mesmo e passar como `ArrayBuffer` na opção `fonts`. Padrão recomendado pela documentação oficial: colocar o arquivo `.ttf` em `apps/web/assets/fonts/` e carregar com `fetch(new URL('../assets/fonts/Inter-Bold.ttf', import.meta.url))` — funciona em Edge Runtime porque o asset é embutido no build.
- **Garanta que a fonte escolhida cubra o bloco Unicode Latin Extended-A** (ã, õ, ç, á, é, í, ó, ú, â, ê, ô) — a maioria das variable fonts populares (Inter, Geist) cobre isso no subset "latin" do Google Fonts, mas se você baixar um `.ttf` picotado (só ASCII básico) o "ã" e "ç" saem quebrados ou somem no preview do WhatsApp. Teste explicitamente com um nome como "João" ou "Conceição" antes de considerar pronto — é o tipo de bug que só aparece com dado real brasileiro.
- Prefira `ttf`/`otf` a `woff` — parse mais rápido no Edge Runtime (a função de OG image tem orçamento de tempo curto).
- Evite montar a fonte na função do componente a cada request — carregue uma vez no escopo do módulo (top-level `const fontData = await fetch(...)`) e reuse.
### 4. Renderização da página pública do cartão — SSR vs ISR vs static+revalidate
- O cartão é editado pelo dono ocasionalmente (não é conteúdo que muda a cada segundo) — uma defasagem de até 60s entre salvar no dashboard e o público ver a mudança é aceitável e não compromete a experiência.
- Como o dashboard chama o `apps/api` (.NET) diretamente via fetch com header `Authorization` (conforme spec 02) — e não passa por uma Server Action do Next.js — **não há um jeito simples de disparar `revalidatePath()` no exato momento do save** sem criar um Route Handler intermediário em `apps/web` que faça proxy da chamada de update só para poder chamar `revalidatePath` depois. Isso é complexidade extra que não se paga no MVP: prefira o `revalidate` por tempo, documentado como "atualiza em até 1 minuto", e trate revalidação sob demanda como melhoria de fase futura se o tempo de propagação incomodar.
- Mantém a página estática entre requests dentro da janela de 60s, o que é bom para o free tier: menos invocações de function no Vercel, resposta mais rápida pra quem escaneia o QR (o caso de uso mobile mais comum).
### 5. Upload/hosting da foto do cartão (`photo_url`)
| Opção | Free tier | Precisa cartão de crédito | Observação |
|-------|-----------|---------------------------|------------|
| **Vercel Blob** (recomendado) | 1GB storage + 10GB transferência/mês, até 100 stores no Hobby | Não | Já é a mesma conta usada para hospedar `apps/web` — zero cadastro novo, zero credencial nova para gerenciar |
| Supabase Storage | 1GB storage, 5GB egress/mês | Não | Só faz sentido se o Postgres também for Supabase — mas ver seção 7: recomendamos Neon para o banco, o que tornaria Supabase Storage uma conta extra sem necessidade |
| Cloudinary | 25 "créditos"/mês (1 crédito = 1GB storage OU 1GB banda OU 1000 transformações) | Não, mas a métrica de crédito é confusa e mistura storage+banda+transformação no mesmo teto | Só compensa se for usar transformação de imagem avançada (crop automático, otimização); para um caso de uso de "1 foto de perfil por cartão" é over-engineering |
| UploadThing | 2GB, overage cobrado ($0.08/GB depois disso) | Precisa confirmar no cadastro atual — histórico de exigir cartão em alguns planos | Menor teto livre das 4 opções, sem vantagem clara para este caso de uso |
### 6. Chave Pix — validação, não geração
| `pix_key_type` | Formato esperado | Como validar |
|----------------|-------------------|--------------|
| `cpf` | 11 dígitos, com dígito verificador válido | `cpf-cnpj-validator` (`cpf.isValid()`) |
| `cnpj` | 14 dígitos numéricos **ou** o novo formato alfanumérico (Receita Federal, vigente a partir de jul/2026) | `cpf-cnpj-validator` 2.1.2 — já suporta o CNPJ alfanumérico, verificado no changelog do pacote |
| `email` | e-mail válido (RFC 5322 simplificado) | `zod` (`.email()`) ou regex padrão — não precisa de lib dedicada |
| `telefone` | Número BR, com ou sem `+55`, DDD + 8/9 dígitos | Regex simples: `/^(\+55\s?)?\(?\d{2}\)?\s?9?\d{4}-?\d{4}$/` — não existe (nem faz sentido existir) uma lib só pra isso |
| `aleatoria` | UUID v4 (formato que o Bacen usa pra chave aleatória) | Regex UUID padrão: `/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i` |
### 7. .NET + Postgres — versões e cold start
| Pacote | Versão mais recente estável | Alvo |
|--------|------------------------------|------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | **10.0.3** | .NET 10 |
| `Microsoft.EntityFrameworkCore` (e pacotes companheiros: `.Design`, `.Tools`) | **10.0.11** | .NET 10 |
| `Npgsql` (driver puro, se usado sem EF em algum ponto) | **10.0.3** | .NET 10 |
- **Neon (free tier):** compute "scale to zero" **sempre ativo** no plano free, suspende após 5 minutos de inatividade, mas **acorda sozinho em ~500ms** na primeira query seguinte — sem nenhuma ação manual. Storage fica intacto, é só o compute que hiberna e reativa por request.
- **Supabase (free tier):** projeto inteiro **pausa depois de 7 dias sem atividade de banco** — e diferente do Neon, isso **não** se auto-resolve na próxima request: precisa entrar no dashboard e clicar "Restore project" manualmente. Enquanto pausado, o banco recusa toda conexão.
## Installation
# apps/web
# zod + react-hook-form só se ainda não estiverem no projeto
# apps/api (dotnet CLI)
## Alternatives Considered
| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|--------------------------|
| `qrcode` (npm) | API externa (ex: `api.qrserver.com`) | Nunca aqui — dependência de terceiro pra algo trivial de gerar localmente, some se o serviço cair, e ainda cria round-trip de rede desnecessário |
| vCard mão na massa | `vcards-js` (2.11.1) | Se o cartão crescer para incluir endereço postal completo, múltiplos telefones tipados, ou aniversário — aí a lib compensa a complexidade extra |
| `next/og` (Edge) | `@vercel/og` com Node runtime | Se a rota de OG image precisar de alguma lib Node-only (ex: acesso a filesystem fora do padrão de asset embutido, ou driver de banco direto) |
| ISR `revalidate=60` | `force-dynamic` (SSR puro) | Se simplicidade de modelo mental > custo de function invocations — defensável também, ver seção 4 |
| Vercel Blob | Supabase Storage | Somente se você decidir usar Supabase como banco (contradiz a recomendação da seção 7) — nesse caso, unificar storage+banco na mesma conta faz sentido |
| Neon (Postgres) | Supabase (Postgres) | Se o roadmap já previr uso pesado de features exclusivas do Supabase (Auth, Realtime, Edge Functions) — não é o caso aqui, já que a auth é JWT próprio no .NET |
| `cpf-cnpj-validator` | `validator-brazil`, `validation-br` | Equivalentes funcionalmente; troque se preferir zero-dependency (`validator-brazil` não tem dependências) |
## What NOT to Use
| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `qr-code-styling` | Depende de `canvas`/`jsdom` — binário nativo que sofre para compilar/rodar em Vercel Functions (serverless) | `qrcode` (puro JS) |
| Gerar Pix BR Code / payload EMV (qualquer lib) | Fora de escopo por decisão de produto explícita — não existe necessidade técnica disso na v1 | Copiar chave para clipboard, texto puro |
| `next/font/google` dentro de `ImageResponse` | Não funciona — o renderer do OG image não processa a injeção de fonte do `next/font`, precisa do binário bruto via `fetch`/`fs` | Buscar `.ttf`/`.otf` manualmente e passar em `fonts: [...]` |
| Ativar `cacheComponents` (Next.js 16) nesta fase | Modelo mental novo (diretiva `"use cache"`) sem necessidade concreta na janela de 2 semanas — custo de aprendizado não se paga | Modelo clássico: `export const revalidate = N` |
| `SSL Mode=Prefer` (default do Npgsql) contra Neon/Supabase | Não valida certificado, funciona "por acaso" localmente e pode confundir troubleshooting em produção | `SSL Mode=Require;Trust Server Certificate=true` explícito na connection string |
| Supabase para o banco (Postgres) neste produto | Pausa o projeto inteiro após 7 dias sem query, exige restauração manual no dashboard — risco real de derrubar silenciosamente o link público | Neon (autosuspend por request, acorda sozinho em ~500ms) |
## Stack Patterns by Variant
- Use `revalidate = 60` ou até `force-dynamic` sem preocupação de custo — o Hobby plan do Vercel absorve isso tranquilamente nessa escala.
- Aí sim vale trocar para revalidação sob demanda (`revalidatePath` disparado na própria Server Action) em vez de `revalidate` por tempo — mas isso é uma mudança de arquitetura de autenticação, não algo a fazer na janela de 2 semanas atual.
## Version Compatibility
| Package A | Compatible With | Notes |
|-----------|------------------|-------|
| `Npgsql.EntityFrameworkCore.PostgreSQL@10.0.3` | `Microsoft.EntityFrameworkCore@10.0.11` + .NET 10 | Ambos seguem versionamento major alinhado ao .NET — não misturar Npgsql 9.x com EF Core 10.x |
| `qrcode@1.5.4` | Node.js runtime do Next.js (não Edge) | `toBuffer`/`toString` para PNG usam `pngjs`, puro JS — funciona em Node runtime das Vercel Functions sem config extra; não precisa (nem deve) rodar em Edge Runtime |
| `next/og` (`ImageResponse`) | Exige `export const runtime = 'edge'` na rota | Diferente da rota do `.vcf` e da rota do QR, que podem ficar em Node runtime (default) |
| `cpf-cnpj-validator@2.1.2` | Node.js e browser (bundle isomórfico) | Pode validar tanto no client (form) quanto, se quiser, num Route Handler do Next — não precisa estar só num lado |
## Sources
- npm registry (verificado via `npm view`, 13/08/2026): `qrcode@1.5.4`, `cpf-cnpj-validator@2.1.2`, `next@16.3.0`, `@vercel/og@1.0.1`, `zod@4.4.3`, `react-hook-form@7.85.0` — HIGH confidence
- NuGet registry (verificado via API, 13/08/2026): `Npgsql.EntityFrameworkCore.PostgreSQL@10.0.3`, `Microsoft.EntityFrameworkCore@10.0.11`, `Npgsql@10.0.3`, `QRCoder@1.8.0`, `BCrypt.Net-Next@4.2.1` — HIGH confidence
- [Announcing .NET 10 — .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/) — confirma .NET 10 como LTS atual, 11/2025–11/2028
- [Next.js — Functions: ImageResponse](https://nextjs.org/docs/app/api-reference/functions/image-response) — formatos de fonte suportados, exigência de Edge Runtime
- [Next.js — Getting Started: Metadata and OG images](https://nextjs.org/docs/app/getting-started/metadata-and-og-images) — `next/og` embutido desde 13.3+
- [Next.js — Guides: Migrating to Cache Components](https://nextjs.org/docs/app/guides/migrating-to-cache-components) e [next.config.js: cacheComponents](https://nextjs.org/docs/app/api-reference/config/next-config-js/cacheComponents) — confirma opt-in em 16.3.0
- [Neon Docs — Compute lifecycle](https://neon.com/docs/introduction/compute-lifecycle) e [Why You Want a Database That Scales to Zero](https://neon.com/blog/why-you-want-a-database-that-scales-to-zero) — autosuspend 5 min, wake ~500ms no free tier
- [Supabase Docs — Project Pausing](https://supabase.com/docs/guides/platform/free-project-pausing) — pausa após 7 dias, restauração manual
- [Supabase Docs — SSL Enforcement](https://supabase.com/docs/guides/platform/ssl-enforcement) e [Npgsql — Security and Encryption](https://www.npgsql.org/doc/security.html) — parâmetros de connection string SSL
- [Render Docs — Deploy for Free](https://render.com/docs/free) — spin-down 15 min de inatividade, cold start ~30-50s
- [Vercel Docs — Vercel Blob Pricing](https://vercel.com/docs/vercel-blob/usage-and-pricing) e [Vercel Docs — Hobby Plan](https://vercel.com/docs/plans/hobby) — 1GB storage/10GB transfer, sem cartão de crédito, uso restrito a projeto pessoal/não-comercial
- WebSearch (múltiplas fontes agregadas, MEDIUM confidence salvo indicação contrária): comparação Cloudinary/UploadThing/Supabase Storage free tier; recomendação `qrcode` vs `qr-code-styling`; vCard 3.0 vs 4.0 compatibilidade iOS/Android; pacotes npm de validação CPF/CNPJ
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
