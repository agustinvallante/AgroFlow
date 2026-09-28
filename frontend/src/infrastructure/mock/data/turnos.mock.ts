import { calcularPrioridadCorte, type EstadoTurno, type Turno } from "../../../domain/entities/Turno";
import { horaConOffset } from "../../../shared/utils/date";
import { CHOFERES, FINCAS, elementoAleatorio, patenteAleatoria } from "./base.mock";

let contadorId = 1;

export function generarTurnosSemilla(): Turno[] {
  const estadosPasado: EstadoTurno[] = ["completado", "completado", "completado", "demorado"];
  const estadosActual: EstadoTurno[] = ["viaje", "cancha", "descargando"];
  const canales: Array<"bot" | "manual"> = ["bot", "bot", "bot", "manual"];

  const turnos: Turno[] = [];
  for (let i = 0; i < 30; i++) {
    const offsetMin = -300 + i * 30;
    let estado: EstadoTurno;
    if (offsetMin < -30) estado = elementoAleatorio(estadosPasado);
    else if (offsetMin < 60) estado = elementoAleatorio(estadosActual);
    else estado = "pendiente";

    const horasDesdeCorte = Math.round(Math.random() * 24);
    turnos.push({
      id: `turno-${contadorId++}`,
      hora: horaConOffset(offsetMin),
      offsetMin,
      patente: patenteAleatoria(),
      chofer: elementoAleatorio(CHOFERES),
      finca: elementoAleatorio(FINCAS),
      flota: Math.random() > 0.5 ? "propia" : "tercero",
      horasDesdeCorte,
      prioridad: calcularPrioridadCorte(horasDesdeCorte),
      estado,
      canal: elementoAleatorio(canales),
      esperaMin: estado === "pendiente" ? 0 : Math.floor(Math.random() * 45) + 2,
    });
  }
  return turnos.sort((a, b) => a.offsetMin - b.offsetMin);
}

export function siguienteIdTurno(): string {
  return `turno-${contadorId++}`;
}
