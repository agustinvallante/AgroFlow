import type { EstadoTurno } from "../../../domain/entities/Turno";
import { ESTADOS_SELECCIONABLES } from "./estadosSeleccionables";

interface CambioEstadoSelectProps {
  estadoActual: EstadoTurno;
  deshabilitado?: boolean;
  onCambiar: (nuevoEstado: EstadoTurno) => void;
}

export function CambioEstadoSelect({ estadoActual, deshabilitado, onCambiar }: CambioEstadoSelectProps) {
  return (
    <select
      className="btn-mini"
      aria-label="Cambiar estado"
      value=""
      disabled={deshabilitado}
      onClick={(e) => e.stopPropagation()}
      onChange={(e) => {
        const nuevo = e.target.value as EstadoTurno;
        const opcion = ESTADOS_SELECCIONABLES.find((o) => o.value === nuevo);
        if (opcion && window.confirm(`¿Cambiar el estado a "${opcion.label}"?`)) onCambiar(nuevo);
      }}
    >
      <option value="" disabled>
        Cambiar estado…
      </option>
      {ESTADOS_SELECCIONABLES.filter((o) => o.value !== estadoActual).map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}
