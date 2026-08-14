import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { getAvatarColor, getInitials } from "@/lib/initials";
import { cn } from "@/lib/utils";

// Fallback de iniciais em círculo colorido (D-12) -- usado sempre que o cartão não
// tem `photoUrl`. Cor determinística por nome via getAvatarColor; tamanho controlável
// por className (o tamanho default do Avatar do shadcn é pequeno demais para o slot
// de foto do formulário, então o chamador normalmente sobrescreve com "size-24" etc).
type AvatarPlaceholderProps = {
  fullName: string | null | undefined;
  className?: string;
};

export function AvatarPlaceholder({ fullName, className }: AvatarPlaceholderProps) {
  const initials = getInitials(fullName);
  const colorClass = getAvatarColor(fullName || "?");

  return (
    <Avatar className={cn("size-24", className)}>
      <AvatarFallback
        className={cn("text-lg font-semibold text-white", colorClass)}
      >
        {initials || "?"}
      </AvatarFallback>
    </Avatar>
  );
}
