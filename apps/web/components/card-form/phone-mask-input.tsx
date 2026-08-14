"use client";

import { IMaskInput } from "react-imask";
import type { MaskedDynamic } from "imask";
import { useFormContext, type FieldPath } from "react-hook-form";
import type { CardFormValues } from "@/lib/card-schema";
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";

// Mascara dinamica BR: alterna entre 10 e 11 digitos (fixo/8-digitos vs celular com o
// nono digito) conforme a quantidade de digitos ja preenchidos, usando o mecanismo de
// `dispatch` do imask para trocar de mascara em tempo real.
const brPhoneMask = [{ mask: "(00) 0000-0000" }, { mask: "(00) 00000-0000" }];

function dispatchBrPhoneMask(appended: string, dynamicMasked: MaskedDynamic) {
  const digits = `${dynamicMasked.value}${appended}`.replace(/\D/g, "");
  return digits.length > 10
    ? dynamicMasked.compiledMasks[1]
    : dynamicMasked.compiledMasks[0];
}

type PhoneMaskInputProps = {
  name: FieldPath<CardFormValues>;
  label: string;
  /** Mensagem exibida em tempo real quando `isValid` retorna false. Opcional -- sem ela, o campo só mostra erro via FormMessage (validação do schema, só após tentativa de salvar). */
  isValid?: (value: string) => boolean;
  invalidMessage?: string;
};

// Campo de telefone com mascara brasileira ao vivo. Compartilhado entre "Telefone" e
// "WhatsApp" na secao de Contato para que os dois tenham a mesma formatacao.
export function PhoneMaskInput({
  name,
  label,
  isValid,
  invalidMessage,
}: PhoneMaskInputProps) {
  const form = useFormContext<CardFormValues>();

  return (
    <FormField
      control={form.control}
      name={name}
      render={({ field }) => {
        const value = typeof field.value === "string" ? field.value : "";
        const filled = value.trim().length > 0;
        const showInvalid = Boolean(
          isValid && invalidMessage && filled && !isValid(value),
        );

        return (
          <FormItem>
            <FormLabel>{label}</FormLabel>
            <FormControl>
              <IMaskInput
                mask={brPhoneMask}
                dispatch={dispatchBrPhoneMask}
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
            {showInvalid && (
              <p className="text-sm leading-[1.5] text-red-600">{invalidMessage}</p>
            )}
            <FormMessage />
          </FormItem>
        );
      }}
    />
  );
}
