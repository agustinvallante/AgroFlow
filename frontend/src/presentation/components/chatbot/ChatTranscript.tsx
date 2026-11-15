import { Fragment, useLayoutEffect, useRef } from "react";
import type { Conversacion, EstadoEnvio, EstadoProcesamiento, MensajeChat } from "../../../domain/entities/Conversacion";
import { etiquetaDiaEnZonaIngenio, horaEnZonaIngenio } from "../../../shared/utils/date";

const META_LABEL: Record<Conversacion["estado"], string> = {
  resuelto: "Resuelto",
  bot: "Gestionado por el bot",
  escalado: "Escalado a un operador",
  revision: "Requiere revisión de un operador",
};

// "Aceptado" es lo que informa el proveedor: no implica entregado ni leído.
const ENVIO_LABEL: Record<EstadoEnvio, string> = {
  pendiente: "Pendiente de envío",
  aceptado: "Aceptado por el proveedor",
  fallido: "Envío fallido",
  incierto: "Envío incierto · requiere revisión",
};

const PROCESAMIENTO_LABEL: Partial<Record<EstadoProcesamiento, string>> = {
  fallido: "Sin respuesta · procesamiento fallido",
  vencido: "Sin respuesta · autorización vencida",
};

interface ChatTranscriptProps {
  conversacion: Conversacion | null;
  /** Cambia con cada clic en la lista: vuelve a llevar la vista al último mensaje. */
  seleccion?: number;
  cargando?: boolean;
  error?: string | null;
}

/** "Ayer · 18:44"; sin el instante, el texto que ya trae la conversación. */
function momentoUltimoMensaje({ ultimoMensajeUtc, hora }: Conversacion): string {
  if (!ultimoMensajeUtc) return hora;
  return `${etiquetaDiaEnZonaIngenio(ultimoMensajeUtc)} · ${horaEnZonaIngenio(ultimoMensajeUtc)}`;
}

/** Día del mensaje; `null` si no trae fecha, y entonces no abre una división. */
const diaDeMensaje = (mensaje: MensajeChat | undefined) =>
  mensaje?.fechaUtc ? etiquetaDiaEnZonaIngenio(mensaje.fechaUtc) : null;

export function ChatTranscript({ conversacion, seleccion = 0, cargando = false, error = null }: ChatTranscriptProps) {
  const cuerpoRef = useRef<HTMLDivElement>(null);
  const seleccionAtendida = useRef<string | null>(null);
  const claveSeleccion = `${conversacion?.id ?? ""}:${seleccion}`;
  const cantidadMensajes = conversacion?.mensajes.length ?? 0;

  // Los mensajes llegan después del clic: se baja una sola vez por selección,
  // cuando ya están, para no mover al usuario en cada refresco.
  useLayoutEffect(() => {
    const cuerpo = cuerpoRef.current;
    if (!cuerpo || cantidadMensajes === 0 || seleccionAtendida.current === claveSeleccion) return;
    cuerpo.scrollTop = cuerpo.scrollHeight;
    seleccionAtendida.current = claveSeleccion;
  }, [claveSeleccion, cantidadMensajes]);

  const detalle = conversacion
    ? [
        conversacion.direccion && conversacion.remitenteReconocido ? conversacion.direccion : null,
        momentoUltimoMensaje(conversacion),
        META_LABEL[conversacion.estado],
      ]
        .filter(Boolean)
        .join(" · ")
    : "Seleccioná una conversación";

  return (
    <div className="chat-transcript">
      <div className="chat-transcript-head">
        <strong>{conversacion?.nombre ?? "—"}</strong>
        <span>{detalle}</span>
      </div>
      <div className="chat-transcript-body" ref={cuerpoRef}>
        {error && <div className="error-state">{error}</div>}
        {cargando && conversacion?.mensajes.length === 0 && <div className="loading-state">Cargando mensajes...</div>}
        {conversacion?.mensajes.map((m, i, mensajes) => {
          const estado = m.estadoEnvio ? ENVIO_LABEL[m.estadoEnvio] : m.procesamiento ? PROCESAMIENTO_LABEL[m.procesamiento] : undefined;
          const dia = diaDeMensaje(m);
          const empiezaDia = dia !== null && dia !== diaDeMensaje(mensajes[i - 1]);
          return (
            <Fragment key={m.id ?? i}>
              {empiezaDia && (
                <div className="chat-dia">
                  <span>{dia}</span>
                </div>
              )}
              <div className={`msg ${m.autor === "transportista" ? "in" : "out"}`}>
                <span className="who">
                  {m.autor === "transportista" ? "Transportista" : "Bot AgroFlow"}
                  {m.hora ? ` · ${m.hora}` : ""}
                </span>
                {m.texto}
                {estado && <span className={`msg-status ${m.estadoEnvio ?? m.procesamiento}`}>{estado}</span>}
              </div>
            </Fragment>
          );
        })}
      </div>
    </div>
  );
}
