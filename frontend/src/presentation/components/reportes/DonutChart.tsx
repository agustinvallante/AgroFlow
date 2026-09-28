import type { DistribucionFlota } from "../../../domain/entities/Reporte";

export function DonutChart({
  distribucion,
}: {
  distribucion: DistribucionFlota;
}) {
  const pct = distribucion.porcentajePropia;
  return (
    <div className="donut-card">
      <div
        className="section-head"
        style={{ border: "none", padding: 0, marginBottom: 0, width: "100%" }}
      >
        <h2 style={{ fontSize: 15 }}>Flota propia vs. tercerizada</h2>
      </div>
      <div className="donut-wrap">
        <div
          className="donut"
          style={{
            background: `conic-gradient(var(--azucar) 0% ${pct}%, var(--cana-700) ${pct}% 100%)`,
          }}
        />
        <div className="donut-center">
          <span className="big">{pct}%</span>
          <span className="small">propia</span>
        </div>
      </div>
      <div className="legend">
        <span>
          <i style={{ background: "var(--azucar)" }} />
          Propia
        </span>
        <span>
          <i style={{ background: "var(--cana-700)" }} />
          Tercerizada
        </span>
      </div>
    </div>
  );
}
