import { EmptyState } from "../components/common/StatusStates";
import { AVISO_SIN_API } from "../components/common/sinDatos";

/** Interruptor sin valor: ni encendido ni apagado, porque la API no lo informa. */
function OpcionSinDato({ titulo, detalle }: { titulo: string; detalle: string }) {
  return (
    <div className="toggle-row">
      <div className="txt">
        <strong>{titulo}</strong>
        <span>{detalle}</span>
      </div>
      <div className="toggle sin-dato" aria-disabled="true" />
    </div>
  );
}

function CampoSinDato({ etiqueta }: { etiqueta: string }) {
  return (
    <div className="form-field">
      <label>{etiqueta}</label>
      <input disabled />
    </div>
  );
}

export function ConfiguracionView() {
  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Configuración</h1>
          <div className="zafra">{AVISO_SIN_API}</div>
        </div>
      </div>

      <div className="config-grid">
        <div className="config-card">
          <h3>Datos del ingenio</h3>
          <div className="form-grid">
            <CampoSinDato etiqueta="Nombre del ingenio" />
            <CampoSinDato etiqueta="Capacidad de molienda (tn/h)" />
            <CampoSinDato etiqueta="Duración de la zafra (días)" />
            <CampoSinDato etiqueta="Horario operativo" />
          </div>
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} disabled>
            Guardar cambios
          </button>
        </div>

        <div className="config-card">
          <h3>Reglas de negocio</h3>
          <div className="sub">Definen cómo el bot arma la cola de turnos</div>
          <div className="form-grid">
            <CampoSinDato etiqueta="Duración de la ventana de turno" />
            <CampoSinDato etiqueta="Prioridad alta a partir de (hs. de corte)" />
          </div>
          <div className="toggle-list">
            <OpcionSinDato titulo="Priorizar flota propia" detalle="Ante turnos empatados, se asigna primero a la flota propia." />
            <OpcionSinDato titulo="Reasignación automática" detalle="Si un camión no llega a tiempo, el turno se libera solo." />
          </div>
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} disabled>
            Guardar reglas
          </button>
        </div>

        <div className="config-card">
          <h3>Integración WhatsApp / n8n</h3>
          <div className="form-field">
            <label>URL del webhook (n8n)</label>
            <div className="webhook-box">
              <input type="text" readOnly disabled />
              <button className="btn-mini" disabled>Copiar</button>
            </div>
          </div>
          <button className="btn-mini" style={{ alignSelf: "flex-start" }} disabled>
            Probar conexión
          </button>
        </div>

        <div className="config-card">
          <h3>Notificaciones</h3>
          <div className="toggle-list">
            <OpcionSinDato titulo="Aviso automático de demora" detalle="Notifica a la flota si la molienda se detiene." />
            <OpcionSinDato titulo="Resumen diario por correo" detalle="Envía un resumen de la jornada a la gerencia." />
            <OpcionSinDato titulo="Alertas de espera prolongada" detalle="Avisa cuando un camión supera el tiempo de espera." />
          </div>
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} disabled>
            Guardar notificaciones
          </button>
        </div>

        <div className="config-card full">
          <h3>Usuarios y permisos</h3>
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Rol</th>
                <th>Turno</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
          </table>
          <EmptyState message="Sin usuarios." />
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} disabled>
            + Invitar usuario
          </button>
        </div>
      </div>
    </div>
  );
}
