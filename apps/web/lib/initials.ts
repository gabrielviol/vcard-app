// Placeholder de iniciais quando o cartão não tem foto (D-12).
//
// getInitials: até 2 letras maiúsculas -- primeira letra da primeira palavra +
// primeira letra da última palavra do nome. Nome de uma palavra só usa apenas essa
// letra. Tolerante a acento (toUpperCase já trata ã/ç/õ corretamente em JS) e a
// espaços múltiplos/nas pontas.
//
// getAvatarColor: cor determinística por hash simples da string (mesma entrada,
// mesma saída sempre) -- paleta fixa de classes Tailwind, decisão de UI deixada a
// critério do executor (01-CONTEXT.md "Claude's Discretion").

const AVATAR_COLOR_PALETTE = [
  "bg-indigo-500",
  "bg-emerald-500",
  "bg-amber-500",
  "bg-rose-500",
  "bg-sky-500",
  "bg-violet-500",
  "bg-teal-500",
  "bg-orange-500",
] as const;

export function getInitials(fullName: string | null | undefined): string {
  const words = (fullName ?? "")
    .trim()
    .split(/\s+/)
    .filter(Boolean);

  if (words.length === 0) return "";
  if (words.length === 1) return words[0].charAt(0).toUpperCase();

  const first = words[0].charAt(0);
  const last = words[words.length - 1].charAt(0);
  return `${first}${last}`.toUpperCase();
}

export function getAvatarColor(seed: string | null | undefined): string {
  const value = seed ?? "";
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) | 0;
  }
  const index = Math.abs(hash) % AVATAR_COLOR_PALETTE.length;
  return AVATAR_COLOR_PALETTE[index];
}
