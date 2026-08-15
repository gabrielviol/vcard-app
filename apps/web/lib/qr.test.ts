import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
  buildCardUrl,
  buildQrResponseHeaders,
  parseQrRequest,
  renderQrPng,
  renderQrSvg,
} from "@/lib/qr";

// Cobertura de SHARE-01/SHARE-02 nas funcoes puras de lib/qr.ts (D-15, D-17, D-18).

describe("buildCardUrl", () => {
  const originalAppUrl = process.env.NEXT_PUBLIC_APP_URL;

  beforeEach(() => {
    process.env.NEXT_PUBLIC_APP_URL = "http://localhost:3000";
  });

  afterEach(() => {
    process.env.NEXT_PUBLIC_APP_URL = originalAppUrl;
  });

  it("monta a URL publica do cartao a partir do slug", () => {
    expect(buildCardUrl("joao-silva")).toBe("http://localhost:3000/joao-silva");
  });

  it("nao produz barra dupla quando a base termina em barra", () => {
    process.env.NEXT_PUBLIC_APP_URL = "http://localhost:3000/";
    expect(buildCardUrl("joao-silva")).toBe("http://localhost:3000/joao-silva");
  });

  it("aplica encodeURIComponent ao slug", () => {
    expect(buildCardUrl("a b")).toBe("http://localhost:3000/a%20b");
  });

  it("lanca Error quando NEXT_PUBLIC_APP_URL nao esta definida", () => {
    delete process.env.NEXT_PUBLIC_APP_URL;
    expect(() => buildCardUrl("joao")).toThrow("NEXT_PUBLIC_APP_URL not set");
  });
});

describe("parseQrRequest", () => {
  it("retorna o default svg/false para query vazia", () => {
    expect(parseQrRequest(new URLSearchParams(""))).toEqual({
      format: "svg",
      download: false,
    });
  });

  it("reconhece format=png e download=1", () => {
    expect(parseQrRequest(new URLSearchParams("format=png&download=1"))).toEqual({
      format: "png",
      download: true,
    });
  });

  it("cai no default para valores nao reconhecidos", () => {
    expect(parseQrRequest(new URLSearchParams("format=jpeg&download=true"))).toEqual({
      format: "svg",
      download: false,
    });
  });
});

describe("buildQrResponseHeaders", () => {
  it("svg sem download: Content-Type svg, sem Content-Disposition", () => {
    const headers = buildQrResponseHeaders({ format: "svg", download: false, slug: "joao" });
    expect(headers["Content-Type"]).toBe("image/svg+xml");
    expect(headers["Content-Disposition"]).toBeUndefined();
  });

  it("svg com download: inclui Content-Disposition com filename .svg", () => {
    const headers = buildQrResponseHeaders({ format: "svg", download: true, slug: "joao" });
    expect(headers["Content-Disposition"]).toBe('attachment; filename="joao-qr.svg"');
  });

  it("png com download: Content-Type png e filename .png", () => {
    const headers = buildQrResponseHeaders({ format: "png", download: true, slug: "joao" });
    expect(headers["Content-Type"]).toBe("image/png");
    expect(headers["Content-Disposition"]).toBe('attachment; filename="joao-qr.png"');
  });
});

describe("renderQrSvg", () => {
  it("resolve para uma string SVG preta no branco", async () => {
    const svg = await renderQrSvg("http://localhost:3000/joao");
    expect(svg.startsWith("<svg")).toBe(true);
    expect(svg).toMatch(/fill="#ffffff"/i);
  });
});

describe("renderQrPng", () => {
  it("resolve para um Buffer PNG de 1024px de largura", async () => {
    const png = await renderQrPng("http://localhost:3000/joao");
    const pngSignature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
    expect(png.subarray(0, 8).equals(pngSignature)).toBe(true);

    // IHDR chunk comeca no byte 16 (8 assinatura + 4 tamanho + 4 tipo "IHDR"), largura
    // eh um uint32 big-endian nos 4 bytes seguintes.
    const width = png.readUInt32BE(16);
    expect(width).toBe(1024);
  });
});
