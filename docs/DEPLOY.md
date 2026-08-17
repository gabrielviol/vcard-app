# Runbook de Deploy — Produção

Este documento descreve como o vCard App roda em produção e como reproduzir o
ambiente do zero. Nenhum segredo real vive aqui — todos os valores sensíveis
ficam nas variáveis de ambiente dos respectivos dashboards (Render, Vercel).

## Topologia

```
Vercel (apps/web, Next.js)  --HTTPS-->  Render (apps/api, container Docker)  --TLS-->  Neon (Postgres)
```

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
| `Cors__WebOrigin` | Origem exata do frontend em produção, ex: `https://<projeto>.vercel.app` | Valor provisório aceitável agora (domínio final entra no plano 02-06); nunca usar `*` — `Program.cs` já restringe a uma única origem via `policy.WithOrigins(webOrigin)` |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Deve bater com `EXPOSE 8080` do `apps/api/Dockerfile` |

## Variáveis de ambiente da Vercel (`apps/web`)

| Variável | Origem do valor | Observação |
|----------|------------------|------------|
| `NEXT_PUBLIC_API_URL` | URL pública do Web Service no Render (ex: `https://vcard-api-xxxx.onrender.com`) | Consumida pelo frontend para chamar o backend |
| `NEXT_PUBLIC_APP_URL` | Domínio do produto em produção | Valor provisório `https://<projeto>.vercel.app` aceitável agora — troca para o domínio `.com.br` final acontece no plano 02-06, sem custo de código (D-15 proíbe URL hardcoded em qualquer lugar do app) |
| `BLOB_READ_WRITE_TOKEN` | Já existente desde a Fase 1 (Vercel Blob) | Não precisa ser recriado |

Os valores finais de `NEXT_PUBLIC_API_URL`/`NEXT_PUBLIC_APP_URL` para o domínio
definitivo são preenchidos no plano `02-06`, quando `apps/web` é efetivamente
publicado.

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
