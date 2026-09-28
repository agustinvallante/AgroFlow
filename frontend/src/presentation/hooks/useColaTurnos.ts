import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { container } from "../../composition/container";
import type { EstadoTurno, FiltrosTurno, NuevoTurnoManual, Turno } from "../../domain/entities/Turno";

export type FiltroFlota = "todas" | "propia" | "tercero";
export type FiltroEstado = "todos" | Turno["estado"];
export type OrdenCola = "turno" | "prioridad" | "espera";

/**
 * - "cargando": todavía no hubo respuesta para los filtros actuales.
 * - "ok": la última consulta respondió (la lista puede estar vacía).
 * - "error": la última consulta falló. Si antes hubo una respuesta para
 *   estos mismos filtros, se sigue mostrando marcada como desactualizada.
 */
export type EstadoCarga = "cargando" | "ok" | "error";

/** Consulta periódica de la cola. Solo lee: nunca dispara transiciones. */
export const INTERVALO_POLLING_MS = 4000;

// Formatos de los parámetros de consulta según openapi.yaml.
const PATRON_TELEFONO = /^\+[1-9][0-9]{7,14}$/;
const PATENTE_MIN = 6;
const PATENTE_MAX = 10;

const mensaje = (e: unknown, porDefecto: string) => (e instanceof Error ? e.message : porDefecto);

/**
 * Un filtro escrito a medias no se descarta en silencio: la consulta no se
 * hace y la vista lo informa, para no mostrar turnos que no coinciden.
 */
export function validarFiltros(patente: string, telefono: string): string[] {
  const problemas: string[] = [];
  const plate = patente.trim();
  const phone = telefono.trim();
  if (plate && (plate.length < PATENTE_MIN || plate.length > PATENTE_MAX)) {
    problemas.push(`La patente debe estar completa (entre ${PATENTE_MIN} y ${PATENTE_MAX} caracteres).`);
  }
  if (phone && !PATRON_TELEFONO.test(phone)) {
    problemas.push("El teléfono debe estar completo en formato internacional, por ejemplo +5493815550101.");
  }
  return problemas;
}

/** Resultado de una consulta, asociado a los filtros que la produjeron. */
interface ResultadoConsulta {
  clave: string;
  turnos: Turno[];
  estado: "ok" | "error";
  error: string | null;
  ultimaActualizacion: Date | null;
}

export function useColaTurnos() {
  const [resultado, setResultado] = useState<ResultadoConsulta | null>(null);
  const [errorAccion, setErrorAccion] = useState<string | null>(null);
  const [enCurso, setEnCurso] = useState<string | null>(null);

  // Filtros que resuelve la fuente de datos (API o mock).
  const [filtroEstado, setFiltroEstado] = useState<FiltroEstado>("todos");
  const [fecha, setFecha] = useState("");
  const [patente, setPatente] = useState("");
  const [telefono, setTelefono] = useState("");

  // Filtros y orden locales sobre lo ya recibido.
  const [busqueda, setBusqueda] = useState("");
  const [filtroFlota, setFiltroFlota] = useState<FiltroFlota>("todas");
  const [orden, setOrden] = useState<OrdenCola>("turno");

  const [detalleId, setDetalleId] = useState<string | null>(null);
  const [detalle, setDetalle] = useState<Turno | null>(null);

  const filtrosIncompletos = useMemo(() => validarFiltros(patente, telefono), [patente, telefono]);

  // null = hay filtros incompletos: no se consulta.
  const filtrosApi = useMemo<FiltrosTurno | null>(() => {
    if (filtrosIncompletos.length > 0) return null;
    return {
      fecha: fecha || undefined,
      estado: filtroEstado === "todos" ? undefined : filtroEstado,
      patente: patente.trim() || undefined,
      telefono: telefono.trim() || undefined,
    };
  }, [fecha, filtroEstado, patente, telefono, filtrosIncompletos]);

  const clave = JSON.stringify(filtrosApi);

  // Un resultado de otros filtros no corresponde a lo que se ve: se trata
  // como si todavía no hubiera respuesta.
  const vigente = resultado?.clave === clave ? resultado : null;
  const turnos = useMemo(() => vigente?.turnos ?? [], [vigente]);
  const estadoCarga: EstadoCarga = vigente?.estado ?? "cargando";

  // Descarta respuestas viejas si cambian los filtros con una consulta en vuelo.
  const ultimaConsulta = useRef(0);

  const cargar = useCallback(async () => {
    const consulta = ++ultimaConsulta.current;
    if (!filtrosApi) return;
    try {
      const data = await container.turnos.obtenerTurnosDelDia.execute(filtrosApi);
      if (consulta !== ultimaConsulta.current) return;
      setResultado({ clave, turnos: data, estado: "ok", error: null, ultimaActualizacion: new Date() });
    } catch (e) {
      if (consulta !== ultimaConsulta.current) return;
      const error = mensaje(e, "No se pudo cargar la cola de turnos.");
      // Se conserva la última lista válida para estos filtros, pero la
      // vista la marca como desactualizada.
      setResultado((prev) =>
        prev?.clave === clave
          ? { ...prev, estado: "error", error }
          : { clave, turnos: [], estado: "error", error, ultimaActualizacion: null }
      );
    }
  }, [filtrosApi, clave]);

  const cargarDetalle = useCallback(async (id: string) => {
    try {
      setDetalle(await container.turnos.obtenerDetalleTurno.execute(id));
    } catch (e) {
      setErrorAccion(mensaje(e, "No se pudo obtener el detalle del turno."));
      setDetalleId(null);
      setDetalle(null);
    }
  }, []);

  useEffect(() => {
    cargar();
    const id = setInterval(() => {
      cargar();
      if (detalleId) cargarDetalle(detalleId);
    }, INTERVALO_POLLING_MS);
    return () => clearInterval(id);
  }, [cargar, cargarDetalle, detalleId]);

  const abrirDetalle = useCallback(
    (id: string) => {
      setDetalleId(id);
      setDetalle(null);
      cargarDetalle(id);
    },
    [cargarDetalle]
  );

  const cerrarDetalle = useCallback(() => {
    setDetalleId(null);
    setDetalle(null);
  }, []);

  /**
   * Una acción solo se considera exitosa cuando responde la fuente de datos.
   * Ante cualquier error (p. ej. 409) se conserva lo visible, se informa y
   * se recarga para mostrar el estado vigente.
   */
  const ejecutarAccion = useCallback(
    async (id: string, accion: () => Promise<Turno>) => {
      setErrorAccion(null);
      setEnCurso(id);
      try {
        const actualizado = await accion();
        setResultado((prev) =>
          prev && { ...prev, turnos: prev.turnos.map((t) => (t.id === id ? actualizado : t)) }
        );
        setDetalle((prev) => (prev?.id === id ? actualizado : prev));
      } catch (e) {
        setErrorAccion(mensaje(e, "No se pudo actualizar el turno."));
        if (detalleId) cargarDetalle(detalleId);
      } finally {
        setEnCurso(null);
        cargar();
      }
    },
    [cargar, cargarDetalle, detalleId]
  );

  const cambiarEstado = useCallback(
    (id: string, nuevoEstado: EstadoTurno) =>
      ejecutarAccion(id, () => container.turnos.cambiarEstadoTurno.execute(id, nuevoEstado)),
    [ejecutarAccion]
  );

  const cancelar = useCallback(
    (id: string) => ejecutarAccion(id, () => container.turnos.cancelarTurno.execute(id)),
    [ejecutarAccion]
  );

  const reasignar = useCallback(
    (id: string, nuevaHora: string) =>
      ejecutarAccion(id, () => container.turnos.reasignarHorarioTurno.execute(id, nuevaHora)),
    [ejecutarAccion]
  );

  const crearTurno = useCallback(
    async (datos: NuevoTurnoManual) => {
      // El error se propaga para que el formulario lo muestre.
      await container.turnos.crearTurnoManual.execute(datos);
      await cargar();
    },
    [cargar]
  );

  const turnosFiltrados = useMemo(() => {
    const texto = busqueda.toLowerCase().trim();
    const lista = turnos.filter((t) => {
      const coincideTexto =
        !texto ||
        t.patente.toLowerCase().includes(texto) ||
        t.chofer.toLowerCase().includes(texto) ||
        t.finca.toLowerCase().includes(texto);
      const coincideFlota = filtroFlota === "todas" || t.flota === filtroFlota;
      return coincideTexto && coincideFlota;
    });

    return [...lista].sort((a, b) => {
      if (orden === "prioridad") return b.horasDesdeCorte - a.horasDesdeCorte;
      if (orden === "espera") return (b.esperaMin ?? 0) - (a.esperaMin ?? 0);
      return a.offsetMin - b.offsetMin;
    });
  }, [turnos, busqueda, filtroFlota, orden]);

  const resumen = useMemo(
    () => ({
      total: turnos.length,
      pendientes: turnos.filter((t) => t.estado === "pendiente").length,
      completados: turnos.filter((t) => t.estado === "completado").length,
      cancelados: turnos.filter((t) => t.estado === "cancelado").length,
    }),
    [turnos]
  );

  return {
    turnos: turnosFiltrados,
    resumen,
    estadoCarga,
    cargando: estadoCarga === "cargando",
    error: vigente?.error ?? null,
    ultimaActualizacion: vigente?.ultimaActualizacion ?? null,
    /** Hay datos en pantalla, pero la última consulta falló. */
    desactualizado: estadoCarga === "error" && !!vigente?.ultimaActualizacion,
    filtrosIncompletos,
    errorAccion,
    limpiarErrorAccion: () => setErrorAccion(null),
    enCurso,
    filtros: { busqueda, filtroFlota, filtroEstado, orden, fecha, patente, telefono },
    setBusqueda,
    setFiltroFlota,
    setFiltroEstado,
    setOrden,
    setFecha,
    setPatente,
    setTelefono,
    detalleId,
    detalle,
    abrirDetalle,
    cerrarDetalle,
    crearTurno,
    cambiarEstado,
    cancelar,
    reasignar,
    recargar: cargar,
  };
}
