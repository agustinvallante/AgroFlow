/** Elemento de GET /api/v1/farms (docs/contracts/openapi.yaml, esquema Farm). */
export interface FarmDto {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}
