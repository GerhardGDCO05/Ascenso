## Why

La sección 6 del cambio `tech-bootstrap-int4-msbeca` implementa la capa de aplicación y los casos de uso del otorgamiento de becas de MS-BEC. Sin esta capa, el dominio y la persistencia de las secciones 1–5 no podían orquestarse en comandos y consultas concretos, ni verificarse los flujos de emisión, aceptación, rechazo, anulación, expiración y compromiso del becario.

Este histórico documenta y conserva el trabajo de la sección 6 **sin archivar el cambio activo**, que todavía tiene pendientes las secciones 7–14. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en el cambio activo y no se sincronizan aquí.

## What Changes

- Implementar CQRS-Lite con `ICommandDispatcher` e `IQueryDispatcher`, validación FluentValidation previa al handler y registro por ensamblado.
- Crear los DTOs y validadores de emisión, aceptación, rechazo, anulación, expiración, carga y revisión de compromiso, y de las consultas de oferta, beca y becas activas.
- Implementar `EmitirOfertaBecaHandler` con consulta de adjudicabilidad a `MS-POS`, validación de proyecciones locales, cálculo de montos, reserva en `MS-PRG`, generación y subida de carta, programación de expiración y persistencia con Outbox.
- Implementar `AceptarOfertaBecaHandler` con transacción `Serializable`, bloqueo por estudiante, bloqueo de fila y política de no doble beneficio.
- Implementar los handlers de rechazo, anulación y expiración de ofertas con transiciones terminales, `tipoCierre` y justificación mínima.
- Implementar la carga y revisión del compromiso del becario con validación de PDF, tamaño máximo, hash SHA-256, intentos y actualización de `habilitadaParaDesembolso`.
- Implementar las consultas internas sin datos personales ni bancarios.
- Integrar la idempotencia con la unidad de trabajo y coordinar la transacción con el puerto de Outbox.
- Incorporar pruebas de aplicación con dobles y pruebas de integración de `UnidadDeTrabajo` sobre PostgreSQL/Testcontainers.

## Capabilities

### New Capabilities

- Ninguna. El trabajo es la capa de aplicación dentro de `MS-BEC`.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` siguen perteneciendo al cambio activo `tech-bootstrap-int4-msbeca` y no se sincronizan en este histórico.

## Impact

- Código afectado: `Apps/services/ms-becas/Ascenso.Becas.Application/`, `Ascenso.Becas.Infrastructure/Persistence/`, `Ascenso.Becas.Infrastructure/Tiempo/`, `Ascenso.Becas.Api/Contexto/`, `Ascenso.Becas.Api/Program.cs` y pruebas de aplicación e integración.
- Base de datos: sin migraciones nuevas; se reutilizan las tablas de becas, proyecciones e idempotencia de la sección 4.
- Configuración: se registra `IContextoOperacionActual` desde HTTP y se integra `AgregarAplicacion()` en el arranque.
- Pruebas: la suite completa quedó en 441/441 correctas y el build Release con `--warnaserror` en 0 errores.
- Fuera de alcance: secciones 7–14 del cambio activo, clientes HTTP reales, almacenamiento/PDF real, jobs Hangfire, mensajería Outbox real y sincronización de specs principales.
