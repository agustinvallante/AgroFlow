import { MetricCard } from "../components/common/MetricCard";
import { EmptyState } from "../components/common/StatusStates";
import { AVISO_SIN_API, SIN_DATO } from "../components/common/sinDatos";

export function TransportistasView() {
  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Transportistas</h1>
          <div className="zafra">{AVISO_SIN_API}</div>
        </div>
        <button className="btn-primary" disabled>
          + Nuevo transportista
        </button>
      </div>

      <div className="metrics" style={{ marginBottom: 20 }}>
        <MetricCard label="Total transportistas" value={SIN_DATO} />
        <MetricCard label="Flota propia" value={SIN_DATO} />
        <MetricCard label="Flota tercerizada" value={SIN_DATO} />
        <MetricCard label="Activos hoy" value={SIN_DATO} />
      </div>

      <div className="filters-bar">
        <input type="text" placeholder="Buscar por nombre, patente o finca..." disabled />
        <select disabled>
          <option>Toda la flota</option>
        </select>
        <select disabled>
          <option>Todos</option>
        </select>
      </div>

      <table>
        <thead>
          <tr>
            <th>Nombre</th>
            <th>Patente</th>
            <th>Teléfono</th>
            <th>Finca habitual</th>
            <th>Flota</th>
            <th>Turnos este mes</th>
            <th>Estado</th>
            <th></th>
          </tr>
        </thead>
      </table>
      <EmptyState message="Sin transportistas." />
    </div>
  );
}
