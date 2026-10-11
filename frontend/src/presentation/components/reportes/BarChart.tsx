import type { EsperaPorDia } from "../../../domain/entities/Reporte";

export function BarChart({ datos }: { datos: EsperaPorDia[] }) {
  const max = Math.max(...datos.map((d) => d.minutos), 1);
  return (
    <div className="bar-chart">
      <div className="section-head" style={{ border: "none", padding: 0, marginBottom: 0 }}>
        <h2 style={{ fontSize: 15 }}>Espera promedio por día</h2>
      </div>
      <div className="bar-chart-grid">
        {datos.map((d) => (
          <div className="bar-col" key={d.dia}>
            <span className="bar-val">{d.minutos}m</span>
            <div className="bar" style={{ height: `${Math.round((d.minutos / max) * 100)}%` }} />
            <span className="bar-label">{d.dia}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
