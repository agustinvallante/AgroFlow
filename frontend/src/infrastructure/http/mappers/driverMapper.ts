import type { NuevoTransportista, Transportista } from "../../../domain/entities/Transportista";
import type { DriverDto, DriverInputDto } from "../dto/DriverDto";

export function mapDriver(dto: DriverDto): Transportista {
  return {
    id: dto.id,
    nombre: dto.name,
    dni: dto.dni,
    telefono: dto.whatsApp,
    // Camiones asociados (sólo lectura); la API no publica cómo asociarlos.
    patente: dto.activeTruckPlates.length > 0 ? dto.activeTruckPlates.join(", ") : null,
    finca: null,
    flota: null,
    turnosEsteMes: null,
    activo: dto.isActive,
    puedeSolicitarTurnos: dto.canRequestAppointments,
  };
}

export function toDriverInput(datos: NuevoTransportista): DriverInputDto {
  return {
    name: datos.nombre.trim(),
    dni: (datos.dni ?? "").trim(),
    whatsApp: datos.telefono.trim(),
  };
}
