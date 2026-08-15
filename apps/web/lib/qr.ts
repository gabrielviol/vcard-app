// Funcoes puras de QR code (SHARE-01, SHARE-02).
//
// Nenhuma chamada de rede/banco aqui -- so transformacao de string e geracao de
// imagem a partir de uma URL ja montada. Isso e o que garante que a rota publica
// /{slug}/qr nunca herda o cold start do apps/api (D-15).
//
// D-17: QR puro, sem legenda/texto embutido. D-18: sempre preto no branco, sem cor
// de marca -- por isso renderQrSvg/renderQrPng nao recebem nenhuma opcao de cor.

import QRCode from "qrcode";

export type QrFormat = "svg" | "png";

/**
 * Monta a URL publica do cartao a partir do slug, usando SEMPRE
 * NEXT_PUBLIC_APP_URL como base -- nunca um header da requisicao (Host,
 * X-Forwarded-Host) e nunca um domínio hardcoded no código (D-15, T-02-07).
 */
export function buildCardUrl(slug: string): string {
  const base = process.env.NEXT_PUBLIC_APP_URL;
  if (!base) {
    throw new Error("NEXT_PUBLIC_APP_URL not set");
  }

  const trimmedBase = base.endsWith("/") ? base.slice(0, -1) : base;
  return `${trimmedBase}/${encodeURIComponent(slug)}`;
}

/**
 * Interpreta a query string da rota /{slug}/qr. Apenas "png" e reconhecido como
 * formato alternativo e apenas "1" liga o download -- qualquer outro valor cai no
 * default (svg, sem download).
 */
export function parseQrRequest(searchParams: URLSearchParams): {
  format: QrFormat;
  download: boolean;
} {
  const format: QrFormat = searchParams.get("format") === "png" ? "png" : "svg";
  const download = searchParams.get("download") === "1";
  return { format, download };
}

/**
 * Monta os headers de resposta HTTP do QR. A chave Content-Disposition so existe
 * quando download=true (D-16) -- setar attachment incondicionalmente quebraria a
 * previa <img src> usada na tela de edicao do cartao (plano 02-04, 02-RESEARCH.md).
 */
export function buildQrResponseHeaders(opts: {
  format: QrFormat;
  download: boolean;
  slug: string;
}): Record<string, string> {
  const { format, download, slug } = opts;
  const headers: Record<string, string> = {
    "Content-Type": format === "png" ? "image/png" : "image/svg+xml",
  };

  if (download) {
    headers["Content-Disposition"] = `attachment; filename="${slug}-qr.${format}"`;
  }

  return headers;
}

/**
 * Gera o QR como SVG inline (previa na tela). Preto no branco, sem cor de marca
 * (D-18); errorCorrectionLevel M e o suficiente porque a v1 nao embute logo (CLAUDE.md
 * raiz, secao 1).
 */
export async function renderQrSvg(url: string): Promise<string> {
  return QRCode.toString(url, { type: "svg", errorCorrectionLevel: "M" });
}

/**
 * Gera o QR como PNG de 1024px de largura (download para impressao). Mesmo padrao
 * de cor/erro que renderQrSvg.
 */
export async function renderQrPng(url: string): Promise<Buffer> {
  return QRCode.toBuffer(url, { type: "png", width: 1024, errorCorrectionLevel: "M" });
}
