import type { Configuracion } from "../../../domain/entities/Configuracion";
import type { ConfiguracionRepository } from "../../../domain/repositories/ConfiguracionRepository";
import { delay } from "../../../shared/utils/delay";
import { generarConfiguracionSemilla } from "../data/configuracion.mock";

export class MockConfiguracionRepository implements ConfiguracionRepository {
  private config: Configuracion = generarConfiguracionSemilla();

  async obtenerConfiguracion(): Promise<Configuracion> {
    return delay(structuredClone(this.config));
  }

  async guardarDatosIngenio(datos: Configuracion["ingenio"]): Promise<void> {
    this.config.ingenio = datos;
    return delay(undefined);
  }

  async guardarReglasNegocio(reglas: Configuracion["reglas"]): Promise<void> {
    this.config.reglas = reglas;
    return delay(undefined);
  }

  async guardarNotificaciones(notificaciones: Configuracion["notificaciones"]): Promise<void> {
    this.config.notificaciones = notificaciones;
    return delay(undefined);
  }

  async probarConexionN8n(): Promise<boolean> {
    return delay(true, 600);
  }
}
