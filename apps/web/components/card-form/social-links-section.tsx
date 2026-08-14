"use client";

import { useState } from "react";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { toast } from "sonner";
import type { SocialLink } from "@/components/card-form/card-form";
import { ApiError, apiFetch } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { SocialLinkRow, type SocialPlatform } from "@/components/card-form/social-link-row";

type SocialLinksSectionProps = {
  // Ausente no modo create (o cartao ainda nao tem id) -- ver nota fixa abaixo.
  cardId?: string;
  initialLinks: SocialLink[];
};

const GENERIC_SAVE_ERROR = "Não foi possível salvar. Verifique sua conexão e tente novamente.";

// Secao de links sociais (CARD-10/D-03): adicionar, remover e reordenar por arraste, com
// persistencia via endpoints proprios (nao pelo PUT /cards/{id} do resto do formulario) --
// por isso gerencia o proprio estado a partir de initialLinks, fora do useForm do cartao.
export function SocialLinksSection({ cardId, initialLinks }: SocialLinksSectionProps) {
  const [links, setLinks] = useState<SocialLink[]>(initialLinks);
  const [draft, setDraft] = useState<{ platform: SocialPlatform; url: string } | null>(null);
  const [savingDraft, setSavingDraft] = useState(false);
  const [removingId, setRemovingId] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  // Modo create: o cartao ainda nao existe, entao nao ha cardId para os endpoints de
  // link social funcionarem -- nota fixa em vez da secao interativa (plano 07, acao 6).
  if (!cardId) {
    return (
      <section className="flex flex-col gap-4">
        <h2 className="text-[20px] font-semibold leading-[1.2]">Links sociais</h2>
        <div className="rounded-lg border border-dashed border-zinc-200 p-8 text-center">
          <p className="text-base leading-[1.5] text-zinc-600">
            Salve o cartão para começar a adicionar links.
          </p>
        </div>
      </section>
    );
  }

  async function handleAddConfirm() {
    if (!draft) return;
    setSavingDraft(true);
    try {
      const created = await apiFetch<SocialLink>(`/cards/${cardId}/social-links`, {
        method: "POST",
        body: JSON.stringify({ platform: draft.platform, url: draft.url }),
      });
      setLinks((prev) => [...prev, created]);
      setDraft(null);
    } catch (error) {
      if (error instanceof ApiError && error.code === "platform_invalid") {
        toast.error("Plataforma inválida.");
      } else if (error instanceof ApiError && error.code === "url_invalid") {
        toast.error("URL inválida. Use um link https válido.");
      } else {
        toast.error(GENERIC_SAVE_ERROR);
      }
    } finally {
      setSavingDraft(false);
    }
  }

  async function handleRemove(linkId: string) {
    setRemovingId(linkId);
    try {
      await apiFetch<void>(`/cards/${cardId}/social-links/${linkId}`, { method: "DELETE" });
      // Resequencia local espelhando o servidor (SocialLinkService.Resequence) -- nunca
      // deixa buraco entre os display_order restantes.
      setLinks((prev) => prev.filter((link) => link.id !== linkId).map((link, index) => ({ ...link, displayOrder: index })));
    } catch {
      toast.error(GENERIC_SAVE_ERROR);
    } finally {
      setRemovingId(null);
    }
  }

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = links.findIndex((link) => link.id === active.id);
    const newIndex = links.findIndex((link) => link.id === over.id);
    if (oldIndex === -1 || newIndex === -1) return;

    const previous = links;
    const reordered = arrayMove(links, oldIndex, newIndex).map((link, index) => ({
      ...link,
      displayOrder: index,
    }));
    setLinks(reordered);

    try {
      await apiFetch<SocialLink[]>(`/cards/${cardId}/social-links/order`, {
        method: "PUT",
        body: JSON.stringify({ orderedIds: reordered.map((link) => link.id) }),
      });
    } catch {
      // Reverte para a ordem anterior em caso de falha (plano 07, acao 3).
      setLinks(previous);
      toast.error(GENERIC_SAVE_ERROR);
    }
  }

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-[20px] font-semibold leading-[1.2]">Links sociais</h2>

      {links.length === 0 && !draft ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-zinc-200 p-8 text-center">
          <p className="text-base font-semibold leading-[1.2]">Nenhum link ainda</p>
          <p className="text-base leading-[1.5] text-zinc-600">
            Adicione Instagram, LinkedIn ou outro link para aparecer no seu cartão.
          </p>
        </div>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={links.map((link) => link.id)} strategy={verticalListSortingStrategy}>
            <ul className="flex flex-col gap-2">
              {links.map((link) => (
                <SocialLinkRow
                  key={link.id}
                  mode="persisted"
                  link={link}
                  onRemove={handleRemove}
                  removing={removingId === link.id}
                />
              ))}
            </ul>
          </SortableContext>
        </DndContext>
      )}

      {draft ? (
        <SocialLinkRow
          mode="draft"
          platform={draft.platform}
          url={draft.url}
          onPlatformChange={(platform) => setDraft((current) => (current ? { ...current, platform } : current))}
          onUrlChange={(url) => setDraft((current) => (current ? { ...current, url } : current))}
          onConfirm={handleAddConfirm}
          onCancel={() => setDraft(null)}
          saving={savingDraft}
        />
      ) : (
        <Button type="button" variant="outline" onClick={() => setDraft({ platform: "instagram", url: "" })}>
          Adicionar link
        </Button>
      )}
    </section>
  );
}
