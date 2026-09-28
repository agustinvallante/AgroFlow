import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { container } from "../../composition/container";
import type { FiltrosTurno, NuevoTurnoManual, Turno } from "../../domain/entities/Turno";

export type FiltroFlota = "todas" | "propia" | "tercero";
export type FiltroEstado = "todos" | Turno["estado"];
export type OrdenCola = "turno" | "prioridad" | "espera";

/** Consulta periódica de la cola. Solo lee: nunca dispara transiciones. */
const INTERVALO_POLLING_MS = 4000;

const mensaje = (e: unknown, porDefecto: string) => (e instanceof Error ? e.message : porDefecto);

export function useColaTurnos() {
  const [turnos, setTurnos] = useState<Turno[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
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

  const filtrosApi = useMemo<FiltrosTurno>(() => {
    const plate = patente.trim();
    const phone = telefono.trim();
    return {
      fecha: fecha || undefined,
      estado: filtroEstado === "todos" ? undefined : filtroEstado,
      // La API exige la patente completa (6–10) y el teléfono en E.164.
      patente: plate.length >= 6 ? plate : undefined,
      telefono: /^\+[1-9][0-9]{7,14}$/.test(phone) ? phone : undefined,
    };
  }, [fecha, filtroEstado, patente, telefono]);

  // Descarta respuestas viejas si cambian los filtros con una consulta en vuelo.
  const ultimaConsulta = useRef(0);

  const cargar = useCallback(
    async (silencioso = false) => {
      const consulta = ++ultimaConsulta.current;
      if (!silencioso) setCargando(true);
      try {
        const data = await container.turnos.obtenerTurnosDelDia.execute(filtrosApi);
        if (consulta !== ultimaConsulta.current) return;
        setTurnos(data);
        setError(null);
      } catch (e) {
        if (consulta !== ultimaConsulta.current) return;
        setError(mensaje(e, "No se pudo cargar la cola de turnos."));
      } finally {
        if (consulta === ultimaConsulta.current) setCargando(false);
      }
    },
    [filtrosApi]
  );

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
  }, [cargar]);

  useEffect(() => {
    const id = setInterval(() => {
      cargar(true);
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
        setTurnos((prev) => prev.map((t) => (t.id === id ? actualizado : t)));
        setDetalle((prev) => (prev?.id === id ? actualizado : prev));
      } catch (e) {
        setErrorAccion(mensaje(e, "No se pudo actualizar el turno."));
        if (detalleId) cargarDetalle(detalleId);
      } finally {
        setEnCurso(null);
        cargar(true);
      }
    },
    [cargar, cargarDetalle, detalleId]
  );

  const avanzar = useCallback(
    (id: string) => ejecutarAccion(id, () => container.turnos.avanzarEstadoTurno.execute(id)),
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
      await cargar(true);
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
    cargando,
    error,
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
    avanzar,
    cancelar,
    reasignar,
    recargar: cargar,
  };
}
