"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiFetch } from "@/lib/api-client";
import { clearToken } from "@/lib/auth-storage";
import { Button } from "@/components/ui/button";

type Me = { id: string; email: string };

// Esta chamada a GET /auth/me e a prova fim a fim do walking skeleton (D-06/D-07):
// dispara o redirect reativo de 401 quando o token expira/e invalido, e e a unica
// fonte de verdade para o e-mail exibido -- o layout do grupo (dashboard) so checa a
// PRESENCA do token, nunca a validade dele.
export default function DashboardPage() {
  const router = useRouter();
  const [me, setMe] = useState<Me | null>(null);

  useEffect(() => {
    apiFetch<Me>("/auth/me")
      .then(setMe)
      .catch(() => {
        // Erros ja tratados pelo interceptor de api-client.ts (401 limpa o token e
        // redireciona); outros erros de rede simplesmente deixam a tela sem dado.
      });
  }, []);

  function handleLogout() {
    clearToken();
    router.push("/login");
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 bg-zinc-50 px-4">
      <div className="flex flex-col items-center gap-2 text-center">
        <h1 className="text-[20px] font-semibold leading-[1.2]">Dashboard</h1>
        <p className="text-base text-zinc-600">{me ? me.email : "Carregando..."}</p>
      </div>
      <Button variant="outline" onClick={handleLogout}>
        Sair
      </Button>
    </div>
  );
}
