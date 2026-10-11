import { useState } from "react";
import { useConfiguracion } from "../hooks/useConfiguracion";
import { ErrorState, LoadingState } from "../components/common/StatusStates";
import type { Configuracion } from "../../domain/entities/Configuracion";

export function ConfiguracionView() {
  const {
    config,
    cargando,
    error,
    probandoConexion,
    guardarIngenio,
    guardarReglas,
    guardarNotificaciones,
    probarConexion,
  } = useConfiguracion();

  const [ingenioForm, setIngenioForm] = useState<Configuracion["ingenio"] | null>(null);
  const [reglasForm, setReglasForm] = useState<Configuracion["reglas"] | null>(null);
  const [notifForm, setNotifForm] = useState<Configuracion["notificaciones"] | null>(null);
  const [mensajeConexion, setMensajeConexion] = useState<string | null>(null);

  const ingenio = ingenioForm ?? config?.ingenio ?? null;
  const reglas = reglasForm ?? config?.reglas ?? null;
  const notificaciones = notifForm ?? config?.notificaciones ?? null;

  if (error) return <ErrorState message={error} />;
  if (cargando || !config || !ingenio || !reglas || !notificaciones) {
    return <LoadingState label="Cargando configuración..." />;
  }

  const handleProbarConexion = async () => {
    const ok = await probarConexion();
    setMensajeConexion(ok ? "Conexión verificada correctamente." : "No se pudo verificar la conexión.");
    setTimeout(() => setMensajeConexion(null), 3000);
  };

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Configuración</h1>
          <div className="zafra">Reglas de negocio e integración · Ingenio San Ramón</div>
        </div>
      </div>

      <div className="config-grid">
        <div className="config-card">
          <h3>Datos del ingenio</h3>
          <div className="form-grid">
            <div className="form-field">
              <label>Nombre del ingenio</label>
              <input
                value={ingenio.nombre}
                onChange={(e) => setIngenioForm({ ...ingenio, nombre: e.target.value })}
              />
            </div>
            <div className="form-field">
              <label>Capacidad de molienda (tn/h)</label>
              <input
                type="number"
                value={ingenio.capacidadMoliendaTnH}
                onChange={(e) => setIngenioForm({ ...ingenio, capacidadMoliendaTnH: Number(e.target.value) })}
              />
            </div>
            <div className="form-field">
              <label>Duración de la zafra (días)</label>
              <input
                type="number"
                value={ingenio.duracionZafraDias}
                onChange={(e) => setIngenioForm({ ...ingenio, duracionZafraDias: Number(e.target.value) })}
              />
            </div>
            <div className="form-field">
              <label>Horario operativo</label>
              <input
                value={ingenio.horarioOperativo}
                onChange={(e) => setIngenioForm({ ...ingenio, horarioOperativo: e.target.value })}
              />
            </div>
          </div>
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} onClick={() => guardarIngenio(ingenio)}>
            Guardar cambios
          </button>
        </div>

        <div className="config-card">
          <h3>Reglas de negocio</h3>
          <div className="sub">Definen cómo el bot arma la cola de turnos</div>
          <div className="form-grid">
            <div className="form-field">
              <label>Duración de la ventana de turno</label>
              <select
                value={reglas.ventanaTurnoMin}
                onChange={(e) => setReglasForm({ ...reglas, ventanaTurnoMin: Number(e.target.value) as 15 | 30 | 45 | 60 })}
              >
                <option value={15}>15 min</option>
                <option value={30}>30 min</option>
                <option value={45}>45 min</option>
                <option value={60}>60 min</option>
              </select>
            </div>
            <div className="form-field">
              <label>Prioridad alta a partir de (hs. de corte)</label>
              <input
                type="number"
                value={reglas.prioridadAltaDesdeHoras}
                onChange={(e) => setReglasForm({ ...reglas, prioridadAltaDesdeHoras: Number(e.target.value) })}
              />
            </div>
          </div>
          <div className="toggle-list">
            <div className="toggle-row">
              <div className="txt">
                <strong>Priorizar flota propia</strong>
                <span>Ante turnos empatados, se asigna primero a la flota propia.</span>
              </div>
              <div
                className={`toggle ${reglas.priorizarFlotaPropia ? "" : "off"}`}
                onClick={() => setReglasForm({ ...reglas, priorizarFlotaPropia: !reglas.priorizarFlotaPropia })}
              />
            </div>
            <div className="toggle-row">
              <div className="txt">
                <strong>Reasignación automática</strong>
                <span>Si un camión no llega en 15 min, el turno se libera solo.</span>
              </div>
              <div
                className={`toggle ${reglas.reasignacionAutomatica ? "" : "off"}`}
                onClick={() => setReglasForm({ ...reglas, reasignacionAutomatica: !reglas.reasignacionAutomatica })}
              />
            </div>
          </div>
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} onClick={() => guardarReglas(reglas)}>
            Guardar reglas
          </button>
        </div>

        <div className="config-card">
          <h3>Integración WhatsApp / n8n</h3>
          <div className="conn-status">
            <i />
            {config.integracion.conectado ? "Webhook activo" : "Webhook inactivo"}
          </div>
          <div className="form-field">
            <label>URL del webhook (n8n)</label>
            <div className="webhook-box">
              <input type="text" value={config.integracion.webhookUrl} readOnly />
              <button className="btn-mini" onClick={() => alert("URL copiada (simulado).")}>Copiar</button>
            </div>
          </div>
          <button
            className="btn-mini"
            style={{ alignSelf: "flex-start" }}
            onClick={handleProbarConexion}
            disabled={probandoConexion}
          >
            {probandoConexion ? "Probando..." : "Probar conexión"}
          </button>
          {mensajeConexion && <p style={{ color: "var(--ok)", fontSize: 12.5 }}>{mensajeConexion}</p>}
        </div>

        <div className="config-card">
          <h3>Notificaciones</h3>
          <div className="toggle-list">
            <div className="toggle-row">
              <div className="txt">
                <strong>Aviso automático de demora</strong>
                <span>Notifica a la flota si la molienda se detiene.</span>
              </div>
              <div
                className={`toggle ${notificaciones.avisoAutomaticoDemora ? "" : "off"}`}
                onClick={() =>
                  setNotifForm({ ...notificaciones, avisoAutomaticoDemora: !notificaciones.avisoAutomaticoDemora })
                }
              />
            </div>
            <div className="toggle-row">
              <div className="txt">
                <strong>Resumen diario por correo</strong>
                <span>Envía un resumen de la jornada a la gerencia.</span>
              </div>
              <div
                className={`toggle ${notificaciones.resumenDiarioPorCorreo ? "" : "off"}`}
                onClick={() =>
                  setNotifForm({ ...notificaciones, resumenDiarioPorCorreo: !notificaciones.resumenDiarioPorCorreo })
                }
              />
            </div>
            <div className="toggle-row">
              <div className="txt">
                <strong>Alertas de espera prolongada</strong>
                <span>Avisa cuando un camión supera los 45 min de espera.</span>
              </div>
              <div
                className={`toggle ${notificaciones.alertasEsperaProlongada ? "" : "off"}`}
                onClick={() =>
                  setNotifForm({ ...notificaciones, alertasEsperaProlongada: !notificaciones.alertasEsperaProlongada })
                }
              />
            </div>
          </div>
          <button
            className="btn-primary"
            style={{ alignSelf: "flex-start" }}
            onClick={() => guardarNotificaciones(notificaciones)}
          >
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
            <tbody>
              {config.usuarios.map((u) => (
                <tr key={u.id}>
                  <td>{u.nombre}</td>
                  <td>{u.rol}</td>
                  <td>{u.turno}</td>
                  <td>
                    <span className="estado descargando">
                      <i />
                      {u.activo ? "Activo" : "Inactivo"}
                    </span>
                  </td>
                  <td><button className="btn-mini">Editar</button></td>
                </tr>
              ))}
            </tbody>
          </table>
          <button className="btn-primary" style={{ alignSelf: "flex-start" }} onClick={() => alert("Invitación enviada (simulado).")}>
            + Invitar usuario
          </button>
        </div>
      </div>
    </div>
  );
}
