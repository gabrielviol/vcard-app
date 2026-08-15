// Pre-aquecimento fire-and-forget da URL publica do cartao (PUB-03).
//
// Dispara um GET best-effort contra a propria pagina publica logo apos um save
// bem-sucedido -- atravessa Vercel -> Render -> Neon e acorda os tres antes de
// alguem escanear o QR minutos depois. NAO substitui o keep-alive externo (PUB-04,
// plano 02-05): este mecanismo so dispara quando o dono edita; um cartao parado ha
// semanas depende do cron externo (02-RESEARCH.md Pitfall 4).
//
// Por isso a falha e sempre engolida aqui: um ping de pre-aquecimento que falha
// jamais deve virar erro de salvamento para o usuario -- diverge deliberadamente da
// cadeia de tratamento de ApiError de card-form.tsx, que trata falhas reais de
// persistencia.

import { buildCardUrl } from "@/lib/qr";

export function prewarmPublicCard(slug: string): void {
  if (!slug || !slug.trim()) {
    return;
  }

  let url: string;
  try {
    url = buildCardUrl(slug);
  } catch {
    // buildCardUrl lanca quando NEXT_PUBLIC_APP_URL nao esta definida -- sem base
    // nao ha para onde disparar o ping, e isso tambem nao pode virar erro de save.
    return;
  }

  // Sem await deliberadamente -- nao pode bloquear o toast de sucesso do save.
  fetch(url, { cache: "no-store" }).catch(() => {
    // Silenciamento deliberado (PUB-03): pre-aquecimento e best-effort, o keep-alive
    // externo (PUB-04) e o fallback durável. Uma falha de ping nunca deve aparecer
    // para o usuario nem interromper o fluxo de salvamento.
  });
}
