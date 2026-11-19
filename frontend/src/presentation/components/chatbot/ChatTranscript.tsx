import { Fragment, useLayoutEffect, useRef, useState, type ReactNode, type UIEvent } from "react";
import type { Conversacion, EstadoEnvio, EstadoProcesamiento, MensajeChat } from "../../../domain/entities/Conversacion";
import { etiquetaDiaEnZonaIngenio, horaEnZonaIngenio } from "../../../shared/utils/date";
import { AvatarConversacion } from "./AvatarConversacion";

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

const ENVIO_ICONO: Record<EstadoEnvio, ReactNode> = {
  pendiente: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </>
  ),
  aceptado: <path d="m5 12 5 5L20 7" />,
  fallido: <path d="M12 3 2 20h20L12 3zM12 10v4M12 17h.01" />,
  incierto: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .9-1 1.6v.3M12 17h.01" />
    </>
  ),
};

/** Distancia al final, en px, por debajo de la cual el operador sigue "abajo". */
const UMBRAL_ABAJO_PX = 48;

interface ChatTranscriptProps {
  conversacion: Conversacion | null;
  /** Cambia con cada clic en la lista: vuelve a llevar la vista al último mensaje. */
  seleccion?: number;
  cargando?: boolean;
  error?: string | null;
  /** Mensajes llegados después de mostrar la conversación: entran con animación. */
  nuevos?: ReadonlySet<string>;
  /** Cuántos mensajes llegaron desde que se mostró la conversación. */
  llegadas?: number;
  /** `false` cuando el último refresco falló. */
  enVivo?: boolean;
}

/** "Ayer · 18:44"; sin el instante, el texto que ya trae la conversación. */
function momentoUltimoMensaje({ ultimoMensajeUtc, hora }: Conversacion): string {
  if (!ultimoMensajeUtc) return hora;
  return `${etiquetaDiaEnZonaIngenio(ultimoMensajeUtc)} · ${horaEnZonaIngenio(ultimoMensajeUtc)}`;
}

/** Día del mensaje; `null` si no trae fecha, y entonces no abre una división. */
const diaDeMensaje = (mensaje: MensajeChat | undefined) =>
  mensaje?.fechaUtc ? etiquetaDiaEnZonaIngenio(mensaje.fechaUtc) : null;

/** Sólo lo que informa la API: el último mensaje es entrante y su procesamiento sigue en curso. */
function botRespondiendo(mensajes: MensajeChat[]): boolean {
  const ultimo = mensajes.at(-1);
  return ultimo?.autor === "transportista" && ultimo.procesamiento === "en-curso";
}

const SIN_IDS: ReadonlySet<string> = new Set();

function Puntos() {
  return (
    <span className="chat-puntos" aria-hidden="true">
      <span />
      <span />
      <span />
    </span>
  );
}

function IconoEnvio({ estado }: { estado: EstadoEnvio }) {
  const label = ENVIO_LABEL[estado];
  return (
    <svg className={`chat-envio ${estado}`} width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" role="img" aria-label={label}>
      <title>{label}</title>
      {ENVIO_ICONO[estado]}
    </svg>
  );
}

export function ChatTranscript({
  conversacion,
  seleccion = 0,
  cargando = false,
  error = null,
  nuevos = SIN_IDS,
  llegadas = 0,
  enVivo = true,
}: ChatTranscriptProps) {
  const cuerpoRef = useRef<HTMLDivElement>(null);
  const seleccionAtendida = useRef<string | null>(null);
  const claveSeleccion = `${conversacion?.id ?? ""}:${seleccion}`;
  const cantidadMensajes = conversacion?.mensajes.length ?? 0;
  const respondiendo = botRespondiendo(conversacion?.mensajes ?? []);

  // Posición del operador en esta selección; cada selección vuelve a empezar abajo.
  const [desplazamiento, setDesplazamiento] = useState({ clave: "", abajo: true, vistas: 0 });
  const vigente = desplazamiento.clave === claveSeleccion;
  const abajo = vigente ? desplazamiento.abajo : true;
  const vistas = vigente ? desplazamiento.vistas : llegadas;
  const pendientes = abajo ? 0 : Math.max(0, llegadas - vistas);

  // Los mensajes llegan después del clic: se baja una sola vez por selección, cuando ya están.
  useLayoutEffect(() => {
    const cuerpo = cuerpoRef.current;
    if (!cuerpo || cantidadMensajes === 0 || seleccionAtendida.current === claveSeleccion) return;
    cuerpo.scrollTop = cuerpo.scrollHeight;
    seleccionAtendida.current = claveSeleccion;
  }, [claveSeleccion, cantidadMensajes]);

  // Lo que llega mientras el operador está abajo lo mantiene abajo; si subió, no se lo mueve.
  useLayoutEffect(() => {
    const cuerpo = cuerpoRef.current;
    if (cuerpo && abajo) cuerpo.scrollTop = cuerpo.scrollHeight;
  }, [llegadas, respondiendo, abajo]);

  const alDesplazar = (e: UIEvent<HTMLDivElement>) => {
    const { scrollHeight, scrollTop, clientHeight } = e.currentTarget;
    const ahoraAbajo = scrollHeight - scrollTop - clientHeight < UMBRAL_ABAJO_PX;
    // Mientras estuvo abajo vio todo lo llegado hasta ahora.
    const vistasAhora = abajo || ahoraAbajo ? llegadas : vistas;
    if (vigente && ahoraAbajo === abajo && vistasAhora === vistas) return;
    setDesplazamiento({ clave: claveSeleccion, abajo: ahoraAbajo, vistas: vistasAhora });
  };

  const irAbajo = () => {
    const cuerpo = cuerpoRef.current;
    if (cuerpo) cuerpo.scrollTop = cuerpo.scrollHeight;
    setDesplazamiento({ clave: claveSeleccion, abajo: true, vistas: llegadas });
  };

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
    <section className="chat-transcript" aria-label="Conversación abierta">
      <header className="chat-head">
        {conversacion && <AvatarConversacion conversacion={conversacion} grande />}
        <div className="chat-head-texto">
          <strong>{conversacion?.nombre ?? "—"}</strong>
          <span className="chat-head-detalle">{detalle}</span>
        </div>
        <div className="chat-head-estado">
          <span role="status" className="chat-escribiendo">
            {respondiendo && (
              <>
                El bot está respondiendo
                <Puntos />
              </>
            )}
          </span>
          <span className={`chat-en-vivo ${enVivo ? "" : "pausado"}`}>
            <i aria-hidden="true" />
            {enVivo ? "En vivo" : "Sin actualizar"}
          </span>
        </div>
      </header>
      <div className="chat-transcript-body" ref={cuerpoRef} onScroll={alDesplazar}>
        {error && <div className="error-state">{error}</div>}
        {cargando && cantidadMensajes === 0 && <div className="loading-state">Cargando mensajes...</div>}
        {conversacion?.mensajes.map((m, i, mensajes) => {
          const entra = m.autor === "transportista";
          const procesamiento = !m.estadoEnvio && m.procesamiento ? PROCESAMIENTO_LABEL[m.procesamiento] : undefined;
          const dia = diaDeMensaje(m);
          const empiezaDia = dia !== null && dia !== diaDeMensaje(mensajes[i - 1]);
          const nuevo = m.id !== undefined && nuevos.has(m.id);
          return (
            <Fragment key={m.id ?? i}>
              {empiezaDia && (
                <div className="chat-dia">
                  <span>{dia}</span>
                </div>
              )}
              <div className={`chat-msg ${entra ? "in" : "out"}${nuevo ? " chat-msg-nuevo" : ""}`}>
                <span className="chat-msg-autor">{entra ? "Transportista" : "Bot ZafraFlow"}</span>
                <span className="chat-msg-texto">{m.texto}</span>
                {(m.hora || m.estadoEnvio) && (
                  <span className="chat-msg-pie">
                    {m.hora}
                    {m.estadoEnvio && <IconoEnvio estado={m.estadoEnvio} />}
                  </span>
                )}
                {procesamiento && <span className={`msg-status ${m.procesamiento}`}>{procesamiento}</span>}
              </div>
            </Fragment>
          );
        })}
        {respondiendo && (
          <div className="chat-msg out chat-msg-nuevo chat-msg-escribiendo" aria-hidden="true">
            <Puntos />
          </div>
        )}
      </div>
      {pendientes > 0 && (
        <button type="button" className="chat-nuevos" onClick={irAbajo}>
          {pendientes === 1 ? "1 mensaje nuevo" : `${pendientes} mensajes nuevos`}
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4"
            strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M12 5v14M5 12l7 7 7-7" />
          </svg>
        </button>
      )}
      <footer className="chat-pie">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
          strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <rect x="4" y="8" width="16" height="12" rx="3" />
          <path d="M12 8V4M8 14h.01M16 14h.01" />
        </svg>
        Responde el bot automáticamente. La respuesta manual del operador todavía no está disponible.
      </footer>
    </section>
  );
}
