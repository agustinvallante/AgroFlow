import { useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { MensajeChat } from "../../domain/entities/Conversacion";

/**
 * Para el panel general no hace falta el detalle completo de cada
 * conversación (eso vive en la vista Chatbot); alcanza con un feed
 * plano de mensajes recientes para transmitir actividad en vivo.
 */
export function useBotFeed() {
  const [mensajes, setMensajes] = useState<MensajeChat[]>([]);

  useEffect(() => {
    let activo = true;
    let pool: MensajeChat[] = [];
    let idx = 0;

    container.conversaciones.obtenerConversaciones.execute().then((conversaciones) => {
      if (!activo) return;
      pool = conversaciones.flatMap((c) => c.mensajes);
      setMensajes(pool.slice(0, 6));
      idx = 6;
    });

    const id = setInterval(() => {
      if (pool.length === 0) return;
      const siguiente = pool[idx % pool.length];
      idx += 1;
      setMensajes((prev) => [...prev, siguiente].slice(-14));
    }, 12000);

    return () => {
      activo = false;
      clearInterval(id);
    };
  }, []);

  return mensajes;
}
