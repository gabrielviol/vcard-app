"use client";

import { IMaskInput } from "react-imask";
import type { MaskedDynamic } from "imask";
import { useFormContext } from "react-hook-form";
import type { CardFormValues } from "@/lib/card-schema";
import { formatWhatsappPreview, isValidWhatsapp } from "@/lib/whatsapp-normalize";
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";

// Mascara dinamica BR: alterna entre 10 e 11 digitos (fixo/8-digitos vs celular com o
// nono digito) conforme a quantidade de digitos ja preenchidos, usando o mecanismo de
// `dispatch` do imask para trocar de mascara em tempo real (D-11).
const whatsappMask = [{ mask: "(00) 0000-0000" }, { mask: "(00) 00000-0000" }];

function dispatchWhatsappMask(appended: string, dynamicMasked: MaskedDynamic) {
  const digits = `${dynamicMasked.value}${appended}`.replace(/\D/g, "");
  return digits.length > 10
    ? dynamicMasked.compiledMasks[1]
    : dynamicMasked.compiledMasks[0];
}

// Campo de WhatsApp com mascara brasileira ao vivo e previa do formato final
// normalizado (D-11). O valor do form permanece o texto digitado (com mascara) -- a
// normalizacao para digito puro + DDI 55 que de fato e persistida acontece no servidor
// (WhatsappNormalizer.Normalize), nao aqui.
export function WhatsappInput() {
  const form = useFormContext<CardFormValues>();

  return (
    <FormField
      control={form.control}
      name="whatsappNumber"
      render={({ field }) => {
        const value = field.value ?? "";
        const filled = value.trim().length > 0;
        const valid = !filled || isValidWhatsapp(value);
        const preview = filled && valid ? formatWhatsappPreview(value) : "";

        return (
          <FormItem>
            <FormLabel>WhatsApp</FormLabel>
            <FormControl>
              <IMaskInput
                mask={whatsappMask}
                dispatch={dispatchWhatsappMask}
                value={value}
                unmask={false}
                onAccept={(maskedValue: string) => field.onChange(maskedValue)}
                onBlur={field.onBlur}
                inputRef={field.ref}
                placeholder="(11) 98765-4321"
                autoComplete="tel"
                className="flex h-9 w-full min-w-0 rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-xs outline-none transition-[color,box-shadow] placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 md:text-sm"
              />
            </FormControl>
            {filled && !valid && (
              <p className="text-sm leading-[1.5] text-red-600">
                Número de WhatsApp inválido.
              </p>
            )}
            {filled && valid && preview && (
              <p className="text-sm font-semibold leading-[1.5] text-zinc-600">
                Vai ser salvo como: {preview}
              </p>
            )}
            <FormMessage />
          </FormItem>
        );
      }}
    />
  );
}
