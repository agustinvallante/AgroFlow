import { useCallback, useEffect, useRef, useState } from "react";
import {
  calcularViaje,
  type FincaMapa,
  type PuntoGeo,
  type ViajeCalculado,
  type ViajeSimulado,
} from "../../domain/services/simulacionViajes";

/** x1 es el tiempo real; las otras velocidades sólo sirven para probar un viaje largo. */
export const MULTIPLICADORES = [1, 10, 60] as const;
export type Multiplicador = (typeof MULTIPLICADORES)[number];

export type IngenioMapa = PuntoGeo & { nombre: string };

const INTERVALO_MS = 100;
const MS_POR_MINUTO = 60_000;
const CLAVE_ALMACENAMIENTO = "agroflow.mapa-simulado";

export type DatosFinca = IngenioMapa;

export interface DatosViaje {
  patente: string;
  fincaId: string;
  velocidadKmh: number;
  saleEnMin: number;
  retrasoMin: number;
}

/** Lo que se conserva: un viaje dura más que una visita a la pestaña. */
interface DatosCargados {
  ingenio: IngenioMapa | null;
  fincas: FincaMapa[];
  viajes: ViajeSimulado[];
  siguienteId: number;
  /** Tiempo que el reloj se adelantó por las velocidades de prueba. */
  adelantoMs: number;
}

const SIN_DATOS: DatosCargados = { ingenio: null, fincas: [], viajes: [], siguienteId: 1, adelantoMs: 0 };

function leerDatos(): DatosCargados {
  try {
    const guardado = JSON.parse(localStorage.getItem(CLAVE_ALMACENAMIENTO) ?? "null") as DatosCargados | null;
    return guardado && Array.isArray(guardado.fincas) && Array.isArray(guardado.viajes) ? guardado : SIN_DATOS;
  } catch {
    return SIN_DATOS;
  }
}

function guardarDatos(datos: DatosCargados) {
  try {
    localStorage.setItem(CLAVE_ALMACENAMIENTO, JSON.stringify(datos));
  } catch {
    // Sin almacenamiento el mapa funciona igual, sólo que no conserva lo cargado.
  }
}

export function useMapaSimulado() {
  const [datos, setDatos] = useState(leerDatos);
  const [ahoraMs, setAhoraMs] = useState(() => Date.now() + datos.adelantoMs);
  const [multiplicador, setMultiplicador] = useState<Multiplicador>(1);
  const ultimoTick = useRef(0);

  useEffect(() => {
    guardarDatos(datos);
  }, [datos]);

  useEffect(() => {
    // Se avanza con el reloj real: un intervalo demorado (pestaña en segundo plano) no atrasa el mapa.
    ultimoTick.current = Date.now();
    const id = setInterval(() => {
      const actual = Date.now();
      const transcurridoMs = actual - ultimoTick.current;
      const extraMs = transcurridoMs * (multiplicador - 1);
      ultimoTick.current = actual;
      if (extraMs > 0) setDatos((d) => ({ ...d, adelantoMs: d.adelantoMs + extraMs }));
      setAhoraMs((previo) => previo + transcurridoMs + extraMs);
    }, INTERVALO_MS);
    return () => clearInterval(id);
  }, [multiplicador]);

  const guardarIngenio = useCallback((ingenio: IngenioMapa) => setDatos((d) => ({ ...d, ingenio })), []);

  const agregarFinca = useCallback((finca: DatosFinca) => {
    setDatos((d) => ({
      ...d,
      siguienteId: d.siguienteId + 1,
      fincas: [...d.fincas, { id: `f${d.siguienteId}`, ...finca }],
    }));
  }, []);

  const quitarFinca = useCallback((id: string) => {
    setDatos((d) => ({
      ...d,
      fincas: d.fincas.filter((f) => f.id !== id),
      viajes: d.viajes.filter((v) => v.fincaId !== id),
    }));
  }, []);

  const agregarViaje = useCallback(({ saleEnMin, ...viaje }: DatosViaje) => {
    setDatos((d) => ({
      ...d,
      siguienteId: d.siguienteId + 1,
      viajes: [
        ...d.viajes,
        { id: `v${d.siguienteId}`, salidaMs: Date.now() + d.adelantoMs + saleEnMin * MS_POR_MINUTO, ...viaje },
      ],
    }));
  }, []);

  const quitarViaje = useCallback((id: string) => {
    setDatos((d) => ({ ...d, viajes: d.viajes.filter((v) => v.id !== id) }));
  }, []);

  const { ingenio } = datos;
  const viajes: ViajeCalculado[] = ingenio
    ? datos.viajes.flatMap((viaje) => {
        const finca = datos.fincas.find((f) => f.id === viaje.fincaId);
        return finca ? [calcularViaje(viaje, finca, ingenio, ahoraMs)] : [];
      })
    : [];

  return {
    ahoraMs,
    adelantoMs: datos.adelantoMs,
    ingenio,
    fincas: datos.fincas,
    viajes,
    multiplicador,
    setMultiplicador,
    guardarIngenio,
    agregarFinca,
    quitarFinca,
    agregarViaje,
    quitarViaje,
  };
}
