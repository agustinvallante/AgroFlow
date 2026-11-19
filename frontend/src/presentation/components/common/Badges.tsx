import type { EstadoTurno } from "../../../domain/entities/Turno";
import { ESTADO_LABELS } from "./estadoLabels";

export function EstadoBadge({ estado }: { estado: EstadoTurno }) {
  return (
    <span className={`estado ${estado}`}>
      <i />
      {ESTADO_LABELS[estado]}
    </span>
  );
}
