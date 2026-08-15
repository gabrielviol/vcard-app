import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { prewarmPublicCard } from "@/lib/prewarm";

// Cobertura de PUB-03: disparo fire-and-forget e silenciamento deliberado da falha
// (D-15, 02-RESEARCH.md Pitfall 4 -- este mecanismo NAO substitui o keep-alive externo,
// so cobre o caso "dono acabou de salvar").

describe("prewarmPublicCard", () => {
  const originalAppUrl = process.env.NEXT_PUBLIC_APP_URL;

  beforeEach(() => {
    process.env.NEXT_PUBLIC_APP_URL = "http://localhost:3000";
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 200 })));
  });

  afterEach(() => {
    process.env.NEXT_PUBLIC_APP_URL = originalAppUrl;
    vi.unstubAllGlobals();
  });

  it("chama fetch exatamente uma vez com a URL de buildCardUrl e cache: no-store", () => {
    prewarmPublicCard("joao-silva");

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch).toHaveBeenCalledWith(
      "http://localhost:3000/joao-silva",
      expect.objectContaining({ cache: "no-store" }),
    );
  });

  it("retorna undefined de forma sincrona, nao um Promise", () => {
    const result = prewarmPublicCard("joao-silva");
    expect(result).toBeUndefined();
  });

  it("nao propaga excecao nem gera unhandled rejection quando fetch rejeita", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    expect(() => prewarmPublicCard("joao-silva")).not.toThrow();

    // Da chance da promise (e o seu .catch) resolverem antes do teste terminar --
    // se o .catch nao estivesse presente, isso apareceria como unhandledRejection.
    await Promise.resolve();
    await Promise.resolve();
  });

  it("nao chama fetch quando NEXT_PUBLIC_APP_URL esta ausente", () => {
    delete process.env.NEXT_PUBLIC_APP_URL;

    expect(() => prewarmPublicCard("joao-silva")).not.toThrow();
    expect(fetch).not.toHaveBeenCalled();
  });

  it("nao chama fetch para slug vazio", () => {
    prewarmPublicCard("");

    expect(fetch).not.toHaveBeenCalled();
  });
});
