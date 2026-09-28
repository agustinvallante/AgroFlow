import type { PlantillaMensaje } from "../../../domain/entities/Conversacion";
import type { ConversacionRepository } from "../../../domain/repositories/ConversacionRepository";
import { delay } from "../../../shared/utils/delay";
import {
  generarConversacionesSemilla,
  generarFlujoBotSemilla,
  generarPlantillasSemilla,
} from "../data/conversaciones.mock";

export class MockConversacionRepository implements ConversacionRepository {
  private conversaciones = generarConversacionesSemilla();
  private flujo = generarFlujoBotSemilla();
  private plantillas = generarPlantillasSemilla();

  async obtenerConversaciones() {
    return delay([...this.conversaciones]);
  }

  async obtenerMetricasChatbot() {
    return delay({
      mensajesHoy: 186,
      turnosAsignadosPorBot: 42,
      tiempoPromedioRespuestaSeg: 3,
      conversacionesEscaladas: this.conversaciones.filter((c) => c.estado === "escalado").length,
    });
  }

  async obtenerFlujoDeAutomatizacion() {
    return delay([...this.flujo]);
  }

  async obtenerPlantillas() {
    return delay([...this.plantillas]);
  }

  async guardarPlantillas(plantillas: PlantillaMensaje[]) {
    this.plantillas = plantillas;
    return delay(undefined);
  }
}
