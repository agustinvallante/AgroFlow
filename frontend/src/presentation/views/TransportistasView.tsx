import { useState } from "react";
import { useTransportistas } from "../hooks/useTransportistas";
import { MetricCard } from "../components/common/MetricCard";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";

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
    filtros,
    setBusqueda,
    setFiltroEstado,
    crear,
    cambiarEstado,
  } = useTransportistas();

  const [mostrarForm, setMostrarForm] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [accionError, setAccionError] = useState<string | null>(null);
  const [form, setForm] = useState(FORM_VACIO);

  const actualizarCampo = (campo: keyof typeof form, valor: string) => {
    setForm((prev) => ({ ...prev, [campo]: valor }));
  };

  const guardar = async () => {
    setFormError(null);
    try {
      await crear(form);
      setForm(FORM_VACIO);
      setMostrarForm(false);
    } catch (e) {
      setFormError(e instanceof Error ? e.message : "No se pudo crear el transportista.");
    }
  };

  const darDeBaja = async (id: string) => {
    setAccionError(null);
    try {
      await cambiarEstado(id);
    } catch (e) {
      setAccionError(e instanceof Error ? e.message : "No se pudo cambiar el estado del transportista.");
    }
  };

  if (error) return <ErrorState message={error} />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Transportistas</h1>
          <div className="zafra">Transportistas registrados · AgroFlow API</div>
        </div>
        <button className="btn-primary" onClick={() => setMostrarForm((v) => !v)}>
          + Nuevo transportista
        </button>
      </div>

      <div className="metrics" style={{ marginBottom: 20 }}>
        <MetricCard label="Total transportistas" value={resumen.total} />
        <MetricCard label="Activos" value={resumen.activos} />
        <MetricCard label="Inactivos" value={resumen.inactivos} />
      </div>

      {mostrarForm && (
        <div className="alert-card" style={{ marginBottom: 22, maxWidth: 640 }}>
          <h3>Nuevo transportista</h3>
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
            <button className="btn-mini" onClick={() => setMostrarForm(false)}>Cancelar</button>
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
                    {t.patente ?? "—"}
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
                    {/* La API publica la inhabilitación, no la reactivación. */}
                    {t.activo && (
                      <button className="btn-mini" onClick={() => darDeBaja(t.id)}>Dar de baja</button>
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
