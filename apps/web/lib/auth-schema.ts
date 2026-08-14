import { z } from "zod";

export const registerSchema = z.object({
  email: z.email("E-mail inválido."),
  // Teto de 72 caracteres (WR-02) espelha o limite de bytes do bcrypt no backend --
  // aproximação por caracteres (não bytes UTF-8), suficiente para feedback client-side;
  // o servidor continua sendo a autoridade sobre o limite real de 72 bytes.
  password: z
    .string()
    .min(8, "A senha precisa ter pelo menos 8 caracteres.")
    .max(72, "A senha pode ter no máximo 72 caracteres."),
});

export type RegisterFormValues = z.infer<typeof registerSchema>;

export const loginSchema = z.object({
  email: z.email("E-mail inválido."),
  password: z.string().min(1, "Informe sua senha."),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
