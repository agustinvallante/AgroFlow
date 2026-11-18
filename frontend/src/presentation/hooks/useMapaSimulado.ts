import { useCallback, useEffect, useRef, useState } from "react";
import {
  calcularViaje,
  esUbicacionValida,
  esViajeValido,
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
/** v2: descarta los datos de prueba que quedaron guardados con la clave anterior. */
export const CLAVE_ALMACENAMIENTO = "agroflow.mapa-simulado.v2";
/**
 * Con x10/x60 el adelanto cambia en cada tick; se guarda como mucho con esta
 * frecuencia, además de al cambiar la velocidad, salir de la pestaña o cerrar la página.
 */
export const INTERVALO_GUARDADO_ADELANTO_MS = 5_000;
/** Tope del adelanto: unos 30 años, lejos de cualquier ensayo. */
const ADELANTO_MAXIMO_MS = 1e12;

export type DatosFinca = IngenioMapa;

export interface DatosViaje {
  patente: string;
  fincaId: string;
  velocidadKmh: number;
  saleEnMin: number;
  retrasoMin: number;
}

/** Lo que el operador carga. */
interface Escenario {
  ingenio: IngenioMapa | null;
  fincas: FincaMapa[];
  viajes: ViajeSimulado[];
  siguienteId: number;
}

/** Lo que se conserva: un viaje dura más que una visita a la pestaña. */
interface DatosCargados extends Escenario {
  /** Tiempo que el reloj se adelantó por las velocidades de prueba. */
  adelantoMs: number;
}

const SIN_DATOS: DatosCargados = { ingenio: null, fincas: [], viajes: [], siguienteId: 1, adelantoMs: 0 };

const esLugarValido = (lugar: Partial<IngenioMapa> | null | undefined): lugar is IngenioMapa =>
  !!lugar && typeof lugar.nombre === "string" && lugar.nombre !== "" && esUbicacionValida(lugar);

/**
 * Lo guardado puede venir de una versión anterior o haberse editado a mano: se
 * descarta lo inválido en lugar de dejar que una fecha imposible rompa la vista.
 */
function sanear(guardado: Partial<DatosCargados>): DatosCargados {
  const fincas = (Array.isArray(guardado.fincas) ? guardado.fincas : []).filter(
    (f): f is FincaMapa => esLugarValido(f) && typeof f.id === "string",
  );
  const viajes = (Array.isArray(guardado.viajes) ? guardado.viajes : []).filter(
    (v) => esViajeValido(v) && fincas.some((f) => f.id === v.fincaId),
  );
  const { adelantoMs, siguienteId } = guardado;
  return {
    ingenio: esLugarValido(guardado.ingenio) ? guardado.ingenio : null,
    fincas,
    viajes,
    siguienteId: typeof siguienteId === "number" && Number.isSafeInteger(siguienteId) && siguienteId > 0 ? siguienteId : 1,
    adelantoMs:
      typeof adelantoMs === "number" && adelantoMs >= 0 && adelantoMs <= ADELANTO_MAXIMO_MS ? adelantoMs : 0,
  };
}

function leerDatos(): DatosCargados {
  try {
    const guardado: unknown = JSON.parse(localStorage.getItem(CLAVE_ALMACENAMIENTO) ?? "null");
    return guardado && typeof guardado === "object" ? sanear(guardado) : SIN_DATOS;
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
  const [{ adelantoMs: adelantoInicial, ...escenarioInicial }] = useState(leerDatos);
  const [escenario, setEscenario] = useState<Escenario>(escenarioInicial);
  const [adelantoMs, setAdelantoMs] = useState(adelantoInicial);
  const [ahoraMs, setAhoraMs] = useState(() => Date.now() + adelantoInicial);
  const [multiplicador, setMultiplicador] = useState<Multiplicador>(1);
  // El intervalo y los guardados leen el último valor sin volver a suscribirse.
  const escenarioRef = useRef(escenario);
  const adelantoRef = useRef(adelantoInicial);

  const guardar = useCallback(
    () => guardarDatos({ ...escenarioRef.current, adelantoMs: adelantoRef.current }),
    [],
  );

  useEffect(() => {
    escenarioRef.current = escenario;
    guardar();
  }, [escenario, guardar]);

  useEffect(() => {
    window.addEventListener("pagehide", guardar);
    return () => window.removeEventListener("pagehide", guardar);
  }, [guardar]);

  useEffect(() => {
    // Se avanza con el reloj real: un intervalo demorado (pestaña en segundo plano) no atrasa el mapa.
    const inicio = Date.now();
    const ultimo = { tick: inicio, guardado: inicio };
    const id = setInterval(() => {
      const actual = Date.now();
      const transcurridoMs = actual - ultimo.tick;
      const extraMs = transcurridoMs * (multiplicador - 1);
      ultimo.tick = actual;
      if (extraMs > 0) {
        adelantoRef.current = Math.min(adelantoRef.current + extraMs, ADELANTO_MAXIMO_MS);
        setAdelantoMs(adelantoRef.current);
        if (actual - ultimo.guardado >= INTERVALO_GUARDADO_ADELANTO_MS) {
          ultimo.guardado = actual;
          guardar();
        }
      }
      setAhoraMs((previo) => previo + transcurridoMs + extraMs);
    }, INTERVALO_MS);
    return () => {
      clearInterval(id);
      // Al cambiar de velocidad o salir de la pestaña se conserva el adelanto exacto.
      guardar();
    };
  }, [multiplicador, guardar]);

  const guardarIngenio = useCallback((ingenio: IngenioMapa) => setEscenario((e) => ({ ...e, ingenio })), []);

  const agregarFinca = useCallback((finca: DatosFinca) => {
    setEscenario((e) => ({
      ...e,
      siguienteId: e.siguienteId + 1,
      fincas: [...e.fincas, { id: `f${e.siguienteId}`, ...finca }],
    }));
  }, []);

  const quitarFinca = useCallback((id: string) => {
    setEscenario((e) => ({
      ...e,
      fincas: e.fincas.filter((f) => f.id !== id),
      viajes: e.viajes.filter((v) => v.fincaId !== id),
    }));
  }, []);

  const agregarViaje = useCallback(({ saleEnMin, ...viaje }: DatosViaje) => {
    const salidaMs = Date.now() + adelantoRef.current + saleEnMin * MS_POR_MINUTO;
    setEscenario((e) => ({
      ...e,
      siguienteId: e.siguienteId + 1,
      viajes: [...e.viajes, { id: `v${e.siguienteId}`, salidaMs, ...viaje }],
    }));
  }, []);

  const quitarViaje = useCallback((id: string) => {
    setEscenario((e) => ({ ...e, viajes: e.viajes.filter((v) => v.id !== id) }));
  }, []);

  const { ingenio } = escenario;
  const viajes: ViajeCalculado[] = ingenio
    ? escenario.viajes.flatMap((viaje) => {
        const finca = escenario.fincas.find((f) => f.id === viaje.fincaId);
        return finca ? [calcularViaje(viaje, finca, ingenio, ahoraMs)] : [];
      })
    : [];

  return {
    ahoraMs,
    adelantoMs,
    ingenio,
    fincas: escenario.fincas,
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
