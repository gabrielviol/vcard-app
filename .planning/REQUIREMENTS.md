# Requirements: vCard App (nome a definir)

**Defined:** 2026-08-13
**Core Value:** Alguém recebe o cartão (por QR ou link) e consegue te chamar ou te pagar em um toque — sem sair da página, sem digitar nada, sem etapa intermediária.

## v1 Requirements

Requisitos da primeira release. Cada um mapeia para uma fase do roadmap.

### Marca (BRAND)

Nome e domínio saíram de "pendência" para caminho crítico quando marca + intenção de upgrade entraram na v1 — o rodapé do plano grátis não existe sem marca.

- [ ] **BRAND-01**: Produto tem nome definido e domínio registrado, apontando para o frontend em produção
- [ ] **BRAND-02**: Cartão do plano grátis exibe a marca do produto no rodapé, com link para a home do produto (`is_branded`)

### Conta (ACCT)

- [ ] **ACCT-01**: Visitante cria conta com e-mail e senha, com senha armazenada como hash BCrypt
- [ ] **ACCT-02**: Usuário faz login com e-mail e senha e recebe um access token JWT
- [ ] **ACCT-03**: Usuário permanece autenticado ao navegar e ao recarregar o dashboard
- [ ] **ACCT-04**: Usuário é redirecionado para `/login` quando o token está ausente ou expirado
- [ ] **ACCT-05**: Rotas de escrita de Card, SocialLink e views retornam 401 sem token válido

### Cartão (CARD)

- [ ] **CARD-01**: Usuário cria seu cartão escolhendo um slug único para a URL pública
- [ ] **CARD-02**: Sistema rejeita slugs reservados (`login`, `dashboard`, `api`, `_next`, `admin`, etc.) e slugs já em uso
- [ ] **CARD-03**: Usuário edita identidade do cartão: nome completo, cargo e empresa
- [ ] **CARD-04**: Usuário edita canais de contato: telefone, e-mail e número de WhatsApp
- [ ] **CARD-05**: Usuário cadastra chave Pix escolhendo o tipo (CPF, CNPJ, e-mail, telefone, aleatória)
- [ ] **CARD-06**: Sistema valida o formato da chave Pix conforme o tipo escolhido antes de salvar (dígito verificador em CPF/CNPJ, UUID v4 em chave aleatória) e exibe prévia formatada
- [ ] **CARD-07**: Usuário é avisado, antes de salvar, que a chave Pix ficará visível publicamente — com aviso reforçado quando o tipo for CPF
- [ ] **CARD-08**: Sistema normaliza o número de WhatsApp para dígitos puros com DDI 55 no momento de salvar, independente da máscara digitada
- [ ] **CARD-09**: Usuário faz upload da foto do cartão direto do browser, e a foto aparece no cartão público
- [ ] **CARD-10**: Usuário adiciona, remove e reordena links sociais (Instagram, LinkedIn, Twitter, TikTok, YouTube, site)

### Cartão Público (PUB)

- [ ] **PUB-01**: Qualquer pessoa acessa o cartão em `/[slug]` sem autenticação, com layout mobile-first
- [ ] **PUB-02**: Página pública é servida por ISR, sem depender do backend estar acordado a cada visita
- [ ] **PUB-03**: Cartão recém-criado ou recém-editado é pré-aquecido no momento do save, para que o primeiro acesso ao slug não caia em cold start
- [ ] **PUB-04**: Keep-alive externo mantém backend e banco acordados em intervalo regular
- [ ] **PUB-05**: Edição feita no dashboard se reflete no cartão público sem exigir novo deploy
- [ ] **PUB-06**: Slug inexistente retorna página 404 própria, não erro de servidor

### Contato (CONT)

- [ ] **CONT-01**: Visitante toca no botão de WhatsApp e abre conversa direta com o dono do cartão
- [ ] **CONT-02**: Visitante toca em "salvar contato" e baixa um `.vcf` com nome, cargo, empresa, telefone e e-mail do dono
- [ ] **CONT-03**: `.vcf` importa corretamente na agenda em iOS e em Android, preservando acentuação em nomes brasileiros
- [ ] **CONT-04**: Visitante em navegador embutido (Instagram/WhatsApp) recebe orientação visível para abrir no navegador quando o download do `.vcf` não funciona
- [ ] **CONT-05**: Visitante acessa os links sociais do dono, na ordem definida por ele

### Pagamento (PAY)

- [ ] **PAY-01**: Visitante copia a chave Pix do dono com um toque
- [ ] **PAY-02**: Chave Pix permanece copiável (texto selecionável visível) mesmo quando a API de clipboard é bloqueada pelo navegador embutido
- [ ] **PAY-03**: Confirmação de "copiado" só aparece após a cópia ser confirmada, nunca de forma otimista

### Compartilhamento (SHARE)

- [ ] **SHARE-01**: Dono visualiza o QR code do seu cartão na tela, em tamanho utilizável para alguém escanear na hora
- [ ] **SHARE-02**: Dono baixa o QR code em resolução adequada para impressão
- [ ] **SHARE-03**: Link do cartão compartilhado no WhatsApp exibe preview correto (imagem, título e descrição do dono)
- [ ] **SHARE-04**: Preview do link reflete edições do cartão, sem ficar preso em cache antigo
- [ ] **SHARE-05**: Dono compartilha o próprio cartão via WhatsApp a partir do dashboard, com o link já preenchido
- [ ] **SHARE-06**: Dono copia a URL do seu cartão com um toque

### Analytics (VIEW)

- [ ] **VIEW-01**: Sistema registra uma visualização quando uma pessoa real abre o cartão público
- [ ] **VIEW-02**: Contagem de visualizações não é inflada por crawlers de preview de link nem por prefetch
- [ ] **VIEW-03**: Dono vê a contagem total de visualizações do seu cartão no dashboard

### Monetização (UPG)

- [ ] **UPG-01**: Dono vê no dashboard a opção "remover marca — R$9,90/mês" com posicionamento visível
- [ ] **UPG-02**: Clique em "remover marca" registra a intenção de upgrade de forma consultável, sem cobrar nem processar pagamento
- [ ] **UPG-03**: Dono recebe confirmação de que foi registrado e será avisado quando o plano existir

## v2 Requirements

Adiado para depois da validação. Rastreado, mas fora do roadmap atual.

### Pagamento

- **PAY-04**: QR Code Pix estático (BR Code) gerado a partir da chave, escaneável e com string "copia e cola" que o app do banco reconhece — decidido como v1.x, é payload EMV público do Bacen sem PSP nem webhook
- **PAY-05**: Pix dinâmico com valor definido por serviço, via PSP

### Monetização

- **UPG-04**: Checkout de assinatura recorrente
- **UPG-05**: Remoção efetiva da marca para assinantes pagantes
- **UPG-06**: Múltiplos cartões por usuário

### Analytics

- **VIEW-04**: Analytics detalhado — origem do tráfego (`referrer`), série temporal, horários de pico

### Conta

- **ACCT-06**: Recuperação de senha
- **ACCT-07**: Refresh token e renovação automática de sessão
- **ACCT-08**: Login social (Google)
- **ACCT-09**: Rate limiting no login

### Cartão

- **CARD-11**: Temas e personalização visual do cartão

## Out of Scope

Excluído explicitamente. Documentado para evitar scope creep.

| Feature | Reason |
|---------|--------|
| Hardware NFC físico | Concorrentes (Popl, V1CE, Monocard) vendem isso como produto central. Exige produção, estoque e logística — inviável solo e contrário à tese de simplicidade radical. O QR resolve o mesmo problema com custo zero. |
| Integração com CRM / lead capture de eventos | É exatamente a premissa americana de networking corporativo que o produto rejeita. Rejeitado por tese, não adiado. |
| Múltiplos perfis por pessoa (trabalho/pessoal) | Complexidade de UX que serve o usuário corporativo, não o autônomo solo. |
| Enriquecimento automático de contato | Feature do HiHello, exige base de dados de terceiros e levanta problema de privacidade. |
| Apple/Google Wallet pass | Custo de certificado e manutenção alto demais para o retorno na v1. |
| App nativo | O cartão é uma página web aberta a partir de QR ou link — app nativo não tem papel no fluxo do recebedor. |
| Cobrança de fato do plano pago | A v1 mede intenção de pagar. Construir billing antes de saber se alguém quer pagar é a ordem errada. |

## Traceability

Preenchido durante a criação do roadmap.

| Requirement | Phase | Status |
|-------------|-------|--------|
| ACCT-01 | Phase 1 | Pending |
| ACCT-02 | Phase 1 | Pending |
| ACCT-03 | Phase 1 | Pending |
| ACCT-04 | Phase 1 | Pending |
| ACCT-05 | Phase 1 | Pending |
| CARD-01 | Phase 1 | Pending |
| CARD-02 | Phase 1 | Pending |
| CARD-03 | Phase 1 | Pending |
| CARD-04 | Phase 1 | Pending |
| CARD-05 | Phase 1 | Pending |
| CARD-06 | Phase 1 | Pending |
| CARD-07 | Phase 1 | Pending |
| CARD-08 | Phase 1 | Pending |
| CARD-09 | Phase 1 | Pending |
| CARD-10 | Phase 1 | Pending |
| PUB-01 | Phase 2 | Pending |
| PUB-02 | Phase 2 | Pending |
| PUB-03 | Phase 2 | Pending |
| PUB-04 | Phase 2 | Pending |
| PUB-05 | Phase 2 | Pending |
| PUB-06 | Phase 2 | Pending |
| SHARE-01 | Phase 2 | Pending |
| SHARE-02 | Phase 2 | Pending |
| BRAND-01 | Phase 2 | Pending |
| CONT-01 | Phase 3 | Pending |
| CONT-02 | Phase 3 | Pending |
| CONT-03 | Phase 3 | Pending |
| CONT-04 | Phase 3 | Pending |
| CONT-05 | Phase 3 | Pending |
| PAY-01 | Phase 3 | Pending |
| PAY-02 | Phase 3 | Pending |
| PAY-03 | Phase 3 | Pending |
| SHARE-03 | Phase 3 | Pending |
| SHARE-04 | Phase 3 | Pending |
| SHARE-05 | Phase 3 | Pending |
| SHARE-06 | Phase 3 | Pending |
| VIEW-01 | Phase 4 | Pending |
| VIEW-02 | Phase 4 | Pending |
| VIEW-03 | Phase 4 | Pending |
| BRAND-02 | Phase 4 | Pending |
| UPG-01 | Phase 4 | Pending |
| UPG-02 | Phase 4 | Pending |
| UPG-03 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 43 total
- Mapped to phases: 43
- Unmapped: 0 ✓

---
*Requirements defined: 2026-08-13*
*Last updated: 2026-08-13 after roadmap creation*
