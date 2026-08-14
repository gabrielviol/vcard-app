"use client";

import { useEffect, useRef, useState } from "react";
import { useFormContext } from "react-hook-form";
import type { CardFormValues } from "@/lib/card-schema";
import { useDebouncedValue } from "@/lib/use-debounced-value";
import { apiFetch } from "@/lib/api-client";
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";

type SlugAvailableResponse = {
  available: boolean;
  reason: "taken" | "reserved" | "invalid" | null;
};

type AvailabilityState =
  | { status: "idle" }
  | { status: "checking" }
  | { status: "available" }
  | { status: "unavailable"; reason: "taken" | "reserved" };

// Primeiro campo da tela (D-02). A resposta deste endpoint e SO uma dica de UX --
// a garantia real de unicidade e o 409 do servidor (indice unico + catch de 23505),
// tratado no submit do card-form. Duas abas/usuarios podem ver "disponivel" ao mesmo
// tempo; isso e esperado e coberto pelo 409 no save.
export function SlugField({ currentSlug }: { currentSlug?: string }) {
  const form = useFormContext<CardFormValues>();
  const slug = form.watch("slug");
  const debouncedSlug = useDebouncedValue(slug, 400);
  const [availability, setAvailability] = useState<AvailabilityState>({ status: "idle" });
  const latestCheckedSlug = useRef<string | null>(null);

  useEffect(() => {
    const normalized = (debouncedSlug ?? "").trim().toLowerCase();

    // Slug igual ao ja salvo (modo edicao sem alteracao) -- nao precisa checar.
    if (currentSlug && normalized === currentSlug.toLowerCase()) {
      setAvailability({ status: "idle" });
      return;
    }

    if (!normalized || normalized.length < 3) {
      setAvailability({ status: "idle" });
      return;
    }

    let cancelled = false;
    latestCheckedSlug.current = normalized;
    setAvailability({ status: "checking" });

    apiFetch<SlugAvailableResponse>(
      `/cards/slug-available?value=${encodeURIComponent(normalized)}`,
    )
      .then((result) => {
        // Descarta resposta fora de ordem: se o input ja mudou desde que esta
        // checagem foi disparada, ignora o resultado atrasado.
        if (cancelled || latestCheckedSlug.current !== normalized) return;

        if (result.available) {
          setAvailability({ status: "available" });
        } else if (result.reason === "taken" || result.reason === "reserved") {
          setAvailability({ status: "unavailable", reason: result.reason });
        } else {
          setAvailability({ status: "idle" });
        }
      })
      .catch(() => {
        if (cancelled || latestCheckedSlug.current !== normalized) return;
        setAvailability({ status: "idle" });
      });

    return () => {
      cancelled = true;
    };
  }, [debouncedSlug, currentSlug]);

  const host = typeof window !== "undefined" ? window.location.host : "";

  return (
    <FormField
      control={form.control}
      name="slug"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Link do seu cartão</FormLabel>
          <FormControl>
            <Input
              autoComplete="off"
              autoCapitalize="off"
              spellCheck={false}
              {...field}
              onChange={(e) => field.onChange(e.target.value.toLowerCase())}
            />
          </FormControl>
          <p className="text-sm font-semibold text-zinc-600">
            {host}/{field.value || "seu-link"}
          </p>
          {availability.status === "checking" && (
            <p className="text-sm text-zinc-500">Verificando disponibilidade…</p>
          )}
          {availability.status === "available" && (
            <p className="text-sm font-semibold text-indigo-600">Disponível</p>
          )}
          {availability.status === "unavailable" && availability.reason === "taken" && (
            <p className="text-sm text-red-600">
              Esse link já está em uso. Escolha outro.
            </p>
          )}
          {availability.status === "unavailable" && availability.reason === "reserved" && (
            <p className="text-sm text-red-600">
              Esse link é reservado pelo sistema. Escolha outro.
            </p>
          )}
          <FormMessage />
        </FormItem>
      )}
    />
  );
}
