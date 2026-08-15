import { buildCardUrl, buildQrResponseHeaders, parseQrRequest, renderQrPng, renderQrSvg } from "@/lib/qr";

// Route Handler GET /{slug}/qr (SHARE-01, SHARE-02, D-16). Fica no runtime Node
// (default deste arquivo -- nenhum export const runtime e declarado aqui de proposito):
// qrcode usa pngjs, puro JS mas incompativel com o runtime Edge, ver CLAUDE.md raiz
// secao 1/7. Toda a decisao de MIME type, nome de arquivo e parsing de query string
// vem de @/lib/qr -- este arquivo so orquestra.
export async function GET(request: Request, { params }: RouteContext<"/[slug]/qr">) {
  const { slug } = await params;
  const { format, download } = parseQrRequest(new URL(request.url).searchParams);
  const url = buildCardUrl(slug);
  const headers = buildQrResponseHeaders({ format, download, slug });

  if (format === "png") {
    // Buffer<ArrayBufferLike> nao satisfaz BodyInit diretamente (conflito de generics
    // entre @types/node e lib.dom.d.ts) -- Uint8Array simples resolve.
    const png = await renderQrPng(url);
    return new Response(new Uint8Array(png), { headers });
  }

  return new Response(await renderQrSvg(url), { headers });
}
