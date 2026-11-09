import type { EstadoViaje, ViajeCalculado } from "../../domain/services/simulacionViajes";
import { FormularioFinca, FormularioIngenio, FormularioViaje } from "../components/mapa/FormulariosMapa";
import { MapaRutas } from "../components/mapa/MapaRutas";
import { MULTIPLICADORES, useMapaSimulado } from "../hooks/useMapaSimulado";
import { horaEnZonaIngenio } from "../../shared/utils/date";

const ESTADO_LABEL: Record<EstadoViaje, string> = {
  "en-ruta": "En ruta",
  demora: "Demora",
  programado: "Programado",
  "en-playa": "Llegó",
};

/** Orden de la cola: primero lo que está por llegar. */
const ORDEN_ESTADO: Record<EstadoViaje, number> = { demora: 0, "en-ruta": 1, programado: 2, "en-playa": 3 };

const hora = (ms: number) => horaEnZonaIngenio(new Date(ms).toISOString());
const minutos = (ms: number) => Math.max(Math.round(ms / 60_000), 0);

function detalleTiempo({ situacion, llegadaEstimadaMs, llegadaMs, viaje }: ViajeCalculado, ahoraMs: number): string {
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
    viajes,
    multiplicador,
    setMultiplicador,
    guardarIngenio,
    agregarFinca,
    quitarFinca,
    agregarViaje,
    quitarViaje,
  } = useMapaSimulado();

  const contar = (estado: EstadoViaje) => viajes.filter((v) => v.situacion.estado === estado).length;
  const cola = [...viajes].sort(
    (a, b) =>
      ORDEN_ESTADO[a.situacion.estado] - ORDEN_ESTADO[b.situacion.estado] ||
      a.llegadaEstimadaMs - b.llegadaEstimadaMs,
  );
  const patentesEnViaje = viajes.filter((v) => v.situacion.estado !== "en-playa").map((v) => v.viaje.patente);

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Mapa de camiones</h1>
          <div className="zafra">Predicción con datos cargados a mano · No proviene de AgroFlow API</div>
        </div>
        <div className="mapa-controles">
          <span className="mapa-reloj">{hora(ahoraMs)}</span>
          <span className="mapa-modo">{adelantoMs > 0 ? `Adelantado ${minutos(adelantoMs)} min` : "Tiempo real"}</span>
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

      <div className="mapa-layout">
        <div className="mapa-panel">
          <div className="mapa-contadores">
            <span className="mapa-contador en-ruta">En ruta {contar("en-ruta")}</span>
            <span className="mapa-contador llegaron">Llegaron {contar("en-playa")}</span>
            <span className="mapa-contador demora">Demora {contar("demora")}</span>
            <span className="mapa-contador">Programados {contar("programado")}</span>
          </div>
          <MapaRutas ingenio={ingenio} fincas={fincas} viajes={viajes} />
        </div>

        <div className="mapa-panel">
          <h2 className="mapa-titulo">Cola de recepción</h2>
          {cola.length === 0 && <div className="empty-state">Sin viajes cargados.</div>}
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
                  <em className={v.situacion.estado}>{ESTADO_LABEL[v.situacion.estado]}</em>
                  <span>{detalleTiempo(v, ahoraMs)}</span>
                </div>
                <button
                  className="btn-mini"
                  aria-label={`Quitar viaje ${v.viaje.patente}`}
                  onClick={() => quitarViaje(v.viaje.id)}
                >
                  ×
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>

      <div className="mapa-formularios">
        <section>
          <div className="section-head">
            <h2>Ingenio</h2>
          </div>
          <FormularioIngenio onGuardar={guardarIngenio} />
          {ingenio && (
            <p className="mapa-actual">
              {ingenio.nombre} · {ingenio.lat}, {ingenio.lon}
            </p>
          )}
        </section>
        <section>
          <div className="section-head">
            <h2>Cargar finca</h2>
          </div>
          <FormularioFinca fincas={fincas} onAgregar={agregarFinca} />
          <ul className="mapa-fincas">
            {fincas.map((f) => (
              <li key={f.id}>
                <span>
                  {f.nombre} · {f.lat}, {f.lon}
                </span>
                <button className="btn-mini" aria-label={`Quitar ${f.nombre}`} onClick={() => quitarFinca(f.id)}>
                  ×
                </button>
              </li>
            ))}
          </ul>
        </section>
        <section>
          <div className="section-head">
            <h2>Cargar viaje</h2>
          </div>
          <FormularioViaje
            hayIngenio={ingenio !== null}
            fincas={fincas}
            patentesEnViaje={patentesEnViaje}
            onAgregar={agregarViaje}
          />
        </section>
      </div>
    </div>
  );
}
