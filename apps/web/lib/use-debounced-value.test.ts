import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { scheduleDebouncedUpdate } from "./use-debounced-value";

// `useDebouncedValue` e uma casca de useState/useEffect em cima de
// `scheduleDebouncedUpdate` (ver comentario no arquivo de implementacao) -- testamos
// a funcao pura diretamente com timers falsos, replicando o ciclo exato que o React
// executa a cada re-render: roda o efeito (agenda o timer), e ao proximo
// dependency-change primeiro chama o cleanup do efeito anterior (cancela o timer
// antigo) antes de rodar o novo efeito.
describe("scheduleDebouncedUpdate (useDebouncedValue)", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("only emits the value after the delay has fully elapsed", () => {
    const onUpdate = vi.fn();

    scheduleDebouncedUpdate("abc", 400, onUpdate);

    vi.advanceTimersByTime(399);
    expect(onUpdate).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(onUpdate).toHaveBeenCalledTimes(1);
    expect(onUpdate).toHaveBeenCalledWith("abc");
  });

  it("rapid successive changes only emit the last value", () => {
    const onUpdate = vi.fn();

    const cleanup1 = scheduleDebouncedUpdate("a", 400, onUpdate);
    vi.advanceTimersByTime(100);
    cleanup1();

    const cleanup2 = scheduleDebouncedUpdate("b", 400, onUpdate);
    vi.advanceTimersByTime(100);
    cleanup2();

    scheduleDebouncedUpdate("c", 400, onUpdate);
    vi.advanceTimersByTime(400);

    expect(onUpdate).toHaveBeenCalledTimes(1);
    expect(onUpdate).toHaveBeenCalledWith("c");
  });
});
