import { notFound } from "next/navigation";
import { PublicCardView } from "@/components/public-card/public-card-view";
import { fetchPublicCard } from "@/lib/public-card";

// PUB-02/PUB-05: ISR por tempo, sem revalidacao sob demanda no MVP -- decisao ja
// travada em CLAUDE.md (raiz) secao 4. Uma defasagem de ate 60s entre salvar no
// dashboard e o publico ver a mudanca e aceitavel.
export const revalidate = 60;

export default async function PublicCardPage({ params }: PageProps<"/[slug]">) {
  const { slug } = await params;
  const card = await fetchPublicCard(slug);

  if (!card) {
    // PUB-06: slug inexistente -- aciona app/[slug]/not-found.tsx (fase de 404
    // dedicada, plano 02-03). Nenhum loading.tsx neste segmento: streaming
    // rebaixaria o status HTTP de 404 para 200 (02-RESEARCH.md Pitfall 2).
    notFound();
  }

  return <PublicCardView card={card} />;
}
