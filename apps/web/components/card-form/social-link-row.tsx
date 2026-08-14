"use client";

import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, X } from "lucide-react";
import type { SocialLink } from "@/components/card-form/card-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

// Mesma allow-list do backend (apps/api/Services/SocialLinkService.cs) -- espelho, nao
// autoridade: o servidor recusa qualquer outro valor com 400 platform_invalid.
export const SOCIAL_PLATFORM_OPTIONS = [
  { value: "instagram", label: "Instagram" },
  { value: "linkedin", label: "LinkedIn" },
  { value: "twitter", label: "Twitter" },
  { value: "tiktok", label: "TikTok" },
  { value: "youtube", label: "YouTube" },
  { value: "site", label: "Site" },
] as const;

export type SocialPlatform = (typeof SOCIAL_PLATFORM_OPTIONS)[number]["value"];

function platformLabel(platform: string): string {
  return SOCIAL_PLATFORM_OPTIONS.find((option) => option.value === platform)?.label ?? platform;
}

type PersistedRowProps = {
  mode: "persisted";
  link: SocialLink;
  onRemove: (linkId: string) => void;
  removing: boolean;
};

type DraftRowProps = {
  mode: "draft";
  platform: SocialPlatform;
  url: string;
  onPlatformChange: (platform: SocialPlatform) => void;
  onUrlChange: (url: string) => void;
  onConfirm: () => void;
  onCancel: () => void;
  saving: boolean;
};

type SocialLinkRowProps = PersistedRowProps | DraftRowProps;

// Linha de link social (CARD-10): modo "persisted" (ja gravado -- handle de arraste +
// plataforma/URL somente leitura + Remover, ja que os endpoints nao expoem edicao de um
// link existente, so criacao/remocao/reordenacao) e modo "draft" (formulario de criacao
// -- select de plataforma + input de URL editaveis, confirmado via POST pelo pai).
export function SocialLinkRow(props: SocialLinkRowProps) {
  if (props.mode === "draft") {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-zinc-200 bg-white p-3">
        <Select value={props.platform} onValueChange={(value) => props.onPlatformChange(value as SocialPlatform)}>
          <SelectTrigger className="w-36 shrink-0">
            <SelectValue placeholder="Plataforma" />
          </SelectTrigger>
          <SelectContent>
            {SOCIAL_PLATFORM_OPTIONS.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          autoComplete="off"
          placeholder="https://"
          value={props.url}
          onChange={(event) => props.onUrlChange(event.target.value)}
          className="flex-1"
        />
        <Button type="button" size="sm" disabled={props.saving} onClick={props.onConfirm}>
          {props.saving ? "Adicionando..." : "Adicionar"}
        </Button>
        <button
          type="button"
          onClick={props.onCancel}
          aria-label="Cancelar"
          disabled={props.saving}
          className="flex size-11 shrink-0 items-center justify-center text-zinc-600"
        >
          <X className="size-5" />
        </button>
      </div>
    );
  }

  return <PersistedRow {...props} />;
}

function PersistedRow({ link, onRemove, removing }: PersistedRowProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: link.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      className={`flex items-center gap-2 rounded-lg border border-zinc-200 bg-white p-3 ${isDragging ? "opacity-50" : ""}`}
    >
      <button
        type="button"
        {...attributes}
        {...listeners}
        aria-label="Arrastar para reordenar"
        className="flex size-11 shrink-0 cursor-grab items-center justify-center text-zinc-600 active:cursor-grabbing"
      >
        <GripVertical className="size-5" />
      </button>
      <div className="flex flex-1 flex-col gap-0.5 overflow-hidden">
        <span className="text-sm font-semibold leading-[1.5]">{platformLabel(link.platform)}</span>
        <span className="truncate text-base leading-[1.5] text-zinc-600">{link.url}</span>
      </div>
      <button
        type="button"
        onClick={() => onRemove(link.id)}
        disabled={removing}
        aria-label="Remover"
        className="flex size-11 shrink-0 items-center justify-center text-red-600"
      >
        <X className="size-5" />
      </button>
    </li>
  );
}
