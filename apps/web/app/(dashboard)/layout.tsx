"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { getToken } from "@/lib/auth-storage";

// Guard client-side do grupo de rotas (dashboard) -- T-01-11.
//
// NAO pode virar middleware.ts: localStorage nao existe no runtime Edge/servidor do
// Next.js Middleware. Este guard e apenas UX; a autorizacao real e o
// .RequireAuthorization() do apps/api (plano 01) -- nenhum dado protegido e
// renderizado antes da resposta real de GET /auth/me.
//
// Por D-07, no reload a UI assume logado com base no token presente, sem estado de
// loading e sem chamar GET /auth/me antes -- so checa a PRESENCA do token no mount.
const PUBLIC_PATHNAMES = ["/login", "/register"];

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (PUBLIC_PATHNAMES.includes(pathname)) return;
    if (!getToken()) {
      router.replace("/login");
    }
  }, [router, pathname]);

  return <>{children}</>;
}
