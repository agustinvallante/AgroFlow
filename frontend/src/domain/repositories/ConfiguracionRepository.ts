import type { Configuracion } from "../entities/Configuracion";

export interface ConfiguracionRepository {
  obtenerConfiguracion(): Promise<Configuracion>;
  guardarDatosIngenio(datos: Configuracion["ingenio"]): Promise<void>;
  guardarReglasNegocio(reglas: Configuracion["reglas"]): Promise<void>;
  guardarNotificaciones(notificaciones: Configuracion["notificaciones"]): Promise<void>;
  probarConexionN8n(): Promise<boolean>;
}
