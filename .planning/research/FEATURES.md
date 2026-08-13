# Feature Research

**Domínio:** Cartão de visita digital / link-in-bio para freelancers e autônomos no Brasil
**Pesquisado em:** 2026-08-13
**Confiança:** MEDIUM (achados de mercado BR combinam fontes primárias — sites de produto — com WebSearch; achados internacionais são bem documentados via múltiplas fontes convergentes)

## Achado Principal: Existe Concorrente Brasileiro Direto

Sim — **e é mais maduro do que o esperado.** Isso muda a leitura do "espaço em branco" do produto.

- **Monocard** (monocard.me) se apresenta como "a plataforma #1 de cartão de visita digital no Brasil". Já tem: campo de Pix nativo ("perfil dinâmico... incluindo Pix, Instagram, E-mail, Telefone"), NFC físico opcional, múltiplos perfis por contexto, integração com CRM para empresas, e plano pago em **R$9,90/mês** — o mesmo patamar de preço planejado para este produto. Alvo declarado inclui "freelancers, empreendedores e grandes organizações".
- **Monocard NÃO tem WhatsApp como campo/CTA de primeira classe** nas fontes encontradas — é tratado como mais um link/módulo entre outros (Instagram, e-mail, telefone), não como o canal principal de conversão. E o hardware NFC físico é parte central do modelo de negócio deles (cartão físico pago, "ganhe Monocards grátis todo ano"), o que empurra o produto para uma lógica de objeto físico + assinatura, não de link puro e gratuito.
- **Cartão Plus** (cartao.plus) combina cartão digital + link na bio + vitrine + WhatsApp, mas não tem Pix — pedidos são roteados só via WhatsApp, sem menção a pagamento.
- **InfinitePay "Link na Bio"** (infinitepay.io/link-na-bio) é o achado mais estratégico: é uma fintech grande brasileira oferecendo uma página de bio-link **gratuita** com Pix nativo (taxa zero) e parcelamento no cartão, mas **não é um cartão de visita pessoal** — não tem campos de nome/cargo/foto/empresa como identidade profissional, é uma "vitrine de vendas" genérica. O modelo de negócio deles é ganhar na taxa da transação, não na assinatura — o que explica por que não competem diretamente no nicho de "meu cartão profissional".
- Outros players BR (Taggo, CardU, Airgo, cartoes.digital, Digital Card BR) são majoritariamente **hardware-first** (NFC/PVC físico, pagamento único ~R$50-160) ou geradores de link estático sem conta/dashboard recorrente.

**Conclusão para o roadmap:** o ângulo diferenciador real não é "ser o primeiro cartão digital do Brasil com Pix" (Monocard já reivindica isso) — é ser o **primeiro cartão digital brasileiro que trata WhatsApp como CTA central de conversão** (não como mais um ícone de rede social) combinado com Pix simples, **sem vender hardware, sem CRM, sem múltiplos perfis** — ou seja, radicalmente mais simples e focado no autônomo solo do que Monocard, e mais "identidade profissional" do que o InfinitePay Link na Bio. O diferencial é posicionamento e simplicidade, não uma feature isolada que ninguém tem.

## Feature Landscape

### Table Stakes (Usuário Espera / Sem Isso o Produto Parece Quebrado)

Categoria **Cartão**

| Feature | Por Que é Esperado | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| Perfil com nome, cargo, empresa, foto | É o mínimo para "ser um cartão de visita" — todo concorrente (Monocard, HiHello, Blinq, Cartão Plus) tem isso como base | Baixa | Já no schema (`Card`) |
| Link público por slug próprio (`/gabriel`) | Todo produto do setor (Linktree, HiHello, Monocard) usa URL curta e memorizável como unidade de compartilhamento | Baixa | Já decidido; SSR/ISR para carregar rápido no celular |
| Renderização rápida e mobile-first | Quase 100% do tráfego chega via câmera de QR ou app de mensagem no celular — carregamento lento mata a primeira impressão | Média | Depende de Next.js SSR/ISR bem configurado, imagem otimizada |
| Customização básica de tema/cor | Todo concorrente pesquisado (Linktree, Beacons, Cartão Plus, Monocard) permite pelo menos escolher uma cor de destaque/tema — sem isso o cartão parece um template genérico | Baixa | Pode ser 1-2 temas prontos na v1, não precisa de editor completo |

Categoria **Contato**

| Feature | Por Que é Esperado | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| Botão de WhatsApp abrindo conversa direta | É a tese do produto — mas também é table stakes no Brasil: qualquer cartão digital sem isso perde para um post-it com número | Baixa | `wa.me/{numero}` — já no schema (`whatsapp_number`) |
| Botão "salvar contato" (.vcf) | HiHello, Blinq, QRLynx e praticamente todo concorrente internacional têm isso como recurso central ("save to contacts") | Baixa | Geração de `.vcf` sob demanda já especificada em `01-setup.md` |
| Links sociais ordenáveis (Instagram, LinkedIn, etc.) | Todo produto de link-in-bio (Linktree, Beacons) e cartão digital (Monocard, HiHello) tem isso; é a unidade básica do gênero | Baixa | Já no schema (`SocialLink`, `display_order`) |

Categoria **Compartilhamento**

| Feature | Por Que é Esperado | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| QR code gerável e baixável | Linktree, Monocard, HiHello, Blinq, Popl — todos têm QR. É um dos 4 canais de distribuição do produto (QR na tela, QR impresso) | Baixa-Média | Lib de geração client/server-side (ex.: `qrcode` no Node, ou gerar no backend .NET) |
| Preview de link correto no WhatsApp (OG image + título) | O canal "link mandado no WhatsApp" é um dos 4 caminhos declarados; se o preview vier quebrado (sem imagem, título genérico), a taxa de clique despenca — é a metade da primeira impressão | Baixa-Média | Next.js Metadata API (`generateMetadata`) com `og:image` dinâmica por cartão |
| Copiar link com um toque | Recurso trivial mas universal em todo produto do gênero | Baixa | `navigator.clipboard.writeText` |

Categoria **Conta**

| Feature | Por Que é Esperado | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| Cadastro, login, sessão | Sem conta não há "dono do cartão" nem dashboard de edição | Baixa-Média | Já especificado em `02-autentication.md` (JWT Bearer) |
| Dashboard de edição do próprio cartão | Todo concorrente com "conta" (Monocard, HiHello, Blinq) tem uma tela de edição — sem isso o usuário depende de suporte para mudar 1 dado | Baixa-Média | Formulário simples ligado ao schema `Card`/`SocialLink` |

### Diferenciadores (Vantagem Competitiva — Foco Brasil)

Categoria **Pagamento**

| Feature | Proposta de Valor | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| Chave Pix copiável em um toque | Nenhum concorrente internacional (Popl, HiHello, Blinq, V1CE) tem qualquer equivalente nativo — eles assumem cartão de crédito/PayPal. Entre os concorrentes BR, só Monocard e InfinitePay têm Pix, e nenhum combina isso com identidade de "cartão profissional pessoal" simples | Baixa | Já no schema (`pix_key`, `pix_key_type`); é só exibir + botão copiar |
| **QR Code Pix estático (BR Code) gerado localmente** | Achado de pesquisa importante: o BR Code **estático** (sem valor definido, "Pix Copia e Cola") é um payload EMV/TLV público (padrão do Banco Central, manual BR Code) que pode ser **gerado sem PSP, sem webhook, sem conta em adquirente** — só precisa da chave Pix + nome do recebedor + cidade. Isso é tecnicamente diferente do "BR Code dinâmico com valor" que a spec já corretamente exclui (esse sim exige PSP/webhook/conciliação). Vale reavaliar: um QR Pix estático pode ser um upgrade de baixíssimo custo sobre "copiar chave" — quem recebe escaneia e o app do banco já abre com a chave preenchida, sem digitar nada | Baixa | Bibliotecas existentes em JS/.NET para montar o payload EMV (CRC16 + campos TLV) — é geração de string, não integração de API. Ver `PITFALLS.md` para o cuidado de não confundir com Pix dinâmico |
| Botão "compartilhar meu cartão" via WhatsApp (intent `wa.me/?text=`) | Serve diretamente o canal "link mandado no WhatsApp" — o dono do cartão consegue mandar o próprio link em 1 toque, sem copiar/colar manualmente | Baixa | Não estava na lista `Active` do PROJECT.md — recomenda-se adicionar por ser trivial e resolver 1 dos 4 canais de distribuição diretamente |

Categoria **Cartão / Branding**

| Feature | Proposta de Valor | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| Marca do produto no rodapé do plano grátis (`is_branded`) | Padrão comprovado (Linktree, Cartão Plus "Free com anúncios"): grátis = distribuição orgânica, pago = remoção. Cada cartão gratuito compartilhado expõe o produto | Baixa | Já no schema |
| Botão "remover marca" que registra intenção sem cobrar | Nenhum concorrente pesquisado documenta esse padrão explicitamente — é um recurso de validação, não de mercado, mas é a decisão certa dado o objetivo de medir intenção antes de construir billing | Baixa | Grava evento/flag; não é feature de produto competitivo, é ferramenta de aprendizado |
| Simplicidade radical (1 cartão, sem CRM, sem hardware, sem múltiplos perfis) | É o oposto de Monocard (múltiplos perfis, CRM, hardware) e de HiHello/Popl/V1CE (CRM, captura de leads em eventos). Para o autônomo solo, essas features são fricção, não valor | Baixa (é ausência de escopo) | Reforça o posicionamento: "cartão de gente que atende gente", não "ferramenta de vendas corporativa" |

Categoria **Analytics**

| Feature | Proposta de Valor | Complexidade | Notas |
|---------|---------------------|---------------|-------|
| Contagem simples de visualizações no plano grátis, detalhamento (referrer, série temporal, geografia) como gancho de upgrade | Padrão comum no setor (Beacons oferece analytics detalhado até no free, mas a maioria trava atrás do pago — Linktree, HiHello) | Baixa (contagem) / Média (detalhamento) | `CardView` já grava `referrer`; a v1 mostra só contagem — detalhamento fica para v1.x conforme já decidido |

### Anti-Features (Parecem Bons, Mas Não Devem Ser Construídos)

| Feature | Por Que Parece Atraente | Por Que é Problemático Aqui | Alternativa |
|---------|---------------------------|-------------------------------|-------------|
| Cartão físico NFC (hardware) | É o que Monocard, Popl, V1CE, Dot, CardU, Taggo vendem como produto principal — parece "mais profissional" | Exige produção, estoque, logística de envio, custo unitário — inviável para MVP solo em 2 semanas e não resolve nada que o QR não resolva para o público autônomo | QR code gratuito, exibido na tela ou impresso pelo próprio usuário (adesivo, cartão de papel) |
| Integração com CRM / captura de leads em eventos | HiHello, Popl, V1CE e Monocard (plano empresa) vendem isso como diferencial para times de vendas | É a premissa corporativa americana que o produto rejeita explicitamente — o autônomo não tem funil de vendas B2B nem time comercial | Nenhuma — WhatsApp já é o "CRM" informal de quem presta serviço sozinho |
| Múltiplos perfis/contextos por usuário (ex.: Monocard permite variar o cartão por situação) | Parece flexível e "profissional" | Complexidade de gestão desproporcional ao público-alvo; multiplica superfícies de teste e edição num MVP de 2 semanas | 1 cartão por usuário na v1; múltiplos cartões vira feature paga depois (já decidido) |
| Pix dinâmico com valor definido via PSP (Mercado Pago, Asaas, Efí) | Parece "fechar o ciclo" — cobrar de verdade dentro do cartão | Exige webhook, conciliação, conta configurada no PSP — não valida a tese mais rápido, só atrasa o lançamento | "Copiar chave" (v1) e opcionalmente QR Pix estático (ver Diferenciadores) — cobrar valor específico fica para quando houver checkout de verdade |
| Assinatura de e-mail / fundo virtual para chamadas de vídeo (HiHello) | Parece "recurso completo" de cartão profissional | Pressupõe e-mail e chamada de vídeo como canais primários — não é como o autônomo brasileiro fecha negócio | Nenhuma — o canal é WhatsApp, não e-mail corporativo |
| Enriquecimento automático de contato / descoberta de LinkedIn (HiHello) | Parece "smart" e aumenta percepção de valor | Complexidade de integração e questões de privacidade/dados desproporcionais ao problema real do usuário | Nenhuma |
| Vitrine de produtos com checkout embutido (Cartão Plus "Store", Beacons "digital products") | Parece natural já que o Pix está ali — "por que não vender direto?" | Checkout recorrente e catálogo são um produto diferente (e-commerce), não um cartão de contato; infla escopo do MVP e da manutenção | Se necessário no futuro: lista descritiva de serviços com CTA de WhatsApp por item (sem processamento de pagamento) — ver Futuro |
| Boleto bancário / parcelamento no cartão de crédito como opção de recebimento | Parece "mais completo" (InfinitePay e Dvisit oferecem) | Não é o comportamento do público-alvo (autônomo cobrando por Pix é o padrão); adicionaria escolha e complexidade de UI sem mudar a tese | Pix como único método de recebimento no cartão |
| Apple Wallet / Google Wallet pass (Carda, bCard) | Parece moderno e "sem app" | Exige geração de `.pkpass` (certificados Apple, formatos específicos) — custo de implementação alto para valor incerto no público autônomo brasileiro | QR + .vcf cobrem o mesmo caso de uso ("guardar meu contato") com muito menos complexidade |

## Feature Dependencies

```
Cadastro/Login (Conta)
    └──requires──> nenhuma dependência (é a base)

Dashboard de edição do cartão
    └──requires──> Cadastro/Login

Cartão público (slug)
    └──requires──> Card criado no dashboard (dono já cadastrado)

QR code
    └──requires──> Cartão público (precisa da URL já existente para codificar)

Preview de link (OG image)
    └──requires──> Cartão público + photo_url preenchida

Botão salvar contato (.vcf)
    └──requires──> Cartão público (dados de nome/telefone/whatsapp)

Botão WhatsApp
    └──requires──> whatsapp_number preenchido no Card

Botão copiar Pix
    └──requires──> pix_key + pix_key_type preenchidos

QR Code Pix estático ──enhances──> Botão copiar Pix
    (mesma informação, mas em formato escaneável em vez de copiável manualmente)

Contagem de CardView
    └──requires──> Cartão público (tracking de cada acesso)

Analytics detalhado (referrer, série temporal) ──enhances──> Contagem de CardView
    (mesmo dado bruto já capturado, só exige mais processamento/UI)

Marca no rodapé (is_branded) ──conflicts (parcialmente)──> Percepção de "produto profissional completo"
    (é uma troca deliberada: marca visível = distribuição gratuita, não é bug)

Botão "remover marca" (intenção de upgrade) ──requires──> is_branded existir como estado
```

### Notas de Dependência

- **QR code requer Cartão público:** não faz sentido gerar QR antes de existir uma URL estável para codificar — a ordem de construção deve respeitar isso.
- **Preview de link (OG) enhance Cartão público:** tecnicamente é metadata sobre uma página que já existe; pode ser adicionado depois, mas como o canal WhatsApp é central na tese, não deveria ficar para depois na prática — tratar como parte do mesmo corte do "cartão público".
- **QR Pix estático enhances Botão copiar Pix, não o substitui:** ambos devem coexistir — nem todo pagador vai escanear um QR, alguns preferem colar a chave manualmente no app do banco.
- **Analytics detalhado enhance Contagem de CardView, não é feature nova:** o dado (`referrer`) já é gravado desde a v1; é só uma UI que fica trancada atrás do plano pago — importante para o roadmap saber que não há trabalho de captura adicional, só de exibição.
- **Marca no rodapé conflicts parcialmente com percepção de completude:** essa é uma tensão aceita de propósito (modelo freemium), não um bug a resolver.

## MVP Definition

### Lançar Com (v1) — já decidido no PROJECT.md, confirmado pela pesquisa

- [x] Perfil com nome, cargo, empresa, foto — table stakes confirmado por todo o setor
- [x] Cartão público por slug, mobile-first — table stakes confirmado
- [x] Botão de WhatsApp — diferencial central da tese, sem equivalente direto nos concorrentes internacionais e mais forte que o tratamento "mais um link" dos concorrentes BR
- [x] Chave Pix copiável — diferencial confirmado: nenhum concorrente internacional tem, e nenhum concorrente BR combina isso com simplicidade (Monocard tem Pix mas empacotado com CRM/hardware)
- [x] Links sociais ordenáveis — table stakes
- [x] QR code gerável e baixável — table stakes, é 1 dos 4 canais de distribuição
- [x] Preview de link (OG image) correto no WhatsApp — table stakes crítico dado o canal principal de compartilhamento
- [x] Botão salvar contato (.vcf) — table stakes confirmado por concorrentes internacionais
- [x] Contagem de CardView — table stakes mínimo (detalhamento fica para depois)
- [x] Cadastro/login/sessão JWT — base técnica, já especificado
- [x] Marca no rodapé do grátis (`is_branded`) — mecanismo de distribuição orgânica validado pelo mercado (Linktree, Cartão Plus)
- [x] Botão "remover marca" registrando intenção — sinal de validação barato e correto

**Recomendação de pesquisa para considerar incluir no corte da v1** (baixa complexidade, reforça diretamente a tese):
- [ ] Botão "compartilhar meu cartão" via WhatsApp intent (`wa.me/?text=`) — serve o canal de distribuição "link mandado no WhatsApp" sem esforço adicional relevante
- [ ] Copiar link do cartão com 1 toque — trivial, universal no setor

### Adicionar Após Validação (v1.x)

- [ ] Analytics detalhado (referrer, série temporal, geografia) — gatilho: usuários pagantes pedindo "de onde vêm minhas visitas" (dado já capturado, só falta UI)
- [ ] Múltiplos cartões por usuário — gatilho: usuário do plano pago pedindo variar contexto (ex.: cartão pessoal vs. cartão de outro serviço)
- [ ] QR Code Pix estático (BR Code) — gatilho: validar primeiro se "copiar chave" já é suficiente antes de investir na geração de payload EMV; é baixo custo mas não é urgente
- [ ] Temas/cores customizáveis além do padrão — gatilho: usuários pedindo personalização visual

### Consideração Futura (v2+)

- [ ] Pix dinâmico com valor via PSP (Mercado Pago, Asaas, Efí) — adiar até existir modelo de cobrança real validado; exige webhook e conciliação
- [ ] Checkout/assinatura recorrente de verdade — adiar até confirmar que há apetite de pagamento (o botão de intenção é o teste)
- [ ] Lista de serviços/portfólio com CTA de WhatsApp por item (sem checkout) — inspirado no padrão "vitrine" de Cartão Plus, mas mantendo o WhatsApp como fechamento em vez de checkout embutido
- [ ] Apple Wallet / Google Wallet pass — visto em concorrentes BR (Carda, bCard) mas custo de implementação alto para valor incerto no público-alvo
- [ ] NFC físico como add-on opcional (não como produto principal) — só se houver demanda validada de usuários pagantes; não deveria nunca virar o centro do modelo de negócio como é em Monocard/Popl/V1CE
- [ ] CRM / integrações — permanece rejeitado por tese, não é "adiado", é "não vamos construir" a menos que o público-alvo mude

## Feature Prioritization Matrix

| Feature | Valor para o Usuário | Custo de Implementação | Prioridade |
|---------|------------------------|---------------------------|------------|
| Cartão público com dados básicos | ALTO | BAIXO | P1 |
| Botão WhatsApp | ALTO | BAIXO | P1 |
| Botão copiar Pix | ALTO | BAIXO | P1 |
| QR code | ALTO | BAIXO-MÉDIO | P1 |
| Preview de link (OG) | ALTO | BAIXO-MÉDIO | P1 |
| Salvar contato (.vcf) | MÉDIO-ALTO | BAIXO | P1 |
| Links sociais ordenáveis | MÉDIO | BAIXO | P1 |
| Cadastro/login | ALTO (habilitador) | BAIXO-MÉDIO | P1 |
| Marca no rodapé + intenção de upgrade | ALTO (motor de validação) | BAIXO | P1 |
| Contagem simples de views | MÉDIO | BAIXO | P1 |
| Compartilhar via WhatsApp (intent) | MÉDIO-ALTO | BAIXO | P1 (recomendado somar à v1) |
| Analytics detalhado | MÉDIO | MÉDIO | P2 |
| QR Pix estático (BR Code) | MÉDIO-ALTO | BAIXO | P2 |
| Múltiplos cartões | MÉDIO | MÉDIO | P2 |
| Temas customizáveis | BAIXO-MÉDIO | BAIXO-MÉDIO | P2 |
| Pix dinâmico via PSP | MÉDIO | ALTO | P3 |
| Checkout/assinatura | BAIXO (nesta fase) | ALTO | P3 |
| NFC físico | BAIXO (para o público-alvo) | ALTO | P3 |
| CRM/lead capture | MUITO BAIXO (fora da tese) | ALTO | Não construir |

**Chave de prioridade:**
- P1: Essencial para o lançamento
- P2: Desejável, adicionar quando possível (pós-validação)
- P3: Bom ter, consideração futura

## Competitor Feature Analysis

| Feature | Monocard (BR) | HiHello/Blinq/Popl (EUA) | Nosso Approach |
|---------|----------------|-----------------------------|--------------------|
| WhatsApp | Tratado como mais um link/módulo | Ausente (assumem e-mail/CRM) | Campo próprio, CTA principal — a tese do produto |
| Pix | Sim, módulo entre outros | Ausente | Campo próprio (chave copiável), tese do produto |
| Hardware NFC | Central ao modelo de negócio | Central (Popl, V1CE, Dot) | Deliberadamente ausente — QR resolve sem custo de produção |
| CRM/lead capture | Sim, plano empresa | Sim (HiHello, Popl, V1CE) | Deliberadamente ausente — não é o público-alvo |
| Múltiplos perfis/cartões | Sim | Variável (times) | Ausente na v1, feature paga depois |
| Marca no plano grátis | Não claro nas fontes | Não é padrão (foco B2B) | Sim — distribuição orgânica (padrão Linktree) |
| Preço do plano pago | R$9,90/mês | US$6-8/mês (HiHello Pro) | R$9,90-19,90/mês planejado — alinhado ao mercado |
| Analytics | Sim (empresa) | Sim (avançado, todos) | Básico grátis, detalhado pago |

## Sources

- Monocard — monocard.me, monocard.com.br, resultados de busca (WebSearch, MEDIUM confidence — WebFetch direto bloqueado por 403, dados vêm de snippets de busca cruzados de múltiplas páginas do próprio domínio + Amazon.com.br + LinkedIn)
- Cartão Plus — https://cartao.plus/ (WebFetch direto, MEDIUM confidence)
- InfinitePay Link na Bio — https://www.infinitepay.io/link-na-bio (WebFetch direto, MEDIUM-HIGH confidence — fonte primária de uma fintech conhecida)
- Cartões.digital — https://cartoes.digital/ (WebFetch direto, MEDIUM confidence)
- Taggo, CardU, Airgo, Digital Card BR, Dvisit, bCard, Carda, ClickCard, E4Card — WebSearch, LOW-MEDIUM confidence (apenas snippets, não fonte primária completa)
- Blinq/HiHello/Popl comparação — https://blinq.me/blog/top-digital-business-cards-compared (WebFetch, MEDIUM confidence — conteúdo de marketing de concorrente, mas convergente com múltiplas outras fontes)
- V1CE/Mobilo/Dot — WebSearch (LOW-MEDIUM confidence, blogs de marketing dos próprios concorrentes)
- Linktree/Beacons — WebSearch (MEDIUM confidence, múltiplas fontes de comparação de terceiros convergentes)
- Pix BR Code estático / EMV payload — https://www.tabnews.com.br/usrbinenv/entendendo-o-payload-do-pix-copia-e-cola-e-gerando-um-qr-code-estatico e múltiplos geradores públicos (dokehost, qmixdigital, pix2qr.xyz, criarqr.com) — MEDIUM-HIGH confidence, convergência entre fonte técnica detalhada e múltiplas ferramentas públicas que implementam o mesmo padrão do Banco Central (Manual BR Code / EMVCo)
- wa.me / WhatsApp share links — WebSearch, HIGH confidence (padrão documentado e estável, não muda com frequência)

---
*Feature research for: cartão de visita digital para freelancers e autônomos no Brasil*
*Researched: 2026-08-13*
