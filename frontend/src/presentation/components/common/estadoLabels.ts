import type { EstadoTurno } from "../../../domain/entities/Turno";

export const ESTADO_LABELS: Record<EstadoTurno, string> = {
  pendiente: "Asignado",
  viaje: "En camino",
  cancha: "En espera",
  ingresado: "Ingresado",
  descargando: "En descarga",
  completado: "Finalizado",
  cancelado: "Cancelado",
};
