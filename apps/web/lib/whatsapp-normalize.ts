// Normalizacao de numero de WhatsApp brasileiro (CARD-08, D-11).
//
// ESPELHO: este arquivo e apps/api/Services/WhatsappNormalizer.cs implementam a MESMA
// logica nos dois lados (cliente e servidor) -- precisam mudar juntos. O cliente so usa
// isto para a previa em tempo real (D-11); o valor de fato persistido e sempre decidido
// pelo servidor (nunca confiar no valor ja "normalizado" que o cliente enviou -- T-01-22).
//
// Regra do nono digito (ANATEL, 2012-2016): so os DDDs abaixo ganharam um "9" extra no
// inicio do numero local. Aplicar essa regra a qualquer outro DDD corromperia o numero
// (01-RESEARCH.md, Pitfall 2 / Assumptions Log A2 -- whitelist vem de fontes
// comunitarias, nao de documento oficial da ANATEL; recomendavel conferir com numeros
// reais antes do deploy da Fase 3).
export const NINTH_DIGIT_DDDS = new Set([
  "11", "12", "13", "14", "15", "16", "17", "18", "19", // Sao Paulo
  "21", "22", "24", // Rio de Janeiro
  "27", "28", // Espirito Santo
]);

/**
 * Devolve apenas digitos, sempre prefixados por "55" (DDI Brasil).
 * Entrada vazia ou nula devolve string vazia -- o campo e opcional (D-04).
 */
export function normalizeWhatsapp(raw: string | null | undefined): string {
  if (!raw) return "";

  const digits = raw.replace(/\D/g, "");
  if (!digits) return "";

  // Remove o prefixo DDI "55" apenas quando sobrarem digitos suficientes para um numero
  // nacional completo (DDD + local) -- caso contrario um DDD "55" (Rio Grande do Sul)
  // seria confundido com o DDI e cortado por engano.
  let national = digits.startsWith("55") && digits.length > 11 ? digits.slice(2) : digits;

  // Remove um unico "0" de tronco a esquerda, se presente.
  national = national.replace(/^0/, "");

  const ddd = national.slice(0, 2);
  let localNumber = national.slice(2);

  if (NINTH_DIGIT_DDDS.has(ddd) && localNumber.length === 8) {
    localNumber = `9${localNumber}`;
  }

  return `55${ddd}${localNumber}`;
}

/**
 * Devolve a forma legivel "+55 DD XXXXX-XXXX" (ou "+55 DD XXXX-XXXX" para numero fixo
 * de 8 digitos) a partir do valor normalizado -- e a linha de previa exigida por D-11.
 * Entrada vazia ou invalida devolve string vazia.
 */
export function formatWhatsappPreview(raw: string | null | undefined): string {
  const normalized = normalizeWhatsapp(raw);
  if (normalized.length < 4) return "";

  const ddd = normalized.slice(2, 4);
  const localNumber = normalized.slice(4);

  if (localNumber.length === 9) {
    return `+55 ${ddd} ${localNumber.slice(0, 5)}-${localNumber.slice(5)}`;
  }
  if (localNumber.length === 8) {
    return `+55 ${ddd} ${localNumber.slice(0, 4)}-${localNumber.slice(4)}`;
  }
  return "";
}

/**
 * DDD entre 11 e 99 (nenhum DDD brasileiro valido comeca com 0 ou 1 seguido de 0) e
 * numero local com 8 ou 9 digitos apos a normalizacao.
 */
export function isValidWhatsapp(raw: string | null | undefined): boolean {
  const normalized = normalizeWhatsapp(raw);
  if (!normalized) return false;

  const ddd = normalized.slice(2, 4);
  const localNumber = normalized.slice(4);
  const dddNumber = Number(ddd);

  if (ddd.length !== 2 || Number.isNaN(dddNumber) || dddNumber < 11 || dddNumber > 99) {
    return false;
  }

  return localNumber.length === 8 || localNumber.length === 9;
}
