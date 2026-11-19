import { useMemo } from "react";
import { useColaTurnos } from "../hooks/useColaTurnos";
import { useVentanas } from "../hooks/useVentanas";
import { useClock } from "../hooks/useClock";
import { EstadoBadge } from "../components/common/Badges";
import { CambioEstadoSelect } from "../components/common/CambioEstadoSelect";
import { MetricCard } from "../components/common/MetricCard";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { SIN_DATO } from "../components/common/sinDatos";
import { BotFeedPanel } from "../components/panel/BotFeedPanel";
import { Timeline } from "../components/panel/Timeline";
import { ventanaDeRfc3339 } from "../../shared/utils/date";
import { turnosManuales, useMoliendaDemo } from "../hooks/medidasTemporalesPanel";

const ACTIVOS = ["viaje", "cancha", "ingresado", "descargando"];

/**
 * Camiones en curso, camiones en espera y franjas salen de ZafraFlow API.
 * Como medida temporal para la demo, los turnos del bot son los activos que
 * este dashboard no cargó, y la molienda se simula en el navegador. La espera
 * promedio, el feed del chatbot y el aviso a la flota conservan su lugar, sin
 * datos y sin acciones: la API no publica cuándo cambió el estado de un turno.
 */
export function PanelGeneralView() {
  const { turnos, estadoCarga, error, desactualizado, recargar, cambiarEstado, enCurso, errorAccion } = useColaTurnos("activos");
  const { hora, fecha } = useClock();
  const ventanas = useVentanas();

  const activos = useMemo(() => turnos.filter((t) => ACTIVOS.includes(t.estado)), [turnos]);
  // Sin una respuesta válida, "0" sería un dato falso.
  const hayDatos = estadoCarga === "ok" || desactualizado;
  const enEspera = activos.filter((t) => t.estado === "cancha").length;
  const delBot = useMemo(() => {
    const manuales = turnosManuales();
    return turnos.filter((t) => !manuales.has(t.id)).length;
  }, [turnos]);
  const molienda = useMoliendaDemo();

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Panel general</h1>
          <div className="zafra">Camiones y franjas: ZafraFlow API · Turnos del bot y molienda: estimados para la demo</div>
        </div>
        <div className="clockbox">
          <div className="time">{hora}</div>
          <div className="date">{fecha}</div>
        </div>
      </div>

      <div className="metrics">
        <MetricCard label="Camiones en espera ahora" value={hayDatos ? enEspera : SIN_DATO} />
        <MetricCard label="Espera promedio en canchón" value={SIN_DATO} />
        <MetricCard
          label="Turnos gestionados por el bot"
          value={hayDatos ? delBot : SIN_DATO}
          trend={hayDatos ? `de ${turnos.length} turnos activos` : undefined}
        />
        <MetricCard
          label="Estado de la molienda"
          value={molienda.operando ? "Operando" : "Detenida"}
          valueColor={molienda.operando ? "var(--ok)" : "var(--alerta)"}
        />
      </div>

      <section>
        <div className="section-head">
          <h2>Cola del día</h2>
          <div className="franjas-filtros">
            <label>Fecha de franjas <input type="date" value={ventanas.fecha} onChange={(e) => ventanas.setFecha(e.target.value)} /></label>
            <button className="btn-mini" onClick={() => ventanas.setFecha("")}>Hoy del ingenio</button>
            <button className="btn-mini" onClick={ventanas.recargar}>Actualizar franjas</button>
          </div>
        </div>
        <p className="meta">Franjas y cupos del ingenio · {ventanas.fecha || "Hoy según la API"} · Refresco cada 4 segundos. Horarios con el desplazamiento del ingenio.</p>
        {ventanas.cargando ? <LoadingState label="Cargando franjas..." /> : ventanas.error && !ventanas.desactualizado ? (
          <div role="alert">
            <ErrorState message={ventanas.error} />
            <button className="btn-mini" onClick={ventanas.recargar}>Reintentar franjas</button>
          </div>
        ) : (
          <>
            {ventanas.desactualizado && <div className="aviso-desactualizado" role="alert"><strong>Franjas desactualizadas.</strong> {ventanas.error} Se reintenta automáticamente.</div>}
            <div className={ventanas.desactualizado ? "datos-desactualizados" : undefined}>
              {ventanas.ventanas.length === 0 ? <EmptyState message="No hay ventanas configuradas para esta fecha." /> : <Timeline slots={ventanas.ventanas} />}
            </div>
          </>
        )}
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
                          {ventanaDeRfc3339(t.ventana.inicio, t.ventana.fin)}
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
                            onCambiar={async (nuevo) => {
                              await cambiarEstado(t.id, nuevo);
                              ventanas.recargar();
                            }}
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
              <button
                type="button"
                role="switch"
                aria-checked={molienda.operando}
                aria-label="Molienda en funcionamiento"
                className={`toggle ${molienda.operando ? "" : "off"}`}
                onClick={molienda.alternar}
              />
              <span className="toggle-label">{molienda.operando ? "Operando" : "Detenida"}</span>
            </div>
            <p className="meta">Simulado para la demo: no avisa por WhatsApp.</p>
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
