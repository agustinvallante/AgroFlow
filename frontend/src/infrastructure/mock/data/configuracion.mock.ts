import type { Configuracion } from "../../../domain/entities/Configuracion";

export function generarConfiguracionSemilla(): Configuracion {
  return {
    ingenio: {
      nombre: "Ingenio San Ramón",
      capacidadMoliendaTnH: 340,
      duracionZafraDias: 180,
      horarioOperativo: "06:00 – 22:00",
    },
    reglas: {
      ventanaTurnoMin: 30,
      prioridadAltaDesdeHoras: 18,
      priorizarFlotaPropia: true,
      reasignacionAutomatica: true,
    },
    integracion: {
      webhookUrl: "https://n8n.agroflow.app/webhook/san-ramon",
      conectado: true,
    },
    notificaciones: {
      avisoAutomaticoDemora: true,
      resumenDiarioPorCorreo: true,
      alertasEsperaProlongada: true,
    },
    usuarios: [
      { id: "u1", nombre: "María Costas", rol: "Jefa de báscula", turno: "Mañana", activo: true },
      { id: "u2", nombre: "Jorge Villalba", rol: "Operario de báscula", turno: "Tarde", activo: true },
      { id: "u3", nombre: "Sofía Ramallo", rol: "Gerencia", turno: "—", activo: true },
    ],
  };
}
