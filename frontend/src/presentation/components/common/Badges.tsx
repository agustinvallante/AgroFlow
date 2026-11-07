import type { EstadoTurno } from "../../../domain/entities/Turno";

const ESTADO_LABELS: Record<EstadoTurno, string> = {
  pendiente: "Asignado",
  viaje: "En camino",
  cancha: "En espera",
  ingresado: "Ingresado",
  descargando: "En descarga",
  completado: "Finalizado",
  cancelado: "Cancelado",
};

export function EstadoBadge({ estado }: { estado: EstadoTurno }) {
  return (
    <span className={`estado ${estado}`}>
      <i />
      {ESTADO_LABELS[estado]}
    </span>
  );
}
