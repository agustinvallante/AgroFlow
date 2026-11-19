import { useCallback, useEffect, useRef, useState } from "react";
import { container } from "../../composition/container";
import type { Conversacion, MensajeChat } from "../../domain/entities/Conversacion";
import { ListadoIncompleto } from "../../domain/errors/ListadoIncompleto";

/** Un listado incompleto se muestra igual, con su aviso; cualquier otro error se propaga. */
async function conAviso<T>(pedido: Promise<T[]>): Promise<{ items: T[]; aviso: string | null }> {
  try {
    return { items: await pedido, aviso: null };
  } catch (e) {
    if (e instanceof ListadoIncompleto) return { items: e.parciales as T[], aviso: e.message };
    throw e;
  }
}

/**
 * La conversación abierta y la lista se refrescan solas. Una lectura periódica no
 * empieza mientras la anterior del mismo tipo sigue pendiente, y se pausan con la
 * pestaña oculta.
 */
const INTERVALO_MENSAJES_MS = 1_000;
const INTERVALO_LISTA_MS = 3_000;

const mensajeDeError = (e: unknown, porDefecto: string) => (e instanceof Error && e.message ? e.message : porDefecto);
const pestanaOculta = () => typeof document !== "undefined" && document.visibilityState === "hidden";

/** Cambia cuando la conversación muestra actividad nueva en el listado. */
const firma = (c: Conversacion) => c.ultimoMensajeUtc ?? `${c.hora}|${c.preview}`;

const SIN_IDS: ReadonlySet<string> = new Set();

export function useChatbot() {
  const [conversaciones, setConversaciones] = useState<Conversacion[]>([]);
  const [conversacionActivaId, setConversacionActivaId] = useState<string | null>(null);
  // Cuenta los clics en la lista, incluso sobre la conversación ya abierta.
  const [seleccion, setSeleccion] = useState(0);
  const [mensajes, setMensajes] = useState<MensajeChat[]>([]);
  const [cargandoMensajes, setCargandoMensajes] = useState(false);
  const [errorMensajes, setErrorMensajes] = useState<string | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [errorRefresco, setErrorRefresco] = useState<string | null>(null);
  const [avisoConversaciones, setAvisoConversaciones] = useState<string | null>(null);
  const [revisionMensajes, setRevisionMensajes] = useState(0);
  // Mensajes llegados después de mostrar la conversación abierta, y cuántos fueron.
  const [mensajesNuevos, setMensajesNuevos] = useState<ReadonlySet<string>>(SIN_IDS);
  const [llegadas, setLlegadas] = useState(0);
  // Novedades por conversación desde que el operador la vio por última vez (sólo en esta sesión).
  const [noLeidos, setNoLeidos] = useState<Readonly<Record<string, number>>>({});
  const [destacadas, setDestacadas] = useState<ReadonlySet<string>>(SIN_IDS);
  const activaRef = useRef<string | null>(null);
  const cargaValida = useRef(false);
  const ultimaConsulta = useRef(0);
  const ultimoPedidoMensajes = useRef(0);
  const consultaPendiente = useRef<number | null>(null);
  const pedidoMensajesPendiente = useRef<number | null>(null);
  const firmas = useRef<Map<string, string> | null>(null);
  const mostrados = useRef<{ id: string | null; ids: Set<string> | null }>({ id: null, ids: null });
  const invalidarMensajes = useCallback(() => { ultimoPedidoMensajes.current++; }, []);
  const invalidarConsultas = useCallback(() => {
    ultimaConsulta.current++;
    invalidarMensajes();
  }, [invalidarMensajes]);

  const activarConversacion = useCallback((id: string | null) => {
    if (id !== activaRef.current) {
      // Invalida inmediatamente cualquier respuesta de la selección anterior.
      invalidarMensajes();
      mostrados.current = { id, ids: null };
      setMensajes([]);
      setMensajesNuevos(SIN_IDS);
      setLlegadas(0);
      setErrorMensajes(null);
      setCargandoMensajes(id !== null);
    }
    activaRef.current = id;
    setConversacionActivaId(id);
    if (id) setNoLeidos((previos) => {
      if (!(id in previos)) return previos;
      const siguientes = { ...previos };
      delete siguientes[id];
      return siguientes;
    });
  }, [invalidarMensajes]);

  /** Novedades de cada conversación respecto del listado anterior; ninguna en la primera carga. */
  const registrarNovedades = useCallback((convs: Conversacion[], activa: string | null) => {
    const anteriores = firmas.current;
    firmas.current = new Map(convs.map((c) => [c.id, firma(c)]));
    if (!anteriores) return;
    const cambiadas = convs.filter((c) => c.id !== activa && anteriores.get(c.id) !== firma(c)).map((c) => c.id);
    setDestacadas(cambiadas.length > 0 ? new Set(cambiadas) : SIN_IDS);
    if (cambiadas.length === 0) return;
    setNoLeidos((previos) => {
      const siguientes = { ...previos };
      for (const id of cambiadas) siguientes[id] = (siguientes[id] ?? 0) + 1;
      return siguientes;
    });
  }, []);

  const refrescar = useCallback(async (conMensajes: boolean) => {
    const consulta = ++ultimaConsulta.current;
    consultaPendiente.current = consulta;
    if (!cargaValida.current) {
      setCargando(true);
      setError(null);
    }
    try {
      const { items: convs, aviso } = await conAviso(container.conversaciones.obtenerConversaciones.execute());
      if (consulta !== ultimaConsulta.current) return;
      cargaValida.current = true;
      setConversaciones(convs);
      setAvisoConversaciones(aviso);
      setError(null);
      setErrorRefresco(null);
      const seleccionVigente = convs.some((c) => c.id === activaRef.current);
      const activa = seleccionVigente ? activaRef.current : convs[0]?.id ?? null;
      activarConversacion(activa);
      registrarNovedades(convs, activa);
      if (conMensajes) setRevisionMensajes((revision) => revision + 1);
    } catch (e) {
      if (consulta !== ultimaConsulta.current) return;
      if (cargaValida.current) {
        setErrorRefresco(mensajeDeError(e, "No se pudieron actualizar las conversaciones."));
      } else {
        setError(mensajeDeError(e, "No se pudo cargar la información del chatbot."));
      }
    } finally {
      if (consultaPendiente.current === consulta) consultaPendiente.current = null;
      if (consulta === ultimaConsulta.current) setCargando(false);
    }
  }, [activarConversacion, registrarNovedades]);

  /** Acción del usuario: lista y mensajes, aunque haya una lectura pendiente. */
  const recargar = useCallback(() => refrescar(true), [refrescar]);

  useEffect(() => {
    recargar();
    const leerLista = () => {
      if (!pestanaOculta() && consultaPendiente.current === null) refrescar(false);
    };
    const leerMensajes = () => {
      if (pestanaOculta() || !activaRef.current || pedidoMensajesPendiente.current !== null) return;
      setRevisionMensajes((r) => r + 1);
    };
    const alCambiarVisibilidad = () => {
      if (pestanaOculta()) return;
      leerLista();
      leerMensajes();
    };
    const lista = setInterval(leerLista, INTERVALO_LISTA_MS);
    const mensajesAbiertos = setInterval(leerMensajes, INTERVALO_MENSAJES_MS);
    document.addEventListener("visibilitychange", alCambiarVisibilidad);
    return () => {
      clearInterval(lista);
      clearInterval(mensajesAbiertos);
      document.removeEventListener("visibilitychange", alCambiarVisibilidad);
      invalidarConsultas();
    };
  }, [recargar, refrescar, invalidarConsultas]);

  useEffect(() => {
    // Sin conversación activa no hay nada que mostrar: conversacionActiva es null.
    if (!conversacionActivaId) return;
    const pedido = ++ultimoPedidoMensajes.current;
    pedidoMensajesPendiente.current = pedido;
    const sigueVigente = () => pedido === ultimoPedidoMensajes.current && activaRef.current === conversacionActivaId;
    conAviso(container.conversaciones.obtenerMensajesConversacion.execute(conversacionActivaId))
      .then(({ items, aviso }) => {
        if (!sigueVigente()) return;
        // La primera lectura de la conversación no cuenta como llegada: sólo lo posterior.
        const anteriores = mostrados.current.ids;
        mostrados.current = { id: conversacionActivaId, ids: new Set(items.flatMap((m) => (m.id ? [m.id] : []))) };
        const llegados = anteriores ? items.flatMap((m) => (m.id && !anteriores.has(m.id) ? [m.id] : [])) : [];
        if (llegados.length > 0) {
          setMensajesNuevos((previos) => new Set([...previos, ...llegados]));
          setLlegadas((n) => n + llegados.length);
        }
        setMensajes(items);
        setErrorMensajes(aviso);
      })
      .catch((e) => sigueVigente() && setErrorMensajes(mensajeDeError(e, "No se pudieron cargar los mensajes.")))
      .finally(() => {
        if (pedidoMensajesPendiente.current === pedido) pedidoMensajesPendiente.current = null;
        if (sigueVigente()) setCargandoMensajes(false);
      });
    return invalidarMensajes;
  }, [conversacionActivaId, revisionMensajes, invalidarMensajes]);

  const seleccionarConversacion = useCallback((id: string) => {
    activarConversacion(id);
    setSeleccion((n) => n + 1);
  }, [activarConversacion]);

  const base = conversaciones.find((c) => c.id === conversacionActivaId) ?? null;
  const conversacionActiva = base ? { ...base, mensajes } : null;

  return {
    conversaciones,
    conversacionActiva,
    seleccionarConversacion,
    seleccion,
    cargandoMensajes,
    errorMensajes,
    mensajesNuevos,
    llegadas,
    noLeidos,
    destacadas,
    recargar,
    cargando,
    error,
    errorRefresco,
    avisoConversaciones,
  };
}
