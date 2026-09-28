import type { CanalAsignacion, EstadoTurno, FlotaTipo, PrioridadCorte } from "../../../domain/entities/Turno";

/** Para datos que la fuente no informa (p. ej. la API de la demo). */
const SinDato = () => <span style={{ color: "var(--tinta-suave)" }}>—</span>;

export function FlotaBadge({ flota }: { flota: FlotaTipo | null }) {
  if (!flota) return <SinDato />;
  return (
    <span className={`flota-badge ${flota}`}>{flota === "propia" ? "Propia" : "Tercero"}</span>
  );
}

export function PrioridadBadge({ prioridad }: { prioridad: PrioridadCorte | null }) {
  if (!prioridad) return <SinDato />;
  const label = prioridad.charAt(0).toUpperCase() + prioridad.slice(1);
  return <span className={`badge-prioridad ${prioridad}`}>{label}</span>;
}

export function CanalBadge({ canal }: { canal: CanalAsignacion | null }) {
  if (!canal) return <SinDato />;
  return (
    <span className={`badge-canal ${canal === "bot" ? "bot" : ""}`}>
      {canal === "bot" ? "WhatsApp bot" : "Manual"}
    </span>
  );
}

const ESTADO_LABELS: Record<EstadoTurno, string> = {
  pendiente: "Pendiente",
  viaje: "En viaje",
  cancha: "En espera",
  ingresado: "Ingresado",
  descargando: "Descargando",
  completado: "Completado",
  demorado: "Demorado",
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

export function ActivoBadge({ activo }: { activo: boolean }) {
  return (
    <span className={`estado ${activo ? "descargando" : "demorado"}`}>
      <i />
      {activo ? "Activo" : "Inactivo"}
    </span>
  );
}
