# Project Research Summary

**Project:** vCard App (nome a definir)
**Domain:** Cartão de visita digital (link + QR) para o mercado brasileiro, com WhatsApp e Pix como campos de primeira classe
**Researched:** 2026-08-13
**Confidence:** MEDIUM-HIGH

## Executive Summary

Este é um produto de categoria bem estabelecida (link-in-bio / cartão de visita digital), mas com uma tese de diferenciação específica: tratar WhatsApp como CTA de conversão central e Pix como campo nativo, em vez de decoração genérica. A pesquisa confirma que essa tese continua defensável, mas com uma correção importante em relação ao que o PROJECT.md assumia: existe concorrência brasileira direta e mais madura do que o esperado (Monocard, "a plataforma #1 de cartão de visita digital no Brasil", já tem Pix nativo e cobra R$9,90/mês — o mesmo patamar planejado aqui). O espaço em branco real não é "ter Pix no Brasil primeiro" — é combinar WhatsApp como CTA central (que nem Monocard trata assim) com simplicidade radical (sem CRM, sem hardware NFC, sem múltiplos perfis), voltada ao público de autônomo solo.

A recomendação de arquitetura mais importante é que o cartão público não é uma feature isolada — é um bloco único de trabalho junto com ISR, pré-aquecimento no save e QR code, porque a definição de "pronto" deste produto inclui explicitamente que alguém escaneando um QR impresso, na hora, no celular, não pode ver 30-60s de tela em branco por causa do cold start combinado do Render (free tier, dorme após 15 min) e do banco (Neon ou Supabase). A pesquisa também identifica uma incompatibilidade estrutural entre ISR e contagem ingênua de views (contar dentro do componente cacheado subestima views de forma inconsistente) — resolvida com uma única decisão: disparar a contagem via sendBeacon/fetch keepalive num Client Component, pós-hidratação, fora do caminho cacheado.

O maior risco transversal, encontrado de forma convergente em FEATURES, ARCHITECTURE e PITFALLS, é que o cartão será majoritariamente aberto dentro do navegador embutido do Instagram/WhatsApp (in-app browser/WebView) — exatamente onde os dois botões centrais do produto (copiar Pix via Clipboard API, baixar o .vcf) são conhecidos por falhar ou se comportar de forma inconsistente. Não há solução técnica completa para isso (é limitação de plataforma, não bug do produto); a mitigação aceita é fallback visível (texto selecionável para copiar, orientação de "abrir no navegador") em vez de tentar contornar via truques frágeis de redirect. Por fim, a pesquisa força duas decisões que o PROJECT.md deixou implícitas ou em aberto: (1) Neon é a recomendação clara sobre Supabase para o banco, porque Supabase pausa o projeto inteiro após 7 dias de inatividade e exige restauração manual — o pior tipo de falha silenciosa para um link circulando em papel; e (2) existe uma distinção técnica real entre "Pix por copiar chave" (já decidido) e um QR Pix estático (BR Code), que é um payload público do Banco Central gerável sem PSP/webhook — tecnicamente não é a mesma coisa que o "Pix dinâmico com PSP" que o produto rejeitou, e vale uma decisão explícita do usuário sobre isso, não uma resolução silenciosa da pesquisa.

## Key Findings

### Recommended Stack

O stack macro já está travado (docs/specs/01-setup.md / 02-autentication.md: Next.js App Router + .NET minimal API + Postgres, JWT). A pesquisa preenche lacunas de biblioteca: geração de QR e .vcf ficam 100% em apps/web (sem envolver o .NET, pois dependem só da URL/dados públicos já lidos); OG image usa next/og embutido no Next.js (exige Edge Runtime, e exige buscar fontes .ttf manualmente — next/font/google não funciona dentro do ImageResponse, e é preciso testar explicitamente acentuação com nomes como "João"/"Conceição"); upload de foto vai direto do browser para Vercel Blob, sem passar bytes pelo Render. Não é necessária nenhuma lib de geração de payload Pix (a decisão de "copiar chave" elimina essa necessidade), só validação de formato por tipo (cpf-cnpj-validator no front + regex espelho no .NET).

**Core technologies:**
- qrcode (npm) — geração de QR em SVG (impressão) e PNG (fallback) — puro JS, sem dependência nativa problemática em serverless
- vCard 3.0 via template string manual (sem lib) — compatibilidade universal iOS/Android/Google Contacts, cuidado com CRLF e charset
- next/og (ImageResponse, Edge Runtime) — OG image dinâmica por cartão, já embutido no Next.js 16
- Neon (não Supabase) para Postgres — autosuspend de 5 min mas acorda sozinho em ~500ms por request; Supabase pausa o projeto inteiro após 7 dias e exige restauração manual no painel
- Npgsql.EntityFrameworkCore.PostgreSQL 10.x + Microsoft.EntityFrameworkCore 10.x, sempre com connection string de pooling (PgBouncer) e SSL Mode=Require com Trust Server Certificate=true explícito

### Expected Features

O achado mais importante de FEATURES.md é a existência de concorrência brasileira direta (Monocard) — o produto precisa se posicionar contra ela, não apenas contra Popl/HiHello/Blinq americanos. Confirma-se que quase todas as features já "Active" no PROJECT.md são table stakes ou diferenciadores validados; a pesquisa recomenda somar duas features triviais não escopadas ainda.

**Must have (table stakes):**
- Perfil (nome/cargo/empresa/foto), link público por slug, renderização mobile-first rápida
- Botão de WhatsApp, botão salvar contato (.vcf), links sociais ordenáveis
- QR code gerável/baixável, preview de link (OG) correto no WhatsApp, copiar link com 1 toque
- Cadastro/login/dashboard de edição

**Should have (competitivo/diferenciador):**
- Chave Pix copiável — nenhum concorrente internacional tem; nenhum concorrente BR combina isso com simplicidade (Monocard empacota Pix junto com CRM/hardware)
- Marca no rodapé do grátis + botão "remover marca" (intenção de upgrade) — padrão Linktree, motor de distribuição orgânica e sinal de validação
- Duas adições de baixo custo recomendadas para a conversa de escopo, ainda não decididas no PROJECT.md: botão "compartilhar meu cartão" via wa.me/?text= (serve diretamente 1 dos 4 canais de distribuição) e copiar link com 1 toque (trivial, universal no setor)

**Defer (v2+):**
- Analytics detalhado (referrer, série temporal) — dado já capturado, só falta UI
- QR Code Pix estático (BR Code) — ver seção de decisão em aberto abaixo
- Múltiplos cartões, temas customizáveis, Pix dinâmico via PSP, checkout, NFC físico, CRM/lead capture (este último: rejeitado por tese, não "adiado")

### Architecture Approach

apps/web (Next.js/Vercel) concentra tudo que não precisa tocar Postgres ou regra de negócio protegida (QR, .vcf, OG image, disparo do beacon de view); apps/api (.NET/Render) é o único escritor do Postgres e concentra autenticação, validação de slug e regras de negócio. A página pública usa ISR clássico (revalidate), não o novo cacheComponents do Next.js 16 (custo de aprendizado não compensa na janela de 2 semanas). View tracking é resolvido com um Client Component que dispara sendBeacon pós-hidratação — isso resolve, de um só golpe, tanto a incompatibilidade estrutural entre ISR e contagem de views quanto a maior parte do ruído de bots/crawlers de preview (que majoritariamente não executam JavaScript).

**Major components:**
1. apps/web (Vercel) — renderização pública ISR, dashboard autenticado, geração de QR/.vcf/OG, upload de foto (Vercel Blob), disparo do beacon de view
2. apps/api (.NET, Render) — autenticação JWT, validação de slug reservado, único ponto de escrita no Postgres, endpoint de contagem de views
3. Postgres (Neon) — persistência das 4 tabelas, acessado exclusivamente pelo apps/api

### Critical Pitfalls

1. Link de WhatsApp quebrado (9º dígito/formatação) — normalizar telefone para dígitos puros + DDI 55 no backend antes de montar wa.me; nunca confiar no texto mascarado do formulário.
2. Clipboard "copiar Pix" falha em WebView do Instagram/WhatsApp — chamar navigator.clipboard.writeText() como primeira instrução síncrona do handler de clique (sem await antes); sempre ter fallback de texto selecionável, nunca mostrar "Copiado!" otimista.
3. Cold start duplo (Render dorme + Neon suspende) — mitigar com keep-alive externo gratuito (UptimeRobot/cron-job.org a cada 10-14 min) tocando um endpoint que também faz query no banco; aceitar que é mitigação, não eliminação total.
4. Chave Pix errada é dinheiro irreversivelmente perdido — validação de formato por tipo (CPF/CNPJ com dígito verificador, e-mail, telefone E.164, UUID v4) antes de salvar; prévia formatada na tela de edição para reduzir erro de digitação.
5. Preview do link quebrado/cacheado no WhatsApp — meta tags via generateMetadata server-side (nunca client-side); OG image com cache-busting por parâmetro de versão baseado no updatedAt; testar colando o link real numa conversa de WhatsApp antes de qualquer divulgação.

## Constraint da Realidade de WebView (consolidado, cross-cutting)

Três documentos de pesquisa convergem no mesmo ponto: o cartão será majoritariamente aberto dentro do navegador embutido do Instagram/WhatsApp, não num Safari/Chrome "normal". Esse é exatamente o ambiente onde os dois botões centrais da tese do produto tendem a falhar:

- Clipboard API (copiar Pix) exige que a chamada aconteça de forma síncrona dentro do mesmo gesto de clique — no WebKit/Safari (motor de todo WebView em iOS) qualquer operação assíncrona antes quebra a permissão; alguns WebViews de terceiros bloqueiam a API por completo.
- Download do .vcf (salvar contato) frequentemente "cai no vazio" em WebViews restritos — o arquivo é baixado para um cache interno inacessível ou o clique simplesmente não dispara nada.

Mitigação aceita por PITFALLS/ARCHITECTURE: não tentar contornar via truques de redirect automático (esquemas customizados de URL — frágeis, maioria já não funciona de forma confiável). Em vez disso: fallback de UX explícito (texto selecionável "toque e segure para copiar" para o Pix; banner de "abra no navegador" para o .vcf) e teste manual obrigatório dentro do app real do Instagram antes de considerar a feature pronta — testar só em Chrome/Safari desktop não valida nada aqui. Confiança MÉDIA: não existe documentação oficial da Meta sobre o comportamento exato de WebView do Instagram/WhatsApp; a evidência é consenso de múltiplas fontes de comunidade + WebKit/Apple forums (esses últimos, sim, oficiais quanto à restrição de gesto da Clipboard API).

## Decisões que a Pesquisa Desafia ou Resolve (não silenciar)

### 1. Pix "copiar chave" vs. QR Pix estático (BR Code) — decisão em aberto, não resolvida aqui

O PROJECT.md já decidiu corretamente excluir Pix dinâmico com valor via PSP (exige webhook, conciliação, conta em adquirente — fora de escopo, correto). Mas a pesquisa (FEATURES.md) encontrou que o QR Pix estático ("Pix Copia e Cola" sem valor definido) é tecnicamente uma coisa diferente: é um payload EMV/TLV público do Banco Central (Manual BR Code), gerável sem PSP, sem webhook, sem conta em adquirente — só precisa da própria chave Pix mais nome do recebedor e cidade, e existem bibliotecas/algoritmos públicos (CRC16 + campos TLV) para montar essa string. Isso não contradiz a decisão já tomada sobre PSP/webhook, mas é uma opção de baixo custo que não foi avaliada explicitamente antes da decisão ser registrada.

Recomendação para a conversa de escopo: apresentar isso como opção real (baixo custo de implementação, mesma informação que "copiar chave" mas escaneável) e deixar o usuário decidir explicitamente se entra na v1 ou fica para v1.x — a pesquisa não resolve isso sozinha porque é uma decisão de produto, não um fato técnico.

### 2. Neon vs. Supabase — resolvido pela pesquisa, recomendação clara

O docs/specs/01-setup.md deixa essa escolha em aberto. STACK.md encontrou uma diferença operacional relevante: Neon hiberna o compute após 5 min de inatividade mas acorda sozinho (cerca de 500ms) na próxima query — invisível para o usuário final. Supabase pausa o projeto inteiro após 7 dias sem atividade de banco e não se autorecupera — precisa entrar no dashboard e clicar "Restore project" manualmente, e enquanto pausado o banco recusa toda conexão. Para este produto — cartão compartilhado, dono que pode não voltar ao dashboard por dias — isso significa que Supabase pode derrubar silenciosamente o link público em circulação (adesivo impresso, QR físico) até alguém notar. Recomendação: Neon. Confiança HIGH (documentação oficial de ambos).

### 3. Existência de concorrente brasileiro direto (Monocard) — muda a leitura do "espaço em branco"

O PROJECT.md enquadra a diferenciação apenas contra concorrentes americanos (Popl, HiHello, Blinq). FEATURES.md encontrou que Monocard já se posiciona como "a plataforma #1 de cartão de visita digital no Brasil", com Pix nativo, plano pago em R$9,90/mês (mesmo patamar planejado aqui) e alvo declarado incluindo freelancers/autônomos. A diferença real que sobrevive: Monocard trata WhatsApp como mais um módulo/link entre outros, não como CTA central, e seu modelo de negócio empurra para hardware NFC físico + CRM (empresas) — o oposto da simplicidade radical que este produto propõe. O diferencial correto a comunicar não é "ter Pix" — é WhatsApp como CTA central + simplicidade radical sem hardware/CRM/múltiplos perfis. Vale ajustar o "Context" do PROJECT.md para refletir isso na próxima revisão.

## Implications for Roadmap

Baseado na pesquisa combinada — especialmente na conclusão de ARCHITECTURE.md de que página pública + ISR + pré-aquecimento + QR formam um único bloco de trabalho (a mitigação de cold start é parte da definição de "pronto", não um hardening posterior) — sugere-se a seguinte estrutura de fases:

### Phase 1: Fundação (setup + auth + CRUD)
Rationale: nada existe sem conta e sem dado persistido; specs 01/02 já escrevem o caminho, é a base técnica que tudo depende.
Delivers: monorepo funcionando, migrations, health check, register/login/me, CRUD de Card/SocialLink no apps/api (incluindo GET /cards/{slug} público e validação de slug reservado desde já — barato de adicionar aqui, evita retrabalho).
Addresses: requisitos de conta/dashboard do PROJECT.md.
Avoids: Pitfall 9 (impersonação) parcialmente — lista de slugs reservados já nasce nesta fase.

### Phase 2: Dashboard mínimo de edição
Rationale: o dono precisa conseguir criar/editar o próprio cartão antes de existir algo para publicar.
Delivers: formulário funcional (sem polimento visual) para nome/cargo/empresa/foto/WhatsApp/Pix/links sociais.
Addresses: "Dono cria e edita seu cartão pelo dashboard" (PROJECT.md).
Avoids: Pitfall 1 (normalizar telefone), Pitfall 2 (validar formato de Pix por tipo), Pitfall 3 (aviso de exposição de CPF) — devem entrar nesta fase, não depois, porque é onde os campos são criados.

### Phase 3: Cartão público + ISR + pré-aquecimento + QR (bloco único)
Rationale: ARCHITECTURE.md é explícito — não faz sentido dar como "pronta" a página pública sem a mitigação de cold start, porque a definição de "pronto" deste produto inclui a experiência de quem escaneia o QR na hora, no celular. Tratar como 3 fases separadas criaria a ilusão de que o cartão "funciona" antes de resolver o cold start, que é justamente quando ele mais precisa funcionar (uso presencial).
Delivers: página pública /[slug] mobile-first, revalidate configurado, generateStaticParams com slugs conhecidos, pré-aquecimento no save (fetch à própria URL pública após revalidatePath), keep-warm externo (ping 10-14 min), geração de QR (tela + download SVG/PNG).
Addresses: "Cartão público acessível por slug", "QR code gerado" (ambos P1 em FEATURES.md; QR é o corte prioritário do PROJECT.md).
Avoids: Pitfall 7 (cold start duplo Render+Neon) — mitigação central desta fase, não um adendo.

### Phase 4: Compartilhamento e complementos (WhatsApp, Pix, .vcf, OG image)
Rationale: dependem do cartão público já existir (mesma leitura de dados); podem entrar em paralelo/logo após a Fase 3 sem bloquear o "cartão no ar".
Delivers: botão WhatsApp (wa.me normalizado), botão copiar Pix (com fallback de texto selecionável), botão salvar contato (.vcf com detecção de in-app browser e orientação de fallback), OG image dinâmica com cache-busting.
Addresses: table stakes restantes de FEATURES.md; recomenda-se avaliar aqui as duas adições de baixo custo (compartilhar via wa.me/?text=, copiar link) para a conversa de escopo com o usuário.
Avoids: Pitfall 4 (Clipboard em WebView), Pitfall 5 (.vcf em WebView), Pitfall 6 (preview OG quebrado/cacheado) — todos exigem teste manual dentro do app real do Instagram, não só em navegador desktop.

### Phase 5: View tracking + marca/upgrade intent
Rationale: é o gancho de validação (métrica de negócio), não o que faz o cartão existir — pode vir depois que o cartão já estiver publicado e recebendo tráfego real. Também depende do nome/domínio do produto estar resolvido (pendência bloqueante do PROJECT.md) para o rodapé de marca fazer sentido.
Delivers: Client Component com sendBeacon pós-hidratação, endpoint POST /cards/{slug}/views com filtro de User-Agent conhecido e dedupe por cookie/dia, rodapé is_branded, botão "remover marca" registrando intenção.
Addresses: "Registro de visualizações", "Marca do produto", "Botão remover marca" (PROJECT.md).
Avoids: Anti-Padrão 1 de ARCHITECTURE.md (contar view dentro do render cacheado) — a decisão de arquitetura (beacon client-side) já resolve isso por design.

### Phase Ordering Rationale

- A ordem segue estritamente as dependências de FEATURES.md ("Feature Dependencies"): QR requer cartão público, que requer Card criado, que requer login.
- Fase 3 é deliberadamente um bloco único (não 3 fases) porque ARCHITECTURE.md identifica que separar a mitigação de cold start da página pública criaria uma falsa sensação de "pronto".
- Fase 5 vem por último porque depende de uma pendência externa (nome/domínio) e porque é o gancho de aprendizado/validação, não o caminho crítico de "cartão no ar" definido no PROJECT.md.
- Upload de foto via Vercel Blob foi deliberadamente omitido da estrutura de fases acima como bloco isolável — pode entrar em qualquer fase (2 ou 4) com placeholder de avatar padrão até lá, sem bloquear nada.

### Research Flags

Phases likely needing deeper research during planning:
- Phase 3 (cartão público + ISR + cold start): é a decisão mais arriscada do projeto segundo ARCHITECTURE.md; validar localmente o comportamento de precedência de rotas estáticas vs. dinâmicas (/login vs /[slug]) antes de confiar nisso, e confirmar na prática que a combinação ISR + pré-aquecimento + keep-warm é suficiente.
- Phase 4 (WhatsApp/Pix/.vcf em WebView): comportamento de in-app browser não tem documentação oficial da Meta — exige teste manual real (não simulação) dentro do app do Instagram/WhatsApp antes de considerar pronto.

Phases with standard patterns (skip research-phase):
- Phase 1 (fundação/auth): já especificado em detalhe nas specs 01/02, padrão bem documentado.
- Phase 2 (dashboard/CRUD): CRUD simples sobre schema já definido, sem ambiguidade técnica relevante.
- Phase 5 (view tracking): padrão de sendBeacon + filtro de UA é direto, bem documentado.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM-HIGH | Versões verificadas diretamente no npm/NuGet registry; comportamento de Neon/Supabase/Render confirmado em documentação oficial. Escolhas de "60s de revalidate" e "não usar lib de vCard" são julgamento de engenharia, não fato verificável. |
| Features | MEDIUM | Achados de mercado BR combinam fontes primárias (sites de produto) com WebSearch — WebFetch direto bloqueado (403) para Monocard, dados vêm de snippets cruzados. Achados internacionais (Popl/HiHello/Blinq) são bem documentados via múltiplas fontes convergentes. |
| Architecture | MEDIUM-HIGH | Estrutura macro já travada pelas specs; mecanismos de Next.js (ISR, generateStaticParams, precedência de rotas) confirmados em documentação oficial. Comportamento de crawlers do WhatsApp não executando JS é consenso técnico sem fonte oficial única da Meta. |
| Pitfalls | MEDIUM-ALTA | Comportamento de Clipboard API/WebKit é ALTA confiança (fonte oficial WebKit + Apple forums). Comportamento de WebView do Instagram/WhatsApp, cache de preview do WhatsApp, e cold start do Render são MÉDIA confiança — sem SLA/documentação oficial, mas múltiplas fontes de terceiros convergem. |

Overall confidence: MEDIUM-HIGH

### Gaps to Address

- Decisão de Pix estático (BR Code) não resolvida — precisa ser levada explicitamente à conversa de escopo com o usuário, não decidida pela pesquisa (ver seção "Decisões que a Pesquisa Desafia").
- Comportamento exato de WebView do Instagram/WhatsApp — não há documentação oficial da Meta; a mitigação (fallback de UX + teste manual) é sólida, mas o comportamento específico só se confirma testando em dispositivos reais durante a implementação da Fase 4.
- Cobertura de glifos latinos (ã, ç, õ) na fonte usada em next/og — precisa de teste manual explícito com nomes reais brasileiros antes de considerar a OG image pronta; não é algo que se garante só lendo documentação.
- Precedência de rotas estáticas vs. dinâmicas no App Router (/login vs /[slug]) — comportamento esperado é MEDIUM-HIGH confidence, mas ARCHITECTURE.md recomenda um teste local de 5 minutos antes de confiar nisso estruturalmente.
- Tempo real de cold start do Render em 2026 — não há SLA oficial; os números usados (30-60s) vêm de fontes de terceiros e podem variar; vale medir na prática após o primeiro deploy.

## Sources

### Primary (HIGH confidence)
- Neon Docs — Compute lifecycle (neon.com/docs/introduction/compute-lifecycle) — autosuspend e comportamento de wake
- Supabase Docs — Project Pausing (supabase.com/docs/guides/platform/free-project-pausing) — pausa após 7 dias, restauração manual
- Vercel Blob — Client Uploads (vercel.com/docs/vercel-blob/client-upload) — upload direto do browser
- Next.js — Dynamic Segments / ImageResponse docs (nextjs.org/docs/app/api-reference/file-conventions/dynamic-routes) — mecanismo de ISR, generateStaticParams, fontes em ImageResponse
- WebKit Blog — Async Clipboard API + Apple Developer Forums — restrição de gesto síncrono
- npm/NuGet registry (verificado 13/08/2026) — versões de qrcode, cpf-cnpj-validator, Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore

### Secondary (MEDIUM confidence)
- Monocard, Cartão Plus, InfinitePay Link na Bio — WebFetch/WebSearch, dados de produto próprio (concorrência BR)
- Blinq/HiHello/Popl comparação — conteúdo de marketing de concorrente, convergente com outras fontes
- Comportamento de crawlers de preview (WhatsApp/facebookexternalhit) não executando JS — consenso de comunidade, sem fonte oficial única
- Cold start do Render (30-60s) — agregado de fontes de terceiros, sem SLA oficial
- Payload EMV/BR Code estático — fonte técnica detalhada (TabNews) + múltiplas ferramentas públicas convergentes

### Tertiary (LOW confidence)
- Taggo, CardU, Airgo, Digital Card BR, Dvisit, bCard, Carda — apenas snippets de busca, não fonte primária completa
- V1CE/Mobilo/Dot — blogs de marketing dos próprios concorrentes

---
Research completed: 2026-08-13
Ready for roadmap: yes
