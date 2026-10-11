import type { Transportista } from "../../../domain/entities/Transportista";
import { CHOFERES, FINCAS, patenteAleatoria } from "./base.mock";

let contadorId = 1;

export function generarTransportistasSemilla(): Transportista[] {
  const lista: Transportista[] = [];
  FINCAS.forEach((finca, i) => {
    CHOFERES.slice(0, 8).forEach((chofer, j) => {
      if ((i + j) % 3 !== 0) return; // reduce combinaciones para no generar demasiadas filas
      lista.push({
        id: `tp-${contadorId++}`,
        nombre: chofer,
        patente: patenteAleatoria(),
        telefono:
          "+54 381 " +
          Math.floor(400 + Math.random() * 500) +
          " " +
          Math.floor(1000 + Math.random() * 8999),
        finca,
        flota: Math.random() > 0.5 ? "propia" : "tercero",
        turnosEsteMes: Math.floor(Math.random() * 40) + 2,
        activo: Math.random() > 0.15,
      });
    });
  });
  return lista;
}

export function siguienteIdTransportista(): string {
  return `tp-${contadorId++}`;
}
