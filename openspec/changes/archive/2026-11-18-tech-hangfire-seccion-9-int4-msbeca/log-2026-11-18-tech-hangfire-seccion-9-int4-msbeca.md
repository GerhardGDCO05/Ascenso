# Registro de Auditoría — Sección 9: Hangfire y trabajos en segundo plano

- **Fecha y hora de ejecución:** 18 de noviembre de 2026, 22:16 (UTC-4)
- **Actualización:** 18 de noviembre de 2026, 22:45 (UTC-4), corrección del arranque del dashboard en CI.
- **Actualización:** 18 de noviembre de 2026, 23:18 (UTC-4), reducción del tiempo de las pruebas de API.
- **Rama:** `Gerhard`
- **Commit de la sección:** ninguno; los cambios de la sección 9 están sin commit. El último commit de la rama es `9115b35` (merge del PR #27).
- **Alcance:** tareas `9.1` a `9.5` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; los demás servicios quedaron intactos.
- **Trabajo local:** este histórico se crea dentro de `openspec/changes/archive/` y queda sin commit. No se archivó el cambio activo ni se sincronizaron specs principales.
- **Estado del cambio activo:** 81/114 tareas; secciones 1–9 completas y secciones 10–14 pendientes (33 tareas). El cambio **no** se movió a `archive/`.

## 1. Objetivo

Incorporar Hangfire como motor persistente de trabajos en segundo plano para `MS-BEC`: almacenamiento PostgreSQL, colas separadas, activador por alcance de DI, dashboard protegido, expiración de ofertas programada, reconciliación de vencidas, limpieza de idempotencia/Outbox y verificación de supervivencia de los jobs tras reiniciar el host.

## 2. Cambios realizados

### 2.1 Hangfire persistente y colas (9.1)

- `RegistroHangfire` usa `UsePostgreSqlStorage` con `SchemaName = "hangfire"` y `PrepareSchemaIfNecessary = true`, compatibilidad `Version_180` y serialización recomendada.
- Se definieron las colas `critical` (expiración y reconciliación) y `default` (limpieza); el servidor de la API se suscribe a ambas.
- `ActivadorHangfire` crea un alcance de dependencias por job, de modo que el despachador, los repositorios y el contexto de operación sean scoped.
- `Program.cs` usa las constantes de cola, registra los trabajos y mapea el dashboard.

### 2.2 Dashboard protegido (9.1)

- `RegistroDashboardHangfire` expone `/hangfire` con `UseHangfireDashboard` y título `Ascenso · MS-BEC`.
- `FiltroAutorizacionDashboard` exige autenticación, rol administrativo (`COORDINADOR_PROGRAMA` o `DIRECCION`) y segundo factor por `acr` o `amr`.
- Comportamiento verificado: `401` sin autenticación, `403` autenticado sin permiso y `200` administrador con 2FA.
- `HangfireDashboardTests` usa un esquema de autenticación de prueba con encabezados `X-Test-Roles` y `X-Test-Acr`, mensajería deshabilitada y dobles de `ICartaOfertaPdfGenerator` e `IAlmacenamientoArchivos`; estos adaptadores pertenecen a la sección 10.
- `DashboardApiFactory` configura la cadena con `UseSetting`, porque `Program.cs` lee `builder.Configuration` antes de que se apliquen las fuentes de `ConfigureAppConfiguration`. La prueba `200` verifica además que el `BecasDbContext` usa la cadena del contenedor y no la de `appsettings.json`.

### 2.3 Expiración de ofertas (9.2)

- `ExpirarOfertaBecaJob` delega en `ExpirarOfertaBecaCommand` mediante `ICommandDispatcher` y se ejecuta en la cola `critical`.
- El handler relee la beca bajo `SELECT ... FOR UPDATE` y solo expira si la oferta sigue `PENDIENTE_RESPUESTA` y la fecha de vencimiento ya pasó; si ya fue respondida, no falla.
- `ProgramadorExpiracionHangfire` implementa `IProgramadorExpiracionOfertas`: si la fecha ya venció, encola de inmediato; si es futura, programa con retraso. Devuelve la referencia y permite cancelarla.
- La emisión de la oferta persiste la referencia en `referenciaJobExpiracionHangfireId`; la prueba del handler verifica que el valor guardado coincide con la referencia devuelta por el programador.
- `ExpirarOfertaBecaJobTests` verifica que una oferta pendiente expira y una aceptada no, que el evento `OfertaBecaExpiradaEvento` se emite una vez y que ejecutar el job dos veces es idempotente.

### 2.4 Reconciliación de ofertas vencidas (9.3)

- `IOfertaBecaRepository.ObtenerPendientesVencidasAsync` devuelve ofertas pendientes con vencimiento anterior o igual a `ahora`, ordenadas por antigüedad y limitadas por configuración.
- `ReconciliarOfertasPendientesJob` procesa cada oferta con el mismo caso de uso idempotente, sin depender de que el job original exista.
- `ProgramadorTrabajosHangfireRecurrentes` registra el job recurrente con cron `*/N * * * *` en UTC, con `N` validado entre 1 y 59.
- Se usa la sobrecarga `AddOrUpdate<T>(id, cola, expresión, cron, RecurringJobOptions)` para fijar la cola sin la propiedad obsoleta `QueueName`.
- `ReconciliarOfertasPendientesJobTests` crea dos ofertas vencidas (una sin job y otra con job perdido) y una vigente; tras ejecutar la reconciliación dos veces, las vencidas quedan expiradas/revocadas, la vigente sigue pendiente y cada oferta vencida emite exactamente un evento.

### 2.5 Limpieza de idempotencia y Outbox (9.4)

- `LimpiarIdempotenciaJob` elimina solicitudes cuyo `ExpiraEn` ya pasó, con días de gracia configurables; cero elimina exactamente las vencidas.
- `LimpiarOutboxJob` elimina mensajes con `SentTime` no nulo anterior al corte y estados huérfanos sin mensajes; los mensajes pendientes se conservan siempre.
- Los jobs de limpieza se ejecutan diariamente a las 03:00 y 03:15 UTC en la cola `default`.
- `LimpiezaTrabajosTests` verifica que solo se eliminan la solicitud idempotente vencida, el mensaje de Outbox viejo y el estado huérfano; la solicitud vigente y el mensaje reciente permanecen.

### 2.6 Persistencia tras reinicio (9.5)

- `PersistenciaTrabajosTests` programa una expiración con un proveedor de servicios, lo desecha y arranca un host nuevo con el mismo almacenamiento de Hangfire sobre PostgreSQL.
- El job sobrevive en el esquema `hangfire` y la oferta expira dentro del plazo de espera; se verifica estado `EXPIRADA`, beca `REVOCADA` y `tipoCierre = OFERTA_EXPIRADA`.

### 2.7 Corrección de concurrencia del historial

- Al expirar una oferta, las filas nuevas de `HistorialEstadoBeca` eran descubiertas por la navegación de colección de una `Beca` ya rastreada y EF las marcaba como `Modified`, aunque no existieran en la base.
- El `UPDATE` afectaba cero filas y lanzaba `DbUpdateConcurrencyException` en `ExpirarOfertaBecaHandler`.
- Se declaró la clave `Id` de `HistorialEstadoBeca` con `ValueGeneratedNever()`, porque el dominio siempre genera el UUIDv7; EF pasa a insertarlas como `Added`.
- La corrección no cambia el esquema SQL ni debilita el token de concurrencia `Version` de las entidades auditables.

### 2.8 Configuración y entorno de pruebas

- `OpcionesTrabajosHangfire` se enlaza desde `Hangfire:Trabajos` y se valida al arrancar: intervalo 1–59, retenciones no negativas y límite mayor que cero.
- Valores por defecto: 5 minutos de reconciliación, 0 días de retención de idempotencia, 7 días de retención de Outbox y 500 ofertas por reconciliación.
- `EntornoTrabajos` compone persistencia real con Testcontainers PostgreSQL, aplicación, jobs, opciones y un Outbox falso que registra eventos.
- `RegistroPersistencia` registra `IdempotencyRepository` como scoped para que el job de limpieza pueda resolverlo.

### 2.9 Arranque rápido de las pruebas de API

- La suite de integración de CI tardaba ~11 minutos porque cada host de `OfertasBecaApiFactory` pagaba un timeout de 30 s. La causa era `app.MapearDashboardHangfire()`: al construir el pipeline, Hangfire resuelve el `JobStorage` y `PrepareSchemaIfNecessary` intenta crear el esquema en PostgreSQL; sin base accesible, el arranque esperaba el timeout.
- `OfertasBecaApiFactory` usa `UseSetting` para deshabilitar MassTransit (`Mensajeria:Habilitada=false`) y el dashboard (`Hangfire:Dashboard:Habilitado=false`). También elimina el `HealthCheckPublisherHostedService` para que los chequeos no se ejecuten en segundo plano durante las pruebas.
- `Program.cs` mapea `/hangfire` solo si `Hangfire:Dashboard:Habilitado` no es `false`; el valor por defecto sigue habilitado. `HangfireDashboardTests` no define la bandera y sigue verificando `401/403/200` contra la base Testcontainers.
- Medición local: el mismo test de controller con PostgreSQL inalcanzable bajó de 31 s a 0.55 s; la suite de integración completa quedó en 13 s de ejecución (18 s de proceso).

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Trabajos/`: `OpcionesTrabajosHangfire.cs`, `ActivadorHangfire.cs`, `ExpirarOfertaBecaJob.cs`, `ReconciliarOfertasPendientesJob.cs`, `LimpiarIdempotenciaJob.cs`, `LimpiarOutboxJob.cs`, `ProgramadorExpiracionHangfire.cs`, `ProgramadorTrabajosHangfireRecurrentes.cs` y `RegistroTrabajosHangfire.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Trabajos/`: `FiltroAutorizacionDashboard.cs` y `RegistroDashboardHangfire.cs`.
- `Apps/services/ms-becas/tests/Ascenso.Becas.IntegrationTests/Trabajos/`: `EntornoTrabajos.cs`, `ExpirarOfertaBecaJobTests.cs`, `ReconciliarOfertasPendientesJobTests.cs`, `LimpiezaTrabajosTests.cs`, `ProgramadorExpiracionHangfireTests.cs`, `PersistenciaTrabajosTests.cs` y `HangfireDashboardTests.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Program.cs` y `appsettings.json` (modificados).
- `Apps/services/ms-becas/Ascenso.Becas.Domain/Ports/IOfertaBecaRepository.cs` (modificado).
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Dependencias/RegistroHangfire.cs` y `RegistroPersistencia.cs` (modificados).
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Configuraciones/HistorialEstadoBecaConfiguracion.cs` y `Persistence/Repositorios/OfertaBecaRepository.cs` (modificados).
- `Apps/services/ms-becas/tests/Ascenso.Becas.Application.Tests/Otorgamiento/EmitirOfertaBecaHandlerTests.cs` (modificado).
- `openspec/changes/tech-bootstrap-int4-msbeca/tasks.md`: tareas `9.1` a `9.5` marcadas.

## 4. Pruebas incorporadas

| Archivo | Pruebas |
|---|---:|
| `Trabajos/ExpirarOfertaBecaJobTests.cs` | 2 |
| `Trabajos/ReconciliarOfertasPendientesJobTests.cs` | 1 |
| `Trabajos/ProgramadorExpiracionHangfireTests.cs` | 2 |
| `Trabajos/LimpiezaTrabajosTests.cs` | 2 |
| `Trabajos/PersistenciaTrabajosTests.cs` | 1 |
| `Trabajos/HangfireDashboardTests.cs` | 3 |
| **Total sección 9** | **11** |

**Suite completa:** 546/546 correctas (Architecture 8, Domain 136, Application 95, Integration 307). La sección 8 cerró con 535 pruebas; la sección 9 agregó 11.

## 5. Verificación ejecutada

- `dotnet build Apps/services/ms-becas/Ascenso.Becas.sln -warnaserror --no-restore`: 0 errores. Solo se emitieron avisos `NU1900` por la imposibilidad de consultar `https://api.nuget.org/v3/index.json` durante un restore anterior.
- `dotnet format Apps/services/ms-becas/Ascenso.Becas.sln --verify-no-changes --no-restore`: limpio.
- `dotnet test` sobre Domain, Application, Architecture e Integration: 136/136, 95/95, 8/8 y 307/307 correctas.
- Se corrigió el fallo de CI de los 3 tests de `HangfireDashboardTests`: el host intentaba conectar a `127.0.0.1:5432` porque `ConfigureAppConfiguration` no llegaba a la lectura temprana de `Program.cs`. Con `UseSetting` la suite de integración quedó 307/307.
- Se corrigió el tiempo de arranque de las pruebas de API: el dashboard de Hangfire resolvía el `JobStorage` en el pipeline y esperaba 30 s a PostgreSQL en cada host. Con el dashboard y MassTransit desactivados en `OfertasBecaApiFactory`, la suite de integración bajó de ~10.8 min en CI a 13 s de ejecución local (307/307).
- `dotnet ef migrations has-pending-model-changes --project Ascenso.Becas.Infrastructure --no-build`: sin cambios pendientes.
- `openspec validate --all --strict`: 7/7 elementos correctos.
- `git diff --check`: sin errores de espacio.
- `tasks.md`: 81/114; sección 9 completa (5/5) y 33 tareas pendientes en las secciones 10–14.

## 6. Análisis de HUs del roadmap

No se marcó ningún RF de `MS-BEC` en `openspec/roadmaps/por_microservicio.md` ni en `openspec/roadmaps/por_integrante.md`:

- **RF-15 (emisión de carta/oferta):** la expiración programada y la reconciliación están listas, pero faltan los adaptadores reales de PDF/almacenamiento de la sección 10 y las pruebas de extremo a extremo de las secciones 11–12.
- **RF-16 (aceptación o rechazo formal):** los jobs mantienen el estado de la oferta y liberan la beca al expirar, pero faltan las pruebas de extremo a extremo y la observabilidad de la sección 11.
- **RF-17 (firma de compromiso):** la limpieza y la persistencia de trabajos están listas, pero el almacenamiento real y la compuerta completa de desembolsos siguen en secciones posteriores.

Los RF permanecen pendientes hasta cerrar las secciones 10–12.

## 7. Decisiones y riesgos

- La reconciliación puede procesar ofertas ya expiradas por otro job; el caso de uso idempotente evita dobles transiciones y eventos duplicados.
- La retención de idempotencia en cero elimina exactamente lo vencido; si se necesita auditoría, debe configurarse una retención mayor.
- La limpieza del Outbox elimina solo mensajes enviados y estados huérfanos; un mensaje pendiente no se borra aunque supere la retención.
- El dashboard depende de Keycloak y de los claims de rol/2FA; en local sin autenticación no es accesible, lo cual es intencional.
- Las pruebas de integración requieren Docker/Testcontainers y una base PostgreSQL real.
- En pruebas con `WebApplicationBuilder`, `ConfigureAppConfiguration` no reemplaza la configuración leída durante el registro de servicios; para `ConnectionStrings` y `Mensajeria:Habilitada` se usa `UseSetting`.
- El dashboard de Hangfire es desactivable con `Hangfire:Dashboard:Habilitado=false`; las pruebas de API que no lo usan lo desactivan para no depender de PostgreSQL en el arranque.
- La API todavía no puede arrancar en `Development` sin los adaptadores de PDF/almacenamiento de la sección 10; es una brecha previa y las pruebas los sustituyen.
- La corrección de `HistorialEstadoBeca` es metadata de EF y no altera el esquema SQL.
- Los cambios de la sección 9 no se comprometieron en git; este histórico también queda sin commit.

## 8. Estado y próximos pasos

- El cambio activo `tech-bootstrap-int4-msbeca` permanece en `openspec/changes/` con 81/114 tareas.
- Las secciones 1–9 están completas; la siguiente es la sección 10 (Archivos, carta y compromiso).
- Las secciones 10–14 siguen pendientes con 33 tareas.
- Este histórico no mueve ni archiva el cambio activo, no sincroniza specs y no marca RF-15/16/17.
- El código de la sección 9 y este registro histórico quedan sin commit.
