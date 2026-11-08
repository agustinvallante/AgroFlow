/** "revision": la API informa un procesamiento fallido o vencido, o un envío incierto. */
export type EstadoConversacion = "resuelto" | "bot" | "escalado" | "revision";

/**
 * Resultado de envío de una respuesta según la API. "aceptado" significa que
 * el proveedor aceptó el mensaje, no que haya sido entregado o leído.
 */
export type EstadoEnvio = "pendiente" | "aceptado" | "fallido" | "incierto";

/** Procesamiento de un mensaje entrante; "vencido" no se reprocesa solo. */
export type EstadoProcesamiento = "en-curso" | "completado" | "fallido" | "vencido";

export interface MensajeChat {
  id?: string;
  autor: "transportista" | "bot";
  texto: string;
  hora?: string;
  estadoEnvio?: EstadoEnvio;
  procesamiento?: EstadoProcesamiento;
}

export interface Conversacion {
  id: string;
  nombre: string;
  hora: string;
  preview: string;
  estado: EstadoConversacion;
  /** En la fuente http se cargan aparte, al abrir la conversación. */
  mensajes: MensajeChat[];
  /** `false` para un número no registrado o una dirección sin teléfono. */
  remitenteReconocido?: boolean;
  /** Dirección remota enmascarada por la API; nunca la completa. */
  direccion?: string;
}

export interface PasoFlujoBot {
  orden: number;
  titulo: string;
  descripcion: string;
}

export interface PlantillaMensaje {
  id: string;
  titulo: string;
  contenido: string;
}

export interface MetricasChatbot {
  mensajesHoy: number;
  turnosAsignadosPorBot: number;
  tiempoPromedioRespuestaSeg: number;
  conversacionesEscaladas: number;
}
