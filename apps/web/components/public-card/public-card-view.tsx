import { Avatar, AvatarImage } from "@/components/ui/avatar";
import { AvatarPlaceholder } from "@/components/avatar-placeholder";
import type { PublicCardDto } from "@/lib/public-card";

// Renderizacao mobile-first da pagina publica do cartao (PUB-01). Componente de
// apresentacao puro, sem estado -- toda a logica de "esconder secao vazia" (D-19)
// e "estado minimo e valido" (D-21) vive aqui, nao no data-fetch.
type PublicCardViewProps = {
  card: PublicCardDto;
};

export function PublicCardView({ card }: PublicCardViewProps) {
  const subtitle = formatSubtitle(card.role, card.company);
  const hasSocialLinks = card.socialLinks.length > 0;

  return (
    <div className="min-h-screen bg-zinc-50">
      <div className="mx-auto flex w-full max-w-[480px] flex-col items-center px-6 py-16">
        {card.photoUrl ? (
          <Avatar className="size-24">
            <AvatarImage src={card.photoUrl} alt={card.fullName} />
          </Avatar>
        ) : (
          // D-21: nome + iniciais e um estado visual completo por si so -- sem
          // mensagem de "cartao incompleto".
          <AvatarPlaceholder fullName={card.fullName} />
        )}

        <h1 className="mt-2 text-center text-[28px] font-semibold leading-[1.2]">
          {card.fullName}
        </h1>

        {/* D-19: secao de cargo/empresa nao renderiza nada quando ambos estao vazios. */}
        {subtitle && (
          <p className="mt-0 text-center text-base leading-[1.5] text-zinc-600">
            {subtitle}
          </p>
        )}

        {/* D-19: lista de links sociais e omitida por completo do DOM quando vazia. */}
        {hasSocialLinks && (
          <div className="mt-8 flex w-full flex-col gap-2">
            {card.socialLinks.map((link) => (
              <a
                key={`${link.platform}-${link.displayOrder}`}
                href={link.url}
                target="_blank"
                rel="noopener noreferrer"
                className="flex min-h-11 w-full flex-col justify-center gap-0.5 rounded-lg border border-zinc-200 bg-white p-3"
              >
                <span className="text-sm font-semibold leading-[1.5]">
                  {platformLabel(link.platform)}
                </span>
                <span className="truncate text-base leading-[1.5] text-zinc-600">
                  {hostFromUrl(link.url)}
                </span>
              </a>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function formatSubtitle(role: string | null, company: string | null): string | null {
  if (role && company) return `${role} · ${company}`;
  if (role) return role;
  if (company) return company;
  return null;
}

const PLATFORM_LABELS: Record<string, string> = {
  instagram: "Instagram",
  linkedin: "LinkedIn",
  twitter: "Twitter",
  tiktok: "TikTok",
  youtube: "YouTube",
  site: "Site",
};

function platformLabel(platform: string): string {
  return PLATFORM_LABELS[platform] ?? platform;
}

function hostFromUrl(url: string): string {
  try {
    return new URL(url).host;
  } catch {
    return url;
  }
}
