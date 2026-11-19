import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { container } from "../../composition/container";
import type { Finca } from "../../domain/entities/Finca";
import type { Turno } from "../../domain/entities/Turno";
import {
  calcularViaje,
  esUbicacionValida,
  esViajeValido,
  type PuntoGeo,
  type ViajeCalculado,
  type ViajeSimulado,
} from "../../domain/services/simulacionViajes";
import { INGENIO_TEMPORAL, ubicarFincasTemporalmente } from "../../domain/services/ubicacionTemporal";
import { calcularViajeDeTurno, fincaDelTurno } from "../../domain/services/viajesDesdeTurnos";

/** x1 es el tiempo real; las otras velocidades sólo sirven para probar un viaje largo. */
export const MULTIPLICADORES = [1, 10, 60] as const;
export type Multiplicador = (typeof MULTIPLICADORES)[number];

export type IngenioMapa = PuntoGeo & { nombre: string };

const INTERVALO_MS = 100;
/** La cola cambia por el chatbot o el panel de turnos: se vuelve a leer cada tanto. */
export const INTERVALO_TURNOS_MS = 10_000;
const MS_POR_MINUTO = 60_000;
/** v3: las fincas ya no se guardan, vienen de la API; se descartan los escenarios anteriores. */
export const CLAVE_ALMACENAMIENTO = "agroflow.mapa-simulado.v3";
/**
 * Con x10/x60 el adelanto cambia en cada tick; se guarda como mucho con esta
 * frecuencia, además de al cambiar la velocidad, salir de la pestaña o cerrar la página.
 */
export const INTERVALO_GUARDADO_ADELANTO_MS = 5_000;
/** Tope del adelanto: unos 30 años, lejos de cualquier ensayo. */
const ADELANTO_MAXIMO_MS = 1e12;

export interface DatosViaje {
  patente: string;
  fincaId: string;
  velocidadKmh: number;
  saleEnMin: number;
  retrasoMin: number;
}

/**
 * Lo que el operador carga. Ingenio y viajes son una medida temporal del
 * frontend: la API todavía no publica la ubicación del ingenio ni los viajes.
 */
interface Escenario {
  ingenio: IngenioMapa;
  viajes: ViajeSimulado[];
  siguienteId: number;
}

/** Lo que se conserva: un viaje dura más que una visita a la pestaña. */
interface DatosCargados extends Escenario {
  /** Tiempo que el reloj se adelantó por las velocidades de prueba. */
  adelantoMs: number;
}

const SIN_DATOS: DatosCargados = { ingenio: { ...INGENIO_TEMPORAL }, viajes: [], siguienteId: 1, adelantoMs: 0 };

const esLugarValido = (lugar: Partial<IngenioMapa> | null | undefined): lugar is IngenioMapa =>
  !!lugar && typeof lugar.nombre === "string" && lugar.nombre !== "" && esUbicacionValida(lugar);

/**
 * Lo guardado puede venir de una versión anterior o haberse editado a mano: se
 * descarta lo inválido en lugar de dejar que una fecha imposible rompa la vista.
 */
function sanear(guardado: Partial<DatosCargados>): DatosCargados {
  const viajes = (Array.isArray(guardado.viajes) ? guardado.viajes : []).filter((v) => esViajeValido(v));
  const { adelantoMs, siguienteId } = guardado;
  return {
    ingenio: esLugarValido(guardado.ingenio) ? guardado.ingenio : { ...INGENIO_TEMPORAL },
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

/** Catálogo de fincas de la API; el mapa sigue disponible si falla. */
function useFincasApi() {
  const [fincas, setFincas] = useState<Finca[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [intento, setIntento] = useState(0);

  useEffect(() => {
    let vigente = true;
    container.fincas.obtenerFincas
      .execute()
      .then((f) => {
        if (!vigente) return;
        setFincas(f);
        setError(null);
      })
      .catch((e: unknown) => {
        if (vigente) setError(e instanceof Error && e.message ? e.message : "No se pudieron cargar las fincas de la API.");
      });
    return () => {
      vigente = false;
    };
  }, [intento]);

  return { fincas, error, reintentar: useCallback(() => setIntento((i) => i + 1), []) };
}

/**
 * Turnos activos de la cola. Ante una falla se conserva la última lectura.
 * También registra la hora del mapa (`relojMapa`) en que llega cada incidente nuevo: con x10 o x60 el reloj del
 * mapa va adelantado, y la hora real del reporte haría retroceder al camión.
 */
function useTurnosActivos(relojMapa: () => number) {
  const [turnos, setTurnos] = useState<Turno[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [detenidos, setDetenidos] = useState<Record<string, number>>({});

  useEffect(() => {
    let vigente = true;
    const leer = () =>
      container.turnos.obtenerTurnosActivos
        .execute()
        .then((t) => {
          if (!vigente) return;
          setTurnos(t);
          setError(null);
          const ahora = relojMapa();
          setDetenidos((d) => {
            const nuevos = t.flatMap((turno) => (turno.incidente && !(turno.incidente.id in d) ? [turno.incidente.id] : []));
            return nuevos.length > 0 ? { ...d, ...Object.fromEntries(nuevos.map((id) => [id, ahora])) } : d;
          });
        })
        .catch((e: unknown) => {
          if (vigente) setError(e instanceof Error && e.message ? e.message : "No se pudo leer la cola de turnos.");
        });
    leer();
    const id = setInterval(leer, INTERVALO_TURNOS_MS);
    return () => {
      vigente = false;
      clearInterval(id);
    };
  }, [relojMapa]);

  return { turnos, error, detenidos };
}

export function useMapaSimulado() {
  const [{ adelantoMs: adelantoInicial, ...escenarioInicial }] = useState(leerDatos);
  const [escenario, setEscenario] = useState<Escenario>(escenarioInicial);
  const [adelantoMs, setAdelantoMs] = useState(adelantoInicial);
  const [ahoraMs, setAhoraMs] = useState(() => Date.now() + adelantoInicial);
  const [multiplicador, setMultiplicador] = useState<Multiplicador>(1);
  const fincasApi = useFincasApi();
  // El intervalo y los guardados leen el último valor sin volver a suscribirse.
  const escenarioRef = useRef(escenario);
  const adelantoRef = useRef(adelantoInicial);
  const cola = useTurnosActivos(useCallback(() => Date.now() + adelantoRef.current, []));

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

  const { ingenio } = escenario;
  // Las fincas se ubican alrededor del ingenio: al moverlo, se mueven con él.
  const fincas = useMemo(() => ubicarFincasTemporalmente(ingenio, fincasApi.fincas ?? []), [ingenio, fincasApi.fincas]);

  const guardarIngenio = useCallback((nuevo: IngenioMapa) => setEscenario((e) => ({ ...e, ingenio: nuevo })), []);

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

  // Viajes de la cola de turnos: los agrega el mapa solo, sin intervención del operador.
  const deTurnos: ViajeCalculado[] = cola.turnos.flatMap((turno) => {
    const finca = fincaDelTurno(turno, fincas);
    if (!finca) return [];
    const detenidoEnMs = turno.incidente ? (cola.detenidos[turno.incidente.id] ?? ahoraMs) : undefined;
    const viaje = calcularViajeDeTurno(turno, finca, ingenio, ahoraMs, detenidoEnMs);
    return viaje ? [viaje] : [];
  });
  // Un viaje de la demo de una finca que la API ya no publica no se muestra, pero se conserva por si vuelve.
  const deDemo: ViajeCalculado[] = escenario.viajes.flatMap((viaje) => {
    const finca = fincas.find((f) => f.id === viaje.fincaId);
    return finca ? [calcularViaje(viaje, finca, ingenio, ahoraMs)] : [];
  });
  const viajes = [...deTurnos, ...deDemo];

  return {
    ahoraMs,
    adelantoMs,
    ingenio,
    fincas,
    cargandoFincas: fincasApi.fincas === null && !fincasApi.error,
    errorFincas: fincasApi.error,
    reintentarFincas: fincasApi.reintentar,
    errorTurnos: cola.error,
    /** Turnos activos cuya finca no está en el catálogo: no se pueden ubicar en el mapa. */
    turnosSinFinca: fincasApi.fincas ? cola.turnos.filter((t) => !fincaDelTurno(t, fincas)).length : 0,
    viajes,
    multiplicador,
    setMultiplicador,
    guardarIngenio,
    agregarViaje,
    quitarViaje,
  };
}
