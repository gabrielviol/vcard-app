import { z } from "zod";
import { isValidWhatsapp } from "@/lib/whatsapp-normalize";

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
  photoUrl: z.string().optional(),
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
  pixKey: z.string().optional(),
  pixKeyType: z.string().optional(),
  pixConsentConfirmed: z.boolean().optional(),
});

export type CardFormValues = z.infer<typeof cardSchema>;
