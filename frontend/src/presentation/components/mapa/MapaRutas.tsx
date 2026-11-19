import { useEffect, useMemo, useState } from "react";
import type { FincaMapa, ViajeCalculado } from "../../../domain/services/simulacionViajes";
import type { IngenioMapa } from "../../hooks/useMapaSimulado";
import { FondoMapa } from "./FondoMapa";
import { ESTADO_LABEL, hora, km, minutos } from "./formatoMapa";
import { textoIncidenteCorto } from "../common/incidentes";
import {
  cajaDeEtiqueta,
  construirRedVial,
  crearProyeccion,
  polilinea,
  puntoEnRuta,
  ubicarEtiqueta,
  type Caja,
  type Punto,
  type PosicionEtiqueta,
} from "./geometriaMapa";

const ANCHO = 800;
const ALTO = 500;
const MARGEN = 70;
const RADIO_CAMION = 7.5;
/** Media etiqueta de llegada: la mantiene dentro del lienzo. */
const MITAD_AVISO = 100;
const LADO_FINCA = 9;
const LADO_INGENIO = 13;
const RADIO_HALO = 16;

const cajaCentrada = (x: number, y: number, radio: number): Caja => ({
  x: x - radio,
  y: y - radio,
  ancho: radio * 2,
  alto: radio * 2,
});

function posicionesFinca(x: number, y: number): PosicionEtiqueta[] {
  return [
    { x, y: y + 20, anchor: "middle" },
    { x, y: y - 11, anchor: "middle" },
    { x: x + 10, y: y + 4, anchor: "start" },
    { x: x - 10, y: y + 4, anchor: "end" },
  ];
}

function posicionesCamion(x: number, y: number, demorado: boolean): PosicionEtiqueta[] {
  const arriba: PosicionEtiqueta[] = [
    { x: x + 13, y: y - 9, anchor: "start" },
    { x: x - 13, y: y - 9, anchor: "end" },
  ];
  const abajo: PosicionEtiqueta[] = [
    { x: x + 13, y: y + 20, anchor: "start" },
    { x: x - 13, y: y + 20, anchor: "end" },
  ];
  // En demora espera junto al ingenio: debajo no se encima con su nombre.
  return demorado ? [...abajo, ...arriba] : [...arriba, ...abajo];
}

interface DetalleCamionProps {
  viaje: ViajeCalculado;
  ahoraMs: number;
  /** Punto del camión en el lienzo, para anclar el recuadro a su lado. */
  ancla: Punto;
  onCerrar: () => void;
}

function DetalleCamion({ viaje: v, ahoraMs, ancla, onCerrar }: DetalleCamionProps) {
  const { estado, progreso } = v.situacion;
  const restanteKm = estado === "en-playa" ? 0 : v.distanciaKm * (1 - progreso);
  const incidente = v.turno?.incidente;
  const llegada =
    estado === "en-playa"
      ? `${hora(v.llegadaMs)} (llegó)`
      : incidente
        ? `${hora(v.llegadaEstimadaMs)} (demora est. ${incidente.demoraEstimadaMin} min)`
        : estado === "demora"
        ? `${hora(v.llegadaEstimadaMs)} (+${minutos(ahoraMs - v.llegadaEstimadaMs)} min)`
        : `${hora(v.llegadaEstimadaMs)} (en ${minutos(v.llegadaEstimadaMs - ahoraMs)} min)`;
  const filas: [string, string][] = [
    ["Estado", ESTADO_LABEL[estado]],
    ...(incidente && estado !== "en-playa" ? ([["Problema", textoIncidenteCorto(incidente)]] as [string, string][]) : []),
    // Los viajes de la demo no tienen transportista.
    ...(v.turno ? ([["Transportista", v.turno.transportista]] as [string, string][]) : []),
    ["Sale de", v.finca.nombre],
    ["Salida", hora(v.viaje.salidaMs)],
    ["Llegada", llegada],
    ["Distancia", km(v.distanciaKm)],
    ["Restante", km(restanteKm)],
    ["Velocidad", `${v.viaje.velocidadKmh} km/h`],
    ...(v.turno
      ? ([["Ventana", `${hora(Date.parse(v.turno.ventana.inicio))}–${hora(Date.parse(v.turno.ventana.fin))}`]] as [string, string][])
      : ([["Retraso simulado", `${v.viaje.retrasoMin} min`]] as [string, string][])),
  ];
  // Del lado con más lugar, para no salirse del mapa.
  const izquierda = ancla.x > ANCHO / 2;
  const arriba = ancla.y > ALTO / 2;
  return (
    <div
      className="mapa-detalle"
      role="dialog"
      aria-label={`Detalle del camión ${v.viaje.patente}`}
      style={{
        left: `${(ancla.x / ANCHO) * 100}%`,
        top: `${(ancla.y / ALTO) * 100}%`,
        transform: `translate(${izquierda ? "calc(-100% - 14px)" : "14px"}, ${arriba ? "calc(-100% + 10px)" : "-10px"})`,
      }}
    >
      <div className="mapa-detalle-cabeza">
        <strong>{v.viaje.patente}</strong>
        <button className="btn-mini" aria-label="Cerrar detalle" onClick={onCerrar}>
          ×
        </button>
      </div>
      <dl>
        {filas.map(([dato, valor]) => (
          <div key={dato}>
            <dt>{dato}</dt>
            <dd className={dato === "Estado" ? `mapa-estado ${estado}` : dato === "Problema" ? "mapa-problema" : undefined}>
              {valor}
            </dd>
          </div>
        ))}
      </dl>
      <p>Posición y llegada estimadas, sin GPS.</p>
    </div>
  );
}

interface MapaRutasProps {
  ingenio: IngenioMapa | null;
  fincas: FincaMapa[];
  viajes: ViajeCalculado[];
  ahoraMs: number;
}

export function MapaRutas({ ingenio, fincas, viajes, ahoraMs }: MapaRutasProps) {
  const [seleccionId, setSeleccionId] = useState<string | null>(null);

  useEffect(() => {
    if (!seleccionId) return;
    const cerrar = (e: KeyboardEvent) => e.key === "Escape" && setSeleccionId(null);
    window.addEventListener("keydown", cerrar);
    return () => window.removeEventListener("keydown", cerrar);
  }, [seleccionId]);

  // Lo fijo se ubica una sola vez, así un camión que pasa no hace parpadear estas etiquetas.
  const { posIngenio, xAviso, marcas, red, ocupadasFijas } = useMemo(() => {
    const proyectar = crearProyeccion(ingenio ? [ingenio, ...fincas] : fincas, ANCHO, ALTO, MARGEN);
    const destino = ingenio ? proyectar(ingenio) : { x: ANCHO / 2, y: ALTO / 2 };
    const posiciones = fincas.map(proyectar);
    const aviso = Math.min(Math.max(destino.x, MITAD_AVISO), ANCHO - MITAD_AVISO);

    const ocupadas: Caja[] = posiciones.map((p) => cajaCentrada(p.x, p.y, LADO_FINCA / 2));
    if (ingenio) {
      ocupadas.push(
        cajaCentrada(destino.x, destino.y, RADIO_HALO),
        cajaDeEtiqueta(ingenio.nombre, { x: destino.x, y: destino.y - 24, anchor: "middle" }),
        { x: aviso - MITAD_AVISO, y: destino.y - 53, ancho: MITAD_AVISO * 2, alto: 14 },
      );
    }

    const redVial = construirRedVial(destino, ingenio ? posiciones : [], ANCHO, ALTO);

    return {
      posIngenio: ingenio ? destino : null,
      xAviso: aviso,
      ocupadasFijas: ocupadas,
      red: redVial,
      marcas: fincas.map((finca, i) => {
        const pos = posiciones[i];
        return {
          finca,
          pos,
          etiqueta: ubicarEtiqueta(finca.nombre, posicionesFinca(pos.x, pos.y), ocupadas, ANCHO, ALTO),
          ruta: redVial.rutas[i] ?? null,
        };
      }),
    };
  }, [ingenio, fincas]);

  const llegados = viajes
    .filter((v) => v.situacion.estado === "en-playa")
    .sort((a, b) => b.llegadaMs - a.llegadaMs);
  const ultimaLlegada = llegados[0];
  const rutaDe = (v: ViajeCalculado) => marcas.find((m) => m.finca.id === v.finca.id)?.ruta ?? null;

  // Las demoras eligen primero: son lo que más importa ver.
  const circulando = viajes
    .filter((v) => v.situacion.estado === "en-ruta" || v.situacion.estado === "demora")
    .sort((a, b) => Number(b.situacion.estado === "demora") - Number(a.situacion.estado === "demora"))
    .flatMap((v) => {
      const ruta = rutaDe(v);
      return ruta ? [{ ...v, ...puntoEnRuta(ruta, v.situacion.progreso) }] : [];
    });

  const ocupadas = [...ocupadasFijas, ...circulando.map((c) => cajaCentrada(c.x, c.y, RADIO_CAMION))];
  const camiones = circulando.map((c) => {
    const texto = `${c.viaje.patente} · ${ESTADO_LABEL[c.situacion.estado].toUpperCase()}`;
    const posiciones = posicionesCamion(c.x, c.y, c.situacion.estado === "demora");
    return { ...c, texto, etiqueta: ubicarEtiqueta(texto, posiciones, ocupadas, ANCHO, ALTO) };
  });

  // Recorrido resaltado de cada camión en movimiento, sobre los caminos del fondo.
  const recorridos = [
    ...new Map(
      circulando.map((c) => [`${c.finca.id}-${c.situacion.estado}`, { finca: c.finca, estado: c.situacion.estado }]),
    ).values(),
  ].flatMap(({ finca, estado }) => {
    const ruta = marcas.find((m) => m.finca.id === finca.id)?.ruta;
    return ruta ? [{ id: `${finca.id}-${estado}`, d: polilinea(ruta.muestras), estado }] : [];
  });

  const seleccionado = viajes.find((v) => v.viaje.id === seleccionId) ?? null;
  const enMapa = camiones.find((c) => c.viaje.id === seleccionId);
  const ancla: Punto | null = seleccionado
    ? enMapa ?? (seleccionado.situacion.estado === "en-playa" ? posIngenio : marcas.find((m) => m.finca.id === seleccionado.finca.id)?.pos) ?? null
    : null;

  return (
    <div className="mapa-lienzo">
      <svg
        className="mapa-svg"
        viewBox={`0 0 ${ANCHO} ${ALTO}`}
        role="img"
        aria-label="Mapa simulado de camiones entre fincas e ingenio"
        onClick={() => setSeleccionId(null)}
      >
        <FondoMapa ancho={ANCHO} alto={ALTO} caminos={red.caminos} rios={red.rios} />

        {(!ingenio || fincas.length === 0) && (
          <text x={ANCHO / 2} y={ALTO / 2} textAnchor="middle" className="mapa-vacio">
            {ingenio ? "Sin fincas de la API para mostrar rutas" : "Guardá el ingenio para ver las rutas"}
          </text>
        )}

        {recorridos.map(({ id, d, estado }) => (
          <path key={id} d={d} className={`mapa-ruta ${estado === "demora" ? "demorada" : "activa"}`} />
        ))}

        {marcas.map(({ finca, pos, etiqueta }) => (
          <g key={finca.id}>
            <rect
              x={pos.x - LADO_FINCA / 2}
              y={pos.y - LADO_FINCA / 2}
              width={LADO_FINCA}
              height={LADO_FINCA}
              className="mapa-finca"
            >
              {!etiqueta && <title>{finca.nombre}</title>}
            </rect>
            {etiqueta && (
              <text x={etiqueta.x} y={etiqueta.y} textAnchor={etiqueta.anchor} className="mapa-texto">
                {finca.nombre}
              </text>
            )}
          </g>
        ))}

        {ingenio && posIngenio && (
          <g>
            <rect
              x={posIngenio.x - RADIO_HALO}
              y={posIngenio.y - RADIO_HALO}
              width={RADIO_HALO * 2}
              height={RADIO_HALO * 2}
              rx={3}
              className={`mapa-ingenio-halo ${ultimaLlegada ? "con-llegada" : ""}`}
            />
            <rect
              x={posIngenio.x - LADO_INGENIO / 2}
              y={posIngenio.y - LADO_INGENIO / 2}
              width={LADO_INGENIO}
              height={LADO_INGENIO}
              className="mapa-ingenio"
            />
            <text x={posIngenio.x} y={posIngenio.y - 24} textAnchor="middle" className="mapa-texto ingenio">
              {ingenio.nombre.toUpperCase()}
            </text>
            {ultimaLlegada && (
              <text x={xAviso} y={posIngenio.y - 42} textAnchor="middle" className="mapa-llegada">
                ✓ {ultimaLlegada.viaje.patente} · LLEGÓ{llegados.length > 1 ? ` (+${llegados.length - 1})` : ""}
              </text>
            )}
          </g>
        )}

        {camiones.map(({ viaje, situacion, x, y, texto, etiqueta }) => (
          <g
            key={viaje.id}
            className={`mapa-camion ${situacion.estado} ${viaje.id === seleccionId ? "seleccionado" : ""}`}
            role="button"
            tabIndex={0}
            aria-label={`Ver camión ${viaje.patente}`}
            onClick={(e) => {
              e.stopPropagation();
              setSeleccionId(viaje.id);
            }}
            onKeyDown={(e) => (e.key === "Enter" || e.key === " ") && setSeleccionId(viaje.id)}
          >
            <circle cx={x} cy={y} r={RADIO_CAMION}>
              {!etiqueta && <title>{texto}</title>}
            </circle>
            {etiqueta && (
              <text x={etiqueta.x} y={etiqueta.y} textAnchor={etiqueta.anchor}>
                {texto}
              </text>
            )}
          </g>
        ))}
      </svg>

      {seleccionado && ancla && (
        <DetalleCamion viaje={seleccionado} ahoraMs={ahoraMs} ancla={ancla} onCerrar={() => setSeleccionId(null)} />
      )}
    </div>
  );
}
