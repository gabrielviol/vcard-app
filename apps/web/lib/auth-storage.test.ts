import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { clearToken, getToken, setToken } from "./auth-storage";

// Ambiente do vitest.config.ts e "node" (funcoes puras, sem DOM real) -- por isso os
// testes que precisam de um `window.localStorage` funcional usam um stub minimo via
// `vi.stubGlobal`, em vez de puxar jsdom como dependencia nova so para este arquivo.
function stubBrowserWindow() {
  const store = new Map<string, string>();
  vi.stubGlobal("window", {
    localStorage: {
      getItem: (key: string) => (store.has(key) ? store.get(key)! : null),
      setItem: (key: string, value: string) => {
        store.set(key, value);
      },
      removeItem: (key: string) => {
        store.delete(key);
      },
    },
  });
}

describe("auth-storage", () => {
  beforeEach(() => {
    stubBrowserWindow();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("setToken then getToken returns the stored value", () => {
    setToken("abc123");
    expect(getToken()).toBe("abc123");
  });

  it("clearToken then getToken returns null", () => {
    setToken("abc123");
    clearToken();
    expect(getToken()).toBeNull();
  });

  it("getToken does not throw and returns null when window is undefined (server env)", () => {
    vi.unstubAllGlobals();
    expect(typeof window).toBe("undefined");

    expect(() => getToken()).not.toThrow();
    expect(getToken()).toBeNull();
  });

  it("setToken and clearToken do not throw when window is undefined (server env)", () => {
    vi.unstubAllGlobals();
    expect(typeof window).toBe("undefined");

    expect(() => setToken("abc123")).not.toThrow();
    expect(() => clearToken()).not.toThrow();
  });
});
