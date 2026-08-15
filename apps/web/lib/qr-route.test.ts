import { beforeEach, describe, expect, it } from "vitest";
import { GET } from "@/app/[slug]/qr/route";
import { buildCardUrl, renderQrSvg } from "@/lib/qr";

// Cobertura do Route Handler real GET /{slug}/qr (Task 3, 02-02-PLAN.md). Mora em
// lib/ (nao em app/[slug]/qr/) porque vitest.config.ts restringe include a lib/**.

function makeRequest(path: string): Request {
  return new Request(`http://localhost:3000${path}`);
}

function makeContext(slug: string) {
  return { params: Promise.resolve({ slug }) };
}

describe("GET /{slug}/qr", () => {
  beforeEach(() => {
    process.env.NEXT_PUBLIC_APP_URL = "http://localhost:3000";
  });

  it("responde SVG inline por padrao, sem Content-Disposition", async () => {
    const response = await GET(makeRequest("/joao/qr"), makeContext("joao"));

    expect(response.status).toBe(200);
    expect(response.headers.get("content-type")).toBe("image/svg+xml");
    expect(response.headers.get("content-disposition")).toBeNull();

    const body = await response.text();
    expect(body.startsWith("<svg")).toBe(true);
  });

  it("com ?download=1 inclui Content-Disposition attachment", async () => {
    const response = await GET(makeRequest("/joao/qr?download=1"), makeContext("joao"));

    expect(response.headers.get("content-disposition")).toBe(
      'attachment; filename="joao-qr.svg"',
    );
  });

  it("com ?format=png responde PNG sem Content-Disposition", async () => {
    const response = await GET(makeRequest("/joao/qr?format=png"), makeContext("joao"));

    expect(response.headers.get("content-type")).toBe("image/png");
    expect(response.headers.get("content-disposition")).toBeNull();

    const buffer = Buffer.from(await response.arrayBuffer());
    const pngSignature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
    expect(buffer.subarray(0, 8).equals(pngSignature)).toBe(true);
  });

  it("com ?format=png&download=1 responde PNG com Content-Disposition", async () => {
    const response = await GET(
      makeRequest("/joao/qr?format=png&download=1"),
      makeContext("joao"),
    );

    expect(response.headers.get("content-type")).toBe("image/png");
    expect(response.headers.get("content-disposition")).toBe(
      'attachment; filename="joao-qr.png"',
    );
  });

  it("codifica exatamente buildCardUrl(slug) no QR", async () => {
    const response = await GET(makeRequest("/joao/qr"), makeContext("joao"));
    const body = await response.text();

    const expectedSvg = await renderQrSvg(buildCardUrl("joao"));
    expect(body).toBe(expectedSvg);
  });
});
