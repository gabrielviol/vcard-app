// Espelha PublicCardDto do backend (apps/api/Contracts/PublicCardDtos.cs) -- contrato
// publico minimo (PUB-01/PUB-05): identidade + links sociais, nunca telefone/email/
// whatsapp/pix/timestamps. Deliberadamente separado de api-client.ts: sem token de
// sessao, sem storage do navegador nem `window` -- este helper roda dentro de um
// Server Component (app/[slug]/page.tsx), onde essas APIs de browser nem existem.
export type PublicSocialLinkDto = {
  platform: string;
  url: string;
  displayOrder: number;
};

export type PublicCardDto = {
  slug: string;
  fullName: string;
  role: string | null;
  company: string | null;
  photoUrl: string | null;
  socialLinks: PublicSocialLinkDto[];
};

export async function fetchPublicCard(slug: string): Promise<PublicCardDto | null> {
  const baseUrl = process.env.NEXT_PUBLIC_API_URL;
  if (!baseUrl) {
    throw new Error("NEXT_PUBLIC_API_URL not set");
  }

  const res = await fetch(`${baseUrl}/public/cards/${encodeURIComponent(slug)}`);

  if (res.status === 404) {
    return null;
  }

  if (!res.ok) {
    throw new Error(`public card fetch failed: ${res.status}`);
  }

  return (await res.json()) as PublicCardDto;
}
