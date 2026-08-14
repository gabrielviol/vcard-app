// Fetch wrapper com Authorization Bearer e interceptor de 401 (D-05/D-06/D-07).
//
// Comportamento estritamente reativo por decisao (D-06): nenhum refresh proativo,
// nenhuma revalidacao preventiva. So a proxima chamada de API que voltar 401 dispara
// a limpeza do token e o redirect para /login.

import { clearToken, getToken } from "./auth-storage";

export class ApiError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

type ApiFetchInit = RequestInit & { auth?: boolean };

export async function apiFetch<T>(path: string, init: ApiFetchInit = {}): Promise<T> {
  const baseUrl = process.env.NEXT_PUBLIC_API_URL;
  if (!baseUrl) {
    throw new Error("NEXT_PUBLIC_API_URL not set");
  }

  const { auth, headers, ...rest } = init;
  const finalHeaders = new Headers(headers);
  finalHeaders.set("Content-Type", "application/json");

  if (auth !== false) {
    const token = getToken();
    if (token) {
      finalHeaders.set("Authorization", `Bearer ${token}`);
    }
  }

  const res = await fetch(`${baseUrl}${path}`, {
    ...rest,
    headers: finalHeaders,
  });

  if (res.status === 401) {
    clearToken();
    // Destino fixo, relativo, nunca vindo da resposta do servidor -- fecha open
    // redirect (T-01-12). Redirect duro (nao router.push) porque este interceptor
    // pode ser chamado fora da arvore do React.
    if (typeof window !== "undefined") {
      window.location.replace("/login?expired=1");
    }
    throw new ApiError(401, "unauthorized", "Sessao expirada.");
  }

  if (!res.ok) {
    let code = "unknown_error";
    let message = "Nao foi possivel completar a operacao.";
    try {
      const body = await res.json();
      if (body?.error) {
        code = body.error;
        message = body.error;
      }
    } catch {
      // corpo nao-JSON ou vazio -- mantem os defaults
    }
    throw new ApiError(res.status, code, message);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return (await res.json()) as T;
}
