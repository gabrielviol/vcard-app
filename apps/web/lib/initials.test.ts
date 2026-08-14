import { describe, expect, it } from "vitest";
import { getAvatarColor, getInitials } from "@/lib/initials";

describe("getInitials", () => {
  it("usa a primeira letra do primeiro e do ultimo nome", () => {
    expect(getInitials("Gabriel Oliveira")).toBe("GO");
  });

  it("usa so a primeira letra quando ha apenas uma palavra", () => {
    expect(getInitials("Conceição")).toBe("C");
  });

  it("ignora a preposicao do meio e usa a ultima palavra real", () => {
    expect(getInitials("João da Silva")).toBe("JS");
  });

  it("devolve string vazia para entrada vazia", () => {
    expect(getInitials("")).toBe("");
  });

  it("devolve string vazia para nulo/indefinido", () => {
    expect(getInitials(null)).toBe("");
    expect(getInitials(undefined)).toBe("");
  });

  it("ignora espacos multiplos e nas pontas", () => {
    expect(getInitials("  Ana   Paula  ")).toBe("AP");
  });
});

describe("getAvatarColor", () => {
  it("e deterministica -- mesma entrada, mesma saida", () => {
    expect(getAvatarColor("Gabriel Oliveira")).toBe(getAvatarColor("Gabriel Oliveira"));
  });

  it("devolve sempre uma classe Tailwind de background", () => {
    expect(getAvatarColor("Gabriel Oliveira")).toMatch(/^bg-[a-z]+-\d{3}$/);
  });

  it("lida com entrada vazia sem lancar erro", () => {
    expect(() => getAvatarColor("")).not.toThrow();
    expect(getAvatarColor("")).toMatch(/^bg-[a-z]+-\d{3}$/);
  });

  it("tende a variar entre nomes diferentes", () => {
    expect(getAvatarColor("Gabriel Oliveira")).not.toBe(getAvatarColor("Zoe Nakamura"));
  });
});
