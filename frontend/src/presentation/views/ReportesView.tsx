import { useReportes } from "../hooks/useReportes";
import { MetricCard } from "../components/common/MetricCard";
import { BarChart } from "../components/reportes/BarChart";
import { DonutChart } from "../components/reportes/DonutChart";
import { ErrorState, LoadingState } from "../components/common/StatusStates";
import type { RangoReporte } from "../../domain/entities/Reporte";

export function ReportesView() {
  const { rango, setRango, reporte, cargando, error } = useReportes();

  if (error) return <ErrorState message={error} />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Reportes</h1>
          <div className="zafra">
            Desempeño de la logística de zafra · Ingenio San Ramón
          </div>
        </div>
      </div>

      <div className="report-toolbar">
        <select
          value={rango}
          onChange={(e) => setRango(e.target.value as RangoReporte)}
        >
          <option value="semana">Última semana</option>
          <option value="mes">Último mes</option>
          <option value="zafra">Zafra completa</option>
        </select>
        <button
          className="btn-mini"
          onClick={() => alert("Exportando reporte a Excel (simulado).")}
        >
          Exportar a Excel
        </button>
      </div>

      {cargando || !reporte ? (
        <LoadingState label="Cargando reporte..." />
      ) : (
        <>
          <div className="metrics" style={{ marginBottom: 24 }}>
            <MetricCard
              label="Espera promedio"
              value={reporte.kpi.esperaPromedioMin}
              unit="min"
            />
            <MetricCard
              label="Reducción vs. asignación manual"
              value={reporte.kpi.reduccionVsManualPct}
              unit="%"
              valueColor="var(--ok)"
            />
            <MetricCard
              label="Turnos totales"
              value={reporte.kpi.turnosTotales.toLocaleString("es-AR")}
            />
            <MetricCard
              label="Gestionados por el bot"
              value={reporte.kpi.porcentajeGestionadoPorBot}
              unit="%"
            />
          </div>

          <div className="charts-row">
            <BarChart datos={reporte.esperaPorDia} />
            <DonutChart distribucion={reporte.distribucionFlota} />
          </div>

          <section>
            <div className="section-head">
              <h2>Fincas con mayor tiempo de espera</h2>
              <div className="meta">Promedio del período seleccionado</div>
            </div>
            <table>
              <thead>
                <tr>
                  <th>Finca</th>
                  <th>Turnos</th>
                  <th>Espera promedio</th>
                  <th>Tendencia</th>
                </tr>
              </thead>
              <tbody>
                {reporte.fincasConMayorEspera.map((f) => (
                  <tr key={f.finca}>
                    <td>{f.finca}</td>
                    <td>{f.turnos}</td>
                    <td>{f.esperaPromedioMin} min</td>
                    <td
                      style={{
                        color: f.tendenciaSube ? "var(--alerta)" : "var(--ok)",
                      }}
                    >
                      {f.tendenciaSube ? "↑ subiendo" : "↓ bajando"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        </>
      )}
    </div>
  );
}
