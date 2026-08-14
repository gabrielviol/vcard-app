"use client";

import { useEffect, useState } from "react";

// Agendador puro (sem React), isolado do hook para ser testavel com timers falsos
// sem precisar de um ambiente DOM -- o `vitest.config.ts` deste projeto roda
// `environment: "node"` (01-RESEARCH.md: Vitest e para testes de funcao pura, nao
// renderizacao de componente). `useDebouncedValue` abaixo e so uma casca fina de
// `useState` + `useEffect` em cima desta funcao: o corpo do efeito agenda o timer,
// e o cleanup que o React chama a cada re-render/desmontagem cancela o anterior --
// exatamente o mesmo ciclo exercitado pelos testes deste arquivo.
export function scheduleDebouncedUpdate<T>(
  value: T,
  delayMs: number,
  onUpdate: (value: T) => void,
): () => void {
  const timer = setTimeout(() => onUpdate(value), delayMs);
  return () => clearTimeout(timer);
}

// Hook proprio em vez de instalar `use-debounce` (01-RESEARCH.md, Alternatives
// Considered) -- so emite o ultimo valor depois de `delayMs` sem novas mudancas.
export function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    return scheduleDebouncedUpdate(value, delayMs, setDebounced);
  }, [value, delayMs]);

  return debounced;
}
