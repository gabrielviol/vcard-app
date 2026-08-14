"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api-client";
import { CardForm, type CardResponseDto } from "@/components/card-form/card-form";

export default function EditCardPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const [card, setCard] = useState<CardResponseDto | null>(null);

  useEffect(() => {
    let cancelled = false;

    apiFetch<CardResponseDto>("/cards/me")
      .then((result) => {
        if (cancelled) return;

        // O id da rota precisa bater com o cartao do usuario logado -- se nao bate
        // (ex: id de outro cartao, ou id antigo apos troca), redireciona para o id
        // correto em vez de deixar o form editar o cartao errado.
        if (result.id !== params.id) {
          router.replace(`/dashboard/cards/${result.id}/edit`);
          return;
        }

        setCard(result);
      })
      .catch((error) => {
        if (cancelled) return;
        if (error instanceof ApiError && error.status === 404) {
          router.replace("/dashboard/cards/new");
          return;
        }
        // Outros erros ja tratados pelo interceptor de api-client.ts (401 limpa o
        // token e redireciona); nada mais a fazer aqui.
      });

    return () => {
      cancelled = true;
    };
  }, [params.id, router]);

  if (!card) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-zinc-50">
        <p className="text-base text-zinc-500">Carregando…</p>
      </div>
    );
  }

  return <CardForm mode="edit" initialCard={card} />;
}
