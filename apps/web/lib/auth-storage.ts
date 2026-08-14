// Leitura/escrita/limpeza do accessToken em localStorage (D-05).
//
// Todas as funcoes sao seguras quando `typeof window === "undefined"` porque este
// modulo pode ser importado por codigo que roda durante o build/SSR (Server Component,
// ou o proprio Next.js ao coletar tipos de rota), onde `localStorage` nao existe.

const TOKEN_KEY = "accessToken";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(TOKEN_KEY);
}
