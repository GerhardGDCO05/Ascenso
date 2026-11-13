# Specification: MS-PLT (Microservicio de Plataforma)

## Context

Gestión centralizada de la identidad de dominio, control de acceso basado en roles (RBAC), autorizaciones transaccionales críticas (OTP), definición de periodos académicos y el motor de comunicaciones institucionales seguras.

## Invariants

- Solo puede existir un único `PeriodoAcademico` activo (`esActual == true`) en cualquier momento dado en todo el sistema.
- Las entidades del dominio no deben exponer setters públicos; toda mutación de estado (ej. asignar roles, consumir OTP, activar periodo) debe realizarse a través de métodos de comportamiento que validen las reglas de negocio.
- Una `AutorizacionOTP` es inmutable tras su creación y solo puede transicionar a estado `Consumida` una única vez, siempre que no haya superado su `FechaExpiracion`.
- Una `NotificacionComite` (dirigida a evaluadores) nunca puede contener propiedades de Información de Identificación Personal (PII) del `Postulante` (nombres, cédula, ingresos), limitándose exclusivamente al `CodigoExpediente`.

## Scenarios

<!-- OpenCode inyectará los escenarios GIVEN/WHEN/THEN detallados al archivar cada HU -->

## Behaviors / Business Rules

### 1. Control de Acceso y Roles (RBAC)

- **Autenticación Fuerte:** Cualquier `Usuario` que posea un `Rol` administrativo o de evaluación (`COORDINADOR_PROGRAMA`, `MIEMBRO_COMITE`, `ADMIN_FINANCIERO`, `DIRECCION`, `AUDITOR_INTERNO`, `AUDITOR_EXTERNO`) requiere obligatoriamente validación de doble factor (2FA) para operar en el dominio.
- **Aislamiento de Expedientes:** Un `Usuario` asignado como `MIEMBRO_COMITE` tiene su acceso restringido estrictamente a la lectura de expedientes vinculados a su comité asignado, y se le ocultan las evaluaciones emitidas por sus pares mientras la fase de evaluación esté abierta.

### 2. Autorizaciones Transaccionales (OTP)

- **Vigencia y Vinculación:** Cuando el dominio genera una `AutorizacionOTP`, se le asigna un tiempo de vida máximo de 5 minutos. La autorización está vinculada irreversiblemente a una tupla específica: `UsuarioId`, `Operacion`, `RecursoId` y `HuellaCriptografica`.
- **Bloqueo por Intentos:** Si la validación de una `AutorizacionOTP` falla 3 veces consecutivas, el dominio debe marcarla como `Bloqueada`, suspender temporalmente (15 minutos) la capacidad del `Usuario` para solicitar nuevos OTPs para esa operación, y emitir un evento de dominio indicando la vulneración de seguridad.

### 3. Notificaciones y Recordatorios Preventivos

- **Idempotencia de Recordatorios:** Al evaluar el envío de un `Recordatorio`, el dominio debe verificar el `RegistroRecordatorio`. Si el `PlazoVigilado` asociado ya está marcado como `Cumplido`, el envío se aborta silenciosamente para evitar comunicaciones redundantes al usuario.
- **Transiciones de Ciclo:** Al activar un nuevo `PeriodoAcademico`, el dominio debe clausurar automáticamente el periodo anterior (si existe) y emitir un evento de dominio notificando el cambio de ciclo a los demás contextos (Programas y Otorgamiento).
