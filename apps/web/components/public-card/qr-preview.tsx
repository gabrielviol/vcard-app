// Previa do QR code do cartao (SHARE-01). Sempre visivel na tela de edicao, nunca
// atras de um clique (D-16), e nunca tingida -- preto no branco puro (D-18).
//
// <img> puro, sem o componente de imagem otimizada do Next.js: o recurso e um SVG
// gerado pela Route Handler /{slug}/qr (plano 02-02), e o otimizador de imagem da
// Vercel nao agrega nada aqui.
type QrPreviewProps = {
  slug: string;
};

export function QrPreview({ slug }: QrPreviewProps) {
  return (
    <img
      src={`/${slug}/qr`}
      alt="QR code do cartão"
      width={240}
      height={240}
      className="rounded-lg border border-zinc-200 bg-white"
    />
  );
}
