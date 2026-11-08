import { useMemo } from "react";
import type { FincaMapa, ViajeCalculado } from "../../../domain/services/simulacionViajes";
import type { IngenioMapa } from "../../hooks/useMapaSimulado";
import { FondoMapa } from "./FondoMapa";
import {
  cajaDeEtiqueta,
  construirRuta,
  crearProyeccion,
  puntoEnRuta,
  ubicarEtiqueta,
  type Caja,
  type PosicionEtiqueta,
} from "./geometriaMapa";

const ANCHO = 800;
const ALTO = 500;
const MARGEN = 70;
const LADO_CAMION = 11;
/** Media etiqueta de llegada: la mantiene dentro del lienzo. */
const MITAD_AVISO = 100;
const RADIO_FINCA = 4;
const RADIO_HALO = 16;

const ESTADO_LABEL: Partial<Record<ViajeCalculado["situacion"]["estado"], string>> = {
  "en-ruta": "EN RUTA",
  demora: "DEMORA",
};

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
    { x: x + 12, y: y - 9, anchor: "start" },
    { x: x - 12, y: y - 9, anchor: "end" },
  ];
  const abajo: PosicionEtiqueta[] = [
    { x: x + 12, y: y + 20, anchor: "start" },
    { x: x - 12, y: y + 20, anchor: "end" },
  ];
  // En demora espera junto al ingenio: debajo no se encima con su nombre.
  return demorado ? [...abajo, ...arriba] : [...arriba, ...abajo];
}

interface MapaRutasProps {
  ingenio: IngenioMapa | null;
  fincas: FincaMapa[];
  viajes: ViajeCalculado[];
}

export function MapaRutas({ ingenio, fincas, viajes }: MapaRutasProps) {
  // Lo fijo se ubica una sola vez, así un camión que pasa no hace parpadear estas etiquetas.
  const { posIngenio, xAviso, marcas, ocupadasFijas } = useMemo(() => {
    const proyectar = crearProyeccion(ingenio ? [ingenio, ...fincas] : fincas, ANCHO, ALTO, MARGEN);
    const destino = ingenio ? proyectar(ingenio) : null;
    const posiciones = fincas.map(proyectar);
    const aviso = destino ? Math.min(Math.max(destino.x, MITAD_AVISO), ANCHO - MITAD_AVISO) : 0;

    const ocupadas: Caja[] = posiciones.map((p) => cajaCentrada(p.x, p.y, RADIO_FINCA));
    if (ingenio && destino) {
      ocupadas.push(
        cajaCentrada(destino.x, destino.y, RADIO_HALO),
        cajaDeEtiqueta(ingenio.nombre, { x: destino.x, y: destino.y - 24, anchor: "middle" }),
        { x: aviso - MITAD_AVISO, y: destino.y - 53, ancho: MITAD_AVISO * 2, alto: 14 },
      );
    }

    return {
      posIngenio: destino,
      xAviso: aviso,
      ocupadasFijas: ocupadas,
      marcas: fincas.map((finca, i) => {
        const pos = posiciones[i];
        return {
          finca,
          pos,
          etiqueta: ubicarEtiqueta(finca.nombre, posicionesFinca(pos.x, pos.y), ocupadas, ANCHO, ALTO),
          ruta: destino ? construirRuta(pos, destino, i) : null,
        };
      }),
    };
  }, [ingenio, fincas]);

  const llegados = viajes
    .filter((v) => v.situacion.estado === "en-playa")
    .sort((a, b) => b.llegadaMs - a.llegadaMs);
  const ultimaLlegada = llegados[0];

  // Las demoras eligen primero: son lo que más importa ver.
  const circulando = viajes
    .filter((v) => v.situacion.estado === "en-ruta" || v.situacion.estado === "demora")
    .sort((a, b) => Number(b.situacion.estado === "demora") - Number(a.situacion.estado === "demora"))
    .flatMap((v) => {
      const ruta = marcas.find((m) => m.finca.id === v.finca.id)?.ruta;
      return ruta ? [{ ...v, ...puntoEnRuta(ruta, v.situacion.progreso) }] : [];
    });

  const ocupadas = [...ocupadasFijas, ...circulando.map((c) => cajaCentrada(c.x, c.y, LADO_CAMION / 2))];
  const camiones = circulando.map((c) => {
    const texto = `${c.viaje.patente} · ${ESTADO_LABEL[c.situacion.estado]}`;
    const posiciones = posicionesCamion(c.x, c.y, c.situacion.estado === "demora");
    return { ...c, texto, etiqueta: ubicarEtiqueta(texto, posiciones, ocupadas, ANCHO, ALTO) };
  });

  return (
    <svg className="mapa-svg" viewBox={`0 0 ${ANCHO} ${ALTO}`} role="img" aria-label="Mapa simulado de camiones entre fincas e ingenio">
      <FondoMapa />

      {(!ingenio || fincas.length === 0) && (
        <text x={ANCHO / 2} y={ALTO / 2} textAnchor="middle" className="mapa-vacio">
          Cargá el ingenio y al menos una finca para ver las rutas
        </text>
      )}

      {marcas.map(({ finca, ruta }) => {
        if (!ruta) return null;
        const enFinca = camiones.filter((c) => c.finca.id === finca.id);
        const clase = enFinca.some((c) => c.situacion.estado === "en-ruta")
          ? "activa"
          : enFinca.length > 0
            ? "demorada"
            : "";
        return <path key={finca.id} d={ruta.d} className={`mapa-ruta ${clase}`} />;
      })}

      {marcas.map(({ finca, pos, etiqueta }) => (
        <g key={finca.id}>
          <circle cx={pos.x} cy={pos.y} r={RADIO_FINCA} className="mapa-finca">
            {!etiqueta && <title>{finca.nombre}</title>}
          </circle>
          {etiqueta && (
            <text x={etiqueta.x} y={etiqueta.y} textAnchor={etiqueta.anchor} className="mapa-texto">
              {finca.nombre}
            </text>
          )}
        </g>
      ))}

      {ingenio && posIngenio && (
        <g>
          <circle
            cx={posIngenio.x}
            cy={posIngenio.y}
            r={RADIO_HALO}
            className={`mapa-ingenio-halo ${ultimaLlegada ? "con-llegada" : ""}`}
          />
          <circle cx={posIngenio.x} cy={posIngenio.y} r={6} className="mapa-ingenio" />
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
        <g key={viaje.id} className={`mapa-camion ${situacion.estado}`}>
          <rect x={x - LADO_CAMION / 2} y={y - LADO_CAMION / 2} width={LADO_CAMION} height={LADO_CAMION} rx={1.5}>
            {!etiqueta && <title>{texto}</title>}
          </rect>
          {etiqueta && (
            <text x={etiqueta.x} y={etiqueta.y} textAnchor={etiqueta.anchor}>
              {texto}
            </text>
          )}
        </g>
      ))}
    </svg>
  );
}
