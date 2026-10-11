import { useState } from "react";
import { container } from "../../composition/container";
import { useColaTurnos } from "../hooks/useColaTurnos";
import { MetricCard } from "../components/common/MetricCard";
import { CanalBadge, EstadoBadge, FlotaBadge, PrioridadBadge } from "../components/common/Badges";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { CambioEstadoSelect } from "../components/common/CambioEstadoSelect";
import { ESTADOS_SELECCIONABLES } from "../components/common/estadosSeleccionables";
import type { Turno } from "../../domain/entities/Turno";
import { datetimeLocalARfc3339, fechaHoraDeRfc3339 } from "../../shared/utils/date";

const ESTADO_OPCIONES: { value: string; label: string }[] = [
  { value: "todos", label: "Todos los estados" },
  ...ESTADOS_SELECCIONABLES,
];

const esApi = container.fuenteDatos === "http";

const FORM_VACIO = { telefono: "", patente: "", codigoFinca: "", corteEn: "", cargaTon: "" };


const horaActualizacion = (d: Date | null) => (d ? d.toLocaleTimeString("es-AR") : "—");

export function ColaTurnosView() {
  const {
    turnos,
    resumen,
    estadoCarga,
    error,
    ultimaActualizacion,
    desactualizado,
    filtrosIncompletos,
    recargar,
    errorAccion,
    limpiarErrorAccion,
    enCurso,
    filtros,
    setBusqueda,
    setFiltroFlota,
    setFiltroEstado,
    setOrden,
    setFecha,
    setPatente,
    setTelefono,
    detalleId,
    detalle,
    abrirDetalle,
    cerrarDetalle,
    crearTurno,
    cambiarEstado,
    cancelar,
    reasignar,
  } = useColaTurnos();

  const [mostrarForm, setMostrarForm] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);
  const [form, setForm] = useState(FORM_VACIO);

  const actualizarCampo = (campo: keyof typeof form, valor: string) => {
    setForm((prev) => ({ ...prev, [campo]: valor }));
  };

  const guardarTurno = async () => {
    setFormError(null);
    setGuardando(true);
    try {
      await crearTurno({
        telefono: form.telefono,
        patente: form.patente,
        codigoFinca: form.codigoFinca,
        // datetime-local no trae zona: se envía con el desplazamiento explícito.
        corteEn: datetimeLocalARfc3339(form.corteEn),
        cargaTon: Number(form.cargaTon) || 0,
      });
      setForm(FORM_VACIO);
      setMostrarForm(false);
    } catch (e) {
      setFormError(e instanceof Error ? e.message : "No se pudo crear el turno.");
    } finally {
      setGuardando(false);
    }
  };

  const handleReasignar = (id: string, horaActual: string) => {
    const nuevaHora = window.prompt("Nueva hora de turno (formato HH:MM):", horaActual);
    if (nuevaHora) reasignar(id, nuevaHora);
  };

  const handleCancelar = (t: Turno) => {
    if (window.confirm(`¿Cancelar el turno de ${t.patente}?`)) cancelar(t.id);
  };

  const acciones = (t: Turno) => (
    <div className="row-actions">
      <CambioEstadoSelect
        estadoActual={t.estado}
        deshabilitado={enCurso === t.id}
        onCambiar={(nuevo) => cambiarEstado(t.id, nuevo)}
      />
      <button className="btn-mini" disabled={enCurso === t.id} onClick={() => handleCancelar(t)}>
        Cancelar
      </button>
      {!esApi && (
        <button className="btn-mini" disabled={enCurso === t.id} onClick={() => handleReasignar(t.id, t.hora)}>
          Reasignar
        </button>
      )}
    </div>
  );

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Cola de turnos</h1>
          <div className="zafra">
            Turnos programados · {esApi ? `AgroFlow API (${container.apiUrl})` : "datos mock"}
          </div>
        </div>
        <button className="btn-primary" onClick={() => setMostrarForm((v) => !v)}>
          + Nuevo turno
        </button>
      </div>

      <div className="metrics" style={{ marginBottom: 20 }}>
        <MetricCard label="Turnos" value={resumen.total} />
        <MetricCard label="Asignados" value={resumen.pendientes} />
        <MetricCard label="Finalizados" value={resumen.completados} />
        <MetricCard label="Cancelados" value={resumen.cancelados} />
      </div>

      {mostrarForm && (
        <div className="alert-card" style={{ marginBottom: 22, maxWidth: 640 }}>
          <h3>Cargar turno manual</h3>
          <p>La API asigna la primera ventana futura con cupo. Usá datos existentes en el seed.</p>
          {formError && <ErrorState message={formError} />}
          <div className="form-grid">
            <div className="form-field">
              <label>Teléfono del transportista</label>
              <input value={form.telefono} onChange={(e) => actualizarCampo("telefono", e.target.value)} placeholder="+5493815550101" />
            </div>
            <div className="form-field">
              <label>Patente</label>
              <input value={form.patente} onChange={(e) => actualizarCampo("patente", e.target.value)} placeholder="AF123BC" />
            </div>
            <div className="form-field">
              <label>Código de finca</label>
              <input value={form.codigoFinca} onChange={(e) => actualizarCampo("codigoFinca", e.target.value)} placeholder="FINCA-NORTE" />
            </div>
            <div className="form-field">
              <label>Momento de corte</label>
              <input type="datetime-local" value={form.corteEn} onChange={(e) => actualizarCampo("corteEn", e.target.value)} />
            </div>
            <div className="form-field">
              <label>Carga estimada (tn)</label>
              <input type="number" min={0} step="0.1" value={form.cargaTon} onChange={(e) => actualizarCampo("cargaTon", e.target.value)} placeholder="28.5" />
            </div>
          </div>
          <div style={{ display: "flex", gap: 10, marginTop: 6 }}>
            <button className="btn-primary" disabled={guardando} onClick={guardarTurno}>
              {guardando ? "Guardando..." : "Guardar turno"}
            </button>
            <button className="btn-mini" onClick={() => setMostrarForm(false)}>Cancelar</button>
          </div>
        </div>
      )}

      {errorAccion && (
        <div onClick={limpiarErrorAccion} style={{ cursor: "pointer", marginBottom: 12 }} title="Cerrar">
          <ErrorState message={errorAccion} />
        </div>
      )}

      {detalleId && (
        <div className="alert-card detalle-turno" style={{ marginBottom: 22, maxWidth: 640 }}>
          <h3>Detalle del turno</h3>
          {!detalle ? (
            <LoadingState label="Cargando detalle..." />
          ) : (
            <>
              <dl>
                <dt>Estado</dt>
                <dd><EstadoBadge estado={detalle.estado} /></dd>
                <dt>Ventana</dt>
                <dd>{detalle.hora}{detalle.ventanaFin ? ` – ${detalle.ventanaFin}` : ""}</dd>
                <dt>Patente</dt>
                <dd>{detalle.patente}</dd>
                <dt>Transportista</dt>
                <dd>{detalle.chofer}</dd>
                <dt>Finca</dt>
                <dd>{detalle.finca}</dd>
                <dt>Corte</dt>
                <dd>{fechaHoraDeRfc3339(detalle.corteEn)} ({detalle.horasDesdeCorte} h)</dd>
                <dt>Carga estimada</dt>
                <dd>{detalle.cargaTon !== undefined ? `${detalle.cargaTon} tn` : "—"}</dd>
                <dt>Creado</dt>
                <dd>{fechaHoraDeRfc3339(detalle.creadoEn)}</dd>
              </dl>
              <div style={{ display: "flex", gap: 10 }}>
                {acciones(detalle)}
                <button className="btn-mini" onClick={cerrarDetalle}>Cerrar</button>
              </div>
            </>
          )}
        </div>
      )}

      <div className="filters-bar">
        <input
          type="text"
          placeholder="Buscar por patente, chofer o finca..."
          value={filtros.busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
        />
        <select value={filtros.filtroEstado} onChange={(e) => setFiltroEstado(e.target.value as typeof filtros.filtroEstado)}>
          {ESTADO_OPCIONES.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        {esApi ? (
          <>
            <input type="date" value={filtros.fecha} onChange={(e) => setFecha(e.target.value)} title="Fecha de la ventana (vacío = hoy)" />
            <input type="text" placeholder="Patente exacta" value={filtros.patente} onChange={(e) => setPatente(e.target.value)} />
            <input type="text" placeholder="Teléfono +549..." value={filtros.telefono} onChange={(e) => setTelefono(e.target.value)} />
          </>
        ) : (
          <>
            <select value={filtros.filtroFlota} onChange={(e) => setFiltroFlota(e.target.value as typeof filtros.filtroFlota)}>
              <option value="todas">Toda la flota</option>
              <option value="propia">Propia</option>
              <option value="tercero">Tercerizada</option>
            </select>
            <select value={filtros.orden} onChange={(e) => setOrden(e.target.value as typeof filtros.orden)}>
              <option value="turno">Ordenar por turno</option>
              <option value="prioridad">Ordenar por prioridad de corte</option>
              <option value="espera">Ordenar por espera</option>
            </select>
          </>
        )}
      </div>

      {filtrosIncompletos.length > 0 ? (
        // No se consulta ni se muestra una lista que no respeta el filtro.
        <div className="aviso-filtro" role="status">
          <strong>Filtro incompleto.</strong> Completalo para ver los turnos que coinciden:
          <ul>
            {filtrosIncompletos.map((p) => (
              <li key={p}>{p}</li>
            ))}
          </ul>
        </div>
      ) : estadoCarga === "cargando" ? (
        <LoadingState label="Cargando turnos..." />
      ) : estadoCarga === "error" && !desactualizado ? (
        // Nunca hubo una respuesta válida: no hay lista que mostrar.
        <div role="alert">
          <ErrorState message={error ?? "No se pudo cargar la cola de turnos."} />
          <button className="btn-mini" onClick={() => recargar()}>Reintentar</button>
        </div>
      ) : (
        <>
          {desactualizado && (
            <div className="aviso-desactualizado" role="alert">
              <strong>Datos desactualizados.</strong> Última actualización: {horaActualizacion(ultimaActualizacion)}.
              {" "}{error} Se reintenta automáticamente.
            </div>
          )}
          <table className={desactualizado ? "datos-desactualizados" : undefined}>
            <thead>
              <tr>
                <th>Ventana</th>
                <th>Patente</th>
                <th>Transportista</th>
                <th>Finca / Origen</th>
                {esApi ? <th>Carga</th> : <th>Flota</th>}
                <th>Corte</th>
                {!esApi && <th>Prioridad</th>}
                <th>Estado</th>
                {!esApi && <th>Canal</th>}
                <th></th>
              </tr>
            </thead>
            <tbody>
              {turnos.map((t) => (
                <tr
                  key={t.id}
                  className={`fila-seleccionable ${t.id === detalleId ? "activa" : ""}`}
                  onClick={() => abrirDetalle(t.id)}
                >
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace", fontSize: 12.5 }}>{t.hora}</td>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{t.patente}</td>
                  <td>{t.chofer}</td>
                  <td>{t.finca}</td>
                  {esApi ? (
                    <td style={{ fontSize: 12.5 }}>{t.cargaTon !== undefined ? `${t.cargaTon} tn` : "—"}</td>
                  ) : (
                    <td><FlotaBadge flota={t.flota} /></td>
                  )}
                  <td style={{ fontSize: 12.5 }}>{t.horasDesdeCorte} h</td>
                  {!esApi && <td><PrioridadBadge prioridad={t.prioridad} /></td>}
                  <td><EstadoBadge estado={t.estado} /></td>
                  {!esApi && <td><CanalBadge canal={t.canal} /></td>}
                  <td onClick={(e) => e.stopPropagation()}>{acciones(t)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {turnos.length === 0 && (
            <EmptyState
              message={
                desactualizado
                  ? `No había turnos en la última actualización (${horaActualizacion(ultimaActualizacion)}).`
                  : "No hay turnos que coincidan con estos filtros."
              }
            />
          )}
        </>
      )}
    </div>
  );
}
