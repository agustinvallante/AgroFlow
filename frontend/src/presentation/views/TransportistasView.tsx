import { useState } from "react";
import { useTransportistas } from "../hooks/useTransportistas";
import { useFlotaAsociada } from "../hooks/useFlotaAsociada";
import { CamionesAsociados } from "../components/transportistas/CamionesAsociados";
import { MetricCard } from "../components/common/MetricCard";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import type { Transportista } from "../../domain/entities/Transportista";

const FORM_VACIO = { nombre: "", dni: "", telefono: "" };

/**
 * El alta pide lo que publica el contrato de transportistas (nombre, DNI y
 * WhatsApp E.164). Finca, flota y turnos del mes no forman parte de él y no
 * se muestran ni se inventan.
 */
export function TransportistasView() {
  const {
    transportistas,
    resumen,
    cargando,
    error,
    errorRefresco,
    recargar,
    filtros,
    setBusqueda,
    setFiltroEstado,
    crear,
    editar,
    cambiarEstado,
  } = useTransportistas();
  const { flota, errorFlota, recargarFlota, asociar, desasociar } = useFlotaAsociada(recargar);

  const [mostrarForm, setMostrarForm] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [accionError, setAccionError] = useState<string | null>(null);
  const [form, setForm] = useState(FORM_VACIO);
  /** El mismo formulario sirve para el alta y para corregir un transportista activo. */
  const [editandoId, setEditandoId] = useState<string | null>(null);

  const actualizarCampo = (campo: keyof typeof form, valor: string) => {
    setForm((prev) => ({ ...prev, [campo]: valor }));
  };

  const guardar = async () => {
    setFormError(null);
    try {
      if (editandoId) await editar(editandoId, form);
      else await crear(form);
      cerrarForm();
    } catch (e) {
      setFormError(e instanceof Error ? e.message : "No se pudo guardar el transportista.");
    }
  };

  const abrirAlta = () => {
    setEditandoId(null);
    setForm(FORM_VACIO);
    setFormError(null);
    setMostrarForm(true);
  };

  const abrirEdicion = (t: Transportista) => {
    setEditandoId(t.id);
    setForm({ nombre: t.nombre, dni: t.dni, telefono: t.telefono });
    setFormError(null);
    setMostrarForm(true);
  };

  function cerrarForm() {
    setForm(FORM_VACIO);
    setEditandoId(null);
    setMostrarForm(false);
  }

  const darDeBaja = async (id: string) => {
    setAccionError(null);
    try {
      await cambiarEstado(id);
    } catch (e) {
      setAccionError(e instanceof Error ? e.message : "No se pudo cambiar el estado del transportista.");
    }
  };

  if (error) return (
    <div className="view" role="alert">
      <ErrorState message={error} />
      <button className="btn-mini" onClick={recargar}>Reintentar</button>
    </div>
  );

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Transportistas</h1>
          <div className="zafra">Transportistas registrados · AgroFlow API</div>
        </div>
        <div className="row-actions">
          <button className="btn-mini" onClick={() => { recargar(); recargarFlota(); }}>Actualizar</button>
          <button className="btn-primary" onClick={() => (mostrarForm && !editandoId ? cerrarForm() : abrirAlta())}>
            + Nuevo transportista
          </button>
        </div>
      </div>

      <div className="metrics" style={{ marginBottom: 20 }}>
        <MetricCard label="Total transportistas" value={resumen.total} />
        <MetricCard label="Activos" value={resumen.activos} />
        <MetricCard label="Inactivos" value={resumen.inactivos} />
      </div>

      {mostrarForm && (
        <div className="alert-card" style={{ marginBottom: 22, maxWidth: 640 }}>
          <h3>{editandoId ? "Editar transportista" : "Nuevo transportista"}</h3>
          {formError && <ErrorState message={formError} />}
          <div className="form-grid">
            <div className="form-field">
              <label>Nombre y apellido</label>
              <input value={form.nombre} onChange={(e) => actualizarCampo("nombre", e.target.value)} placeholder="Nombre completo" />
            </div>
            <div className="form-field">
              <label>DNI</label>
              <input value={form.dni} onChange={(e) => actualizarCampo("dni", e.target.value)} placeholder="20123456789" />
            </div>
            <div className="form-field">
              <label>Teléfono (WhatsApp) · formato internacional</label>
              <input value={form.telefono} onChange={(e) => actualizarCampo("telefono", e.target.value)} placeholder="+5493810000000" />
            </div>
          </div>
          <div style={{ display: "flex", gap: 10, marginTop: 6 }}>
            <button className="btn-primary" onClick={guardar}>Guardar transportista</button>
            <button className="btn-mini" onClick={cerrarForm}>Cancelar</button>
          </div>
        </div>
      )}

      <div className="filters-bar">
        <input
          type="text"
          placeholder="Buscar por nombre, DNI o teléfono..."
          value={filtros.busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
        />
        <select value={filtros.filtroEstado} onChange={(e) => setFiltroEstado(e.target.value as typeof filtros.filtroEstado)}>
          <option value="todos">Todos</option>
          <option value="activo">Activos</option>
          <option value="inactivo">Inactivos</option>
        </select>
      </div>

      {accionError && <ErrorState message={accionError} />}
      {errorFlota && (
        <div role="alert">
          <strong>No se pudieron cargar los camiones.</strong>
          <ErrorState message={errorFlota} />
          <p>Se muestran las patentes informadas por el listado. Usá Actualizar para reintentar la asociación.</p>
        </div>
      )}
      {errorRefresco && (
        <div role="alert">
          <strong>Datos desactualizados.</strong>
          <ErrorState message={errorRefresco} />
          <p>Se conserva el último listado obtenido. Usá Actualizar para reintentar.</p>
        </div>
      )}

      {cargando ? (
        <LoadingState label="Cargando transportistas..." />
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>DNI</th>
                <th>Teléfono</th>
                <th>Camión asociado</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {transportistas.map((t) => (
                <tr key={t.id}>
                  <td>{t.nombre}</td>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{t.dni}</td>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace", fontSize: 12.5 }}>{t.telefono}</td>
                  <td>
                    {errorFlota ? (t.patente ?? "—") : (
                      <CamionesAsociados
                        nombreTransportista={t.nombre}
                        activo={t.activo}
                        asociados={flota.porTransportista[t.id] ?? []}
                        libres={flota.libres}
                        onAsociar={(camionId) => asociar(t.id, camionId)}
                        onDesasociar={desasociar}
                      />
                    )}
                    {!t.puedeSolicitarTurnos && (
                      <span className="fuente-demo">
                        {t.activo ? "sin camión asociado · no puede solicitar turnos" : "inhabilitado"}
                      </span>
                    )}
                  </td>
                  <td>
                    <span className={`estado ${t.activo ? "descargando" : "cancelado"}`}>
                      <i />
                      {t.activo ? "Activo" : "Inactivo"}
                    </span>
                  </td>
                  <td>
                    {/* La API publica la edición y la inhabilitación de activos, no la reactivación. */}
                    {t.activo && (
                      <div className="row-actions">
                        <button className="btn-mini" aria-label={`Editar ${t.nombre}`} onClick={() => abrirEdicion(t)}>Editar</button>
                        <button className="btn-mini" onClick={() => darDeBaja(t.id)}>Dar de baja</button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {transportistas.length === 0 && <EmptyState message="No hay transportistas que coincidan con estos filtros." />}
        </>
      )}
    </div>
  );
}
