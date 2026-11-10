import { useChatbot } from "../hooks/useChatbot";
import { MetricCard } from "../components/common/MetricCard";
import { ConversationList } from "../components/chatbot/ConversationList";
import { ChatTranscript } from "../components/chatbot/ChatTranscript";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { SIN_DATO } from "../components/common/sinDatos";

export function ChatbotView() {
  const {
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
  } = useChatbot();

  if (error) return <ErrorState message={error} />;
  if (cargando) return <LoadingState label="Cargando el chatbot..." />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Chatbot de WhatsApp</h1>
          <div className="zafra">Conversaciones: AgroFlow API · Métricas y plantillas: sin datos</div>
        </div>
        <div className="conn-status">
          <i />
          Historial persistido en AgroFlow API
        </div>
      </div>

      <div className="metrics" style={{ marginBottom: 22 }}>
        <MetricCard label="Mensajes hoy" value={SIN_DATO} />
        <MetricCard label="Turnos asignados por el bot" value={SIN_DATO} />
        <MetricCard label="Tiempo promedio de respuesta" value={SIN_DATO} />
        <MetricCard label="Escaladas a un operador" value={SIN_DATO} />
      </div>

      <section>
        <div className="section-head">
          <h2>Conversaciones recientes</h2>
          <button className="btn-mini" onClick={recargar}>
            Actualizar
          </button>
        </div>
        {errorRefresco && <ErrorState message={errorRefresco} />}
        <div className="chat-layout">
          <ConversationList
            conversaciones={conversaciones}
            activaId={conversacionActiva?.id ?? null}
            onSeleccionar={seleccionarConversacion}
          />
          <ChatTranscript
            conversacion={conversacionActiva}
            seleccion={seleccion}
            cargando={cargandoMensajes}
            error={errorMensajes}
          />
        </div>
      </section>

      <section>
        <div className="section-head">
          <h2>Plantillas de mensajes</h2>
          <div className="meta">Usadas por el bot en cada paso del flujo</div>
        </div>
        <EmptyState message="Sin plantillas." />
        <button className="btn-primary" style={{ marginTop: 16 }} disabled>
          Guardar plantillas
        </button>
      </section>
    </div>
  );
}
