"use client";

import { useRef, useState } from "react";
import { upload } from "@vercel/blob/client";
import { useFormContext } from "react-hook-form";
import { toast } from "sonner";
import { X } from "lucide-react";
import type { CardFormValues } from "@/lib/card-schema";
import { getToken } from "@/lib/auth-storage";
import { AvatarPlaceholder } from "@/components/avatar-placeholder";
import { Button } from "@/components/ui/button";

const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp"];

// Upload direto do navegador para o Vercel Blob (CARD-09), prévia e remoção. Sem
// editor de crop (D-13) -- a imagem é ajustada só via CSS `object-fit: cover` no
// círculo de prévia, igual ao layout final do cartão.
export function PhotoSection() {
  const form = useFormContext<CardFormValues>();
  const photoUrl = form.watch("photoUrl");
  const fullName = form.watch("fullName");
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);

  async function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    // Permite reenviar o mesmo arquivo depois (ex: tentar de novo após erro).
    event.target.value = "";
    if (!file) return;

    if (!ALLOWED_TYPES.includes(file.type)) {
      toast.error("Não foi possível enviar a foto. Tente novamente.");
      return;
    }

    const token = getToken();
    setUploading(true);
    setProgress(0);
    try {
      const blob = await upload(file.name, file, {
        access: "public",
        handleUploadUrl: "/api/upload",
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
        onUploadProgress: ({ percentage }) => setProgress(percentage),
      });
      form.setValue("photoUrl", blob.url, {
        shouldValidate: true,
        shouldDirty: true,
      });
    } catch {
      toast.error("Não foi possível enviar a foto. Tente novamente.");
    } finally {
      setUploading(false);
      setProgress(0);
    }
  }

  function handleRemove() {
    form.setValue("photoUrl", "", { shouldValidate: true, shouldDirty: true });
  }

  return (
    <div className="flex items-center gap-4">
      <div className="relative">
        {photoUrl ? (
          <img
            src={photoUrl}
            alt="Foto do cartão"
            className="size-24 rounded-full object-cover"
          />
        ) : (
          <AvatarPlaceholder fullName={fullName} />
        )}
        {photoUrl && (
          <button
            type="button"
            onClick={handleRemove}
            aria-label="Remover foto"
            className="absolute -right-1 -bottom-1 flex size-11 items-center justify-center rounded-full bg-white text-red-600 shadow ring-1 ring-zinc-200"
          >
            <X className="size-5" />
          </button>
        )}
      </div>

      <div className="flex flex-col gap-2">
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="hidden"
          onChange={handleFileChange}
        />
        <Button
          type="button"
          variant="outline"
          disabled={uploading}
          onClick={() => fileInputRef.current?.click()}
        >
          {uploading ? `Enviando... ${progress}%` : "Enviar foto"}
        </Button>
      </div>
    </div>
  );
}
