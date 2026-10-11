export interface DatosIngenio {
  nombre: string;
  capacidadMoliendaTnH: number;
  duracionZafraDias: number;
  horarioOperativo: string;
}

export interface ReglasNegocio {
  ventanaTurnoMin: 15 | 30 | 45 | 60;
  prioridadAltaDesdeHoras: number;
  priorizarFlotaPropia: boolean;
  reasignacionAutomatica: boolean;
}

export interface IntegracionN8n {
  webhookUrl: string;
  conectado: boolean;
}

export interface Notificaciones {
  avisoAutomaticoDemora: boolean;
  resumenDiarioPorCorreo: boolean;
  alertasEsperaProlongada: boolean;
}

export interface UsuarioAdmin {
  id: string;
  nombre: string;
  rol: string;
  turno: string;
  activo: boolean;
}

export interface Configuracion {
  ingenio: DatosIngenio;
  reglas: ReglasNegocio;
  integracion: IntegracionN8n;
  notificaciones: Notificaciones;
  usuarios: UsuarioAdmin[];
}
