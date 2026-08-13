# Pitfalls Research — Cartão de Visita Digital (BR)

**Domínio:** Cartão de visita digital brasileiro (Next.js + .NET + Postgres), WhatsApp e Pix como canais de primeira classe
**Pesquisado em:** 2026-08-13
**Confiança geral:** MÉDIA-ALTA — a maioria dos comportamentos de WhatsApp/in-app browser não tem documentação oficial da Meta; a evidência vem de múltiplas fontes de comunidade convergentes, docs oficiais da Neon/Vercel/Render, WebKit/Apple forums e RFCs de vCard. Marcado por item.

---

## Critical Pitfalls

### Pitfall 1: Link do WhatsApp quebrado pelo "nono dígito"

**O que dá errado:**
O botão de WhatsApp não abre a conversa, ou abre a conversa errada, porque o número foi salvo/formatado sem o 9º dígito (celulares brasileiros desde 2012–2016 têm 9 dígitos após o DDD) ou com caracteres não numéricos (`(11) 99999-9999` em vez de `5511999999999`).

**Por que acontece:**
O formato exigido pelo `wa.me` e `api.whatsapp.com` é DDI (55) + DDD (2 dígitos) + número, **apenas dígitos, sem `+`, espaço, parênteses ou hífen**. Se o usuário digita o número com máscara visual no formulário do dashboard e o campo salva o texto exatamente como veio do input (com parênteses/traço), o link gerado quebra silenciosamente — o WhatsApp Web/app às vezes tenta interpretar mesmo assim, mas o comportamento é inconsistente entre iOS e Android.

**Como evitar:**
- Normalizar o telefone no momento de salvar (não confiar no que o crawler/link vai receber): stripar tudo que não é dígito, garantir prefixo `55`, validar que o total é 12 dígitos (fixo) ou 13 dígitos (móvel com 9º dígito) via regex simples no backend.
- Gerar o link sempre como `https://wa.me/<somente_digitos>` (preferir `wa.me` a `api.whatsapp.com/send` — mesmo destino, mas `wa.me` é o link "oficial" curto documentado pela própria Meta e o mais reconhecido por usuários).
- Mensagem pré-preenchida via `?text=`: usar `encodeURIComponent`, não concatenação manual — texto com acento (`Olá, vim pelo seu cartão!`) precisa ser URL-encoded corretamente ou quebra em alguns navegadores Android.
- Fallback de UX: mostrar o número formatado ao lado do botão, para o caso raro do link falhar, a pessoa ainda discar manualmente.

**Sinais de alerta:**
Testar o botão em pelo menos 2 aparelhos reais (1 iOS, 1 Android) com WhatsApp instalado, incluindo um número de teste com DDD de 2 dígitos e 9 na frente do celular. Se o botão abre o WhatsApp mas cai na tela de "novo contato" em vez da conversa, o número está malformado.

**Fase:** Página pública do cartão (implementação do botão de WhatsApp)
**Severidade:** Crítico — é o botão mais importante do produto; se quebra, quebra a tese inteira.

---

### Pitfall 2: Chave Pix errada ou mal digitada — perda de dinheiro irreversível

**O que dá errado:**
O dono do cartão digita a chave Pix errada (typo, ou copia/cola de outra chave, ou seleciona o `pix_key_type` errado — ex.: marca "telefone" mas cola um e-mail). Quem recebe o cartão copia a chave e faz uma transferência Pix que vai para uma conta de terceiro. Diferente de um erro de UX comum, **Pix não tem chargeback** — é dinheiro perdido de verdade, e o produto foi o intermediário que exibiu a chave errada.

**Por que acontece:**
O produto não valida a chave contra o Bacen (isso exigiria integração com DICT/PSP, fora de escopo). Sem qualquer validação de formato, um campo de texto livre aceita qualquer string.

**Como evitar (validação de formato, não de titularidade — isso é o que cabe em 2 semanas):**
- Validar o **formato** de cada tipo antes de salvar:
  - CPF: 11 dígitos + dígito verificador válido (algoritmo módulo 11 — trivial de implementar, poucas linhas).
  - CNPJ: 14 dígitos + dígito verificador válido (mesmo algoritmo, mais dígitos).
  - Telefone: formato E.164 (`+55` + DDD + número, 12-13 dígitos).
  - Email: regex padrão de e-mail.
  - Aleatória (EVP): UUID v4 (`xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`).
- Mostrar uma **prévia formatada** da chave na tela de edição antes de salvar ("Você está cadastrando: CPF 123.456.789-00 — confirma?") — reduz erro de digitação por confirmação visual, é barato de implementar.
- Na página pública, mostrar o tipo da chave ao lado do valor (`Pix: CPF`) para quem vai pagar conseguir notar uma inconsistência óbvia (ex. um "CPF" com formato de e-mail).
- **Não** tentar validar titularidade (nome bate com CPF) — isso exigiria PSP/Receita Federal e está corretamente fora do escopo de 2 semanas. Documentar isso como limitação conhecida.

**Sinais de alerta:**
Se o campo de Pix no dashboard aceita qualquer string sem nenhuma validação de formato antes do "salvar", esse pitfall não foi endereçado.

**Fase:** Dashboard (criação/edição do cartão) — validação deve entrar na mesma fase em que o campo é criado, não depois.
**Severidade:** Crítico — é o único ponto do produto onde um bug de software vira perda financeira real e irreversível para terceiros.

---

### Pitfall 3: CPF como chave Pix exposta publicamente — questão de LGPD

**O que dá errado:**
Quando `pix_key_type = cpf`, o CPF do dono do cartão fica exposto em uma página **pública, sem autenticação, indexável por buscadores**, junto com nome completo, foto e outros dados. CPF é dado pessoal (LGPD, art. 5º, I) — não é dado sensível por si só, mas sua exposição combinada com nome completo e foto aumenta risco de uso indevido (golpes de engenharia social, abertura de conta em nome de terceiro, negativação fraudulenta). O ponto crítico: **quem expõe o CPF é o próprio dono do cartão, mas a plataforma é quem processa e disponibiliza publicamente esse dado** — o que traz responsabilidade de controlador de dados (LGPD art. 6º, princípios de minimização e transparência).

**Por que acontece:**
O Bacen permite CPF como chave Pix porque no contexto de um app bancário a chave só é visível para quem já está no fluxo de pagamento (não é indexada, não é pública por padrão). Nesse produto, a chave vira conteúdo de uma página web pública — um contexto de exposição completamente diferente do desenhado pelo Bacen.

**Avaliação concreta (o que fazer em 2 semanas, honesto sobre o que não cabe):**
- **Não é viável nem correto bloquear CPF como tipo de chave** — é uma decisão legítima do usuário sobre o próprio dado, e o produto já pede consentimento implícito ao permitir a escolha do tipo.
- **O mínimo obrigatório e barato:** adicionar um aviso inline no formulário, no momento em que o usuário escolhe `pix_key_type = cpf` ("Atenção: seu CPF ficará visível para qualquer pessoa que acessar seu cartão publicamente. Se preferir mais privacidade, use uma chave aleatória ou e-mail."). Isso é uma checkbox de affordance, não uma feature — custa menos de uma hora.
- **Mascaramento parcial é tentador mas não funciona para o propósito do produto**: se você mascarar o CPF (`123.***.***-00`), a pessoa não consegue copiar a chave completa para pagar — quebra a função central do botão. Não fazer.
- `noindex` na página pública **não resolve** o problema de exposição para quem recebe o link (só evita indexação no Google) — mas é barato e vale fazer de qualquer forma (`<meta name="robots" content="noindex">` seletivo, com cuidado para não quebrar o preview do WhatsApp, que não depende de indexação).
- Política de privacidade mínima mencionando que dados de contato/Pix inseridos pelo usuário ficam públicos por design do produto — necessário para adequação formal à LGPD (informar o titular do dado sobre o tratamento), e é só texto, cabe em 1-2 horas.
- **Fora de escopo de 2 semanas, mas documentar como dívida:** um mecanismo de "solicitar remoção de dados" (direito do titular, LGPD art. 18) — para v1, um e-mail de contato no rodapé já atende o mínimo legal.

**Sinais de alerta:**
Se o formulário de cadastro de Pix não tem nenhum texto de aviso quando o tipo é CPF, e não existe nenhuma política de privacidade mencionando que os dados do cartão são públicos, isso é lacuna de conformidade a resolver antes do lançamento (não depois).

**Fase:** Dashboard (criação/edição do cartão) + página pública — o aviso deve entrar junto com o campo de Pix.
**Severidade:** Alto — não é bloqueador técnico, mas é o tipo de risco que pode gerar reclamação formal ou dano de reputação se ignorado. Confiança: MÉDIA (a lei não proíbe o uso, mas a doutrina e a ANPD reforçam o dever de transparência e minimização — não há jurisprudência específica sobre "CPF como chave Pix em cartão de visita público").

---

### Pitfall 4: Clipboard "copiar" não funciona dentro do navegador do Instagram/WhatsApp

**O que dá errado:**
O botão "copiar chave Pix" (a segunda função mais importante do produto) simplesmente não copia nada, ou lança um erro silencioso, quando o cartão é aberto dentro do navegador embutido do Instagram ou do WhatsApp (in-app browser / WebView) — que é exatamente onde a maior parte do tráfego vai chegar, segundo a própria tese do produto (bio do Instagram, link no WhatsApp).

**Por que acontece:**
A Clipboard API (`navigator.clipboard.writeText`) no Safari/WebKit (que é o motor por trás de todo WebView em iOS, incluindo o do Instagram e WhatsApp por regra da Apple) **exige que a chamada aconteça dentro do mesmo gesto do usuário (clique/toque), de forma síncrona**. Se o código faz qualquer `await` antes de chamar `writeText` (ex.: uma chamada de analytics, ou até um `setState` que reagenda o handler), a Promise é rejeitada com `NotAllowedError` — o clique "perde" a permissão do gesto. Em WebViews de terceiros isso é ainda mais frágil porque alguns bloqueiam a API por completo ou implementam parcialmente.

**Como evitar:**
- Chamar `navigator.clipboard.writeText()` **como a primeira coisa dentro do handler de clique**, sem `await` antes.
- Sempre ter fallback com `document.execCommand('copy')` via um `<input>` temporário selecionado — funciona em mais WebViews antigos, mesmo sendo API deprecated.
- **Feedback visual obrigatório e imediato** ("Copiado!") só deve aparecer se a Promise resolveu — nunca mostrar sucesso otimista. Se falhar, mostrar a chave em texto selecionável com instrução "toque e segure para copiar" como plano B manual — isso é barato e cobre 100% dos casos, inclusive navegadores muito restritivos.
- Testar especificamente abrindo o link de dentro do app do Instagram (não só no Safari/Chrome) — o comportamento é visivelmente diferente.

**Sinais de alerta:**
Se o teste de "copiar chave" só foi feito em Chrome/Safari desktop ou mesmo mobile "normal", esse pitfall não foi validado — o ambiente real de uso é o WebView.

**Fase:** Página pública do cartão (botão de copiar Pix)
**Severidade:** Crítico — junto com o WhatsApp, é a segunda ação mais importante da página; e o ambiente onde mais falha (in-app browser) é o canal de distribuição principal do produto.

---

### Pitfall 5: Download do `.vcf` não funciona dentro do navegador do Instagram

**O que dá errado:**
O botão "salvar contato" (que baixa o `.vcf`) não faz nada, baixa um arquivo que o usuário não consegue abrir, ou abre uma tela genérica de "não é possível baixar" quando o cartão é acessado de dentro do app do Instagram (bio) ou de outros WebViews restritos.

**Por que acontece:**
WebViews embutidos (especialmente no iOS) frequentemente não implementam o fluxo completo de download de arquivo com `Content-Disposition: attachment` — o navegador do sistema (Safari) sabe empacotar isso para o app Contatos, mas o WebView do Instagram não tem essa integração e o download "cai no vazio" (o arquivo é baixado para um cache interno inacessível, ou o clique simplesmente não dispara nada). Esse é um problema documentado e recorrente para qualquer tipo de arquivo "abrir com outro app" (`.ics`, `.vcf`) dentro de in-app browsers, não uma peculiaridade nossa.

**Como evitar (mitigação, não solução perfeita — é uma limitação de plataforma real):**
- Detectar user-agent de in-app browser conhecido (string contém `Instagram`, `FBAN`/`FBAV`, `Line`, etc.) e, quando detectado, mostrar um banner acima do botão: "Para salvar o contato, toque em ⋮ (menu) e escolha 'Abrir no navegador'" — orientar a saída em vez de tentar contornar via truque de redirect automático (esses truques — `intent://`, `x-safari-https://` — são frágeis e a maioria já não funciona de forma confiável nas versões atuais de iOS/Android).
- Servir o `.vcf` com headers corretos independente do ambiente (`Content-Type: text/vcard; charset=utf-8` e `Content-Disposition: attachment; filename="nome.vcf"`) — isso já é o correto para quando o usuário está no Safari/Chrome real, que é a maioria do tráfego fora do Instagram.
- Não investir tempo tentando "resolver" o download dentro do WebView do Instagram via JS — não há uma solução garantida e universalmente aceita; o retorno sobre esforço não compensa dentro da janela de 2 semanas. Documentar como limitação conhecida e oferecer a saída manual.

**Sinais de alerta:**
Testar o botão de salvar contato abrindo o link do cartão a partir de um post/bio real no app do Instagram (não simulado no navegador) em iOS. Se o botão não faz nada visível e não há nenhuma orientação de "abrir no navegador", o problema não foi tratado.

**Fase:** Geração de `.vcf` / QR / compartilhamento
**Severidade:** Alto — o "salvar contato" é uma feature ativa do produto (spec confirma), mas o WhatsApp e o Pix continuam funcionando (parcialmente) mesmo se essa falhar; então é sério mas não paralisante como os pitfalls 1 e 4.

---

### Pitfall 6: Preview do link quebrado no WhatsApp (OG tags)

**O que dá errado:**
Ao colar o link do cartão numa conversa do WhatsApp, não aparece nenhum preview (card com imagem/título), ou aparece uma imagem cortada/distorcida, ou — pior — o preview mostra dados desatualizados de uma versão antiga do cartão porque o WhatsApp cacheou a versão anterior.

**Por que acontece (verificado, MÉDIA confiança — não há doc oficial da Meta, mas múltiplas fontes convergem):**
- O crawler do WhatsApp **não executa JavaScript**. As tags `<meta property="og:...">` precisam estar no HTML retornado na primeira resposta do servidor — o que bate com o plano de usar SSR/ISR na página `/[slug]`, mas quebraria se as meta tags fossem geradas client-side (ex.: via `useEffect` ou por um componente client sem `generateMetadata` do App Router).
- Exige HTTPS na URL e na imagem.
- Especificações de imagem: JPEG/PNG (evitar WebP/SVG por segurança, mesmo que alguns relatos digam que funciona — não arriscar), recomendação de pelo menos 1200×630px, abaixo de ~300KB, proporção não muito distante de 1.91:1 (aspect ratio muito esticado é cortado).
- **Cache agressivo e sem debugger oficial**: diferente do Facebook (tem Sharing Debugger) e LinkedIn (Post Inspector), o WhatsApp não oferece ferramenta para forçar re-scrape. Se o dono do cartão editar nome/foto/cargo depois de já ter compartilhado o link uma vez, o preview antigo pode ficar "grudado" para quem já recebeu ou reenviar o mesmo link exato.

**Como evitar:**
- Garantir que a página `/[slug]` gera as meta tags via `generateMetadata` (Next.js App Router, server-side) — não montar OG tags no client.
- Gerar uma OG image dinâmica por cartão (nome + foto + cargo) usando `@vercel/og` (Image Response API do Next.js, roda no Edge, grátis no Hobby dentro do limite de invocações) — isso cobre "identidade visual do preview" sem precisar de design manual por cartão.
- Fixar dimensão exata 1200×630 e formato PNG/JPEG para a imagem gerada.
- Estratégia de cache-busting: incluir um parâmetro de versão na URL da imagem OG (`?v=<updated_at timestamp>`) para que, quando o cartão for editado, a imagem tenha uma URL nova e o WhatsApp precise buscar de novo — isso é a única mitigação confiável e é barata de implementar (é literalmente 1 query param).
- Para o link em si (a URL do cartão), como o WhatsApp cacheia por URL exata, considerar documentar para o usuário: "se você editar o cartão depois de já ter compartilhado, pode levar um tempo para o preview atualizar em conversas antigas" — expectativa correta é mais barata que engenharia.

**Sinais de alerta:**
Colar o link real (não localhost) numa conversa de WhatsApp entre dois celulares de teste, antes do lançamento, e conferir se aparece preview com imagem e título. Se meta tags foram implementadas num client component sem `generateMetadata`, isso vai falhar silenciosamente.

**Fase:** Página pública do cartão (deve ser testado antes de qualquer divulgação real, já que é o canal de aquisição orgânica do produto)
**Severidade:** Crítico para o canal de distribuição (WhatsApp/Instagram) mesmo não sendo um "bug" que trava o produto — o próprio PROJECT.md já identifica isso como "metade da primeira impressão".

---

### Pitfall 7: Cold start duplo — Render dorme e Neon suspende ao mesmo tempo

**O que dá errado:**
Alguém escaneia o QR code impresso (situação presencial, a pior hora para atraso) e a página demora 10-50+ segundos para carregar, porque o backend no Render "dormiu" por inatividade **e** o banco no Neon também suspendeu o compute — os dois cold starts não acontecem em paralelo de forma útil; o request no Render só desperta o processo, que então faz uma query que desperta o Neon, então os atrasos se somam (não seriam simultâneos porque um depende do outro).

**Por que acontece (verificado):**
- Render free tier: serviço web dorme após 15 minutos de inatividade; o próximo request tem cold start de 30 a 60 segundos.
- Neon free tier: compute suspende após 5 minutos de inatividade (não é configurável no free tier); a reativação em si é rápida (300ms-1s), mas só acontece depois que uma query chega — ou seja, some **depois** do Render já ter acordado, não em paralelo.
- Resultado prático: no pior caso (ambos dormindo), o tempo total até a primeira renderização pode passar de 30-60 segundos — inviável para alguém esperando a página abrir na frente de outra pessoa (o cenário de uso presencial mais comum do produto).

**Como evitar (mitigações reais dentro do orçamento zero):**
- **Ping de keep-alive gratuito**: configurar um serviço externo (UptimeRobot, cron-job.org, ambos free) para bater no health check do backend a cada 10-14 minutos. Isso evita o Render dormir (mantém o serviço "quente") e, como efeito colateral, mantém a conexão com o Neon ativa também (evitando o autosuspend de 5 min), desde que o health check faça pelo menos uma query trivial no banco (não só um `200 OK` estático).
- Isso é uma mitigação, não eliminação total — nos primeiros minutos após o deploy ou em picos de tráfego zero prolongado, ainda pode haver 1 cold start ocasional. Aceitável para v1 dado o orçamento zero, mas documentar a limitação.
- Evitar qualquer estratégia que dependa de manter conexões de banco abertas indefinidamente do lado do EF Core (ver Pitfall 8) — o keep-alive deve ser uma query leve e pontual, não uma conexão persistente.
- Página pública (`/[slug]`) deve, na medida do possível, não depender de round-trip síncrono ao backend .NET a cada view se puder usar ISR/cache do Next.js — revalidação periódica (ex. `revalidate: 60`) reduz a frequência com que o Next.js precisa chamar o backend "frio", absorvendo parte do problema no proprio Vercel.

**Sinais de alerta:**
Medir o tempo de resposta do backend depois de 20 minutos sem tráfego (simular manualmente). Se ultrapassar ~5 segundos de forma consistente, o keep-alive não está configurado ou não está sendo efetivo.

**Fase:** Deploy/Infra (configuração de Render + Neon)
**Severidade:** Alto — não impede o produto de funcionar, mas ataca diretamente o cenário de uso presencial (QR na tela ou impresso), que é um dos 4 canais centrais do produto segundo o PROJECT.md.

---

### Pitfall 8: Esgotamento de connection pool do Postgres (Neon/Supabase free) com EF Core

**O que dá errado:**
Sob qualquer pico de tráfego (mesmo pequeno — um cartão viralizando localmente, ou vários scans simultâneos de um QR em um evento), a aplicação começa a lançar `NpgsqlException: connection pool has been exhausted` ou erros de "too many connections" do próprio Postgres.

**Por que acontece:**
- O free tier do Neon e Supabase tem um limite de conexões diretas relativamente baixo (dezenas, não centenas) quando não se usa o **pooler** (PgBouncer) — a connection string "direta" do Postgres é fácil de copiar por engano em vez da connection string de pooling.
- Por padrão, o pool do Npgsql/EF Core (`Max Pool Size`) é 100 — mas isso é o pool *do processo .NET*, não do banco. Se o Render também escala (ou reinicia) instâncias, ou se o DbContext não é descartado corretamente (scoped lifetime incorreto, ou uso de `AddDbContext` fora do padrão scoped-per-request), conexões "vazam" e não voltam ao pool.
- Cold starts (Pitfall 7) pioram isso: uma conexão que ficou "presa" enquanto o compute do Neon suspendia pode ser tratada como válida pelo Npgsql mas estar morta do lado do servidor, gerando erros até o pool expirar essas conexões mortas.

**Como evitar (barato e correto desde o início, não é retrabalho depois):**
- Usar a **connection string de pooling** (via PgBouncer) fornecida pelo Neon/Supabase, não a conexão direta — é literalmente trocar qual string copiar do painel, custo zero.
- Manter `AddDbContext` (não `AddDbContextPool` sem entender a diferença) com lifetime scoped padrão do ASP.NET Core — cada request abre e fecha sua própria conexão; não guardar `DbContext` em singleton nem reutilizar entre requests.
- Configurar explicitamente um `Maximum Pool Size` mais conservador na connection string (ex. 10-20) alinhado ao limite do free tier, em vez de confiar no default de 100 — evita que a aplicação tente abrir mais conexões do que o Postgres aceita.
- Não é necessário (nem cabe em 2 semanas) implementar retry policies sofisticadas — apenas garantir a configuração correta de pooling resolve 90% do risco para o volume esperado de uma v1.

**Sinais de alerta:**
Rodar um teste simples de carga (mesmo manual, abrindo a página pública em 15-20 abas/dispositivos ao mesmo tempo) antes do lançamento. Se aparecer erro 500 relacionado a conexão, o pooling não está configurado corretamente.

**Fase:** Deploy/Infra (configuração do backend .NET + string de conexão)
**Severidade:** Médio para o volume esperado de uma v1 solo, mas Alto se o cartão do próprio dev "bombar" nas redes (é o teste real mencionado no PROJECT.md) — o pior momento para isso quebrar é exatamente quando o produto está sendo validado.

---

### Pitfall 9: Impersonação — alguém cria um cartão com o nome de outra pessoa e a própria chave Pix

**O que dá errado:**
Como não há verificação de identidade no cadastro (só e-mail/senha), nada impede que uma pessoa má-intencionada crie um cartão com `full_name = "João Silva"` (nome de outra pessoa, real ou fictício, conhecido do alvo) e coloque sua própria chave Pix. Se esse cartão for distribuído (QR falso colado sobre um QR real, ou link enviado se passando pela pessoa), quem recebe paga para a pessoa errada acreditando ser o titular legítimo — um vetor de fraude direto e específico deste produto (nenhum "link in bio" genérico tem esse risco porque não carrega dado financeiro).

**Por que acontece:**
O produto foi desenhado para ser rápido de criar (sem fricção) — o que é correto para conversão, mas significa que não há nenhum sinal de que "o dono do slug é quem diz ser".

**Como evitar (dentro do razoável para 2 semanas — não é viável eliminar o risco, só mitigar):**
- **Não é um problema que se resolve com verificação de identidade em 2 semanas** — isso exigiria KYC, fora de escopo total. A mitigação realista é reduzir a superfície de ataque, não eliminar o vetor.
- Reservar/bloquear slugs que podem ser usados para phishing de marca (nomes de bancos, "suporte", "atendimento", palavras associadas a golpe) numa lista simples de slugs reservados — mitigação barata (uma lista estática) contra o caso mais grosseiro.
- Rate limiting básico na criação de contas (mesmo que rate limiting de login esteja formalmente fora de escopo pela spec 02, criar múltiplas contas em sequência rápida do mesmo IP é um sinal de abuso em massa, não de uso normal) — mitigação de baixo custo (throttle simples por IP no endpoint de registro) que reduz criação em massa de cartões fraudulentos, mesmo que não resolva impersonação pontual.
- Isso é fundamentalmente um risco que **qualquer produto de "link + Pix" carrega** — o mesmo existiria com um Linktree + Pix colado manualmente na bio. Vale documentar isso como limitação conhecida e um item de termos de uso ("você é responsável pelas informações que insere; impersonação é violação dos termos e sujeita a remoção").
- Mecanismo de denúncia simples (um link/e-mail de "denunciar este cartão" na página pública) é barato (não precisa de fluxo automatizado, só um `mailto:` ou formulário simples) e dá ao dev um jeito de agir manualmente se algo for reportado — melhor investimento de tempo que qualquer verificação automática nesta janela.

**Sinais de alerta:**
Se não existe nenhuma lista de slugs reservados nem um canal de denúncia antes do lançamento, o produto está exposto ao cenário mais óbvio de abuso do próprio conceito.

**Fase:** Dashboard (criação de cartão / slug) + página pública (rodapé/denúncia)
**Severidade:** Alto — é o pitfall mais específico e mais sério deste produto em particular (a combinação identidade + dado financeiro público é o que o diferencia de um link-in-bio comum), mesmo sabendo que a solução completa está fora do orçamento de tempo.

---

## Technical Debt Patterns

| Atalho | Benefício imediato | Custo de longo prazo | Quando é aceitável |
|--------|--------------------|------------------------|---------------------|
| Salvar telefone/Pix como texto livre sem normalizar no backend | Rápido de implementar | Links de WhatsApp quebrados, chaves Pix inconsistentes (ver Pitfalls 1 e 2) | Nunca — normalizar custa poucas linhas e o custo de não fazer é alto |
| `localStorage` para token JWT (conforme já decidido na spec 02) | Simples, sem CORS/cookie cross-domain | Vulnerável a XSS (token acessível via JS); some se o navegador limpar dados (comum em WebView do Instagram/iOS) | Aceitável para v1 solo — já é decisão tomada na spec 02, mas documentar que sessão pode cair sozinha dentro de in-app browsers |
| Confiar em `wa.me` sem normalizar/validar antes de gerar o link | Menos código | Botão principal quebra silenciosamente para números malformados | Nunca — é o coração do produto |
| Não implementar rate limiting em nenhum endpoint público | Economiza tempo de dev | Scraping de todos os cartões, spam de criação de conta, custo de infra em picos | Aceitável só se houver rate limiting mínimo por IP na Vercel/CDN (gratuito, ex. Vercel Firewall básico) — não deixar 100% aberto |
| Gerar OG image estática genérica (sem foto/nome por cartão) | Zero esforço de implementação | Preview genérico reduz taxa de clique — enfraquece o canal de distribuição principal | Aceitável só como fallback temporário nos primeiros dias, não como decisão final |
| Pular teste em in-app browser real (só testar em Chrome/Safari "normal") | Economiza tempo | Os dois botões mais importantes (copiar Pix, WhatsApp) podem falhar exatamente no canal onde a maioria abre o cartão | Nunca — 15 minutos de teste manual no app do Instagram evita o pior cenário |

## Integration Gotchas

| Integração | Erro comum | Abordagem correta |
|------------|------------|---------------------|
| WhatsApp (`wa.me`) | Usar número com máscara/formatação visual no link | Normalizar para dígitos puros com DDI 55 antes de montar a URL; `encodeURIComponent` na mensagem |
| WhatsApp (preview OG) | Gerar meta tags client-side ou depender de client component | Usar `generateMetadata` (server-side, Next.js App Router); garantir HTML já vem com as tags na primeira resposta |
| Neon/Supabase (Postgres) | Copiar a connection string "direta" em vez da de pooling (PgBouncer) | Sempre usar a string com pooling fornecida no painel; configurar `Maximum Pool Size` explícito e conservador |
| Render (deploy backend) | Assumir que o serviço está sempre ativo | Configurar keep-alive externo (UptimeRobot/cron-job.org) batendo em endpoint que também toca o banco |
| Clipboard API | Chamar `writeText` depois de qualquer `await`/lógica assíncrona | Chamar como primeira instrução síncrona do handler de clique; sempre ter fallback de "toque e segure para copiar" |
| Vercel (Image Optimization / OG dinâmica) | Gerar imagem OG por request sem cache, estourando limite de invocações/imagens do Hobby | Usar `@vercel/og` com cache via query param estável (`?v=updatedAt`), não gerar em toda requisição |

## Performance Traps

| Armadilha | Sintomas | Prevenção | Quando quebra |
|-----------|----------|-----------|----------------|
| Cold start duplo Render + Neon | Página pública demora 30-60s+ após período ocioso | Keep-alive externo a cada ~10-14 min tocando backend + banco | Sempre que não houver tráfego por mais de 15 min — cenário comum fora de horário de pico para um produto novo |
| Pool de conexões sem limite alinhado ao free tier | Erros 500 esporádicos sob picos pequenos de tráfego simultâneo | Connection string com pooling + `Max Pool Size` conservador | A partir de ~15-20 acessos simultâneos no free tier, dependendo do plano exato |
| Limite de invocações de função na Vercel (Hobby: 100k/mês) | Deploy "pausa" sem aviso ao estourar o limite | Preferir ISR com `revalidate` longo na página pública em vez de SSR puro a cada view; monitorar uso no painel Vercel | Se o cartão viralizar (o próprio cenário de sucesso do produto) — vale a pena revisitar antes de qualquer campanha de divulgação maior |
| Limite de imagens otimizadas na Vercel (Hobby: 1.000/mês) | Otimização de imagem para de funcionar (fotos de perfil, OG images) | Cachear/gerar OG image só quando o cartão muda (via `updatedAt` no path/query), não a cada view | Poucas dezenas de cartões ativos já podem se aproximar do limite se cada view gerar otimização nova |

## Security Mistakes

| Erro | Risco | Prevenção |
|------|-------|-----------|
| CPF exposto como chave Pix sem aviso ao usuário | Exposição de dado pessoal em página pública/indexável; risco reputacional e de conformidade LGPD | Aviso inline no formulário ao escolher tipo CPF + política de privacidade mínima explicando que dados do cartão são públicos por design |
| Nenhuma validação de formato na chave Pix | Chave incorreta exibida publicamente → transferência para terceiro sem chargeback possível | Validação de formato por tipo (CPF/CNPJ com dígito verificador, e-mail, telefone E.164, UUID v4 para EVP) antes de salvar |
| Ausência de lista de slugs reservados | Slug squatting de marcas/termos sensíveis (phishing usando o domínio do produto) | Lista estática de slugs bloqueados (nomes de bancos, "admin", "suporte", palavras associadas a golpe) checada na criação |
| Nenhum mecanismo de denúncia de cartão | Impersonação (nome de terceiro + Pix do fraudador) sem canal de resposta | Link/e-mail simples de denúncia no rodapé da página pública, mesmo que o fluxo de moderação seja manual |
| Token JWT em `localStorage` acessível a qualquer script | XSS rouba o token; token também é perdido silenciosamente em in-app browsers que limpam storage | Já é decisão tomada (spec 02) — mitigar com CSP básico e curto tempo de vida do token (15-30 min, já definido) |

## UX Pitfalls

| Armadilha | Impacto no usuário | Abordagem melhor |
|-----------|---------------------|-------------------|
| Feedback "Copiado!" otimista mesmo quando a Promise falhou | Usuário acha que copiou a chave Pix, cola em outro app e é a chave errada/vazia — risco direto de erro de pagamento | Só mostrar sucesso após a Promise resolver; mostrar chave em texto selecionável como fallback garantido |
| Botão de salvar contato sem nenhuma orientação dentro de in-app browser | Usuário toca, "nada acontece", desiste — perde o dado de contato | Detectar in-app browser e mostrar instrução de "abrir no navegador" antes de tentar o download |
| Link do WhatsApp sem fallback visível do número | Se o link falhar (aparelho sem WhatsApp, número malformado), a pessoa não tem como contatar de outro jeito | Mostrar o número formatado como texto ao lado/abaixo do botão |
| Preview de link genérico ou ausente no WhatsApp | Quem recebe o link não clica — perda de conversão silenciosa, sem erro visível para o dono do cartão | OG image dinâmica por cartão + teste manual real antes de qualquer divulgação |

## "Parece Pronto Mas Não Está" — Checklist

- [ ] **Botão de WhatsApp:** parece pronto quando abre no seu próprio celular — verifique se o número foi normalizado no backend (sem máscara) e teste com número real de 9 dígitos em iOS e Android.
- [ ] **Botão de copiar Pix:** parece pronto quando testado em Chrome desktop — verifique especificamente dentro do navegador embutido do Instagram (não simulado, abrindo de um post/bio de verdade).
- [ ] **Preview de link (OG):** parece pronto quando abre bem no navegador — verifique colando a URL real numa conversa de WhatsApp entre dois celulares, não apenas inspecionando o HTML.
- [ ] **Download do `.vcf`:** parece pronto quando funciona no Safari — verifique dentro do WebView do Instagram e confirme que existe uma saída (orientação de abrir no navegador) quando falhar.
- [ ] **Cadastro de chave Pix:** parece pronto quando salva qualquer string — verifique se há validação de formato por tipo e aviso de exposição pública para CPF.
- [ ] **Deploy de backend:** parece pronto no dia do deploy (tudo "quentinho") — verifique o tempo de resposta depois de 20 minutos sem tráfego.
- [ ] **Slug do cartão:** parece pronto quando aceita qualquer texto — verifique se nomes reservados/óbvios de abuso estão bloqueados.

## Recovery Strategies

| Pitfall | Custo de recuperação | Passos |
|---------|------------------------|--------|
| Link de WhatsApp malformado já publicado em cartões existentes | BAIXO | Rodar uma migration/script único para normalizar todos os `whatsapp_number` existentes (stripar não-dígitos, garantir DDI 55) |
| Preview do WhatsApp cacheado com dado antigo | BAIXO-MÉDIO | Adicionar `?v=<timestamp>` na URL de compartilhamento a partir de agora; não há como forçar re-scrape de links já compartilhados anteriormente — aceitar e seguir em frente |
| Chave Pix errada já publicada e possivelmente já usada por alguém | ALTO | Não há recuperação técnica do dinheiro (Pix é irreversível); o produto só pode mitigar daqui pra frente com validação de formato + aviso; comunicar claramente nos termos de uso que a responsabilidade pela exatidão da chave é do dono do cartão |
| Cartão de impersonação já no ar | MÉDIO | Processo manual: receber denúncia, suspender o cartão (soft-delete ou flag `is_suspended`), sem necessidade de fluxo automatizado nesta fase |
| Connection pool esgotado em produção | BAIXO | Ajustar connection string (trocar para pooling) e redeploy — não requer mudança de schema nem migração de dados |

## Pitfall-to-Phase Mapping

| Pitfall | Fase de prevenção | Como verificar que foi prevenido |
|---------|--------------------|-------------------------------------|
| Link de WhatsApp quebrado (9º dígito, formatação) | Página pública do cartão | Testar botão em iOS e Android reais com número de 9 dígitos |
| Chave Pix errada/mal formatada | Dashboard (criação/edição do cartão) | Validação de formato por tipo bloqueia salvar chave inválida |
| CPF exposto publicamente (LGPD) | Dashboard (campo de Pix) + página pública | Aviso inline presente ao escolher tipo CPF; política de privacidade publicada |
| Clipboard não funciona em in-app browser | Página pública do cartão | Testar "copiar Pix" de dentro do app do Instagram real |
| Download `.vcf` falha em in-app browser | Geração de vCard/QR/compartilhamento | Testar "salvar contato" de dentro do app do Instagram real; confirmar orientação de fallback |
| Preview OG quebrado/cacheado no WhatsApp | Página pública do cartão (antes de qualquer divulgação) | Colar link real numa conversa de WhatsApp entre 2 celulares e conferir preview |
| Cold start duplo Render + Neon | Deploy/Infra | Medir tempo de resposta após 20 min de inatividade simulada |
| Esgotamento de connection pool | Deploy/Infra | Teste de carga manual com 15-20 acessos simultâneos sem erro 500 |
| Impersonação (nome de terceiro + Pix próprio) | Dashboard (slug) + página pública (rodapé) | Lista de slugs reservados ativa; canal de denúncia visível na página pública |

## Sources

- Community/blogs sobre formatação `wa.me` e nono dígito — [api-wa.me](https://api-wa.me/blog/o-que-e-wa-me), [Acessei — DDI obrigatório](https://acessei.com.br/blog/codigo-pais-link-whatsapp-ddi/) (MÉDIA confiança — sem doc oficial da Meta sobre wa.me para BR especificamente)
- Requisitos de OG/preview do WhatsApp — [LinkPreview.eu](https://www.linkpreview.eu/en/blog/fix-link-preview-whatsapp), [OpenGraphPlus — crawling behavior](https://opengraphplus.com/consumers/whatsapp/crawling) (MÉDIA confiança — engenharia reversa de comunidade, sem doc oficial da Meta)
- Comportamento de in-app browsers (Instagram/Facebook) — [Flyn — Instagram In-App Browser](https://www.flyn.to/blog/instagram-in-app-browser), [add-to-calendar-button issue #72](https://github.com/add2cal/add-to-calendar-button/issues/72), [InAppRedirect](https://www.inappredirect.com/blogs/how-to-bypass-instagram-s-in-app-browser-for-better-roas-with-in-app-redirect) (MÉDIA confiança — múltiplas fontes convergem no mesmo comportamento)
- Clipboard API e restrição de gesto no Safari/WebKit — [WebKit Blog — Async Clipboard API](https://webkit.org/blog/10855/async-clipboard-api/), [Apple Developer Forums #691873](https://developer.apple.com/forums/thread/691873), [Apple Developer Forums #772275](https://developer.apple.com/forums/thread/772275) (ALTA confiança — fonte oficial WebKit + Apple forums)
- Versões de vCard e compatibilidade iOS/Android — [Univik — vCard 2.1 vs 3.0 vs 4.0](https://univik.com/blog/vcard-21-vs-30-vs-40-differences/), [CorrectVCF](https://correctvcf.com/help/generate-correct-vcf-files/) (MÉDIA confiança — consistente com RFC 6350, mas fontes são blogs de terceiros, não a RFC diretamente)
- Autosuspend do Neon free tier — [Neon Docs — Connection errors](https://neon.com/docs/connect/connection-errors), [Neon FAQ — auto-pause](https://neon.com/faqs/postgres-hosting-options-auto-pause-database) (ALTA confiança — documentação oficial)
- Cold start do Render free tier — [Render Docs — Free tier](https://render.com/docs/free), [blog.samkiel.dev](https://blog.samkiel.dev/your-render-free-tier-is-not-broken-its-just-cold) (ALTA confiança para o comportamento geral — doc oficial confirma spin-down por inatividade)
- Limites do plano Hobby da Vercel — [deploywise.dev — Vercel Free Tier Limits 2026](https://deploywise.dev/blog/vercel-free-tier-limits-2026) (MÉDIA confiança — números específicos de terceiros, recomenda-se confirmar no painel Vercel antes do lançamento pois limites mudam com frequência)
- Erros de connection pool do Npgsql/EF Core — [npgsql/npgsql issues](https://github.com/npgsql/npgsql/issues/5156), [npgsql/efcore.pg #2890](https://github.com/npgsql/efcore.pg/issues/2890) (ALTA confiança — issues oficiais do próprio projeto Npgsql)
- Tipos e formato de chave Pix (EVP/UUID v4, validação por tipo) — [Blog Stark Bank — Chave aleatória do Pix](https://blog.starkbank.com/chave-aleatoria-do-pix/), [iugu — EVP Pix](https://www.iugu.com/blog/evp-pix) (MÉDIA confiança — consistente entre fontes de mercado de pagamentos brasileiro)
- LGPD e exposição de CPF — [Migalhas — CPF dado pessoal "especial"](https://www.migalhas.com.br/depeso/429450/seria-o-cpf-um-dado-pessoal-especial--dados-pessoais-em-documentos), [Serasa — dados sensíveis](https://www.serasa.com.br/blog/saiba-o-que-sao-dados-sensiveis-na-lgpd/) (MÉDIA confiança — CPF não é dado sensível por definição legal estrita, mas doutrina reforça risco de exposição combinada com outros dados; não há jurisprudência específica para este caso de uso)

---
*Pesquisa de pitfalls para: cartão de visita digital brasileiro (Next.js + .NET + Postgres)*
*Pesquisado em: 2026-08-13*
