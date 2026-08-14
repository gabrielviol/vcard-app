import { z } from "zod";
import { isValidWhatsapp } from "@/lib/whatsapp-normalize";
import { isValidPixKey, PIX_ERROR_MESSAGES, type PixKeyType } from "@/lib/pix-validation";

// Unicos campos obrigatorios sao slug e fullName (D-04) -- todo o resto fica opcional
// desde ja para que os planos 04-07 apenas refinem regras (WhatsApp, Pix, links
// sociais) em vez de reescrever este schema.
export const cardSchema = z.object({
  slug: z
    .string()
    .min(3, "Use apenas letras minúsculas, números e hífen.")
    .max(32, "Use apenas letras minúsculas, números e hífen.")
    .regex(/^[a-z0-9]+(-[a-z0-9]+)*$/, "Use apenas letras minúsculas, números e hífen."),
  fullName: z.string().min(2, "Informe o nome completo."),
  role: z.string().optional(),
  company: z.string().optional(),
  // Opcional (D-04/D-12); quando preenchida, precisa ser uma URL https -- espelha a
  // validacao de host permitido que o servidor aplica (CardEndpoints.ValidatePhotoUrl).
  photoUrl: z
    .string()
    .optional()
    .refine((value) => !value || (z.url().safeParse(value).success && value.startsWith("https://")), {
      message: "URL da foto inválida.",
    }),
  phone: z.string().optional(),
  // Opcional (D-04), mas quando preenchido precisa ser um e-mail valido.
  email: z
    .string()
    .optional()
    .refine((value) => !value || z.email().safeParse(value).success, {
      message: "E-mail inválido.",
    }),
  // Opcional (D-04); a mascara/normalizacao real acontece no servidor (CARD-08) -- este
  // refine so bloqueia o submit no cliente quando o numero digitado nao fecha a regra
  // do nono digito por DDD (isValidWhatsapp espelha WhatsappNormalizer.IsValid).
  whatsappNumber: z
    .string()
    .optional()
    .refine((value) => !value || isValidWhatsapp(value), {
      message: "Número de WhatsApp inválido.",
    }),
  // Opcionais (D-04). pixKeyType so aceita os 5 valores conhecidos; pixKey e validada por
  // tipo via superRefine abaixo (precisa dos dois campos juntos, discriminatedUnion sozinho
  // nao da porque pixKeyType/pixKey convivem com o resto dos campos do cartao no mesmo objeto).
  pixKey: z.string().optional(),
  pixKeyType: z.enum(["cpf", "cnpj", "email", "telefone", "aleatoria"]).optional().or(z.literal("")),
  pixConsentConfirmed: z.boolean().optional(),
}).superRefine((values, ctx) => {
  if (values.pixKey && values.pixKeyType) {
    if (!isValidPixKey(values.pixKeyType as PixKeyType, values.pixKey)) {
      ctx.addIssue({
        code: "custom",
        path: ["pixKey"],
        message: PIX_ERROR_MESSAGES[values.pixKeyType as PixKeyType],
      });
    }

    // Mesma regra do servidor (CARD-07/D-09): CPF preenchida exige consentimento
    // explicito para que o botao de salvar reflita o estado real, nao so a UI do modal.
    if (values.pixKeyType === "cpf" && !values.pixConsentConfirmed) {
      ctx.addIssue({
        code: "custom",
        path: ["pixConsentConfirmed"],
        message: "Confirme que quer usar seu CPF como chave Pix.",
      });
    }
  }
});

export type CardFormValues = z.infer<typeof cardSchema>;
