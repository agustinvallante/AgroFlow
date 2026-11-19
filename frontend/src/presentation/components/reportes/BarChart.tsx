import type { EstadoTurno } from "../../../domain/entities/Turno";
import { ESTADO_LABELS } from "../common/estadoLabels";

export function BarChart({ estados }: { estados: EstadoTurno[] }) {
  const filas = (Object.keys(ESTADO_LABELS) as EstadoTurno[]).map((estado) => ({
    label: ESTADO_LABELS[estado],
    cantidad: estados.filter((valor) => valor === estado).length,
  }));
  const max = Math.max(1, ...filas.map(({ cantidad }) => cantidad));

  return (
    <section className="bar-chart" aria-labelledby="reporte-estados">
      <div className="section-head">
        <h2 id="reporte-estados">Turnos por estado</h2>
      </div>
      <ul className="bar-chart-list">
        {filas.map(({ label, cantidad }) => (
          <li key={label}>
            <span>{label}</span>
            <div className="bar-track" aria-hidden="true">
              <div className="bar" style={{ width: `${cantidad / max * 100}%` }} />
            </div>
            <span>{cantidad}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
