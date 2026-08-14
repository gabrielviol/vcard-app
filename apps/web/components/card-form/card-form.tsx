"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { cardSchema, type CardFormValues } from "@/lib/card-schema";
import { PIX_ERROR_MESSAGES, type PixKeyType } from "@/lib/pix-validation";

const KNOWN_PIX_TYPES: readonly PixKeyType[] = ["cpf", "cnpj", "email", "telefone", "aleatoria"];

// O DTO do backend tipa pixKeyType como string solta (apps/api/Contracts/CardDtos.cs); o
// schema do form exige um dos 5 literais conhecidos ou "". Um cartao ja salvo sempre tem um
// tipo valido (o servidor recusa qualquer outro via pix_type_invalid), mas o cast aqui e
// defensivo -- um valor inesperado vira "" em vez de quebrar o formulario.
function toPixKeyType(value: string | null | undefined): PixKeyType | "" {
  return KNOWN_PIX_TYPES.includes(value as PixKeyType) ? (value as PixKeyType) : "";
}
import { ApiError, apiFetch } from "@/lib/api-client";
import { clearToken } from "@/lib/auth-storage";
import { Button } from "@/components/ui/button";
import { SlugField } from "@/components/card-form/slug-field";
import { IdentitySection } from "@/components/card-form/identity-section";
import { ContactSection } from "@/components/card-form/contact-section";
import { PixSection } from "@/components/card-form/pix-section";
import { SocialLinksSection } from "@/components/card-form/social-links-section";

export type SocialLink = {
  id: string;
  platform: string;
  url: string;
  displayOrder: number;
};

// Espelha CardResponseDto do backend (apps/api/Contracts/CardDtos.cs) -- contrato
// definido neste plano, consumido sem redescoberta pelos planos 04-07.
export type CardResponseDto = {
  id: string;
  slug: string;
  fullName: string;
  role: string | null;
  company: string | null;
  photoUrl: string | null;
  phone: string | null;
  email: string | null;
  whatsappNumber: string | null;
  pixKey: string | null;
  pixKeyType: string | null;
  pixConsentConfirmed: boolean;
  isBranded: boolean;
  createdAt: string;
  updatedAt: string;
  socialLinks: SocialLink[];
};

type CardFormProps = {
  mode: "create" | "edit";
  initialCard?: CardResponseDto;
};

function toDefaultValues(initialCard?: CardResponseDto): CardFormValues {
  return {
    slug: initialCard?.slug ?? "",
    fullName: initialCard?.fullName ?? "",
    role: initialCard?.role ?? "",
    company: initialCard?.company ?? "",
    photoUrl: initialCard?.photoUrl ?? "",
    phone: initialCard?.phone ?? "",
    email: initialCard?.email ?? "",
    whatsappNumber: initialCard?.whatsappNumber ?? "",
    pixKey: initialCard?.pixKey ?? "",
    pixKeyType: toPixKeyType(initialCard?.pixKeyType),
    pixConsentConfirmed: initialCard?.pixConsentConfirmed ?? false,
  };
}

export function CardForm({ mode, initialCard }: CardFormProps) {
  const router = useRouter();
  const [pixReopenSignal, setPixReopenSignal] = useState(0);

  const form = useForm<CardFormValues>({
    resolver: zodResolver(cardSchema),
    defaultValues: toDefaultValues(initialCard),
  });

  function handleLogout() {
    clearToken();
    router.push("/login");
  }

  async function onSubmit(values: CardFormValues) {
    const payload = {
      slug: values.slug,
      fullName: values.fullName,
      role: values.role || null,
      company: values.company || null,
      photoUrl: values.photoUrl || null,
      phone: values.phone || null,
      email: values.email || null,
      whatsappNumber: values.whatsappNumber || null,
      pixKey: values.pixKey || null,
      pixKeyType: values.pixKeyType || null,
      pixConsentConfirmed: values.pixConsentConfirmed ?? false,
    };

    try {
      if (mode === "create") {
        const created = await apiFetch<CardResponseDto>("/cards", {
          method: "POST",
          body: JSON.stringify(payload),
        });
        router.push(`/dashboard/cards/${created.id}/edit`);
        return;
      }

      await apiFetch<CardResponseDto>(`/cards/${initialCard!.id}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      toast.success("Alterações salvas.");
    } catch (error) {
      if (error instanceof ApiError && error.code === "slug_taken") {
        form.setError("slug", {
          type: "server",
          message: "Esse link já está em uso. Escolha outro.",
        });
        return;
      }
      if (error instanceof ApiError && error.code === "slug_reserved") {
        form.setError("slug", {
          type: "server",
          message: "Esse link é reservado pelo sistema. Escolha outro.",
        });
        return;
      }
      if (error instanceof ApiError && error.code === "pix_key_invalid") {
        const type = form.getValues("pixKeyType") as PixKeyType | "" | undefined;
        form.setError("pixKey", {
          type: "server",
          message: (type && PIX_ERROR_MESSAGES[type]) || "Chave Pix inválida.",
        });
        return;
      }
      if (error instanceof ApiError && error.code === "pix_type_invalid") {
        form.setError("pixKeyType", {
          type: "server",
          message: "Tipo de chave Pix inválido.",
        });
        return;
      }
      if (error instanceof ApiError && error.code === "photo_url_invalid") {
        form.setError("photoUrl", {
          type: "server",
          message: "URL da foto inválida.",
        });
        return;
      }
      if (error instanceof ApiError && error.code === "pix_consent_required") {
        // Reabre o modal bloqueante de CPF (D-09) -- caminho defensivo, o cliente ja
        // barra este estado antes do submit via superRefine em card-schema.ts.
        setPixReopenSignal((n) => n + 1);
        return;
      }
      toast.error("Não foi possível salvar. Verifique sua conexão e tente novamente.");
    }
  }

  return (
    <div className="min-h-screen bg-zinc-50 px-4 py-16">
      <div className="mx-auto flex w-full max-w-2xl flex-col gap-12">
        <header className="flex items-center justify-between">
          <h1 className="text-[28px] font-semibold leading-[1.2]">
            {mode === "create" ? "Criar seu cartão" : "Editar cartão"}
          </h1>
          <Button variant="outline" onClick={handleLogout}>
            Sair
          </Button>
        </header>

        <FormProvider {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-col gap-8"
          >
            <SlugField currentSlug={initialCard?.slug} />
            <IdentitySection />
            <ContactSection />
            <PixSection reopenConfirmSignal={pixReopenSignal} />
            <SocialLinksSection cardId={initialCard?.id} initialLinks={initialCard?.socialLinks ?? []} />

            <Button
              type="submit"
              disabled={form.formState.isSubmitting}
              className="bg-indigo-600 text-white hover:bg-indigo-700"
            >
              {mode === "create" ? "Criar cartão" : "Salvar alterações"}
            </Button>
          </form>
        </FormProvider>
      </div>
    </div>
  );
}
