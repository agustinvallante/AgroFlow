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

/** La lista y la conversación abierta se refrescan solas. */
const INTERVALO_REFRESCO_MS = 15_000;

const mensajeDeError = (e: unknown, porDefecto: string) => (e instanceof Error && e.message ? e.message : porDefecto);

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
  const activaRef = useRef<string | null>(null);
  const cargaValida = useRef(false);
  const ultimaConsulta = useRef(0);
  const ultimoPedidoMensajes = useRef(0);
  const invalidarMensajes = useCallback(() => { ultimoPedidoMensajes.current++; }, []);
  const invalidarConsultas = useCallback(() => {
    ultimaConsulta.current++;
    invalidarMensajes();
  }, [invalidarMensajes]);

  const activarConversacion = useCallback((id: string | null) => {
    if (id !== activaRef.current) {
      // Invalida inmediatamente cualquier respuesta de la selección anterior.
      invalidarMensajes();
      setMensajes([]);
      setErrorMensajes(null);
      setCargandoMensajes(id !== null);
    }
    activaRef.current = id;
    setConversacionActivaId(id);
  }, [invalidarMensajes]);

  const recargar = useCallback(async () => {
    const consulta = ++ultimaConsulta.current;
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
      activarConversacion(seleccionVigente ? activaRef.current : convs[0]?.id ?? null);
      setRevisionMensajes((revision) => revision + 1);
    } catch (e) {
      if (consulta !== ultimaConsulta.current) return;
      if (cargaValida.current) {
        setErrorRefresco(mensajeDeError(e, "No se pudieron actualizar las conversaciones."));
      } else {
        setError(mensajeDeError(e, "No se pudo cargar la información del chatbot."));
      }
    } finally {
      if (consulta === ultimaConsulta.current) setCargando(false);
    }
  }, [activarConversacion]);

  useEffect(() => {
    recargar();
    const id = setInterval(recargar, INTERVALO_REFRESCO_MS);
    return () => {
      clearInterval(id);
      invalidarConsultas();
    };
  }, [recargar, invalidarConsultas]);

  useEffect(() => {
    // Sin conversación activa no hay nada que mostrar: conversacionActiva es null.
    if (!conversacionActivaId) return;
    const pedido = ++ultimoPedidoMensajes.current;
    const sigueVigente = () => pedido === ultimoPedidoMensajes.current && activaRef.current === conversacionActivaId;
    setCargandoMensajes(true);
    conAviso(container.conversaciones.obtenerMensajesConversacion.execute(conversacionActivaId))
      .then(({ items, aviso }) => {
        if (!sigueVigente()) return;
        setMensajes(items);
        setErrorMensajes(aviso);
      })
      .catch((e) => sigueVigente() && setErrorMensajes(mensajeDeError(e, "No se pudieron cargar los mensajes.")))
      .finally(() => sigueVigente() && setCargandoMensajes(false));
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
    recargar,
    cargando,
    error,
    errorRefresco,
    avisoConversaciones,
  };
}
