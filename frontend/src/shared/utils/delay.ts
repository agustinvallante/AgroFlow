/**
 * Simula la latencia de una llamada real al backend.
 * Se usa únicamente en la capa de infraestructura mock, para que
 * la UI se comporte igual que cuando exista un backend real
 * (loading states, no respuestas instantáneas, etc).
 */
export function delay<T>(value: T, ms = 350): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), ms));
}
