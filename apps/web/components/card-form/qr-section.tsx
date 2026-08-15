import { Button } from "@/components/ui/button";
import { QrPreview } from "@/components/public-card/qr-preview";

// Secao "Seu QR code" da tela de edicao (SHARE-01, SHARE-02). Sempre visivel, sem
// clique previo (D-16) -- por isso nao ha modal, accordion nem botao "Ver QR" aqui.
// Antes do cartao existir (modo criacao, sem slug ainda), mostra a nota "salve
// primeiro" em vez de um QR quebrado, espelhando o padrao ja usado em
// social-links-section.tsx para o mesmo tipo de estado.
type QrSectionProps = {
  slug?: string;
};

export function QrSection({ slug }: QrSectionProps) {
  if (!slug) {
    return (
      <section className="flex flex-col gap-4">
        <h2 className="text-[20px] font-semibold leading-[1.2]">Seu QR code</h2>
        <div className="rounded-lg border border-dashed border-zinc-200 p-8 text-center">
          <p className="text-base leading-[1.5] text-zinc-600">
            Salve o cartão para gerar seu QR code.
          </p>
        </div>
      </section>
    );
  }

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-[20px] font-semibold leading-[1.2]">Seu QR code</h2>
      <div className="flex flex-col items-center gap-2">
        <QrPreview slug={slug} />
        <p className="text-base leading-[1.5] text-zinc-600">
          Aponte a câmera para abrir seu cartão
        </p>
      </div>
      <div className="flex flex-col gap-2 sm:flex-row">
        <Button asChild variant="outline">
          <a href={`/${slug}/qr?download=1`} download>
            Baixar SVG
          </a>
        </Button>
        <Button asChild variant="outline">
          <a href={`/${slug}/qr?format=png&download=1`} download>
            Baixar PNG
          </a>
        </Button>
      </div>
    </section>
  );
}
