import type {
  EstadoViaje,
  ViajeCalculado,
} from "../../domain/services/simulacionViajes";
import {
  FormularioIngenio,
  FormularioViaje,
} from "../components/mapa/FormulariosMapa";
import { MapaRutas } from "../components/mapa/MapaRutas";
import { ESTADO_LABEL, hora, minutos } from "../components/mapa/formatoMapa";
import { TIPO_INCIDENTE_LABEL } from "../components/common/incidentes";
import { MULTIPLICADORES, useMapaSimulado } from "../hooks/useMapaSimulado";

/** Orden de la cola: primero lo que está por llegar. */
const ORDEN_ESTADO: Record<EstadoViaje, number> = {
  demora: 0,
  "en-ruta": 1,
  programado: 2,
  "en-playa": 3,
};

function detalleTiempo(
  { situacion, llegadaEstimadaMs, llegadaMs, viaje, turno }: ViajeCalculado,
  ahoraMs: number,
): string {
  // Problema en ruta informado por el transportista: se muestra su demora estimada.
  if (turno?.incidente && situacion.estado === "demora")
    return `${TIPO_INCIDENTE_LABEL[turno.incidente.tipo]} · demora est. ${turno.incidente.demoraEstimadaMin} min`;
  switch (situacion.estado) {
    case "programado":
      return `Sale ${hora(viaje.salidaMs)}`;
    case "en-ruta":
      return `Llega ${hora(llegadaEstimadaMs)} · ${minutos(llegadaEstimadaMs - ahoraMs)} min`;
    case "demora":
      return `Debía llegar ${hora(llegadaEstimadaMs)} · +${minutos(ahoraMs - llegadaEstimadaMs)} min`;
    case "en-playa":
      return `Al ingenio a las ${hora(llegadaMs)}`;
  }
}

export function MapaView() {
  const {
    ahoraMs,
    adelantoMs,
    ingenio,
    fincas,
    cargandoFincas,
    errorFincas,
    reintentarFincas,
    errorTurnos,
    turnosSinFinca,
    viajes,
    multiplicador,
    setMultiplicador,
    guardarIngenio,
    agregarViaje,
    quitarViaje,
    volverAHoraReal,
  } = useMapaSimulado();

  const contar = (estado: EstadoViaje) =>
    viajes.filter((v) => v.situacion.estado === estado).length;
  const cola = [...viajes].sort(
    (a, b) =>
      ORDEN_ESTADO[a.situacion.estado] - ORDEN_ESTADO[b.situacion.estado] ||
      a.llegadaEstimadaMs - b.llegadaEstimadaMs,
  );
  const patentesEnViaje = viajes
    .filter((v) => v.situacion.estado !== "en-playa")
    .map((v) => v.viaje.patente);

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Mapa de camiones</h1>
          <div className="zafra">
            Turnos y fincas de la API · Ubicaciones y recorridos estimados en el
            navegador, sin GPS
          </div>
        </div>
        <div className="mapa-controles">
          <span className="mapa-reloj">{hora(ahoraMs)}</span>
          <span className="mapa-modo">
            {adelantoMs > 0
              ? `Adelantado ${minutos(adelantoMs)} min`
              : "Tiempo real"}
          </span>
          {adelantoMs > 0 && (
            <button
              className="btn-mini"
              title="Quita el adelanto y borra los viajes de prueba"
              onClick={volverAHoraReal}
            >
              Hora real
            </button>
          )}
          {MULTIPLICADORES.map((m) => (
            <button
              key={m}
              className={`btn-mini ${m === multiplicador ? "activo" : ""}`}
              aria-pressed={m === multiplicador}
              onClick={() => setMultiplicador(m)}
            >
              x{m}
            </button>
          ))}
        </div>
      </div>

      {errorFincas && (
        <div role="alert" className="mapa-aviso">
          <strong>No se pudieron cargar las fincas.</strong> {errorFincas}{" "}
          <button className="btn-mini" onClick={reintentarFincas}>
            Reintentar
          </button>
        </div>
      )}

      {errorTurnos && (
        <div role="alert" className="mapa-aviso">
          <strong>No se pudo leer la cola de turnos.</strong> {errorTurnos} Se
          reintenta sola.
        </div>
      )}
      {turnosSinFinca > 0 && (
        <p className="mapa-nota">
          {turnosSinFinca === 1
            ? "1 turno activo tiene"
            : `${turnosSinFinca} turnos activos tienen`}{" "}
          una finca que no está en el catálogo y no se muestra en el mapa.
        </p>
      )}

      <div className="mapa-layout">
        <div className="mapa-panel">
          <div className="mapa-contadores">
            <span className="mapa-contador en-ruta">
              En ruta {contar("en-ruta")}
            </span>
            <span className="mapa-contador llegaron">
              Llegaron {contar("en-playa")}
            </span>
            <span className="mapa-contador demora">
              Demora {contar("demora")}
            </span>
            <span className="mapa-contador">
              Programados {contar("programado")}
            </span>
          </div>
          <MapaRutas
            ingenio={ingenio}
            fincas={fincas}
            viajes={viajes}
            ahoraMs={ahoraMs}
          />
          <p className="mapa-nota">Tocá un camión para ver su detalle.</p>
        </div>

        <div className="mapa-panel">
          <h2 className="mapa-titulo">Cola de recepción</h2>
          {cola.length === 0 && (
            <div className="empty-state">Sin viajes cargados.</div>
          )}
          <ul className="mapa-cola">
            {cola.map((v) => (
              <li key={v.viaje.id}>
                <div>
                  <strong>{v.viaje.patente}</strong>
                  <span>
                    {v.finca.nombre} · {Math.round(v.distanciaKm)} km
                  </span>
                </div>
                <div className="mapa-cola-estado">
                  <em className={v.situacion.estado}>
                    {ESTADO_LABEL[v.situacion.estado]}
                  </em>
                  <span>{detalleTiempo(v, ahoraMs)}</span>
                </div>
                {/* Los viajes de la cola los maneja la API: sólo se quitan los de la demo. */}
                {!v.turno && (
                  <button
                    className="btn-mini"
                    aria-label={`Quitar viaje ${v.viaje.patente}`}
                    onClick={() => quitarViaje(v.viaje.id)}
                  >
                    ×
                  </button>
                )}
              </li>
            ))}
          </ul>
        </div>
      </div>

      <div className="mapa-demo-titulo">
        <h2>Sólo para la demo</h2>
        <p>
          Estos campos existen únicamente para probar el mapa. Los viajes reales
          se agregan solos desde la cola de turnos.
        </p>
      </div>
      <div className="mapa-formularios">
        <section>
          <div className="section-head">
            <h2>Cargar viaje de prueba</h2>
          </div>
          <FormularioViaje
            fincas={fincas}
            patentesEnViaje={patentesEnViaje}
            onAgregar={agregarViaje}
          />
        </section>
        <section>
          <div className="section-head">
            <h2>Fincas</h2>
          </div>
          {cargandoFincas && (
            <div className="loading-state">Cargando fincas de la API...</div>
          )}
          <ul className="mapa-fincas">
            {fincas.map((f) => (
              <li key={f.id}>
                <span>{f.nombre}</span>
              </li>
            ))}
          </ul>
          <p className="mapa-nota">
            Catálogo de la API. La API no publica coordenadas: el mapa las ubica
            de forma aproximada alrededor del ingenio, como medida temporal.
          </p>
        </section>
        <section>
          <div className="section-head">
            <h2>Ingenio</h2>
          </div>
          <p className="mapa-actual">
            {ingenio.nombre} · {ingenio.lat}, {ingenio.lon}
          </p>
          <p className="mapa-nota">
            Ubicación de referencia hasta que la API la publique. Podés
            cambiarla:
          </p>
          <FormularioIngenio onGuardar={guardarIngenio} />
        </section>
      </div>
    </div>
  );
}
