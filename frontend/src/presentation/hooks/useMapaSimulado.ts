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
import {
  ADELANTO_MAXIMO_MS,
  agregarTramo,
  cerrarTramos,
  horaMapaDe,
  sanearTramos,
  saltarReloj,
  type TramoReloj,
} from "../../domain/services/relojMapa";
import { INGENIO_TEMPORAL, ubicarFincasTemporalmente } from "../../domain/services/ubicacionTemporal";
import { calcularViajeDeTurno, fincaDelTurno } from "../../domain/services/viajesDesdeTurnos";

/** x1 es el tiempo real; las otras velocidades sólo sirven para probar un viaje largo. */
export const MULTIPLICADORES = [1, 10, 60] as const;
export type Multiplicador = (typeof MULTIPLICADORES)[number];

export type IngenioMapa = PuntoGeo & { nombre: string };

const INTERVALO_MS = 100;
/** La cola cambia por el chatbot o el panel de turnos: se vuelve a leer seguido para seguir sus eventos. */
export const INTERVALO_TURNOS_MS = 3_000;
const MS_POR_MINUTO = 60_000;
/** v3: las fincas ya no se guardan, vienen de la API; se descartan los escenarios anteriores. */
export const CLAVE_ALMACENAMIENTO = "agroflow.mapa-simulado.v3";
/**
 * Con x10/x60 el adelanto cambia en cada tick; se guarda como mucho con esta
 * frecuencia, además de al cambiar la velocidad, salir de la pestaña o cerrar la página.
 */
export const INTERVALO_GUARDADO_ADELANTO_MS = 5_000;

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
  /** Tramos del reloj de este navegador, para llevar los instantes de la API a la hora del mapa. */
  tramosReloj: TramoReloj[];
}

const SIN_DATOS: DatosCargados = {
  ingenio: { ...INGENIO_TEMPORAL },
  viajes: [],
  siguienteId: 1,
  adelantoMs: 0,
  tramosReloj: [],
};

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
    tramosReloj: sanearTramos(guardado.tramosReloj),
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

/** Agrega la hora del mapa a las claves que todavía no la tienen. */
function registrarVistos(vistos: Record<string, number>, claves: string[], ahoraMs: number) {
  const nuevas = claves.filter((clave) => !(clave in vistos));
  return nuevas.length > 0 ? { ...vistos, ...Object.fromEntries(nuevas.map((clave) => [clave, ahoraMs])) } : vistos;
}

const limitar = (vistos: Record<string, number>, hastaMs: number) =>
  Object.fromEntries(Object.entries(vistos).map(([clave, ms]) => [clave, Math.min(ms, hastaMs)]));

/**
 * Turnos activos de la cola. Ante una falla se conserva la última lectura.
 * También registra la hora del mapa (`relojMapa`) en que ve por primera vez cada incidente abierto y cada turno en
 * camino: si la API no informa la salida, el camión sale cuando el mapa lo vio en camino.
 */
function useTurnosActivos(relojMapa: () => number) {
  const [turnos, setTurnos] = useState<Turno[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [detenidos, setDetenidos] = useState<Record<string, number>>({});
  const [salidas, setSalidas] = useState<Record<string, number>>({});

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
          const incidentes = t.flatMap((turno) => (turno.incidente ? [turno.incidente.id] : []));
          setDetenidos((d) => registrarVistos(d, incidentes, ahora));
          const enCamino = t.flatMap((turno) => (turno.estado === "viaje" ? [turno.id] : []));
          setSalidas((s) => registrarVistos(s, enCamino, ahora));
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

  /** Al volver el reloj atrás, ningún camión puede quedar detenido ni salir en una hora posterior. */
  const limitarVistos = useCallback((hastaMs: number) => {
    setDetenidos((d) => limitar(d, hastaMs));
    setSalidas((s) => limitar(s, hastaMs));
  }, []);

  return { turnos, error, detenidos, salidas, limitarVistos };
}

export function useMapaSimulado() {
  const [{ adelantoMs: adelantoInicial, tramosReloj: tramosIniciales, ...escenarioInicial }] = useState(leerDatos);
  const [escenario, setEscenario] = useState<Escenario>(escenarioInicial);
  const [adelantoMs, setAdelantoMs] = useState(adelantoInicial);
  const [ahoraMs, setAhoraMs] = useState(() => Date.now() + adelantoInicial);
  const [multiplicador, setMultiplicadorActual] = useState<Multiplicador>(1);
  // Al abrir, la velocidad es x1: si lo guardado terminaba acelerado, desde ahora sigue a x1.
  const [tramos, setTramos] = useState(() => cerrarTramos(tramosIniciales, Date.now(), adelantoInicial));
  const fincasApi = useFincasApi();
  // El intervalo y los guardados leen el último valor sin volver a suscribirse.
  const escenarioRef = useRef(escenario);
  const adelantoRef = useRef(adelantoInicial);
  const tramosRef = useRef(tramos);
  const cola = useTurnosActivos(useCallback(() => Date.now() + adelantoRef.current, []));

  // Se guarda como si el mapa se cerrara ahora: el tiempo que quede cerrado no se cuenta acelerado.
  const guardar = useCallback(
    () =>
      guardarDatos({
        ...escenarioRef.current,
        adelantoMs: adelantoRef.current,
        tramosReloj: cerrarTramos(tramosRef.current, Date.now(), adelantoRef.current),
      }),
    [],
  );

  /** Cada cambio de velocidad y cada salto de reloj abre un tramo nuevo desde este instante. */
  const registrarTramo = useCallback((velocidad: Multiplicador) => {
    tramosRef.current = agregarTramo(tramosRef.current, {
      realInicioMs: Date.now(),
      adelantoInicioMs: adelantoRef.current,
      multiplicador: velocidad,
    });
    setTramos(tramosRef.current);
  }, []);

  const setMultiplicador = useCallback(
    (velocidad: Multiplicador) => {
      if (velocidad === multiplicador) return;
      registrarTramo(velocidad);
      setMultiplicadorActual(velocidad);
    },
    [multiplicador, registrarTramo],
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

  /**
   * Vuelve a la hora real: los turnos de la cola se ubican con las horas reales de la API, como en cualquier otra
   * pantalla sin adelanto. Los viajes de prueba se borran porque su salida se calculó con el reloj adelantado.
   */
  const { limitarVistos } = cola;
  const volverAHoraReal = useCallback(() => {
    const ahora = Date.now();
    adelantoRef.current = 0;
    setAdelantoMs(0);
    setAhoraMs(ahora);
    setMultiplicadorActual(1);
    // Sin adelanto los instantes de la API ya están en la hora del mapa: el registro de tramos sobra.
    tramosRef.current = [];
    setTramos([]);
    limitarVistos(ahora);
    // Cambiar el escenario lo guarda, ya con el adelanto en cero.
    setEscenario((e) => ({ ...e, viajes: [] }));
  }, [limitarVistos]);

  /** Salto de reloj para la demo: adelanta el mapa sin esperar, como x10 o x60 pero de una vez. */
  const adelantar = useCallback(
    (ms: number) => {
      const extraMs = Math.min(ms, ADELANTO_MAXIMO_MS - adelantoRef.current);
      if (extraMs <= 0) return;
      tramosRef.current = saltarReloj(tramosRef.current, Date.now(), adelantoRef.current, extraMs, multiplicador);
      setTramos(tramosRef.current);
      adelantoRef.current += extraMs;
      setAdelantoMs(adelantoRef.current);
      setAhoraMs((previo) => previo + extraMs);
      guardar();
    },
    [guardar, multiplicador],
  );

  // Viajes de la cola de turnos: los agrega el mapa solo, sin intervención del operador.
  const aHoraMapa = (realMs: number) => horaMapaDe(tramos, realMs, adelantoMs);
  const deTurnos: ViajeCalculado[] = cola.turnos.flatMap((turno) => {
    const finca = fincaDelTurno(turno, fincas);
    if (!finca) return [];
    const viaje = calcularViajeDeTurno(turno, finca, ingenio, ahoraMs, {
      aHoraMapa,
      salidaVistaMs: cola.salidas[turno.id],
      detenidoVistoMs: turno.incidente ? cola.detenidos[turno.incidente.id] : undefined,
    });
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
    volverAHoraReal,
    adelantar,
  };
}
