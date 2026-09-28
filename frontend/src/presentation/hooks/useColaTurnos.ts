import { useCallback, useEffect, useMemo, useState } from "react";
import { container } from "../../composition/container";
import type { NuevoTurnoManual, Turno } from "../../domain/entities/Turno";

export type FiltroFlota = "todas" | "propia" | "tercero";
export type FiltroEstado = "todos" | Turno["estado"];
export type OrdenCola = "turno" | "prioridad" | "espera";

export function useColaTurnos() {
  const [turnos, setTurnos] = useState<Turno[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [busqueda, setBusqueda] = useState("");
  const [filtroFlota, setFiltroFlota] = useState<FiltroFlota>("todas");
  const [filtroEstado, setFiltroEstado] = useState<FiltroEstado>("todos");
  const [orden, setOrden] = useState<OrdenCola>("turno");

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      const data = await container.turnos.obtenerTurnosDelDia.execute();
      setTurnos(data);
    } catch {
      setError("No se pudo cargar la cola de turnos.");
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const turnosFiltrados = useMemo(() => {
    const texto = busqueda.toLowerCase().trim();
    let lista = turnos.filter((t) => {
      const coincideTexto =
        !texto ||
        t.patente.toLowerCase().includes(texto) ||
        t.chofer.toLowerCase().includes(texto) ||
        t.finca.toLowerCase().includes(texto);
      const coincideFlota = filtroFlota === "todas" || t.flota === filtroFlota;
      const coincideEstado = filtroEstado === "todos" || t.estado === filtroEstado;
      return coincideTexto && coincideFlota && coincideEstado;
    });

    lista = [...lista].sort((a, b) => {
      if (orden === "prioridad") return b.horasDesdeCorte - a.horasDesdeCorte;
      if (orden === "espera") return b.esperaMin - a.esperaMin;
      return a.offsetMin - b.offsetMin;
    });

    return lista;
  }, [turnos, busqueda, filtroFlota, filtroEstado, orden]);

  const resumen = useMemo(
    () => ({
      total: turnos.length,
      pendientes: turnos.filter((t) => t.estado === "pendiente").length,
      completados: turnos.filter((t) => t.estado === "completado").length,
      demorados: turnos.filter((t) => t.estado === "demorado").length,
    }),
    [turnos]
  );

  const crearTurno = useCallback(async (datos: NuevoTurnoManual) => {
    const nuevo = await container.turnos.crearTurnoManual.execute(datos);
    setTurnos((prev) => [...prev, nuevo]);
  }, []);

  const reasignar = useCallback(async (id: string, nuevaHora: string) => {
    const actualizado = await container.turnos.reasignarHorarioTurno.execute(id, nuevaHora);
    setTurnos((prev) => prev.map((t) => (t.id === id ? actualizado : t)));
  }, []);

  const cancelar = useCallback(async (id: string) => {
    await container.turnos.cancelarTurno.execute(id);
    setTurnos((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return {
    turnos: turnosFiltrados,
    resumen,
    cargando,
    error,
    filtros: { busqueda, filtroFlota, filtroEstado, orden },
    setBusqueda,
    setFiltroFlota,
    setFiltroEstado,
    setOrden,
    crearTurno,
    reasignar,
    cancelar,
    recargar: cargar,
  };
}
