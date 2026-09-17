# Acceso y autorización

## Purpose

Define la autenticación de usuarios internos y la protección de operaciones de AgroFlow para CU-001 y RN-001 a RN-007.

## Requirements

### Requirement: Autenticación de usuarios activos (RN-001, RN-007)

AgroFlow SHALL autenticar únicamente a usuarios registrados y activos y SHALL responder con un mensaje genérico de credenciales inválidas tanto para un usuario inexistente como para una contraseña incorrecta.

#### Scenario: Usuario activo con credenciales válidas

- **GIVEN** un usuario registrado, activo y con credenciales válidas
- **WHEN** solicita iniciar sesión
- **THEN** AgroFlow autentica al usuario sin revelar información sensible

#### Scenario: Credenciales rechazadas sin enumeración de usuarios

- **GIVEN** un usuario inexistente o una contraseña incorrecta
- **WHEN** se intenta iniciar sesión
- **THEN** AgroFlow rechaza la autenticación con el mismo mensaje genérico de credenciales inválidas

### Requirement: Protección de contraseñas (RN-002)

AgroFlow MUST almacenar las contraseñas mediante un algoritmo seguro de hashing y MUST NOT persistirlas ni registrarlas en texto plano.

#### Scenario: Registro o cambio de contraseña

- **GIVEN** una contraseña recibida por un flujo autorizado
- **WHEN** AgroFlow persiste la credencial
- **THEN** almacena únicamente una representación segura derivada mediante hashing

### Requirement: Identidad y rol en el token (RN-003, RN-004)

AgroFlow SHALL emitir tokens JWT únicamente para usuarios con un rol válido y el token SHALL identificar al usuario e incluir su rol para autorizar solicitudes posteriores.

#### Scenario: Emisión de token para usuario con rol

- **GIVEN** un usuario autenticado y asociado a un rol válido
- **WHEN** AgroFlow emite el token de acceso
- **THEN** el token identifica al usuario e incluye el rol vigente

### Requirement: Autorización por rol (RN-005)

AgroFlow MUST verificar en el backend que cada usuario autenticado tenga permiso para la operación solicitada.

#### Scenario: Operación fuera del rol

- **GIVEN** un usuario autenticado cuyo rol no autoriza una operación
- **WHEN** intenta ejecutarla
- **THEN** AgroFlow rechaza la solicitud sin modificar datos

### Requirement: Expiración de tokens (RN-006)

AgroFlow MUST rechazar todo token expirado en recursos protegidos con el estado HTTP `401 Unauthorized`.

#### Scenario: Token expirado

- **GIVEN** una solicitud a un recurso protegido con un token expirado
- **WHEN** la API valida la autenticación
- **THEN** responde `401 Unauthorized` y no ejecuta la operación
