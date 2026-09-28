import { useMemo, useState } from "react";
import { usePanel } from "../hooks/usePanel";
import { useColaTurnos } from "../hooks/useColaTurnos";
import { useBotFeed } from "../hooks/useBotFeed";
import { useClock } from "../hooks/useClock";
import { MetricCard } from "../components/common/MetricCard";
import { EstadoBadge, FlotaBadge } from "../components/common/Badges";
import { Timeline } from "../components/panel/Timeline";
import { BotFeedPanel } from "../components/panel/BotFeedPanel";
import { LoadingState, ErrorState } from "../components/common/StatusStates";

export function PanelGeneralView() {
  const { metricas, timeline, cargando, error, alternarMolienda } = usePanel();
  const { turnos, cargando: cargandoTurnos, cancelar } = useColaTurnos();
  const mensajesBot = useBotFeed();
  const { hora, fecha } = useClock();
  const [avisoEnviado, setAvisoEnviado] = useState<string | null>(null);

  const activos = useMemo(
    () => turnos.filter((t) => ["viaje", "cancha", "descargando"].includes(t.estado)).slice(0, 14),
    [turnos]
  );

  if (error) return <ErrorState message={error} />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Panel general</h1>
          <div className="zafra">
            Día {metricas?.diaDeZafra ?? "—"} de {metricas?.totalDiasZafra ?? "—"} de zafra · Ingenio San Ramón
          </div>
        </div>
        <div className="clockbox">
          <div className="time">{hora}</div>
          <div className="date">{fecha}</div>
        </div>
      </div>

      {cargando || !metricas ? (
        <LoadingState label="Cargando métricas del panel..." />
      ) : (
        <>
          <div className="metrics">
            <MetricCard label="Camiones en espera ahora" value={metricas.camionesEnEsperaAhora} trend="↑ variación según la última hora" trendDirection="up" />
            <MetricCard label="Espera promedio en canchón" value={metricas.esperaPromedioMin} unit="min" trend="↓ 30% vs. asignación manual" trendDirection="down" />
            <MetricCard label="Turnos gestionados por el bot" value={metricas.porcentajeGestionadoPorBot} unit="%" trend="Meta del trimestre: 100%" trendDirection="down" />
            <MetricCard
              label="Estado de la molienda"
              value={metricas.estadoMolienda === "operando" ? "Operando" : "Detenida"}
              trend={`Capacidad actual: ${metricas.capacidadMoliendaTnH} tn/h`}
              valueColor={metricas.estadoMolienda === "operando" ? "var(--ok)" : "var(--alerta)"}
            />
          </div>

          <section>
            <div className="section-head">
              <h2>Cola virtual del día</h2>
              <div className="meta">Ventanas de 30 min · 06:00 – 22:00</div>
            </div>
            <Timeline slots={timeline} />
          </section>

          <div className="grid-2">
            <section>
              <div className="section-head">
                <h2>Camiones en canchón</h2>
                <div className="meta">{activos.length} activos</div>
              </div>
              {cargandoTurnos ? (
                <LoadingState />
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Turno</th>
                      <th>Patente</th>
                      <th>Finca / Origen</th>
                      <th>Flota</th>
                      <th>Estado</th>
                      <th>Espera</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {activos.map((t) => (
                      <tr key={t.id}>
                        <td style={{ fontFamily: "'IBM Plex Mono',monospace", fontSize: 12.5 }}>{t.hora}</td>
                        <td style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{t.patente}</td>
                        <td>
                          {t.finca}
                          <div style={{ fontSize: 11, color: "var(--tinta-suave)" }}>{t.chofer}</div>
                        </td>
                        <td><FlotaBadge flota={t.flota} /></td>
                        <td><EstadoBadge estado={t.estado} /></td>
                        <td className={t.esperaMin > 35 ? "espera-alta" : ""}>{t.esperaMin} min</td>
                        <td>
                          <button className="btn-mini" onClick={() => cancelar(t.id)}>
                            Marcar
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>

            <section>
              <BotFeedPanel mensajes={mensajesBot} />
            </section>
          </div>

          <section>
            <div className="section-head">
              <h2>Avisos y control operativo</h2>
              <div className="meta">Difusión automática a transportistas</div>
            </div>
            <div className="alert-row">
              <div className="alert-card">
                <h3>Estado de molienda</h3>
                <p>Si el ingenio se detiene, el sistema avisa por WhatsApp a los camiones en ruta para que no salgan de la finca.</p>
                <div className="toggle-row">
                  <div
                    className={`toggle ${metricas.estadoMolienda === "detenida" ? "off" : ""}`}
                    onClick={() => alternarMolienda()}
                  />
                  <span className="toggle-label">
                    {metricas.estadoMolienda === "operando" ? "Molienda operando" : "Molienda detenida"}
                  </span>
                </div>
              </div>
              <div className="alert-card">
                <h3>Aviso manual a transportistas</h3>
                <p>Envía un mensaje inmediato a todos los camiones con turno asignado hoy, a través del bot de WhatsApp.</p>
                <button className="btn-primary" onClick={() => setAvisoEnviado("Aviso enviado a la flota (simulado).")}>
                  Enviar aviso a la flota
                </button>
                {avisoEnviado && <p style={{ color: "var(--ok)" }}>{avisoEnviado}</p>}
              </div>
              <div className="alert-card">
                <h3>Asignación manual de turno</h3>
                <p>Para casos excepcionales donde el chofer no puede usar WhatsApp, un operario de báscula puede cargar el turno a mano desde la sección "Cola de turnos".</p>
              </div>
            </div>
          </section>
        </>
      )}
    </div>
  );
}
