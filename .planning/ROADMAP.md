# Roadmap: vCard App (nome a definir)

## Overview

A jornada vai de "ninguém tem conta" até "o cartão está no ar, sendo usado de verdade e gerando sinal de validação de upgrade" em 4 fases verticais. A Fase 1 estabelece a fundação inevitável (conta + cartão completo e validado) porque nada mais pode ser vertical sem ela. A partir daí, cada fase entrega uma capacidade ponta a ponta completa para um tipo de usuário: a Fase 2 faz o cartão existir de verdade no mundo (acessível, rápido mesmo com infra fria, com QR pronto para circular) — tratada como bloco único porque a mitigação de cold start é parte da definição de "pronto", não um retoque posterior. A Fase 3 entrega as ações que cumprem a tese do produto (chamar e pagar em um toque, inclusive dentro de navegadores embutidos do Instagram/WhatsApp, onde a maioria do tráfego chega). A Fase 4 fecha com o gancho de aprendizado do negócio (visualizações reais, marca do plano grátis, sinal de intenção de upgrade) — depende do domínio já estar resolvido (Fase 2) e do cartão já estar circulando (Fase 3).

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Conta e Cartão** - Dono cria conta e monta seu cartão completo (identidade, contato, Pix, foto, links sociais), tudo validado no momento da criação
- [ ] **Phase 2: Cartão Público no Ar** - Cartão público acessível por slug, resiliente a cold start (ISR + pré-aquecimento + keep-alive), com QR pronto para circular, e domínio próprio do produto resolvido
- [ ] **Phase 3: Contato, Pagamento e Compartilhamento** - Visitante consegue chamar no WhatsApp, copiar o Pix, salvar o contato e acessar os links — inclusive dentro de navegadores embutidos — e o link circula com preview correto
- [ ] **Phase 4: Aprendizado e Monetização** - Dono vê quantas visitas o cartão recebeu, o cartão grátis exibe a marca do produto, e o clique em "remover marca" registra o sinal de intenção de upgrade

## Phase Details

### Phase 1: Conta e Cartão

**Goal**: Dono cria conta e monta seu cartão completo — identidade, canais de contato, Pix e links sociais — com todas as validações de segurança/formato aplicadas no momento em que os campos nascem
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: ACCT-01, ACCT-02, ACCT-03, ACCT-04, ACCT-05, CARD-01, CARD-02, CARD-03, CARD-04, CARD-05, CARD-06, CARD-07, CARD-08, CARD-09, CARD-10
**Success Criteria** (what must be TRUE):

  1. Visitante cria conta com e-mail/senha (hash BCrypt) e faz login recebendo um access token JWT
  2. Usuário permanece autenticado ao navegar e recarregar o dashboard, e é redirecionado para `/login` quando o token está ausente ou expirado; rotas de escrita de Card/SocialLink retornam 401 sem token válido
  3. Usuário cria seu cartão escolhendo um slug único (sistema rejeita slugs reservados e já em uso) e edita nome, cargo, empresa e foto
  4. Usuário cadastra telefone, e-mail e WhatsApp (normalizado para DDI 55 no momento de salvar) e chave Pix com validação de formato por tipo, prévia formatada e aviso reforçado de exposição pública quando o tipo é CPF
  5. Usuário adiciona, remove e reordena seus links sociais (Instagram, LinkedIn, Twitter, TikTok, YouTube, site)

**Plans**: 7 plans

Plans:
**Wave 1**

- [x] 01-01-PLAN.md — Esqueleto: apps/api, Postgres migrado, cadastro/login/JWT reais

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 01-02-PLAN.md — Esqueleto: apps/web, sessão em localStorage, criar conta e permanecer logado

**Wave 3** *(blocked on Wave 2 completion)*

- [ ] 01-03-PLAN.md — Slice: criar o cartão com slug único (reservados + unicidade no banco) e identidade

**Wave 4** *(blocked on Wave 3 completion)*

- [ ] 01-04-PLAN.md — Slice: canais de contato com WhatsApp normalizado para DDI 55

**Wave 5** *(blocked on Wave 4 completion)*

- [ ] 01-05-PLAN.md — Slice: chave Pix validada por tipo com consentimento de CPF verificado no servidor

**Wave 6** *(blocked on Wave 5 completion)*

- [ ] 01-06-PLAN.md — Slice: foto do cartão via Vercel Blob e placeholder de iniciais

**Wave 7** *(blocked on Wave 6 completion)*

- [ ] 01-07-PLAN.md — Slice: links sociais reordenáveis + passagem manual end-to-end da fase

**UI hint**: yes

### Phase 2: Cartão Público no Ar

**Goal**: O cartão existe de verdade no mundo — qualquer pessoa acessa `/[slug]` sem autenticação, com carregamento rápido mesmo se o backend/banco estiverem frios, e o dono tem um QR pronto para colocar em circulação
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: PUB-01, PUB-02, PUB-03, PUB-04, PUB-05, PUB-06, SHARE-01, SHARE-02, BRAND-01
**Success Criteria** (what must be TRUE):

  1. Qualquer pessoa acessa o cartão em `/[slug]` sem autenticação, em layout mobile-first, servido por ISR sem depender do backend estar acordado a cada visita
  2. Cartão recém-criado ou recém-editado é pré-aquecido no momento do save e o keep-alive externo mantém backend e banco acordados, para que o primeiro acesso ao slug (ex: alguém escaneando um QR impresso) não caia em cold start de 30-60s
  3. Edição feita no dashboard se reflete no cartão público sem exigir novo deploy, e slug inexistente retorna página 404 própria
  4. Dono visualiza o QR code do seu cartão na tela em tamanho utilizável para escanear na hora, e baixa em resolução adequada para impressão
  5. Produto tem nome definido e domínio próprio registrado, apontando para o frontend em produção

**Plans**: TBD
**UI hint**: yes

### Phase 3: Contato, Pagamento e Compartilhamento

**Goal**: Quem recebe o cartão consegue chamar ou pagar o dono em um toque — inclusive de dentro do navegador embutido do Instagram/WhatsApp, onde a maior parte do tráfego chega — e o link circula com preview correto
**Mode:** mvp
**Depends on**: Phase 2
**Requirements**: CONT-01, CONT-02, CONT-03, CONT-04, CONT-05, PAY-01, PAY-02, PAY-03, SHARE-03, SHARE-04, SHARE-05, SHARE-06
**Success Criteria** (what must be TRUE):

  1. Visitante toca no botão de WhatsApp e abre conversa direta com o dono do cartão
  2. Visitante toca em "salvar contato" e baixa um `.vcf` que importa corretamente na agenda em iOS e Android (com acentuação preservada), recebendo orientação visível para abrir no navegador quando o download não funciona dentro de um navegador embutido
  3. Visitante copia a chave Pix com um toque, mesmo em navegador embutido onde a API de clipboard é bloqueada (fallback de texto selecionável), e só vê a confirmação de "copiado" depois da cópia realmente confirmada
  4. Visitante acessa os links sociais do dono na ordem definida por ele
  5. Link do cartão compartilhado no WhatsApp exibe preview correto (imagem, título, descrição) e reflete edições sem ficar preso em cache antigo; dono compartilha o próprio cartão via WhatsApp com o link já preenchido e copia a URL com um toque

**Plans**: TBD
**UI hint**: yes

### Phase 4: Aprendizado e Monetização

**Goal**: O dono vê o retorno real do cartão em circulação (visitas) e o produto começa a medir intenção de pagar, sem processar cobrança
**Mode:** mvp
**Depends on**: Phase 3
**Requirements**: VIEW-01, VIEW-02, VIEW-03, BRAND-02, UPG-01, UPG-02, UPG-03
**Success Criteria** (what must be TRUE):

  1. Sistema registra uma visualização quando uma pessoa real abre o cartão público, sem inflar a contagem por crawlers de preview ou prefetch
  2. Dono vê a contagem total de visualizações do seu cartão no dashboard
  3. Cartão do plano grátis exibe a marca do produto no rodapé, com link para a home
  4. Dono vê no dashboard a opção "remover marca — R$9,90/mês" com posicionamento visível, e ao clicar tem a intenção de upgrade registrada de forma consultável (sem cobrança) e recebe confirmação de que foi registrado

**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Conta e Cartão | 2/7 | In Progress|  |
| 2. Cartão Público no Ar | 0/TBD | Not started | - |
| 3. Contato, Pagamento e Compartilhamento | 0/TBD | Not started | - |
| 4. Aprendizado e Monetização | 0/TBD | Not started | - |
