"use client";

import { useRouter } from "next/navigation";
import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";
import { cardSchema, type CardFormValues } from "@/lib/card-schema";
import { ApiError, apiFetch } from "@/lib/api-client";
import { clearToken } from "@/lib/auth-storage";
import { Button } from "@/components/ui/button";
import { SlugField } from "@/components/card-form/slug-field";
import { IdentitySection } from "@/components/card-form/identity-section";
import { ContactSection } from "@/components/card-form/contact-section";

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
    pixKeyType: initialCard?.pixKeyType ?? "",
    pixConsentConfirmed: initialCard?.pixConsentConfirmed ?? false,
  };
}

// Blocos de secao reservados para os planos 04 (Contato), 05 (Pix) e 07 (Links
// sociais) -- mantem a ordem estavel definida na UI-SPEC; ate la, so o titulo da
// secao existe, sem nenhum componente/campo importado (nao ha nada a preencher
// ainda, nao e um stub de dado -- e um espaco reservado de layout).
function ReservedSection({ title }: { title: string }) {
  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-[20px] font-semibold leading-[1.2]">{title}</h2>
    </section>
  );
}

export function CardForm({ mode, initialCard }: CardFormProps) {
  const router = useRouter();

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
            <ReservedSection title="Pix" />
            <ReservedSection title="Links sociais" />

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
