"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

type PixCpfConfirmDialogProps = {
  open: boolean;
  onConfirm: () => void;
  onCancel: () => void;
};

// Modal bloqueante de consentimento para CPF como chave Pix (D-09/CARD-07). A logica de
// para-onde-reverter o select de tipo mora em PixSection (o "dono" do estado do form) --
// este componente so decide quando o checkbox libera o botao "Confirmar" e comunica a
// decisao do usuario via onConfirm/onCancel.
export function PixCpfConfirmDialog({ open, onConfirm, onCancel }: PixCpfConfirmDialogProps) {
  const [checked, setChecked] = useState(false);

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setChecked(false);
      onCancel();
    }
  }

  function handleConfirm() {
    setChecked(false);
    onConfirm();
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Usar CPF como chave Pix</DialogTitle>
          <DialogDescription className="text-red-600">
            Sua chave Pix é seu CPF. Isso significa que seu CPF ficará visível para qualquer
            pessoa que acessar seu cartão.
          </DialogDescription>
        </DialogHeader>

        <div className="flex items-center gap-2">
          <Checkbox
            id="pix-cpf-consent"
            checked={checked}
            onCheckedChange={(value) => setChecked(value === true)}
          />
          <label htmlFor="pix-cpf-consent" className="text-sm leading-[1.5]">
            Confirmo que quero usar meu CPF como chave Pix.
          </label>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
            Cancelar
          </Button>
          <Button
            type="button"
            onClick={handleConfirm}
            disabled={!checked}
            className="bg-indigo-600 text-white hover:bg-indigo-700"
          >
            Confirmar
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
