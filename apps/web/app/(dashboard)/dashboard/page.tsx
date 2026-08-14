"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api-client";
import type { CardResponseDto } from "@/components/card-form/card-form";

// Sem dashboard vazio intermediario (D-01): so redireciona para o formulario de
// criacao (sem cartao) ou de edicao (com cartao). Nenhuma UI propria renderizada
// aqui -- este componente e so a logica de roteamento pos-login.
export default function DashboardPage() {
  const router = useRouter();

  useEffect(() => {
    let cancelled = false;

    apiFetch<CardResponseDto>("/cards/me")
      .then((card) => {
        if (cancelled) return;
        router.replace(`/dashboard/cards/${card.id}/edit`);
      })
      .catch((error) => {
        if (cancelled) return;
        if (error instanceof ApiError && error.status === 404) {
          router.replace("/dashboard/cards/new");
          return;
        }
        // Outros erros ja tratados pelo interceptor de api-client.ts (401 limpa o
        // token e redireciona).
      });

    return () => {
      cancelled = true;
    };
  }, [router]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-zinc-50">
      <p className="text-base text-zinc-500">Carregando…</p>
    </div>
  );
}
