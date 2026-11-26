# Proposal

## Why

`MS-PRG` ya administra los programas de becas, sus cupos, su presupuesto y los convenios con universidades, pero no existe el ciclo de postulación que habilita la recepción de solicitudes. Sin `Convocatoria`, el programa no puede abrir un periodo concreto de postulación, y `MS-POS` no tiene un contrato interno del cual leer fechas, cupos, criterios y documentos requeridos (`RF-02`, `RN-PRG-01`, `RN-PRG-05`, `RF-38`).

Se implementa en `MS-PRG` porque el DDD maestro ubica el agregado `Convocatoria` en ese contexto y porque las validaciones de programa, cupos disponibles, presupuesto y convenios vigentes se resuelven dentro de la misma base de datos, sin llamadas distribuidas ni estados inconsistentes. La coordinación con el integrante responsable de `MS-PRG` y la actualización del roadmap quedan como condición de gobierno del cambio.

## What Changes

- Nuevo agregado `Convocatoria` con estado `BORRADOR`, `PUBLICADA`, `CERRADA` o `CANCELADA`, compuesto por `CriteriosElegibilidadCiclo` (objeto de valor) y una colección de `DocumentoRequerido`.
- Casos de uso para crear un borrador, editarlo mientras siga en borrador, asociar o quitar documentos requeridos y publicarlo.
- Endpoints públicos `POST /api/v1/convocatorias`, `PUT /api/v1/convocatorias/{id}`, `GET /api/v1/convocatorias`, `GET /api/v1/convocatorias/{id}`, `POST /api/v1/convocatorias/{id}/publicar` y gestión de documentos requeridos.
- Endpoints internos `GET /internal/v1/convocatorias/{id}` y `GET /internal/v1/convocatorias/abiertas` para que `MS-POS` consuma el ciclo publicado sin acceder a la base de datos de `MS-PRG`.
- Validación al guardar y al publicar: programa existente en estado `CONFIGURADO` o `ACTIVO`, con presupuesto asignado mayor que cero, cupos disponibles suficientes y al menos un convenio vigente; periodo académico existente y vigente; universidades y carreras permitidas dentro de los convenios del programa.
- Normalización de fechas en `America/Caracas` (UTC-4): apertura a las `00:00:00`, cierre y límite de subsanación a las `23:59:59`, almacenamiento en UTC (`timestamptz`), cronología estricta entre apertura, cierre, subsanación y evaluación.
- Publicación únicamente desde `BORRADOR`, con al menos un documento requerido y con fecha de cierre futura; el cierre automático pasa `PUBLICADA` a `CERRADA` a las `23:59:59` (UTC-4) mediante un job recurrente de Hangfire.
- Seguridad PBAC para el módulo: `convocatorias:crear`, `convocatorias:leer`, `convocatorias:editar` y `convocatorias:publicar`, resueltas contra el claim `permissions` del JWT mediante un proveedor dinámico de políticas.
- Persistencia con índice único de `codigo` y restricciones `CHECK` para cupos, edades, promedio y orden de fechas.

**Fuera de alcance:** endpoints de postulación de `MS-POS`, migración de las políticas por rol de Rúbricas y Comités a PBAC, carga real de archivos de soporte (`HU 3.1`), reserva de cupos y activación de programas.

## Capabilities

### New Capabilities

- `ms-prg/convocatorias`: ciclo de vida de la convocatoria de un programa de becas, desde su creación en borrador hasta su publicación y cierre automático, incluyendo criterios de elegibilidad, documentos requeridos, validaciones de disponibilidad y contrato interno de lectura para `MS-POS`.

### Modified Capabilities

Ninguna. `MS-PRG` consume el estado y los cupos ya definidos por `ms-prg/programas` y los convenios de `ms-prg/universidades`; no cambia sus requisitos.

## Impact

- **Dominio:** nuevo módulo `Convocatorias` en `Ascenso.Programas.Domain` (agregado, objeto de valor, entidad, enum, eventos, excepciones y puertos). No se modifica `ProgramaBeca`.
- **Aplicación:** comandos, consultas, validadores, DTOs y registro de manejadores en `Ascenso.Programas.Application`.
- **Infraestructura:** configuración EF Core, repositorio, nueva migración en el esquema `programas`, adaptador HTTP hacia `MS-PLT` para validar el periodo académico y job de Hangfire para el cierre automático.
- **API:** `ConvocatoriasController` público e interno; configuración JWT Bearer de Keycloak, `PermissionPolicyProvider` y manejador de `403 PERMISO_DENEGADO` en `MS-PRG`, que hoy no cuenta con autenticación.
- **Dependencias:** `Microsoft.AspNetCore.Authentication.JwtBearer` en `MS-PRG`; mapper del claim `permissions` en Keycloak (propiedad del equipo de identidad, queda como dependencia documentada); endpoint interno de consulta de periodo por identificador en `MS-PLT`.
- **Datos:** tablas `programas.convocatorias` y `programas.documentos_requeridos`, con índice único y restricciones de integridad.
- **Pruebas:** unitarias de dominio y aplicación, integración de endpoints, persistencia, seguridad y job; pruebas de arquitectura para mantener el aislamiento de `Domain`.
