import { useState } from "react";
import type { ComponentType } from "react";
import { Sidebar } from "./presentation/layout/Sidebar";
import type { VistaId } from "./presentation/layout/vistas";
import { PanelGeneralView } from "./presentation/views/PanelGeneralView";
import { ColaTurnosView } from "./presentation/views/ColaTurnosView";
import { ChatbotView } from "./presentation/views/ChatbotView";
import { TransportistasView } from "./presentation/views/TransportistasView";
import { ReportesView } from "./presentation/views/ReportesView";
import { ConfiguracionView } from "./presentation/views/ConfiguracionView";

const VISTA_COMPONENTES: Record<VistaId, ComponentType> = {
  general: PanelGeneralView,
  cola: ColaTurnosView,
  chatbot: ChatbotView,
  transportistas: TransportistasView,
  reportes: ReportesView,
  config: ConfiguracionView,
};

export default function App() {
  const [vistaActiva, setVistaActiva] = useState<VistaId>("general");
  const VistaActual = VISTA_COMPONENTES[vistaActiva];

  return (
    <div className="app">
      <Sidebar vistaActiva={vistaActiva} onCambiarVista={setVistaActiva} />
      <main>
        <VistaActual />
      </main>
    </div>
  );
}
