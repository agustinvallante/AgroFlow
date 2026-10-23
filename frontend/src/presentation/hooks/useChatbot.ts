import { useCallback, useEffect, useRef, useState } from "react";
import { container } from "../../composition/container";
import type {
  Conversacion,
  MensajeChat,
  MetricasChatbot,
  PasoFlujoBot,
  PlantillaMensaje,
} from "../../domain/entities/Conversacion";

/** Con la API, la lista y la conversación abierta se refrescan solas. */
const INTERVALO_REFRESCO_MS = 15_000;

const mensajeDeError = (e: unknown, porDefecto: string) => (e instanceof Error && e.message ? e.message : porDefecto);

export function useChatbot() {
  const [conversaciones, setConversaciones] = useState<Conversacion[]>([]);
  const [conversacionActivaId, setConversacionActivaId] = useState<string | null>(null);
  const [mensajes, setMensajes] = useState<MensajeChat[]>([]);
  const [cargandoMensajes, setCargandoMensajes] = useState(false);
  const [errorMensajes, setErrorMensajes] = useState<string | null>(null);
  const [metricas, setMetricas] = useState<MetricasChatbot | null>(null);
  const [flujo, setFlujo] = useState<PasoFlujoBot[]>([]);
  const [plantillas, setPlantillas] = useState<PlantillaMensaje[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [errorRefresco, setErrorRefresco] = useState<string | null>(null);
  const [actualizadoEn, setActualizadoEn] = useState(0);
  const activaRef = useRef<string | null>(null);

  useEffect(() => {
    activaRef.current = conversacionActivaId;
  }, [conversacionActivaId]);

  useEffect(() => {
    let activo = true;
    setCargando(true);
    Promise.all([
      container.conversaciones.obtenerConversaciones.execute(),
      container.conversaciones.obtenerMetricasChatbot.execute(),
      container.conversaciones.obtenerFlujoDeAutomatizacion.execute(),
      container.conversaciones.obtenerPlantillas.execute(),
    ])
      .then(([convs, met, fl, plant]) => {
        if (!activo) return;
        setConversaciones(convs);
        setMetricas(met);
        setFlujo(fl);
        setPlantillas(plant);
        setConversacionActivaId(convs[0]?.id ?? null);
      })
      .catch((e) => activo && setError(mensajeDeError(e, "No se pudo cargar la información del chatbot.")))
      .finally(() => activo && setCargando(false));
    return () => {
      activo = false;
    };
  }, []);

  const recargar = useCallback(async () => {
    try {
      const convs = await container.conversaciones.obtenerConversaciones.execute();
      setConversaciones(convs);
      setErrorRefresco(null);
      if (!activaRef.current && convs[0]) setConversacionActivaId(convs[0].id);
      setActualizadoEn(Date.now());
    } catch (e) {
      setErrorRefresco(mensajeDeError(e, "No se pudieron actualizar las conversaciones."));
    }
  }, []);

  useEffect(() => {
    if (container.fuenteDatos !== "http") return;
    const id = setInterval(recargar, INTERVALO_REFRESCO_MS);
    return () => clearInterval(id);
  }, [recargar]);

  useEffect(() => {
    // Sin conversación activa no hay nada que mostrar: conversacionActiva es null.
    if (!conversacionActivaId) return;
    let activo = true;
    setCargandoMensajes(true);
    container.conversaciones.obtenerMensajesConversacion
      .execute(conversacionActivaId)
      .then((m) => {
        if (!activo) return;
        setMensajes(m);
        setErrorMensajes(null);
      })
      .catch((e) => activo && setErrorMensajes(mensajeDeError(e, "No se pudieron cargar los mensajes.")))
      .finally(() => activo && setCargandoMensajes(false));
    return () => {
      activo = false;
    };
  }, [conversacionActivaId, actualizadoEn]);

  const base = conversaciones.find((c) => c.id === conversacionActivaId) ?? null;
  const conversacionActiva = base ? { ...base, mensajes } : null;

  const guardarPlantillas = useCallback(async (nuevas: PlantillaMensaje[]) => {
    await container.conversaciones.guardarPlantillas.execute(nuevas);
    setPlantillas(nuevas);
  }, []);

  return {
    conversaciones,
    conversacionActiva,
    seleccionarConversacion: setConversacionActivaId,
    cargandoMensajes,
    errorMensajes,
    recargar,
    metricas,
    flujo,
    plantillas,
    setPlantillas,
    guardarPlantillas,
    cargando,
    error,
    errorRefresco,
  };
}
