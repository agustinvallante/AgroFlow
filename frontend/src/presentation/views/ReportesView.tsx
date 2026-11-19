import { useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { EstadoTurno } from "../../domain/entities/Turno";
import type { VentanaRecepcion } from "../../domain/entities/VentanaRecepcion";
import { MetricCard } from "../components/common/MetricCard";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { BarChart } from "../components/reportes/BarChart";

interface Consulta {
  fecha: string;
}

type Resultado =
  | { consulta: Consulta; fecha: string; estados: EstadoTurno[]; ventanas: VentanaRecepcion[] }
  | { consulta: Consulta; error: string };

export function ReportesView() {
  const [consulta, consultar] = useState<Consulta>({ fecha: "" });
  const [resultado, setResultado] = useState<Resultado | null>(null);

  useEffect(() => {
    let vigente = true;
    async function cargar() {
      try {
        // Resolver hoy una sola vez en la zona del ingenio para que las dos
        // consultas coincidan incluso si la actualización cruza medianoche.
        const fecha = consulta.fecha || (await container.ingenio.obtener()).localDate;
        if (!vigente) return;
        const [estados, ventanas] = await Promise.all([
          container.turnos.obtenerEstadosDelDia(fecha),
          container.ventanas.obtenerVentanas.execute(fecha),
        ]);
        if (vigente) setResultado({ consulta, fecha, estados, ventanas });
      } catch (error) {
        if (vigente) {
          setResultado({
            consulta,
            error: error instanceof Error ? error.message : "No se pudo cargar el reporte.",
          });
        }
      }
    }
    void cargar();
    return () => { vigente = false; };
  }, [consulta]);

  const actual = resultado?.consulta === consulta ? resultado : null;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Reportes</h1>
          <div className="zafra">Turnos por día de la franja asignada</div>
        </div>
      </div>

      <div className="franjas-filtros report-toolbar">
        <label>Fecha del reporte <input type="date" value={consulta.fecha} onChange={(e) => consultar({ fecha: e.target.value })} /></label>
        <button className="btn-mini" onClick={() => consultar({ fecha: "" })}>Hoy del ingenio</button>
        <button className="btn-mini" onClick={() => consultar({ fecha: consulta.fecha })}>Actualizar reporte</button>
      </div>

      {!actual && <LoadingState label="Cargando reporte..." />}
      {actual && ("error" in actual
        ? <ErrorState message={actual.error} />
        : <ReporteDelDia fecha={actual.fecha} estados={actual.estados} ventanas={actual.ventanas} />)}
    </div>
  );
}

function ReporteDelDia({ fecha, estados, ventanas }: { fecha: string; estados: EstadoTurno[]; ventanas: VentanaRecepcion[] }) {
  const finalizados = estados.filter((estado) => estado === "completado").length;
  const cancelados = estados.filter((estado) => estado === "cancelado").length;

  return (
    <>
      <p className="meta">Fecha consultada: <time dateTime={fecha}>{fecha}</time></p>
      <div className="metrics report-metrics">
        <MetricCard label="Turnos totales" value={estados.length} />
        <MetricCard label="Finalizados" value={finalizados} />
        <MetricCard label="Cancelados" value={cancelados} />
      </div>
      {estados.length > 0
        ? <BarChart estados={estados} />
        : <EmptyState message="No hay turnos asignados a esta fecha." />}

      <section className="report-cupos" aria-labelledby="reporte-cupos">
        <div className="section-head">
          <h2 id="reporte-cupos">Cupos de las franjas</h2>
        </div>
        {ventanas.length === 0
          ? <EmptyState message="No hay franjas configuradas para esta fecha." />
          : (
            <div className="metrics report-metrics">
              <MetricCard label="Cupos totales" value={ventanas.reduce((total, ventana) => total + ventana.capacity, 0)} />
              <MetricCard label="Cupos ocupados" value={ventanas.reduce((total, ventana) => total + ventana.occupied, 0)} />
              <MetricCard label="Cupos disponibles" value={ventanas.reduce((total, ventana) => total + ventana.available, 0)} />
            </div>
          )}
      </section>
    </>
  );
}
