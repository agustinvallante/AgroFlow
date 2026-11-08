import { useMemo } from "react";
import { useColaTurnos } from "../hooks/useColaTurnos";
import { useClock } from "../hooks/useClock";
import { EstadoBadge } from "../components/common/Badges";
import { CambioEstadoSelect } from "../components/common/CambioEstadoSelect";
import { MetricCard } from "../components/common/MetricCard";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { SIN_DATO } from "../components/common/sinDatos";
import { BotFeedPanel } from "../components/panel/BotFeedPanel";
import { Timeline } from "../components/panel/Timeline";

const ACTIVOS = ["viaje", "cancha", "ingresado", "descargando"];

/**
 * Los camiones en curso salen de los turnos de AgroFlow API. Indicadores,
 * timeline, chatbot, molienda y avisos no tienen endpoint: conservan su
 * lugar en la pantalla, sin datos y sin acciones.
 */
export function PanelGeneralView() {
  const { turnos, estadoCarga, error, desactualizado, recargar, cambiarEstado, enCurso, errorAccion } = useColaTurnos();
  const { hora, fecha } = useClock();

  const activos = useMemo(() => turnos.filter((t) => ACTIVOS.includes(t.estado)), [turnos]);

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Panel general</h1>
          <div className="zafra">Camiones en curso: AgroFlow API · Indicadores, cola y avisos: sin datos</div>
        </div>
        <div className="clockbox">
          <div className="time">{hora}</div>
          <div className="date">{fecha}</div>
        </div>
      </div>

      <div className="metrics">
        <MetricCard label="Camiones en espera ahora" value={SIN_DATO} />
        <MetricCard label="Espera promedio en canchón" value={SIN_DATO} />
        <MetricCard label="Turnos gestionados por el bot" value={SIN_DATO} />
        <MetricCard label="Estado de la molienda" value={SIN_DATO} />
      </div>

      <section>
        <div className="section-head">
          <h2>Cola del día</h2>
        </div>
        <Timeline slots={[]} />
      </section>

      <div className="grid-2">
        <section>
          <div className="section-head">
            <h2>Camiones en curso</h2>
            {(estadoCarga === "ok" || desactualizado) && <div className="meta">{activos.length} activos</div>}
          </div>
          {errorAccion && <ErrorState message={errorAccion} />}
          {estadoCarga === "cargando" ? (
            <LoadingState label="Cargando turnos..." />
          ) : estadoCarga === "error" && !desactualizado ? (
            // Nunca hubo una respuesta válida: "0 activos" sería un dato falso.
            <div role="alert">
              <ErrorState message={error ?? "No se pudieron cargar los turnos."} />
              <button className="btn-mini" onClick={() => recargar()}>Reintentar</button>
            </div>
          ) : (
            <>
              {desactualizado && (
                <div className="aviso-desactualizado" role="alert">
                  <strong>Datos desactualizados.</strong> {error} Se reintenta automáticamente.
                </div>
              )}
              {activos.length === 0 ? (
                <EmptyState message="No hay camiones en curso." />
              ) : (
                <table className={desactualizado ? "datos-desactualizados" : undefined}>
                  <thead>
                    <tr>
                      <th>Turno</th>
                      <th>Patente</th>
                      <th>Finca / Transportista</th>
                      <th>Estado</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {activos.map((t) => (
                      <tr key={t.id}>
                        <td style={{ fontFamily: "'IBM Plex Mono',monospace", fontSize: 12.5 }}>
                          {t.hora}
                          {t.ventanaFin ? `–${t.ventanaFin}` : ""}
                        </td>
                        <td style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{t.patente}</td>
                        <td>
                          {t.finca}
                          <div style={{ fontSize: 11, color: "var(--tinta-suave)" }}>{t.chofer}</div>
                        </td>
                        <td>
                          <EstadoBadge estado={t.estado} />
                        </td>
                        <td>
                          <CambioEstadoSelect
                            estadoActual={t.estado}
                            deshabilitado={enCurso === t.id}
                            onCambiar={(nuevo) => cambiarEstado(t.id, nuevo)}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          )}
        </section>

        <section>
          <BotFeedPanel mensajes={[]} />
        </section>
      </div>

      <section>
        <div className="section-head">
          <h2>Avisos y control operativo</h2>
        </div>
        <div className="alert-row">
          <div className="alert-card">
            <h3>Estado de molienda</h3>
            <p>
              Si el ingenio se detiene, el sistema avisa por WhatsApp a los
              camiones en ruta para que no salgan de la finca.
            </p>
            <div className="toggle-row">
              <div className="toggle sin-dato" aria-disabled="true" />
              <span className="toggle-label">Sin datos</span>
            </div>
          </div>
          <div className="alert-card">
            <h3>Aviso manual a transportistas</h3>
            <p>
              Envía un mensaje inmediato a todos los camiones con turno
              asignado hoy, a través del bot de WhatsApp.
            </p>
            <button className="btn-primary" disabled>
              Enviar aviso a la flota
            </button>
          </div>
          <div className="alert-card">
            <h3>Asignación manual de turno</h3>
            <p>
              Para casos excepcionales donde el chofer no puede usar
              WhatsApp, un operario de báscula puede cargar el turno a mano
              desde la sección "Cola de turnos".
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
