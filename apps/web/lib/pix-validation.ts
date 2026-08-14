// Validacao e formatacao de chave Pix por tipo (CARD-06, D-10).
//
// ESPELHO: este arquivo e apps/api/Services/PixValidationService.cs implementam a MESMA
// logica nos dois lados (cliente e servidor) -- precisam mudar juntos. O cliente usa isto
// para prever/validar em tempo real (D-10); o servidor e a autoridade real (nunca confiar
// no formato/consentimento que o cliente enviou -- T-01-27/T-01-28, mesma logica de
// whatsapp-normalize.ts para o WhatsApp).
import { z } from "zod";
import { cpf, cnpj } from "cpf-cnpj-validator";

export type PixKeyType = "cpf" | "cnpj" | "email" | "telefone" | "aleatoria";

const uuidV4Regex = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const brPhoneRegex = /^(\+?55\s?)?\(?\d{2}\)?\s?9?\d{4}-?\d{4}$/;

// Mensagens de erro exatas travadas em 01-UI-SPEC.md.
export const PIX_ERROR_MESSAGES: Record<PixKeyType, string> = {
  cpf: "CPF inválido.",
  cnpj: "CNPJ inválido.",
  email: "E-mail inválido.",
  telefone: "Telefone inválido.",
  aleatoria: "Chave aleatória inválida.",
};

export const pixKeySchema = z.discriminatedUnion("pixKeyType", [
  z.object({
    pixKeyType: z.literal("cpf"),
    pixKey: z.string().refine((value) => cpf.isValid(value), PIX_ERROR_MESSAGES.cpf),
  }),
  z.object({
    pixKeyType: z.literal("cnpj"),
    pixKey: z.string().refine((value) => cnpj.isValid(value), PIX_ERROR_MESSAGES.cnpj),
  }),
  z.object({
    pixKeyType: z.literal("email"),
    pixKey: z.string().refine((value) => z.email().safeParse(value).success, PIX_ERROR_MESSAGES.email),
  }),
  z.object({
    pixKeyType: z.literal("telefone"),
    pixKey: z.string().regex(brPhoneRegex, PIX_ERROR_MESSAGES.telefone),
  }),
  z.object({
    pixKeyType: z.literal("aleatoria"),
    pixKey: z.string().regex(uuidV4Regex, PIX_ERROR_MESSAGES.aleatoria),
  }),
]);

/** Valida a chave conforme o tipo. Tipo desconhecido sempre invalida. */
export function isValidPixKey(type: PixKeyType | string | null | undefined, key: string | null | undefined): boolean {
  if (!type || !key) return false;

  switch (type) {
    case "cpf":
      return cpf.isValid(key);
    case "cnpj":
      return cnpj.isValid(key);
    case "email":
      return z.email().safeParse(key).success;
    case "telefone":
      return brPhoneRegex.test(key);
    case "aleatoria":
      return uuidV4Regex.test(key);
    default:
      return false;
  }
}

/**
 * Previa formatada da chave (D-10): CPF/CNPJ ganham mascara de pontuacao (preservando
 * caracteres alfanumericos do novo formato de CNPJ da RFB); telefone ganha "+55 DD XXXXX-XXXX";
 * e-mail e chave aleatoria sao devolvidos como digitados (nao ha mascara aplicavel).
 */
export function formatPixKey(type: PixKeyType | string | null | undefined, key: string | null | undefined): string {
  if (!key) return "";

  switch (type) {
    case "cpf":
      return cpf.isValid(key) ? cpf.format(key) : key;
    case "cnpj":
      return cnpj.isValid(key) ? cnpj.format(key) : key;
    case "telefone": {
      const digits = key.replace(/\D/g, "");
      const national = digits.startsWith("55") && digits.length > 10 ? digits.slice(2) : digits;
      const ddd = national.slice(0, 2);
      const local = national.slice(2);
      if (local.length === 9) {
        return `+55 ${ddd} ${local.slice(0, 5)}-${local.slice(5)}`;
      }
      if (local.length === 8) {
        return `+55 ${ddd} ${local.slice(0, 4)}-${local.slice(4)}`;
      }
      return key;
    }
    case "email":
    case "aleatoria":
    default:
      return key;
  }
}
