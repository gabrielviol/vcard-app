# Phase 2: Cartão Público no Ar - Context

**Gathered:** 2026-08-14
**Status:** Ready for planning

<domain>
## Phase Boundary

O cartão passa a existir de verdade no mundo: qualquer pessoa acessa `/[slug]` sem autenticação, com carregamento rápido mesmo se backend/banco estiverem frios (ISR + pré-aquecimento no save + keep-alive externo), e o dono tem um QR pronto pra circular (tela + download). Cobre `PUB-01..06`, `SHARE-01`, `SHARE-02`, `BRAND-01`.

Não inclui: botão de WhatsApp/copiar Pix/.vcf na página pública (Fase 3), preview de link OG image (Fase 3), analytics de visualização (Fase 4), remoção efetiva de marca (Fase 4 — `is_branded` é Fase 4/`BRAND-02`, aqui só o nome+domínio do produto em si).

</domain>

<decisions>
## Implementation Decisions

### Nome e domínio do produto
- **D-14:** Nome de trabalho definido como **"Vizzo"**, com domínio alvo `vizzo.com.br` — **provisório**. O usuário ainda precisa confirmar disponibilidade real (registro.br) e ausência de conflito de marca (INPI) antes de travar de vez. Verificação preliminar por busca na web (não é WHOIS/INPI oficial) não encontrou produto ativo com esse nome exato no nicho de cartão digital — colisão mais próxima é "Vizzano" (marca de calçados, não concorrente).
  - Nomes descartados por colisão direta: **Zapcard** (zapcard.com.br já existe, concorrente direto no mesmo nicho), **Zappix** (empresa americana de customer engagement com esse nome exato), **MeuCard** (múltiplos concorrentes diretos: meucard.digital, meucard.pro, meucard.online), **Tapcard** (múltiplos apps de cartão NFC com esse nome exato), **Tokinho** (nome de comediante brasileira conhecida — risco de confusão).
  - Alternativas limpas não escolhidas, mas registradas caso "Vizzo" não vingue: **Cartaum**, **Pixtão**, **Umtoque**.
- **D-15:** TLD preferido: `.com.br` — sinaliza produto brasileiro, mais barato via registro.br, combina com o posicionamento 100% BR.
- Até a confirmação final do domínio, o planner/executor devem usar uma variável de ambiente (`NEXT_PUBLIC_APP_URL` ou equivalente) para a URL base do produto, nunca hardcode — troca de domínio não pode exigir re-trabalho de código.

### QR code no dashboard
- **D-16:** O QR do cartão fica **sempre visível na tela de edição** do cartão (não é uma ação separada tipo "Ver QR" atrás de clique) — zero fricção pra ver/baixar.
- **D-17:** Download do QR pra impressão é **só o código puro, sem legenda/texto embutido** — máxima flexibilidade pro dono que já pode ter peça gráfica própria pronta.
- **D-18:** Cor do QR na tela e no download é **preto no branco (padrão)**, não a cor de marca — prioriza confiabilidade de leitura (câmera ruim, impressão de baixa qualidade) sobre consistência visual.
- Formato de arquivo (SVG padrão + PNG fallback) e nível de correção de erro (M) já são decisões técnicas travadas na pesquisa de stack do projeto — não reabertas aqui.

### Cartão incompleto na página pública
- **D-19:** Campos/seções vazios (ex: sem WhatsApp) **somem inteiramente** da página pública — não aparecem como seção com espaço em branco ou botão desabilitado. Mesma regra vale pra toda seção opcional (Pix, foto, links sociais).
- **D-20:** Um cartão salvo com o mínimo da Fase 1 (só slug + nome) fica **imediatamente acessível publicamente** em `/[slug]` — consistente com D-04 da Fase 1 (cartão incompleto é permitido). Não existe limiar de "preenchimento mínimo" adicional pra publicar.
- **D-21:** O estado extremo (só nome + placeholder de iniciais, tudo mais vazio) é um **estado visual válido por si só** — sem mensagem de "cartão incompleto" ou aviso similar. A página minimalista não pede desculpa por estar incompleta.

### Página 404 do slug
- **D-22:** A 404 de slug inexistente (`PUB-06`) tem **a cara do produto** (identidade visual, copy própria tipo "Esse cartão não existe") — não é a 404 genérica do framework.
- **D-23:** A 404 inclui **CTA de cadastro** ("Crie seu próprio cartão" ou similar) — quem cai num link quebrado é público-alvo em potencial (alguém que recebeu ou tentou acessar um cartão digital), então o erro vira oportunidade de aquisição.

### Claude's Discretion
- Layout exato da tela de edição pra acomodar o QR sempre visível (posição, tamanho na tela vs. tamanho de download) — decisão de UI, não de produto.
- Mecanismo técnico exato de pré-aquecimento (`PUB-03`) e keep-alive (`PUB-04`) — fetch disparado no momento do save + cron/serviço externo de ping, a escolher na pesquisa/planejamento.
- Estrutura exata da rota `/[slug]` (Server Component + `revalidate`, conforme já documentado na pesquisa de stack do projeto) — decisão técnica já pesquisada, não reaberta aqui.
- Copy exata da 404 (D-22/D-23 travam a intenção — "cara do produto" + CTA — não o texto literal).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Produto e requisitos
- `.planning/PROJECT.md` — Tese do produto, constraints (inclui a pendência de nome/domínio, agora endereçada provisoriamente por D-14/D-15).
- `.planning/REQUIREMENTS.md` — `PUB-01..06`, `SHARE-01`, `SHARE-02`, `BRAND-01` com critérios de aceite.
- `.planning/ROADMAP.md` §Phase 2 — Goal e success criteria formais.
- `.planning/CLAUDE.md` (raiz do projeto) — Seção "Technology Stack" já documenta decisões técnicas de QR (biblioteca `qrcode`, SVG+PNG, `errorCorrectionLevel M`) e renderização da página pública (ISR `revalidate`, sem revalidação sob demanda no MVP) — tratar como decisão tomada, não reabrir.

### Fase 1 (base sobre a qual esta fase constrói)
- `.planning/phases/01-conta-e-cart-o/01-VERIFICATION.md` — Confirma o que já existe: schema de 4 tabelas, `cards.slug` único, endpoints autenticados de Card/SocialLink. Nenhum endpoint público de leitura por slug existe ainda (só `/slug-available` para checagem de disponibilidade) — a Fase 2 precisa criar isso do zero.
- `.planning/phases/01-conta-e-cart-o/01-CONTEXT.md` — D-04 (cartão incompleto é permitido) é a base direta de D-20/D-21 desta fase.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `apps/api/Services/SlugService.cs` — normalização e validação de slug já existe (Fase 1); a rota pública deve reusar o mesmo normalizador ao resolver `/[slug]`, não duplicar a lógica.
- `apps/web/lib/initials.ts` — `getInitials`/`getAvatarColor` já existem (Fase 1, placeholder de foto no dashboard) — mesmo componente/lógica deve valer para o placeholder na página pública (D-21).
- `apps/web/lib/api-client.ts` — cliente HTTP autenticado existente; a página pública precisa de um caminho de fetch **não autenticado** separado (Server Component chamando o novo endpoint público), não deve reusar o `apiFetch` com Bearer.

### Established Patterns
- `apps/api/Endpoints/CardEndpoints.cs` — padrão de handler + DTO já estabelecido (Contracts/CardDtos.cs); o novo endpoint público deve seguir a mesma estrutura, mas SEM `.RequireAuthorization()` e retornando só os campos seguros para exibição pública (não vazar `id`/`user_id` desnecessariamente, por exemplo).
- Camadas Endpoints/Services/Data do backend e App Router do frontend — padrão já rodando em toda a Fase 1, continuar.

### Integration Points
- Nenhum endpoint GET público por slug existe ainda — é o primeiro ponto de integração novo desta fase.
- `NEXT_PUBLIC_API_URL` (variável já existente desde a Fase 1) é usada para chamadas do lado do cliente; a rota pública `/[slug]` deve decidir entre Server Component (fetch direto ao backend, sem CORS) vs. cliente — decisão técnica de planejamento, não de produto.

</code_context>

<specifics>
## Specific Ideas

Nenhuma referência visual específica além do que já está em PROJECT.md/REQUIREMENTS.md. As decisões desta fase priorizam: máxima simplicidade visual quando o cartão está incompleto (D-19/D-21), confiabilidade sobre estética no QR (D-18), e transformar até o caminho de erro (404) em oportunidade de aquisição (D-23).

</specifics>

<deferred>
## Deferred Ideas

Nenhuma — discussão ficou dentro do escopo da fase. Nenhum todo pendente encontrado em `.planning/todos/` para cruzar com esta fase (`todo.match-phase` retornou 0 matches).

**Pendência que atravessa fases:** confirmação final e registro real do domínio (D-14/D-15) — não bloqueia o planejamento/execução técnica desta fase (que usa variável de ambiente), mas bloqueia o deploy real em produção. Recomendado resolver antes do fim da Fase 2.

</deferred>

---

*Phase: 2-Cartão Público no Ar*
*Context gathered: 2026-08-14*
