"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";

// Error boundary do segmento publico /[slug]. Cobre o caso NAO-404: backend
// frio que estourou, API fora do ar, timeout de rede -- complementa a 404
// dedicada (not-found.tsx, caso o slug simplesmente nao exista). error.js
// exige "use client" (contrato do App Router).
//
// T-02-11: NUNCA renderizar os detalhes internos do objeto de erro (mensagem
// ou stack trace) na tela -- eles sao usados apenas para log no console (util
// em producao via digest, sem expor nada ao usuario). fetchPublicCard
// (lib/public-card.ts) lanca excecoes que contem a URL do backend e o status
// HTTP, o que vazaria detalhe interno numa pagina publica anonima se fosse
// renderizado. Usar somente a copy fixa travada em 02-UI-SPEC.md na UI.
export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Log apenas no console/servico de observabilidade -- nunca na tela.
    console.error(error);
  }, [error]);

  return (
    <div className="min-h-screen bg-zinc-50">
      <div className="mx-auto flex w-full max-w-[480px] flex-col items-center px-6 py-16">
        <p className="text-center text-base leading-[1.5] text-zinc-600">
          Não foi possível carregar esse cartão agora. Tente novamente em
          instantes.
        </p>

        <Button
          variant="outline"
          onClick={reset}
          className="mt-8"
        >
          Tentar de novo
        </Button>
      </div>
    </div>
  );
}
