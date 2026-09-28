export const FINCAS = [
  "Finca La Esperanza",
  "Finca El Retiro",
  "Finca San José",
  "Finca Los Álamos",
  "Finca Santa Rita",
  "Finca El Bañado",
  "Finca Don Pedro",
  "Finca La Invernada",
];

export const CHOFERES = [
  "R. Aguirre",
  "M. Correa",
  "J. Salazar",
  "P. Nieva",
  "L. Farías",
  "D. Ibáñez",
  "C. Toledo",
  "F. Rearte",
];

export function patenteAleatoria(): string {
  return "AB" + Math.floor(100 + Math.random() * 899) + "CD";
}

export function elementoAleatorio<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}
