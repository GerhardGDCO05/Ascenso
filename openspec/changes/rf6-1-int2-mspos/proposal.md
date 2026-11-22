# Proposal

## Why

RF-06 exige que cada programa de becas tenga un comité evaluador compuesto por uno o más evaluadores internos o externos. Hoy `MS-POS` ya permite definir y bloquear la rúbrica (RF-07), pero no existe el agregado `ComiteEvaluador`: sin él no se puede asignar postulaciones a evaluadores (HU 6.2), autorizar la excepción de evaluador único (HU 6.3) ni registrar conflictos de interés (RF-39). La HU 6.1 entrega la base: **conformar el comité de un programa y convocatoria**, con su jefatura y sus miembros internos y externos.

## What Changes

**Alcance: SOLO la HU 6.1 (RF-06).** `docs/` no contiene el texto formal de la HU 6.1 ni sus criterios de aceptación; se derivan del PRD (RF-06), del DDD V7 (`ComiteEvaluador`, `MiembroComite`) y de la matriz de roles. Las suposiciones están en `design.md` y las preguntas abiertas al final.

- **Agregado `ComiteEvaluador`** (raíz) con entidad hija `MiembroComite`, único por programa y convocatoria. Se crea con su jefatura (`jefaturaUsuarioId`), que ingresa como primer miembro activo (rol `JEFATURA`, tipo `INTERNO`), por lo que el comité siempre tiene al menos un miembro (DDD: `1..*`).
- **Enums nuevos con `CodigoEnum`:** `TipoEvaluador` (`INTERNO`, `EXTERNO`) y `RolMiembroComite` (`JEFATURA`, `EVALUADOR`).
- **Casos de uso (CQRS sin MediatR, síncronos):** `CrearComiteEvaluadorCommand`, `AgregarMiembroComiteCommand`, `DesvincularMiembroComiteCommand`, `ObtenerComiteEvaluadorQuery` y `ObtenerComitePorProgramaQuery`.
- **Invariantes:** un solo comité por programa y convocatoria; un usuario no puede estar dos veces activo en el mismo comité; la jefatura no se desvincula; el desvinculado conserva su historial (`fechaDesvinculacion`, `activo = false`), sin borrado físico; reincorporar a un desvinculado crea un nuevo registro de membresía.
- **API REST v1** `api/v1/comites-evaluadores`, escritura y lectura solo para `COORDINADOR_PROGRAMA`; cualquier otro rol (incluidos `ADMIN_FINANCIERO` y `MIEMBRO_COMITE`) recibe `403` con `codigo = PERMISO_DENEGADO`.
- **Persistencia:** tablas `postulaciones.comites_evaluadores` y `postulaciones.miembros_comite` con `timestamptz` para auditoría, `date` para las fechas de negocio del DDD, índices únicos (parciales para membresía activa y jefatura activa) y restricciones `CHECK`; migración nueva en tarea separada antes de las pruebas con PostgreSQL real.

**Fuera de alcance:**

- HU 6.2 (asignación de postulaciones y balanceo de carga, `AsignacionEvaluacion`), HU 6.3 (excepción de evaluador único) y RF-39 (`ConflictoInteres`, reasignación por conflicto), incluida la regla de «mínimo 2 evaluadores por postulación».
- Cambio de jefatura, desactivación del comité y validación de existencia del programa, la convocatoria o el usuario evaluador en otros microservicios (RF-02, MS-PRG, MS-PLT).
- Hangfire (la HU no exige ejecución en segundo plano), Outbox, MediatR, eventos de integración, frontend, módulos `Postulantes` y `Rubricas` y otros microservicios.

## Capabilities

### New Capabilities

- `ms-pos/comites`: conformación del comité evaluador de un programa y convocatoria (creación, miembros internos y externos, desvinculación con historial, consulta y autorización).

### Modified Capabilities

<!-- Ninguna: el comportamiento nuevo no altera requisitos de las capacidades existentes (`postulantes`, `rubricas`, `fundacion-servicio`). -->

## Impact

- **Especificaciones OpenSpec:** nueva capacidad en `openspec/changes/rf6-1-int2-mspos/specs/ms-pos/comites/spec.md`; al archivar se sincroniza con `openspec/specs/ms-pos/comites/spec.md`.
- **Dominio:** `Comites/Agregados/ComiteEvaluador`, `Comites/Entidades/MiembroComite`, `Enums/TipoEvaluador`, `Enums/RolMiembroComite`, excepciones de comité, puerto `IComiteEvaluadorRepository`. Sin cambios en `Domain/Comun` (se reutilizan `AggregateRoot` y `UuidV7`).
- **Aplicación:** `Comites/Comandos`, `Comites/Consultas`, `Comites/DTOs` (DTOs y mapeador), validadores FluentValidation, registro en `DependencyInjection.cs`.
- **Infraestructura:** configuraciones EF Core, `ComiteEvaluadorRepository`, `PostulacionesDbContext` (dos `DbSet`), `TraductorExcepcionesPersistencia` (índices únicos nuevos), `DependencyInjection`, migración nueva.
- **API:** `ComitesEvaluadoresController`, `PoliticasAutorizacionComites`, registro de políticas en `Program.cs` y ampliación mínima de `ManejadorResultadoAutorizacionRubricas` para responder `PERMISO_DENEGADO` también en las políticas de comités (sin cambiar el comportamiento de rúbricas).
- **Pruebas:** `Domain.Tests`, `Application.Tests` e `IntegrationTests` (modelo EF, endpoints, seguridad, Testcontainers).
- **Roadmaps:** agregar y marcar `HU 6.1` anidada bajo `RF-06`, que permanece en `[ ]` hasta completar las HU 6.2 y 6.3.
