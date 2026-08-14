"use client";

import { useEffect, useRef, useState } from "react";
import { useFormContext } from "react-hook-form";
import type { CardFormValues } from "@/lib/card-schema";
import {
  formatPixKey,
  isValidPixKey,
  PIX_ERROR_MESSAGES,
  type PixKeyType,
} from "@/lib/pix-validation";
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PixCpfConfirmDialog } from "@/components/card-form/pix-cpf-confirm-dialog";

const PIX_TYPE_OPTIONS: { value: PixKeyType; label: string }[] = [
  { value: "cpf", label: "CPF" },
  { value: "cnpj", label: "CNPJ" },
  { value: "email", label: "E-mail" },
  { value: "telefone", label: "Telefone" },
  { value: "aleatoria", label: "Chave aleatória" },
];

// Tipos de risco baixo (D-08): aviso inline nao-bloqueante, sem modal de confirmacao --
// so o CPF (T-01-26) recebe o fluxo bloqueante de PixCpfConfirmDialog.
const LOW_RISK_TYPES: PixKeyType[] = ["email", "telefone", "aleatoria"];

// Secao Pix (CARD-06/CARD-07): tipo selecionavel, previa/validacao de dígito verificador a
// cada tecla (D-10), aviso inline para tipos de baixo risco (D-08) e confirmacao bloqueante
// via modal para CPF (D-09) -- salvar fica impossivel enquanto o tipo for cpf sem
// pixConsentConfirmed=true, porque o schema (superRefine em card-schema.ts) barra o submit
// nesse estado, espelhando a regra que o servidor tambem aplica (CardEndpoints.ValidatePix).
type PixSectionProps = {
  // Incrementado pelo card-form.tsx quando o servidor devolve 400 pix_consent_required --
  // forca a reabertura do modal mesmo se o estado local (tipo/consentimento) nao mudou,
  // caso raro de dessincronia entre abas/sessoes (o cliente normalmente ja bloqueia esse
  // caminho antes do submit, via superRefine em card-schema.ts).
  reopenConfirmSignal?: number;
};

export function PixSection({ reopenConfirmSignal }: PixSectionProps = {}) {
  const form = useFormContext<CardFormValues>();
  const pixKeyType = form.watch("pixKeyType");
  const pixKey = form.watch("pixKey");
  const pixConsentConfirmed = form.watch("pixConsentConfirmed");

  const [dialogOpen, setDialogOpen] = useState(false);
  // Guarda o tipo selecionado ANTES da troca para "cpf" -- e para onde o cancelamento do
  // modal reverte a selecao (D-09). So e atualizado no momento da troca PARA cpf, nunca nas
  // demais trocas, para nao perder o valor a que se deve voltar.
  const revertTypeRef = useRef<CardFormValues["pixKeyType"]>(pixKeyType);

  // Abre automaticamente sempre que o estado do form for "cpf" sem consentimento -- cobre
  // tanto a troca manual abaixo quanto o caso raro de o form montar ja nesse estado.
  useEffect(() => {
    if (pixKeyType === "cpf" && !pixConsentConfirmed) {
      setDialogOpen(true);
    }
  }, [pixKeyType, pixConsentConfirmed]);

  // Reabre o modal quando o servidor recusar por pix_consent_required, mesmo que o
  // estado local ja estivesse (aparentemente) nesse mesmo par tipo/consentimento.
  useEffect(() => {
    if (reopenConfirmSignal) {
      setDialogOpen(true);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [reopenConfirmSignal]);

  function handleTypeChange(value: string) {
    if (value === "cpf") {
      revertTypeRef.current = form.getValues("pixKeyType");
    }

    form.setValue("pixKeyType", value as CardFormValues["pixKeyType"], {
      shouldValidate: true,
    });

    // Trocar para qualquer tipo diferente de cpf zera o consentimento (espelha a regra do
    // servidor em CardEndpoints.ValidatePix).
    if (value !== "cpf") {
      form.setValue("pixConsentConfirmed", false, { shouldValidate: true });
    }
  }

  function handleConfirm() {
    form.setValue("pixConsentConfirmed", true, { shouldValidate: true });
    setDialogOpen(false);
  }

  function handleCancel() {
    form.setValue("pixKeyType", revertTypeRef.current ?? "", { shouldValidate: true });
    form.setValue("pixConsentConfirmed", false, { shouldValidate: true });
    setDialogOpen(false);
  }

  const filled = Boolean(pixKeyType && pixKey);
  const valid = filled ? isValidPixKey(pixKeyType as PixKeyType, pixKey!) : false;

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-[20px] font-semibold leading-[1.2]">Pix</h2>

      <FormField
        control={form.control}
        name="pixKeyType"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Tipo de chave</FormLabel>
            <Select value={field.value || undefined} onValueChange={handleTypeChange}>
              <FormControl>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione o tipo de chave" />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                {PIX_TYPE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={form.control}
        name="pixKey"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Chave Pix</FormLabel>
            <FormControl>
              <Input autoComplete="off" {...field} value={field.value ?? ""} />
            </FormControl>
            {filled &&
              (valid ? (
                <p className="text-sm font-semibold leading-[1.5] text-zinc-600">
                  Prévia: {formatPixKey(pixKeyType as PixKeyType, pixKey!)}
                </p>
              ) : (
                <p className="text-sm leading-[1.5] text-red-600">
                  {PIX_ERROR_MESSAGES[pixKeyType as PixKeyType]}
                </p>
              ))}
            {pixKeyType && LOW_RISK_TYPES.includes(pixKeyType as PixKeyType) && (
              <p className="text-sm leading-[1.5] text-zinc-600">
                Essa chave ficará visível publicamente no seu cartão.
              </p>
            )}
            <FormMessage />
          </FormItem>
        )}
      />

      <PixCpfConfirmDialog open={dialogOpen} onConfirm={handleConfirm} onCancel={handleCancel} />
    </section>
  );
}
