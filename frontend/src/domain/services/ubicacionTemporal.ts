/**
 * MEDIDA TEMPORAL. ZafraFlow API publica el catálogo de fincas pero no sus
 * coordenadas ni las del ingenio. Hasta que lo haga, el mapa ubica cada finca
 * de forma determinista alrededor del ingenio: siempre en el mismo lugar, a
 * una distancia verosímil para un viaje de caña. No es la ubicación real.
 */
import type { Finca } from "../entities/Finca";
import type { FincaMapa, PuntoGeo } from "./simulacionViajes";

/** Ingenio de referencia mientras la API no publique su ubicación (zona de Tucumán). */
export const INGENIO_TEMPORAL = { nombre: "Ingenio", lat: -26.83, lon: -65.2 } as const;

/** Distancias en línea recta entre finca e ingenio; por ruta son un 30 % más. */
const DISTANCIA_MIN_KM = 14;
const DISTANCIA_MAX_KM = 30;
const KM_POR_GRADO_LAT = 111.32;

/** Si el nombre indica un rumbo, la finca se ubica hacia ese lado (0° = este, 90° = norte). */
const RUMBOS: [RegExp, number][] = [
  [/nor(d)?este/, 45],
  [/noroeste/, 135],
  [/sudeste|sureste/, -45],
  [/sudoeste|suroeste/, -135],
  [/norte/, 90],
  [/\bsur\b/, -90],
  [/\beste\b/, 0],
  [/oeste/, 180],
];

const normalizar = (texto: string) =>
  texto.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[-_]/g, " ");

/** Entero estable a partir de un texto: la misma finca cae siempre en el mismo lugar. */
function hash(texto: string): number {
  let h = 2166136261;
  for (let i = 0; i < texto.length; i++) h = Math.imul(h ^ texto.charCodeAt(i), 16777619);
  return h >>> 0;
}

function rumbo(finca: Finca, indice: number): number {
  const texto = normalizar(`${finca.nombre} ${finca.codigo}`);
  const conocido = RUMBOS.find(([patron]) => patron.test(texto));
  // Sin rumbo en el nombre se reparten con el ángulo áureo, que no las amontona.
  const base = conocido ? conocido[1] : indice * 137.5;
  // Un leve desvío evita que dos fincas "norte" queden alineadas.
  return base + ((hash(finca.codigo) % 21) - 10);
}

/** Punto a `distanciaKm` del origen en la dirección `grados`, con la aproximación plana local. */
export function desplazar(origen: PuntoGeo, distanciaKm: number, grados: number): PuntoGeo {
  const radianes = (grados * Math.PI) / 180;
  const kmPorGradoLon = KM_POR_GRADO_LAT * Math.cos((origen.lat * Math.PI) / 180);
  return {
    lat: origen.lat + (Math.sin(radianes) * distanciaKm) / KM_POR_GRADO_LAT,
    lon: origen.lon + (Math.cos(radianes) * distanciaKm) / kmPorGradoLon,
  };
}

export function ubicarFincasTemporalmente(ingenio: PuntoGeo, fincas: Finca[]): FincaMapa[] {
  return fincas.map((finca, indice) => {
    const fraccion = (hash(`${finca.codigo}:distancia`) % 1000) / 1000;
    const distancia = DISTANCIA_MIN_KM + fraccion * (DISTANCIA_MAX_KM - DISTANCIA_MIN_KM);
    return { id: finca.id, nombre: finca.nombre, ...desplazar(ingenio, distancia, rumbo(finca, indice)) };
  });
}
