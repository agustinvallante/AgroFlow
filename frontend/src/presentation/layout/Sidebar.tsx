import { VISTAS, type VistaId } from "./vistas";
import logo from "../../assets/logo.png";
import { container } from "../../composition/container";

interface SidebarProps {
  vistaActiva: VistaId;
  onCambiarVista: (vista: VistaId) => void;
}

export function Sidebar({ vistaActiva, onCambiarVista }: SidebarProps) {
  return (
    <aside className="sidebar">
      <div className="brand">
        <div className="mark">
          <img src={logo} alt="AgroFlow" width={200} height={38} />
        </div>
        <div className="brand-sub">Control de Playa</div>
        <select className="ingenio-select" defaultValue="san-ramon">
          <option value="san-ramon">Ingenio San Ramón — Tucumán</option>
        </select>
      </div>

      <nav>
        {VISTAS.map((vista) => (
          <div
            key={vista.id}
            className={`nav-item ${vista.id === vistaActiva ? "active" : ""}`}
            onClick={() => onCambiarVista(vista.id)}
          >
            {vista.id === vistaActiva && <span className="dot" />}
            {vista.label}
          </div>
        ))}
      </nav>

      <div className="sidebar-foot">
        Sesión: María Costas
        <br />
        Jefa de Báscula · Turno mañana
        <br />
        {container.fuenteDatos === "http" ? (
          <>
            Turnos: AgroFlow API · {container.apiUrl}
            <br />
            Resto de las vistas: datos de demostración
          </>
        ) : (
          "v0.4 prototipo · datos mock"
        )}
      </div>
    </aside>
  );
}
