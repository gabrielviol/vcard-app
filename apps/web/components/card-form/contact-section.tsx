"use client";

import { useFormContext } from "react-hook-form";
import type { CardFormValues } from "@/lib/card-schema";
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { PhoneMaskInput } from "@/components/card-form/phone-mask-input";
import { isValidWhatsapp } from "@/lib/whatsapp-normalize";

// Telefone, e-mail e WhatsApp -- todos opcionais (D-04). Telefone e WhatsApp usam o
// mesmo componente de mascara BR (mesma formatacao); e-mail e um input simples ligado
// pelo mesmo useFormContext usado no resto da tela unica (D-03).
export function ContactSection() {
  const form = useFormContext<CardFormValues>();

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-[20px] font-semibold leading-[1.2]">Contato</h2>
      <PhoneMaskInput name="phone" label="Telefone" />
      <FormField
        control={form.control}
        name="email"
        render={({ field }) => (
          <FormItem>
            <FormLabel>E-mail</FormLabel>
            <FormControl>
              <Input
                type="email"
                autoComplete="email"
                {...field}
                value={field.value ?? ""}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <PhoneMaskInput
        name="whatsappNumber"
        label="WhatsApp"
        isValid={isValidWhatsapp}
        invalidMessage="Número de WhatsApp inválido."
      />
    </section>
  );
}
