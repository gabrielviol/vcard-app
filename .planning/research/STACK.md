# Stack Research

**Domínio:** Cartão de visita digital (Brasil) — lacunas de biblioteca sobre o stack já travado (Next.js App Router + .NET minimal API + Postgres, specs 01/02)
**Pesquisado em:** 2026-08-13
**Confiança geral:** MEDIUM-HIGH (versões verificadas direto no npm/NuGet registry; padrões de arquitetura verificados via docs oficiais e WebSearch)

> Este documento **não** reabre as decisões de `docs/specs/01-setup.md` e `02-autentication.md` (monorepo, Next.js, .NET minimal API, Postgres, JWT, BCrypt). Ele preenche as lacunas de biblioteca que essas specs deixam em aberto, para as 7 áreas pedidas.

---

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

---

## As 7 áreas — decisão e justificativa

### 1. QR code — geração e onde mora

**Decisão: gerar no `apps/web` (Next.js), não no `apps/api`.**

O conteúdo do QR é só a URL pública do cartão (`https://dominio/gabriel`) — uma string que o próprio Next.js já possui, sem precisar de dado do banco nem de autenticação. Gerar no .NET criaria uma chamada cross-service para produzir uma imagem derivada de uma string que o frontend já tem em mãos. Mantenha essa responsabilidade 100% em `apps/web`.

**Biblioteca:** `qrcode` (pacote npm "qrcode", também conhecido como node-qrcode) — versão **1.5.4** (verificado no registry npm, 13/08/2026). É puro JavaScript, sem dependência nativa (ao contrário de `qr-code-styling`, que depende de `canvas`/`jsdom` e sofre com compilação nativa em ambientes serverless como as Vercel Functions). Mais de 1M downloads/semana, é a lib de fato padrão do ecossistema Node para isso.

**Como usar as duas necessidades (tela + impressão):**
- **Exibição na tela** (dashboard, "veja seu QR"): `QRCode.toString(url, { type: 'svg' })` — gera SVG inline, renderiza nítido em qualquer tamanho de tela sem servir arquivo.
- **Download para impressão**: ofereça SVG como padrão (vetor puro, resolução infinita, ideal para adesivo/gráfica) via Route Handler `apps/web/app/[slug]/qr/route.ts` retornando `Content-Type: image/svg+xml` com `Content-Disposition: attachment`. Como fallback para quem precisa de PNG (algumas gráficas não aceitam SVG), gere também com `QRCode.toBuffer(url, { type: 'png', width: 1024, errorCorrectionLevel: 'M' })` — 1024px é suficiente para impressão em adesivo pequeno/médio a boa resolução.
- Use `errorCorrectionLevel: 'M'` (padrão da lib) — nível `H` só compensa se for embutir logo no centro do QR, o que não está no escopo da v1.

**Confiança:** HIGH — verificado versão no registry, comportamento de dependências confirmado via múltiplas fontes.

---

### 2. vCard (.vcf) — formato e biblioteca

**Decisão: vCard 3.0, mão na massa (sem biblioteca).**

vCard 3.0 é o formato com compatibilidade universal — suportado nativamente por iOS, Android, Google Contacts e Outlook. vCard 4.0 adiciona campos (ex: `X-SOCIALPROFILE`, tipos de valor múltiplos) mas tem suporte inconsistente em apps de contato mais antigos e em algumas implementações Android — não vale o risco para um cartão cujo objetivo é "salvar contato" funcionar sem fricção na primeira tentativa.

**Por que não usar uma lib (`vcards-js`, `vcard-creator`):** o payload do Card no schema é pequeno e fixo (nome, cargo, empresa, telefone, whatsapp, email, foto opcional) — dá para gerar com um template string de ~15 linhas. Uma lib de terceiros adiciona uma dependência para resolver um problema de baixa complexidade, e a maioria dessas libs foca em campos de vCard 4.0 ou tem APIs verbosas para o que é essencialmente concatenação de string. Gerar na mão também dá controle total sobre os pontos que mais causam bugs de compatibilidade:

- **Terminadores de linha `\r\n` (CRLF), não `\n`** — é o erro mais comum ao gerar vCard: parsers de iOS/Android esperam CRLF estrito; `\n` sozinho quebra silenciosamente em alguns clients.
- **Escapar `,` `;` `\` dentro de valores** (ex: se `role` ou `company` tiver vírgula).
- **Content-Type correto na Route Handler:** `text/vcard; charset=utf-8` e `Content-Disposition: attachment; filename="nome.vcf"` — sem isso, nomes com acento (ã, ç, é) corrompem no download em Safari/iOS.
- Não inclua `PHOTO;VALUE=uri:` apontando para o `photo_url` na v1 — muitos clients de contato baixam e embutem a imagem (base64), o que infla o arquivo e pode falhar silenciosamente se a URL não for acessível sem redirecionamento; se quiser foto no contato, é um approfundamento de fase futura, não do corte de 2 semanas.
- Links sociais (Instagram, LinkedIn etc.) não têm campo padronizado limpo em vCard 3.0 — jogue-os em `NOTE:` como texto simples, ou omita do .vcf (o objetivo do .vcf é nome + telefone + whatsapp + email, não replicar o cartão inteiro).

**Onde mora:** `apps/web/app/[slug]/vcard.vcf/route.ts` (já definido na spec 01) — Route Handler que busca os dados públicos do cartão (mesma fonte que a página `[slug]/page.tsx` usa) e monta a string. Não precisa do `apps/api` além do endpoint público de leitura do cartão que já existe.

**Confiança:** HIGH — vCard 3.0 como escolha de compatibilidade é consenso entre as fontes consultadas; a recomendação de não usar lib é uma chamada de engenharia (custo/benefício), não um fato verificável, então trate como MEDIUM nessa parte específica.

---

### 3. Open Graph image — `next/og` / `ImageResponse`

**Decisão: usar `next/og` (já embutido no Next.js, não precisa instalar `@vercel/og` à parte).**

A partir do Next.js 13.3+, o App Router já inclui `@vercel/og` internamente e o exporta como `next/og` — instalar o pacote separado é redundante. Confirmado: Next.js atual é **16.3.0** (agosto/2026).

**Runtime:** `ImageResponse` importado de `next/og` **exige Edge Runtime** (vs. importar de `@vercel/og` direto, que roda em Node ou Edge). Como a rota de OG image só precisa buscar os dados públicos do cartão (nome, cargo, foto) via fetch simples — algo que roda igualmente bem em Edge — fique com `next/og` e Edge Runtime por ser o caminho oficialmente documentado e testado pela Vercel.

**Fontes e acentuação em português — o ponto que mais quebra:**
- `ImageResponse` só aceita `.ttf`, `.otf` ou `.woff` — **não** aceita `next/font/google` (isso é só para HTML normal, não funciona dentro do renderer do `ImageResponse`).
- Você precisa buscar o binário da fonte você mesmo e passar como `ArrayBuffer` na opção `fonts`. Padrão recomendado pela documentação oficial: colocar o arquivo `.ttf` em `apps/web/assets/fonts/` e carregar com `fetch(new URL('../assets/fonts/Inter-Bold.ttf', import.meta.url))` — funciona em Edge Runtime porque o asset é embutido no build.
- **Garanta que a fonte escolhida cubra o bloco Unicode Latin Extended-A** (ã, õ, ç, á, é, í, ó, ú, â, ê, ô) — a maioria das variable fonts populares (Inter, Geist) cobre isso no subset "latin" do Google Fonts, mas se você baixar um `.ttf` picotado (só ASCII básico) o "ã" e "ç" saem quebrados ou somem no preview do WhatsApp. Teste explicitamente com um nome como "João" ou "Conceição" antes de considerar pronto — é o tipo de bug que só aparece com dado real brasileiro.
- Prefira `ttf`/`otf` a `woff` — parse mais rápido no Edge Runtime (a função de OG image tem orçamento de tempo curto).
- Evite montar a fonte na função do componente a cada request — carregue uma vez no escopo do módulo (top-level `const fontData = await fetch(...)`) e reuse.

**Confiança:** HIGH (comportamento de runtime e formatos de fonte confirmados na documentação oficial do Next.js); MEDIUM na cobertura exata de glifos de uma fonte específica sem teste manual — trate como item de checklist de QA, não como garantia.

---

### 4. Renderização da página pública do cartão — SSR vs ISR vs static+revalidate

**Decisão: ISR clássico com `export const revalidate = 60` no `apps/web/app/[slug]/page.tsx` — não ativar o novo modelo "Cache Components" do Next.js 16.**

Contexto importante: Next.js 16 introduziu `cacheComponents` (sucessor estabilizado do antigo `experimental.dynamicIO`/PPR), que inverte o modelo padrão de cache (nada é cacheado a menos que você use `"use cache"` explicitamente). **Esse flag ainda é opt-in em 16.3.0** — se você não ligar `cacheComponents: true` no `next.config.ts`, o comportamento é o modelo clássico (idêntico ao Next.js 15): fetch não-cacheado por padrão, mas a config de segmento de rota (`revalidate`, `dynamic`) continua controlando se a página é estática+revalidada ou dinâmica a cada request.

Para uma janela de 2 semanas solo, **não ative `cacheComponents`** — é um modelo mental novo (diretiva `"use cache"`, granularidade por componente) que não compensa o custo de aprendizado para o ganho que traria aqui. Fique com o modelo clássico.

**Por que `revalidate = 60` e não `force-dynamic`:**
- O cartão é editado pelo dono ocasionalmente (não é conteúdo que muda a cada segundo) — uma defasagem de até 60s entre salvar no dashboard e o público ver a mudança é aceitável e não compromete a experiência.
- Como o dashboard chama o `apps/api` (.NET) diretamente via fetch com header `Authorization` (conforme spec 02) — e não passa por uma Server Action do Next.js — **não há um jeito simples de disparar `revalidatePath()` no exato momento do save** sem criar um Route Handler intermediário em `apps/web` que faça proxy da chamada de update só para poder chamar `revalidatePath` depois. Isso é complexidade extra que não se paga no MVP: prefira o `revalidate` por tempo, documentado como "atualiza em até 1 minuto", e trate revalidação sob demanda como melhoria de fase futura se o tempo de propagação incomodar.
- Mantém a página estática entre requests dentro da janela de 60s, o que é bom para o free tier: menos invocações de function no Vercel, resposta mais rápida pra quem escaneia o QR (o caso de uso mobile mais comum).

**Se o volume de acessos for baixo o suficiente** (esperado nas primeiras semanas — dezenas de views, não milhares), `force-dynamic` (SSR puro a cada request) também é uma opção defensável e mais simples de raciocinar (sempre reflete o estado atual, zero stale data) — a diferença de custo no Hobby plan é desprezível nessa escala. Escolha `revalidate = 60` como padrão, mas não é um erro trocar por `force-dynamic` se preferir simplicidade de modelo mental a essa micro-otimização.

**Confiança:** HIGH para o mecanismo do Next.js (documentação oficial); MEDIUM para a recomendação específica de 60s vs dinâmico — é uma escolha de produto, não um fato técnico único.

---

### 5. Upload/hosting da foto do cartão (`photo_url`)

**Decisão: Vercel Blob.**

| Opção | Free tier | Precisa cartão de crédito | Observação |
|-------|-----------|---------------------------|------------|
| **Vercel Blob** (recomendado) | 1GB storage + 10GB transferência/mês, até 100 stores no Hobby | Não | Já é a mesma conta usada para hospedar `apps/web` — zero cadastro novo, zero credencial nova para gerenciar |
| Supabase Storage | 1GB storage, 5GB egress/mês | Não | Só faz sentido se o Postgres também for Supabase — mas ver seção 7: recomendamos Neon para o banco, o que tornaria Supabase Storage uma conta extra sem necessidade |
| Cloudinary | 25 "créditos"/mês (1 crédito = 1GB storage OU 1GB banda OU 1000 transformações) | Não, mas a métrica de crédito é confusa e mistura storage+banda+transformação no mesmo teto | Só compensa se for usar transformação de imagem avançada (crop automático, otimização); para um caso de uso de "1 foto de perfil por cartão" é over-engineering |
| UploadThing | 2GB, overage cobrado ($0.08/GB depois disso) | Precisa confirmar no cadastro atual — histórico de exigir cartão em alguns planos | Menor teto livre das 4 opções, sem vantagem clara para este caso de uso |

Fotos de perfil de cartão de visita são arquivos pequenos (uma imagem redimensionada, tipicamente <500KB depois de otimizada) — mesmo com centenas de usuários, 1GB do Vercel Blob dura muito além da janela de validação de 2 semanas.

**Como integrar sem sobrecarregar o `apps/api`:** use o client SDK `@vercel/blob` (`upload()` client-side, com `handleUpload`/token de upload assinado por uma Route Handler em `apps/web`) para subir o arquivo **direto do browser para o Blob**, sem passar o binário da imagem pelo `.NET`. O `apps/api` só recebe e persiste a URL final (`photo_url`) que o Blob retorna — mantém o backend .NET livre de lidar com multipart/binary upload, que não é o ponto forte de uma minimal API simples.

**Ressalva de ToS:** o plano Hobby da Vercel (incluindo Blob) é licenciado para uso pessoal/não-comercial. Para a janela de validação pré-receita isso é adequado (é literalmente o estágio do produto agora), mas no momento em que o freemium começar a gerar cobrança real, migrar para o plano Pro da Vercel é necessário — vale registrar como gatilho de "quando migrar de free tier", não como bloqueio agora.

**Confiança:** HIGH para os números de free tier (verificados via múltiplas fontes de 2026); HIGH para a recomendação — a integração com a mesma conta Vercel é uma vantagem operacional clara para um dev solo.

---

### 6. Chave Pix — validação, não geração

**Confirmação: nenhuma biblioteca de Pix é necessária.** A decisão de produto (`copiar chave`, sem BR Code/EMV, sem PSP) elimina completamente a necessidade de qualquer lib de geração de payload Pix (ex: `qrcode-pix`, `pix-utils`) — essas libs resolvem o problema de gerar o "Pix Copia e Cola" (payload EMV com CRC16), que é exatamente o que este produto decidiu **não** fazer. O campo `pix_key` no schema é só texto livre copiado para a área de transferência via `navigator.clipboard.writeText()` — zero dependência.

**O que de fato precisa de atenção: validação de formato por `pix_key_type`.**

| `pix_key_type` | Formato esperado | Como validar |
|----------------|-------------------|--------------|
| `cpf` | 11 dígitos, com dígito verificador válido | `cpf-cnpj-validator` (`cpf.isValid()`) |
| `cnpj` | 14 dígitos numéricos **ou** o novo formato alfanumérico (Receita Federal, vigente a partir de jul/2026) | `cpf-cnpj-validator` 2.1.2 — já suporta o CNPJ alfanumérico, verificado no changelog do pacote |
| `email` | e-mail válido (RFC 5322 simplificado) | `zod` (`.email()`) ou regex padrão — não precisa de lib dedicada |
| `telefone` | Número BR, com ou sem `+55`, DDD + 8/9 dígitos | Regex simples: `/^(\+55\s?)?\(?\d{2}\)?\s?9?\d{4}-?\d{4}$/` — não existe (nem faz sentido existir) uma lib só pra isso |
| `aleatoria` | UUID v4 (formato que o Bacen usa pra chave aleatória) | Regex UUID padrão: `/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i` |

**Onde validar:** `apps/web` no formulário de edição (feedback imediato, via `cpf-cnpj-validator` + `zod`/`react-hook-form`). No `apps/api`, replique uma validação de formato leve (regex, sem a lib de dígito verificador) antes de persistir — não é necessário instalar `cpf-cnpj-validator` equivalente em C# para isso; um `Regex.IsMatch` por tipo já cobre o objetivo de "defesa em profundidade" sem duplicar uma dependência JS no lado .NET. Se quiser validação completa de dígito verificador de CPF/CNPJ também no backend, o algoritmo é ~15 linhas de C# — não precisa de pacote NuGet dedicado para isso.

**Confiança:** HIGH — a ausência de necessidade de lib de Pix é uma consequência direta e não-ambígua da decisão de produto já registrada em `.planning/PROJECT.md`. MEDIUM na escolha específica de `cpf-cnpj-validator` (existem alternativas equivalentes como `validator-brazil`, `validation-br` — a escolha é por ser a mais baixada e ativamente mantida, verificado no npm em 13/08/2026).

---

### 7. .NET + Postgres — versões e cold start

**Confirmado via NuGet registry (13/08/2026):**

| Pacote | Versão mais recente estável | Alvo |
|--------|------------------------------|------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | **10.0.3** | .NET 10 |
| `Microsoft.EntityFrameworkCore` (e pacotes companheiros: `.Design`, `.Tools`) | **10.0.11** | .NET 10 |
| `Npgsql` (driver puro, se usado sem EF em algum ponto) | **10.0.3** | .NET 10 |

**.NET 10 é a versão "mais recente estável" hoje** (LTS, lançado em 11/11/2025, suporte até 11/2028) — confirma que a spec 01 ("versão mais recente estável") aponta para .NET 10, não .NET 9 (que junto com .NET 8 encerra suporte em 11/2026). Use o pareamento **Npgsql.EntityFrameworkCore.PostgreSQL 10.x + Microsoft.EntityFrameworkCore 10.x** — os dois seguem o mesmo esquema de versionamento major alinhado à versão do .NET, não misture com 9.x.

**Gotcha de connection string com Neon/Supabase:** ambos exigem SSL. O padrão do Npgsql (`SSL Mode=Prefer`) não valida certificado; para managed Postgres em nuvem use explicitamente:
```
Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<senha>;SSL Mode=Require;Trust Server Certificate=true
```
Sem isso, é comum a conexão falhar silenciosamente ou cair em erro de certificado ao trocar do Docker Compose local (sem SSL) para o banco gerenciado.

**Recomendação de escolha entre Neon e Supabase (a spec deixa em aberto "Neon ou Supabase"): prefira Neon.**

Esse é o achado mais importante desta seção e impacta diretamente a confiabilidade do link público do cartão:

- **Neon (free tier):** compute "scale to zero" **sempre ativo** no plano free, suspende após 5 minutos de inatividade, mas **acorda sozinho em ~500ms** na primeira query seguinte — sem nenhuma ação manual. Storage fica intacto, é só o compute que hiberna e reativa por request.
- **Supabase (free tier):** projeto inteiro **pausa depois de 7 dias sem atividade de banco** — e diferente do Neon, isso **não** se auto-resolve na próxima request: precisa entrar no dashboard e clicar "Restore project" manualmente. Enquanto pausado, o banco recusa toda conexão.

Para este produto, isso importa mais do que parece: o cenário mais provável nas primeiras semanas é um cartão compartilhado, algumas visitas, e o dono não voltando ao dashboard todo dia. Se o banco for Supabase e passar 7 dias sem query, a página pública do cartão (que também depende do banco) para de funcionar até alguém notar e restaurar manualmente pelo painel — o pior tipo de falha silenciosa para um link que está circulando em cartão impresso. Com Neon, o pior caso é uma latência extra de meio segundo na primeira visita depois de um período ocioso — invisível pro usuário final.

**Cold start combinado (Render + banco):** o `apps/api` no Render free tier hiberna depois de 15 minutos sem tráfego e leva de ~30 a ~50 segundos para acordar na primeira requisição — esse é o cold start dominante, bem maior que qualquer latência de banco. Isso afeta principalmente a **primeira carga da página pública** depois de um período ocioso (a página do Next.js chama o `apps/api` para buscar os dados do cartão). Não há solução de biblioteca para isso — é uma característica do free tier do Render que a spec já aceita explicitamente ("aceita cold start nesta fase"). Vale só registrar como conhecimento operacional: se o crawler do WhatsApp (que busca o OG image/preview ao colar o link) bater justo durante esse cold start e não tiver paciência de esperar, o preview do link pode falhar na primeira tentativa depois de o serviço dormir — normalmente resolve sozinho ao tentar de novo, mas é bom não descobrir isso pela primeira vez apresentando o produto pra alguém.

**Confiança:** HIGH para as versões e para o mecanismo de suspensão do Neon (documentação oficial, verificado com data de referência 2026); HIGH para o comportamento de pausa do Supabase (documentação oficial + múltiplas fontes independentes); MEDIUM para o comportamento específico do crawler do WhatsApp em relação a cold start (não há documentação oficial do WhatsApp sobre timeout de scraping — é inferência razoável, não fato verificado).

---

## Installation

```bash
# apps/web
npm install qrcode cpf-cnpj-validator @vercel/blob
npm install -D @types/qrcode

# zod + react-hook-form só se ainda não estiverem no projeto
npm install zod react-hook-form @hookform/resolvers
```

```bash
# apps/api (dotnet CLI)
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.11
```

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

**Se o slug do cartão for acessado com muito baixo tráfego (esperado nas primeiras semanas):**
- Use `revalidate = 60` ou até `force-dynamic` sem preocupação de custo — o Hobby plan do Vercel absorve isso tranquilamente nessa escala.

**Se, mais adiante, o dashboard passar a usar Server Actions do Next.js em vez de fetch direto ao `.NET` com Bearer token:**
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

---
*Stack research for: cartão de visita digital (Brasil) — lacunas de biblioteca*
*Researched: 2026-08-13*
