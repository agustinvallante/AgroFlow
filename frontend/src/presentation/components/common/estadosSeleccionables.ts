import type { EstadoTurno } from "../../../domain/entities/Turno";

/**
 * Estados del contrato (AppointmentStatus), en el orden en que los publica
 * openapi.yaml. Es solo la lista de opciones: el frontend no decide cuáles
 * son válidas desde el estado actual; eso lo resuelve la API (409 si no).
 */
export const ESTADOS_SELECCIONABLES: { value: EstadoTurno; label: string }[] = [
  { value: "pendiente", label: "Asignado" },
  { value: "viaje", label: "En camino" },
  { value: "cancha", label: "En espera" },
  { value: "ingresado", label: "Ingresado" },
  { value: "descargando", label: "En descarga" },
  { value: "completado", label: "Finalizado" },
  { value: "cancelado", label: "Cancelado" },
];
