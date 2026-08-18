# Runbook de Deploy — Produção

Este documento descreve como o vCard App roda em produção e como reproduzir o
ambiente do zero. Nenhum segredo real vive aqui — todos os valores sensíveis
ficam nas variáveis de ambiente dos respectivos dashboards (Render, Vercel).

## Topologia

```
Vercel (apps/web, Next.js)  --HTTPS-->  Render (apps/api, container Docker)  --TLS-->  Neon (Postgres)
```

- **Frontend (Vercel):** `https://vcard-app-one.vercel.app` — ainda não há
  domínio próprio registrado (`BRAND-01` segue **deferido** por escolha do
  usuário; ver seção "DNS" abaixo). Essa é a URL de produção real hoje.
- **Backend (Render):** `https://vcard-app-tihd.onrender.com` — publicado no
  plano 02-05.
- **Vercel** hospeda `apps/web`. O cartão público (`/[slug]`) é servido via ISR
  (`revalidate` por tempo) na edge da Vercel — a maioria das visitas (QR code,
  link direto) **não** toca o Render, o que reduz cold starts desnecessários.
- **Render** hospeda `apps/api` (.NET minimal API) como container Docker, plano
  Free (spin-down após 15 min de inatividade).
- **Neon** é o Postgres de produção — compute "scale to zero", autosuspend
  após 5 min de inatividade no request, acorda sozinho em ~500ms na query
  seguinte. Escolhido em vez de Supabase porque Supabase pausa o projeto
  inteiro após 7 dias sem atividade e exige restauração manual no dashboard
  (decisão travada no `CLAUDE.md` raiz).

## Variáveis de ambiente do Render (`apps/api`)

O `Program.cs` lê estas chaves **eagerly, com `?? throw`, antes de
`builder.Build()`** — se qualquer uma faltar, o processo falha no startup
(não sobe parcialmente).

| Variável | Origem do valor | Observação |
|----------|------------------|------------|
| `ConnectionStrings__Default` | Neon Console -> Project -> Connection Details -> string .NET/Npgsql | Deve conter `SSL Mode=Require;Trust Server Certificate=true` explicitamente — o default `Prefer` do Npgsql não valida certificado |
| `JWT_SECRET` | Gerar valor aleatório novo de 32+ bytes (ex: `openssl rand -base64 32`) | **NUNCA** reusar o segredo de desenvolvimento local nem o `TestAppFactory.TestJwtSecret` (versionado em `apps/api/Api.Tests/TestAppFactory.cs`) — reutilizá-lo permitiria forjar tokens válidos a partir de um segredo público no repositório |
| `Jwt__Issuer` | `vcard-api` | Mesmo valor usado em dev/teste, não é segredo |
| `Jwt__Audience` | `vcard-web` | Mesmo valor usado em dev/teste, não é segredo |
| `Jwt__ExpiresMinutes` | `20` (ou o valor decidido para produção) | Controla validade do token; sem refresh token no MVP |
| `Cors__WebOrigin` | Origem exata do frontend em produção | Valor atual: `https://vcard-app-one.vercel.app` (sem domínio próprio ainda — `BRAND-01` deferido). Nunca usar `*` — `Program.cs` já restringe a uma única origem via `policy.WithOrigins(webOrigin)` |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Deve bater com `EXPOSE 8080` do `apps/api/Dockerfile` |

## Vercel

- **Root Directory:** `apps/web`. Framework preset Next.js, detectado
  automaticamente.
- **URL de produção atual:** `https://vcard-app-one.vercel.app` (não há
  domínio próprio configurado — ver seção "DNS").

Variáveis do environment **Production**:

| Variável | Origem do valor | Observação |
|----------|------------------|------------|
| `NEXT_PUBLIC_API_URL` | URL pública do Web Service no Render (`https://vcard-app-tihd.onrender.com`) | Consumida por `lib/api-client.ts` (browser, dashboard autenticado) e por `lib/public-card.ts` (servidor, Server Component da página pública) |
| `NEXT_PUBLIC_APP_URL` | `https://vcard-app-one.vercel.app` (valor provisório enquanto `BRAND-01` estiver deferido) | Consumida por `lib/qr.ts` (`buildCardUrl`) e, por reuso, por `lib/prewarm.ts` — é o que o QR codifica |
| `BLOB_READ_WRITE_TOKEN` | Vercel Dashboard -> Storage -> Blob Store -> `.env.local` (já existente desde a Fase 1) | Server-only, consumida por `app/api/upload/route.ts` |

**Nenhuma das três termina em barra final.** A montagem de URL em `lib/qr.ts`
remove a barra final se presente, mas `lib/api-client.ts` e
`lib/public-card.ts` concatenam a URL base direto com o path (`${base}/path`)
— uma barra final sobrando produz `//path` e pode quebrar o roteamento da
API ou do fetch server-side.

Quando `BRAND-01` for resolvido (nome e domínio definidos), a troca é só de
configuração: ver seção "Trocar o domínio" abaixo. Nenhum código precisa
mudar (D-15).

## DNS

**Não há domínio próprio registrado ainda.** `BRAND-01` está explicitamente
**deferido** por decisão do usuário (ver `.planning/STATE.md`) — o produto
roda hoje inteiramente sobre URLs provisórias (`*.vercel.app` /
`*.onrender.com`). Não existem registros DNS para documentar nesta fase.

Quando um domínio for registrado, o padrão a seguir (documentado pela Vercel
e válido para qualquer domínio `.com.br` ou similar) é:

- **Domínio apex** (ex: `vizzo.com.br`): registro **A**, apontando para o IP
  que a Vercel indicar no momento em que o domínio for adicionado em
  Settings -> Domains.
- **Subdomínio** (ex: `www.vizzo.com.br`): registro **CNAME**, apontando
  para o host que a Vercel indicar.
- Os valores exatos (IP do A, host do CNAME) **sempre vêm do painel da
  Vercel no momento da configuração** — não usar valores de memória, de
  tutorial ou de outro projeto, porque a Vercel pode alocar infraestrutura
  diferente por conta/projeto.
- Depois de criar os registros no painel de DNS do registrador, aguardar a
  propagação (minutos a algumas horas) e confirmar na Vercel que o domínio
  aparece como *Valid Configuration* com certificado TLS emitido.

## Trocar o domínio

Procedimento válido tanto para registrar o primeiro domínio próprio (destrava
`BRAND-01`) quanto para trocar um domínio já configurado:

1. Adicionar o novo domínio na Vercel (Project -> Settings -> Domains).
2. Aplicar no painel de DNS do registrador exatamente os registros que a
   Vercel exibir (A no apex, CNAME no subdomínio — ver seção "DNS").
3. Atualizar `NEXT_PUBLIC_APP_URL` na Vercel (environment Production) para
   `https://{novo-domínio}` e redeployar.
4. Atualizar `Cors__WebOrigin` no Render para a mesma origem exata e
   redeployar o serviço — sem isso, o dashboard autenticado passa a ser
   bloqueado por CORS no domínio novo.
5. **Regerar/reimprimir os QR codes existentes.** O QR codifica a URL no
   momento em que é gerado e **não se atualiza sozinho** — qualquer adesivo
   já impresso com o domínio antigo continua apontando para ele até ser
   trocado fisicamente.

O nome do produto ("Vizzo") é provisório (D-14) — este procedimento é o
custo real de uma troca de nome/domínio no futuro, e é intencionalmente
barato de código (zero) e caro de operação (redeploys + reimpressão física).

## Checklist de go-live

Um item por requisito da fase. Marcar apenas depois de verificar contra a
infraestrutura real de produção (ver Task 3 do plano `02-06` para o
passo a passo detalhado de verificação).

- [ ] `PUB-01` — Cartão público abre em `/{slug}` sem autenticação, de
      qualquer dispositivo. Verificar: abrir a URL pública numa aba anônima.
- [ ] `PUB-02` — Página pública servida por ISR, sem depender do backend
      acordado a cada visita. Verificar: medir `curl -o /dev/null -s -w
      "%{time_total}\n"` duas vezes seguidas contra a mesma URL — a segunda
      deve ser da ordem de centenas de milissegundos.
- [ ] `PUB-03` — Pré-aquecimento dispara ao salvar o cartão. Verificar: com a
      aba Network aberta, salvar e confirmar a requisição a `/{slug}` logo
      após o `PUT`, sem bloquear o toast de sucesso.
- [ ] `PUB-04` — Keep-alive ativo, cron batendo em `/health` a cada 5
      minutos. Verificar: histórico de execução no cron-job.org mostrando
      respostas `200` recorrentes.
- [ ] `PUB-05` — Edição no dashboard reflete na página pública em até 60s,
      sem novo deploy. Verificar: editar, recarregar a URL pública logo em
      seguida e de novo após 60s.
- [ ] `PUB-06` — Slug inexistente devolve HTTP 404 com a página brandada.
      Verificar: `curl -I https://{domínio-atual}/slug-que-nao-existe` →
      primeira linha `404`.
- [ ] `SHARE-01` — QR visível na tela de edição, sem clique adicional.
      Verificar: abrir a tela de edição em produção e conferir que o QR
      renderiza.
- [ ] `SHARE-02` — Download do QR em SVG e PNG, ambos escaneáveis para o
      mesmo cartão. Verificar: baixar os dois formatos e escanear com a
      câmera de outro celular.
- [ ] `BRAND-01` — **Deferido / não aplicável ainda.** Domínio próprio não
      foi registrado (decisão explícita do usuário, registrada em
      `.planning/STATE.md`). O produto roda em produção sobre
      `https://vcard-app-one.vercel.app` (frontend) e
      `https://vcard-app-tihd.onrender.com` (backend). Quando um domínio for
      registrado, seguir a seção "Trocar o domínio" acima para destravar
      este item.

## Riscos conhecidos e o que monitorar

- **cron-job.org (free tier):** o intervalo mínimo confiável e os limites
  exatos do plano gratuito são suposições não confirmadas por fonte primária
  (`02-RESEARCH.md`, Assumptions A1/A2). Se o cronjob parar de rodar ou o
  intervalo de 5 min deixar de ser respeitado, o fallback é um workflow
  agendado do GitHub Actions (`schedule: cron: '*/5 * * * *'`) rodando um
  `curl` simples contra `/health`.
- **Neon dorme independentemente do keep-alive do Render:** o compute do
  Neon faz autosuspend após 5 min de inatividade *de query*, mesmo com o
  Render sendo mantido acordado a cada 5 min via `/health` (que sim, bate no
  banco via `CanConnectAsync()` — mas o timing pode não coincidir
  perfeitamente). Isso é aceitável porque o wake do Neon é rápido (~500ms) e
  não exige mitigação dedicada.
- **Sem rate limiting no endpoint público** (decisão de MVP, T-02-04 do
  plano `02-04`). Sintomas a observar: picos incomuns de uso de compute no
  Neon (Neon Console -> Usage) ou de banda no Render (Render Dashboard ->
  Metrics) que não correspondem a tráfego orgânico esperado.
- **Sem IaC:** não existe `render.yaml` nem equivalente para a Vercel — todo
  o setup de infraestrutura é manual, feito nos dashboards, por decisão de
  janela de tempo (`02-RESEARCH.md`, Open Question 2). Este runbook
  (`docs/DEPLOY.md`) é a única fonte de reprodutibilidade do ambiente; se ele
  ficar desatualizado, reproduzir o deploy do zero fica mais lento.

## Criação do Web Service no Render

1. Render Dashboard -> New -> Web Service.
2. Conectar ao repositório do projeto (branch `main`).
3. Runtime: **Docker**.
4. Root directory: `apps/api`.
5. Dockerfile path: `apps/api/Dockerfile`.
6. Plano: **Free**.
7. Health check path: `/health`.
8. Preencher as variáveis de ambiente da tabela acima antes do primeiro deploy
   (o app falha no startup se qualquer uma das 5 obrigatórias estiver
   ausente, por causa das leituras eager com `?? throw`).

> **Pitfall:** o campo "Dockerfile Path" do Render é relativo ao "Root
> Directory", não à raiz do repositório. Com Root Directory `apps/api`, o
> Dockerfile Path correto é **`Dockerfile`** (não `apps/api/Dockerfile`).
> Preencher os dois campos como se fossem relativos à raiz do repo produz um
> caminho duplicado e o build falha com
> `lstat .../apps/api/apps: no such file or directory`.

## Migrations

O `apps/api/Program.cs` **não** chama `db.Database.Migrate()` no startup —
migrations são aplicadas manualmente, fora do processo do app, a partir da
máquina de desenvolvimento. Toda nova migration exige rodar isso antes do
próximo deploy que dependa dela:

```bash
dotnet tool install --global dotnet-ef   # ou: dotnet tool update --global dotnet-ef
dotnet ef database update --project apps/api/Api.csproj --connection "<connection string do Neon>"
```

A connection string deve ser passada por variável de ambiente da sessão ou
argumento de linha de comando — **nunca** gravada em `appsettings.json` nem em
`appsettings.Production.json`. A migration inicial (`20260814015302_InitialSchema`)
cria as tabelas `users`, `cards`, `social_links` e `card_views`.

Além de `ConnectionStrings__Default`, o comando `dotnet ef database update`
também exige que `JWT_SECRET`, `Jwt__Issuer`, `Jwt__Audience` e
`Cors__WebOrigin` estejam definidos na sessão (mesmo que com valores
provisórios) — o EF Tools constrói o host via `Program.cs`, que lê essas 5
chaves eagerly antes de `builder.Build()`.

## Keep-alive (PUB-04)

O Render dorme após 15 minutos de inatividade (plano Free) — um cartão
impresso num adesivo, sem visitas recentes, faria o próximo visitante esperar
30-60s de cold start. Para evitar isso, um cron externo bate periodicamente em
`/health`:

- **Serviço:** cron-job.org (gratuito).
- **Alvo:** `GET {RENDER_API_URL}/health`.
- **Intervalo:** 5 minutos — deliberadamente menor que os 14 min máximos
  teóricos, porque schedulers gratuitos têm jitter (variação no horário real
  de execução); um intervalo de 5 min dá margem confortável contra a janela
  de spin-down de 15 min (`02-RESEARCH.md`, Pitfall 3).
- **Alternativas avaliadas e descartadas:** Vercel Cron no plano Hobby é
  limitado a 1 execução/dia, inutilizável aqui; UptimeRobot mudou o ToS do
  plano free em dez/2024 para restringir a uso não comercial, o que colide
  com a intenção de monetização da Fase 4.
- **Fallback:** se o free tier do cron-job.org não permitir intervalos de até
  10 minutos (suposição não confirmada por fonte primária, ver `02-RESEARCH.md`
  Assumption A2), usar um workflow agendado do GitHub Actions
  (`schedule: cron: '*/5 * * * *'`) rodando um `curl` simples contra `/health`.

`/health` faz `db.Database.CanConnectAsync()` — o keep-alive, portanto, também
mantém o compute do Neon acordado por consequência da query, embora o Neon
já acorde sozinho em ~500ms mesmo sem isso.

## Cold start esperado

| Componente | Cenário dormindo | Tempo esperado |
|------------|-------------------|-----------------|
| Render (spin-down 15 min) | Sem tráfego/keep-alive por mais de 15 min | ~30-60s até a primeira resposta |
| Neon (autosuspend 5 min) | Compute suspenso, primeira query após inatividade | ~500ms, automático, sem ação manual |

O keep-alive de 5 minutos existe para evitar o cenário do Render (o custo
alto); o wake do Neon é rápido o suficiente para não precisar de mitigação
dedicada.

## Rotação de segredo (`JWT_SECRET`)

Trocar `JWT_SECRET` em produção invalida **todas** as sessões ativas
imediatamente — comportamento aceito no MVP, já que não existe refresh token.
Para rotacionar:

1. Gerar um novo valor aleatório de 32+ bytes.
2. Atualizar a variável `JWT_SECRET` no Render Dashboard.
3. Fazer redeploy do Web Service (Render reinicia o processo automaticamente
   ao salvar uma env var).
4. Avisar usuários ativos de que precisarão logar novamente — não há
   invalidação seletiva, é global.
</content>
