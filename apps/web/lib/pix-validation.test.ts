import { describe, expect, it } from "vitest";
import { formatPixKey, isValidPixKey } from "@/lib/pix-validation";

describe("isValidPixKey - cpf", () => {
  it("aceita CPF valido conhecido (com e sem mascara)", () => {
    expect(isValidPixKey("cpf", "295.379.955-93")).toBe(true);
    expect(isValidPixKey("cpf", "29537995593")).toBe(true);
  });

  it("rejeita CPF com digito verificador errado", () => {
    expect(isValidPixKey("cpf", "29537995594")).toBe(false);
  });

  it("rejeita CPF de digitos repetidos (soma ponderada ingenua deixaria passar)", () => {
    expect(isValidPixKey("cpf", "11111111111")).toBe(false);
    expect(isValidPixKey("cpf", "00000000000")).toBe(false);
  });
});

describe("isValidPixKey - cnpj", () => {
  it("aceita CNPJ numerico valido", () => {
    expect(isValidPixKey("cnpj", "54.550.752/0001-55")).toBe(true);
  });

  it("rejeita CNPJ numerico invalido", () => {
    expect(isValidPixKey("cnpj", "54.550.752/0001-56")).toBe(false);
  });

  it("aceita CNPJ alfanumerico da RFB (exemplo canonico publicado, pergunta 14 do PDF oficial)", () => {
    // Vetor oficial citado na 01-RESEARCH.md Open Question 1 e no README da lib.
    // Resultado real registrado no SUMMARY conforme instrucao da action step 3.
    expect(isValidPixKey("cnpj", "12.ABC.345/01DE-35")).toBe(true);
    expect(isValidPixKey("cnpj", "12ABC34501DE35")).toBe(true);
  });
});

describe("isValidPixKey - email", () => {
  it("rejeita e-mail invalido", () => {
    expect(isValidPixKey("email", "nao-e-email")).toBe(false);
  });

  it("aceita e-mail valido", () => {
    expect(isValidPixKey("email", "dono@example.com")).toBe(true);
  });
});

describe("isValidPixKey - telefone", () => {
  it("aceita telefone com +55", () => {
    expect(isValidPixKey("telefone", "+55 11 98765-4321")).toBe(true);
  });

  it("aceita telefone sem +55", () => {
    expect(isValidPixKey("telefone", "11987654321")).toBe(true);
  });

  it("rejeita telefone com poucos digitos", () => {
    expect(isValidPixKey("telefone", "12345")).toBe(false);
  });
});

describe("isValidPixKey - aleatoria", () => {
  it("aceita UUID v4 valido", () => {
    expect(isValidPixKey("aleatoria", "9c858901-8a57-4791-81fe-4c455b099bc9")).toBe(true);
  });

  it("rejeita UUID v1 (versao errada)", () => {
    expect(isValidPixKey("aleatoria", "9c858901-8a57-1791-81fe-4c455b099bc9")).toBe(false);
  });

  it("rejeita string qualquer", () => {
    expect(isValidPixKey("aleatoria", "nao-e-um-uuid")).toBe(false);
  });
});

describe("isValidPixKey - casos gerais", () => {
  it("rejeita tipo desconhecido", () => {
    expect(isValidPixKey("bitcoin", "qualquer-valor")).toBe(false);
  });

  it("rejeita chave vazia/nula", () => {
    expect(isValidPixKey("cpf", "")).toBe(false);
    expect(isValidPixKey("cpf", null)).toBe(false);
    expect(isValidPixKey(null, "29537995593")).toBe(false);
  });
});

describe("formatPixKey", () => {
  it("formata CPF com pontos e hifen", () => {
    expect(formatPixKey("cpf", "29537995593")).toBe("295.379.955-93");
  });

  it("formata CNPJ numerico", () => {
    expect(formatPixKey("cnpj", "54550752000155")).toBe("54.550.752/0001-55");
  });

  it("formata CNPJ alfanumerico preservando os caracteres alfanumericos", () => {
    expect(formatPixKey("cnpj", "12ABC34501DE35")).toBe("12.ABC.345/01DE-35");
  });

  it("formata telefone com +55 DD XXXXX-XXXX", () => {
    expect(formatPixKey("telefone", "11987654321")).toBe("+55 11 98765-4321");
  });

  it("devolve e-mail e chave aleatoria como digitados", () => {
    expect(formatPixKey("email", "dono@example.com")).toBe("dono@example.com");
    expect(formatPixKey("aleatoria", "9c858901-8a57-4791-81fe-4c455b099bc9")).toBe(
      "9c858901-8a57-4791-81fe-4c455b099bc9",
    );
  });

  it("devolve string vazia para chave vazia", () => {
    expect(formatPixKey("cpf", "")).toBe("");
  });
});
