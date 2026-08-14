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
import { WhatsappInput } from "@/components/card-form/whatsapp-input";

// Telefone, e-mail e WhatsApp -- todos opcionais (D-04). O WhatsApp tem seu proprio
// componente (mascara + previa, D-11); telefone e e-mail sao inputs simples ligados
// pelo mesmo useFormContext usado no resto da tela unica (D-03).
export function ContactSection() {
  const form = useFormContext<CardFormValues>();

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-[20px] font-semibold leading-[1.2]">Contato</h2>
      <FormField
        control={form.control}
        name="phone"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Telefone</FormLabel>
            <FormControl>
              <Input
                type="tel"
                autoComplete="tel"
                {...field}
                value={field.value ?? ""}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
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
      <WhatsappInput />
    </section>
  );
}
