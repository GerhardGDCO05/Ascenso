## Context

La sección 9 se implementó sobre la fundación de las secciones 1–8. Su objetivo fue incorporar Hangfire como motor persistente de trabajos en segundo plano para la expiración de ofertas, la reconciliación de vencidas y la limpieza de idempotencia/Outbox, además de un dashboard operativo protegido. Las secciones 10–14 continúan en el cambio activo y no forman parte de este histórico.

## Decisions

### Almacenamiento, colas y activación

- Hangfire usa PostgreSQL con `PostgreSqlStorageOptions.SchemaName = "hangfire"` y `PrepareSchemaIfNecessary = true`.
- Se usa `CompatibilityLevel.Version_180`, serializador simple de ensamblados y configuración de serialización recomendada.
- Las colas son `critical` para expiración/reconciliación y `default` para limpieza; el servidor de la API se suscribe a ambas.
- `ActivadorHangfire` crea un alcance de DI por job para que los handlers, repositorios y el contexto de operación sean scoped.

### Expiración de ofertas

- `ExpirarOfertaBecaJob` delega en `ExpirarOfertaBecaCommand` mediante `ICommandDispatcher`.
- El handler relee la beca bajo `SELECT ... FOR UPDATE`, comprueba que la oferta siga pendiente y vencida, y no falla si ya fue respondida. Por tanto, el job es idempotente y seguro ante reintentos.
- `ProgramadorExpiracionHangfire` implementa `IProgramadorExpiracionOfertas`: si la fecha ya venció, encola de inmediato; si es futura, programa con retraso. Devuelve la referencia del job y permite cancelarlo.
- La emisión de la oferta guarda la referencia en `referenciaJobExpiracionHangfireId`; si la emisión falla, la compensación cancela el job y elimina el archivo.

### Reconciliación

- `ReconciliarOfertasPendientesJob` consulta las ofertas pendientes vencidas con `ObtenerPendientesVencidasAsync`, ordenadas por vencimiento y limitadas por `LimiteOfertasPorReconciliacion`.
- Cada oferta se procesa con el mismo caso de uso idempotente de expiración, por lo que no importa si el job original existe, se perdió o ya se ejecutó.
- `ProgramadorTrabajosHangfireRecurrentes` registra el job recurrente con cron `*/N * * * *` en UTC, donde N se valida entre 1 y 59.
- Se usa `AddOrUpdate<T>(id, cola, expresión, cron, RecurringJobOptions)` para fijar la cola sin recurrir a la propiedad obsoleta `QueueName`.

### Limpieza

- `LimpiarIdempotenciaJob` elimina solicitudes cuyo `ExpiraEn` ya pasó, con días de gracia configurables; cero elimina exactamente las vencidas.
- `LimpiarOutboxJob` elimina mensajes con `SentTime` no nulo anterior al corte y estados huérfanos sin mensajes. Los mensajes pendientes de envío se conservan siempre.
- Los jobs de limpieza se ejecutan diariamente a las 03:00 y 03:15 UTC en la cola `default`.

### Dashboard

- `/hangfire` se expone con `UseHangfireDashboard` y `FiltroAutorizacionDashboard`.
- El filtro exige autenticación, rol administrativo (`COORDINADOR_PROGRAMA` o `DIRECCION`) y segundo factor por `acr` o `amr`.
- Sin autenticación el middleware responde `401`; autenticado sin permiso responde `403`; administrador con 2FA recibe `200`.
- El dashboard se puede desactivar con `Hangfire:Dashboard:Habilitado=false`; el valor por defecto es habilitado. Las pruebas de API que no lo usan lo desactivan porque `UseHangfireDashboard` resuelve el `JobStorage` al construir el pipeline y `PrepareSchemaIfNecessary` intenta conectar a PostgreSQL.

### Configuración y validación

- `OpcionesTrabajosHangfire` se enlaza desde `Hangfire:Trabajos` y se valida al arrancar: intervalo 1–59, retenciones no negativas y límite mayor que cero.
- Los valores por defecto son 5 minutos de reconciliación, 0 días de retención de idempotencia, 7 días de retención de Outbox y 500 ofertas por reconciliación.

### Concurrencia del historial

- Las filas nuevas de `HistorialEstadoBeca` se descubrían por la navegación de colección de una `Beca` ya rastreada y EF las marcaba como `Modified`, aunque no existieran en la base.
- El `UPDATE` afectaba cero filas y lanzaba `DbUpdateConcurrencyException`. Se declaró la clave `Id` con `ValueGeneratedNever()`, porque el dominio siempre genera el UUIDv7, y EF pasa a insertarlas como `Added`.
- La corrección no cambia el esquema SQL ni debilita el token de concurrencia `Version` de las entidades auditables.

### Pruebas

- `EntornoTrabajos` compone persistencia real con Testcontainers PostgreSQL, aplicación, jobs, opciones y un Outbox falso que registra eventos.
- `PersistenciaTrabajosTests` programa un job, desecha el proveedor y arranca un host nuevo con el mismo almacenamiento para comprobar que la oferta expira tras el reinicio.
- `HangfireDashboardTests` usa un esquema de autenticación de prueba y encabezados `X-Test-*`; la factory sustituye `ICartaOfertaPdfGenerator` e `IAlmacenamientoArchivos`, que se implementarán en la sección 10.
- `OfertasBecaApiFactory` desactiva MassTransit y el dashboard con `UseSetting`, elimina los servidores de Hangfire y el publicador de health checks para que los hosts de prueba no dependan de PostgreSQL ni RabbitMQ.

## Risks / Trade-offs

- La reconciliación puede procesar ofertas ya expiradas por otro job; el caso de uso idempotente evita dobles transiciones y eventos duplicados.
- La retención de idempotencia en cero elimina exactamente lo vencido; si se necesita auditoría, debe configurarse una retención mayor.
- La limpieza del Outbox elimina solo mensajes enviados y estados huérfanos; un mensaje pendiente no se borra aunque supere la retención.
- El dashboard depende de Keycloak y de los claims de rol/2FA; en local sin autenticación no es accesible, lo cual es intencional.
- Las pruebas de integración requieren Docker/Testcontainers y una base PostgreSQL real.
- La API sigue sin poder arrancar en `Development` sin los adaptadores de PDF/almacenamiento de la sección 10; es una brecha previa y las pruebas los sustituyen.
- Los cambios de la sección 9 no se comprometieron en git; este histórico también queda sin commit.

## Migration Plan

No hay migraciones nuevas de la aplicación. Hangfire prepara su esquema `hangfire` al iniciar si no existe. La corrección de `HistorialEstadoBeca` es metadata de EF y no altera el esquema SQL. El orden de despliegue es aplicar migraciones existentes, desplegar la API con Hangfire y verificar `/health/ready` y `/hangfire`. El rollback es forward-only; no se elimina el esquema en producción durante una reversión.

## Open Questions

- Ninguna para la sección 9. Las secciones 10–14 continúan en el cambio activo `tech-bootstrap-int4-msbeca`.
