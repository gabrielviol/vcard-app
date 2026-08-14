import { describe, expect, it } from "vitest";
import {
  formatWhatsappPreview,
  isValidWhatsapp,
  normalizeWhatsapp,
} from "@/lib/whatsapp-normalize";

describe("normalizeWhatsapp", () => {
  it("adiciona o nono digito para DDD 11 (SP) com 8 digitos locais", () => {
    expect(normalizeWhatsapp("1187654321")).toBe("5511987654321");
  });

  it("mantem inalterado um numero de DDD 11 que ja tem 9 digitos locais", () => {
    expect(normalizeWhatsapp("11987654321")).toBe("5511987654321");
  });

  it("NAO adiciona nono digito para DDD 85 (Fortaleza) com 8 digitos locais", () => {
    expect(normalizeWhatsapp("8587654321")).toBe("558587654321");
  });

  it("adiciona o nono digito para DDD 27 (ES) com 8 digitos locais", () => {
    expect(normalizeWhatsapp("2734567890")).toBe("5527934567890");
  });

  it("e idempotente para entrada ja com +55 e mascara completa", () => {
    expect(normalizeWhatsapp("+55 (11) 98765-4321")).toBe("5511987654321");
  });

  it("remove um zero de tronco a esquerda", () => {
    expect(normalizeWhatsapp("011987654321")).toBe("5511987654321");
  });

  it("remove a mascara (11) 98765-4321", () => {
    expect(normalizeWhatsapp("(11) 98765-4321")).toBe("5511987654321");
  });

  it("devolve string vazia para entrada vazia", () => {
    expect(normalizeWhatsapp("")).toBe("");
  });

  it("devolve string vazia para entrada nula/indefinida", () => {
    expect(normalizeWhatsapp(null)).toBe("");
    expect(normalizeWhatsapp(undefined)).toBe("");
  });
});

describe("formatWhatsappPreview", () => {
  it("formata numero de 9 digitos locais", () => {
    expect(formatWhatsappPreview("11987654321")).toBe("+55 11 98765-4321");
  });

  it("formata numero de 8 digitos locais fora da whitelist do nono digito", () => {
    expect(formatWhatsappPreview("8587654321")).toBe("+55 85 8765-4321");
  });

  it("devolve string vazia para entrada vazia", () => {
    expect(formatWhatsappPreview("")).toBe("");
  });
});

describe("isValidWhatsapp", () => {
  it("rejeita numero com poucos digitos (5)", () => {
    expect(isValidWhatsapp("12345")).toBe(false);
  });

  it("aceita numero nacional com 10 digitos (DDD + 8 digitos locais)", () => {
    expect(isValidWhatsapp("8587654321")).toBe(true);
  });

  it("aceita numero nacional com 11 digitos (DDD + 9 digitos locais)", () => {
    expect(isValidWhatsapp("11987654321")).toBe(true);
  });

  it("rejeita entrada vazia", () => {
    expect(isValidWhatsapp("")).toBe(false);
  });
});
