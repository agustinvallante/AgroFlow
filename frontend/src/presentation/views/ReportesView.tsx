import { MetricCard } from "../components/common/MetricCard";
import { EmptyState } from "../components/common/StatusStates";
import { AVISO_SIN_API, SIN_DATO } from "../components/common/sinDatos";
import { BarChart } from "../components/reportes/BarChart";
import { DonutChart } from "../components/reportes/DonutChart";

export function ReportesView() {
  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Reportes</h1>
          <div className="zafra">{AVISO_SIN_API}</div>
        </div>
      </div>

      <div className="report-toolbar">
        <select disabled>
          <option>Última semana</option>
        </select>
        <button className="btn-mini" disabled>
          Exportar a Excel
        </button>
      </div>

      <div className="metrics" style={{ marginBottom: 24 }}>
        <MetricCard label="Espera promedio" value={SIN_DATO} />
        <MetricCard label="Reducción vs. asignación manual" value={SIN_DATO} />
        <MetricCard label="Turnos totales" value={SIN_DATO} />
        <MetricCard label="Gestionados por el bot" value={SIN_DATO} />
      </div>

      <div className="charts-row">
        <BarChart datos={[]} />
        <DonutChart distribucion={null} />
      </div>

      <section>
        <div className="section-head">
          <h2>Fincas con mayor tiempo de espera</h2>
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
        </table>
        <EmptyState message="Sin fincas para mostrar." />
      </section>
    </div>
  );
}
