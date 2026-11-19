## Why

La sección 9 del cambio `tech-bootstrap-int4-msbeca` incorpora Hangfire como motor de trabajos en segundo plano de `MS-BEC`. Sin esta sección, la expiración de ofertas dependía de la ejecución síncrona y no existía una red de seguridad para jobs perdidos, ni limpieza programada de idempotencia/Outbox, ni un dashboard operativo protegido.

Este histórico documenta y conserva el trabajo de la sección 9 **sin archivar el cambio activo**, que todavía tiene pendientes las secciones 10–14. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en el cambio activo y no se sincronizan aquí.

## What Changes

- Configurar Hangfire con almacenamiento PostgreSQL en el esquema `hangfire`, colas `critical` y `default`, activador de jobs por alcance de dependencias y dashboard en `/hangfire` protegido por rol administrativo y segundo factor.
- Implementar `ExpirarOfertaBecaJob`, programado al emitir la oferta y con el identificador persistido en `referenciaJobExpiracionHangfireId`; el job delega en el caso de uso idempotente de expiración.
- Implementar `ReconciliarOfertasPendientesJob` recurrente cada 5 minutos (configurable entre 1 y 59) como red de seguridad para ofertas vencidas sin job o con job perdido.
- Implementar `LimpiarIdempotenciaJob` y `LimpiarOutboxJob` con retención configurable; solo eliminan registros vencidos y mensajes ya enviados, preservando los pendientes.
- Verificar que los jobs sobreviven al reinicio del host recreando el contenedor de API y el almacenamiento de Hangfire sobre la misma base.
- Corregir la concurrencia de las filas nuevas de `HistorialEstadoBeca`, que EF marcaba como `Modified` y provocaba `DbUpdateConcurrencyException` al expirar una oferta.

## Capabilities

### New Capabilities

- Ninguna. La sección 9 implementa infraestructura de trabajos sobre capacidades ya definidas.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` siguen perteneciendo al cambio activo `tech-bootstrap-int4-msbeca` y no se sincronizan en este histórico.

## Impact

- Código afectado: `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Trabajos/`, `Dependencias/RegistroHangfire.cs`, `Dependencias/RegistroPersistencia.cs`, `Persistence/Repositorios/OfertaBecaRepository.cs`, `Persistence/Configuraciones/HistorialEstadoBecaConfiguracion.cs`, `Ascenso.Becas.Api/Trabajos/`, `Program.cs` y `appsettings.json`.
- Base de datos: Hangfire crea y mantiene su propio esquema `hangfire`; no se agregan migraciones de la aplicación. La corrección de `HistorialEstadoBeca` no cambia el esquema SQL.
- Configuración: nueva sección `Hangfire:Trabajos` con intervalo de reconciliación, retenciones y límite de ofertas.
- Pruebas: 11 pruebas de integración nuevas; la suite completa quedó en 546/546 (Domain 136, Application 95, Architecture 8, Integration 307).
- Fuera de alcance: secciones 10–14 del cambio activo, adaptadores reales de PDF/almacenamiento, sincronización de specs principales y commit.
