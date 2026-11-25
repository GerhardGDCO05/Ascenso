# Tareas — tech-bootstrap-int4-msbeca

> **Alcance de este cambio:** fundación técnica y primera rebanada vertical de otorgamiento (secciones 1–14).
>
> **Alcance del microservicio `MS-BEC`:** RF-15, RF-16, RF-17, RF-19, RF-20, RF-21, RF-22, RF-24, RF-25, RF-26 y RF-27, más las contribuciones de `MS-BEC` a RF-18, RF-28, RF-29 y RF-30 y los movimientos contables que alimentan RF-23 (cuyo dueño es `MS-PRG`/`MS-AUD`). Las secciones 15–22 completan el resto del ciclo de vida en cambios posteriores; **no están fuera del alcance de `MS-BEC`**, solo fuera del alcance de este cambio.

> **Sobre las HUs:** los identificadores y títulos se transcriben del documento funcional `Doc General del Proyecto.pdf` (no versionado en Git). Ese documento asigna IDs explícitos a RF-15 a RF-18 y RF-27 a RF-30; para RF-19 a RF-26 usa “Historia funcional 1/2/3”, así que se referencian por RF + número de historia. No se inventan identificadores faltantes. Si el documento funcional cambia, actualice esta matriz sin renumerar las secciones.

## Trazabilidad sección ↔ RF ↔ HU

| Sección | RF / colaboración | HUs cubiertas | Estado |
| :--- | :--- | :--- | :--- |
| 1. Preparación del entorno | Transversal | Habilita todas las HUs de `MS-BEC`. | Completado |
| 2. Shared kernel | Transversal | Contratos reutilizados por todas las HUs. | Completado |
| 3. Dominio de otorgamiento | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2, HU-17.1–HU-17.2 (núcleo). | Completado |
| 4. Persistencia y migraciones | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2, HU-17.1–HU-17.2. | Completado |
| 5. Seguridad e idempotencia | Transversal; OTP de RF-20/RF-22 | HU-15.1, HU-15.3, HU-16.1; RF-20 Historia 3; RF-22 Historia 1. | Completado (base); el OTP se integra en 15.5 y secciones 16–17. |
| 6. Aplicación y casos de uso | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2, HU-17.1–HU-17.2. | Completado |
| 7. Clientes de integración | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2. | Completado con deuda de contrato (15.1–15.7) |
| 8. API HTTP | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2, HU-17.1–HU-17.2. | Completado |
| 9. Hangfire y trabajos | RF-15, RF-16; prepara RF-26 | HU-15.2; base para RF-26 Historia 2. | Completado |
| 10. Archivos, carta y compromiso | RF-15, RF-17 | HU-15.1 (carta), HU-17.1 (plantilla y carga). | Completado |
| 11. Observabilidad y operación | Transversal | Todas las HUs; contribución a RF-18/RF-35. | Pendiente |
| 12. Pruebas de integración y calidad | Todas | Verificación de las HUs implementadas en las secciones 3–10. | En curso |
| 13. Entorno local, contenedores y CI | Transversal | Todas las HUs. | Pendiente |
| 14. Contratos y cierre del cambio | Contratos de RF-15/16/17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2, HU-17.1–HU-17.2. | Pendiente |
| 15. Cierre de integraciones del otorgamiento | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2. | Pendiente |
| 16. Desembolsos y datos de pago | RF-19, RF-20; alimenta RF-23 | RF-19 Historia 1–2; RF-20 Historia 1–3. | Pendiente |
| 17. Suspensión, reanudación y revocación | RF-21, RF-22 | RF-21 Historia 1–2; RF-22 Historia 1–2. | Pendiente |
| 18. Seguimiento académico y permanencia | RF-24, RF-25 | RF-24 Historia 1–2; RF-25 Historia 1–2. | Pendiente |
| 19. Renovación y conclusión | RF-26 | RF-26 Historia 1–2. | Pendiente |
| 20. Pausa y aplazamiento | RF-27 | HU-27A, HU-27B. | Pendiente |
| 21. Historial, auditoría y notificaciones | RF-18, RF-28, RF-29, RF-30 | HU-18.1, HU-28A/B, HU-29A/B, HU-30A/B. | Pendiente |
| 22. Verificación integral del alcance | Todas | Todas las HUs de `MS-BEC`. | Pendiente |

## Matriz de HUs de `MS-BEC`

> **Criterio de estado (2026-11-20):** “Lista” = flujo y pruebas del servicio completos; “Lista (MS-BEC)” = flujo del servicio completo, la liberación o promoción depende de otro microservicio y solo quedan tareas de contrato o verificación; “Parcial” = falta al menos un criterio de la HU; “No lista” = sin implementación; “Contribución” = la HU pertenece a otro microservicio y `MS-BEC` solo aporta eventos, historial o plazos.

### HUs propias y compartidas

| HU | RF | Historia funcional | Dueño | Sección(es) | Estado actual |
| :--- | :--- | :--- | :--- | :--- | :--- |
| HU-15.1 | RF-15 | Generación y emisión de carta con OTP, reserva coordinada y Hangfire | `MS-BEC` | 3, 4, 6–10, 15 | Parcial: faltan las tareas 15.1–15.5. |
| HU-15.2 | RF-15, RF-16 | Procesamiento asíncrono de expiración automática de ofertas vencidas | `MS-BEC` | 9, 15 | Lista (MS-BEC): el job y la reconciliación están listos; la liberación de cupo/presupuesto y la promoción son de `MS-PRG`/`MS-POS`. Deuda 15.4: documentar y probar el ciclo de reserva. |
| HU-15.3 | RF-15 | Anulación expresa de oferta emitida por coordinación | `MS-BEC` | 6, 8, 15 | Parcial: faltan las tareas 15.4, 15.5, 15.7 y la liberación externa. |
| HU-16.1 | RF-16 | Aceptación formal con OTP, transición a Activa y no doble beneficio | `MS-BEC` | 3, 4, 6, 8, 15 | Parcial: faltan las tareas 15.4, 15.5 y 15.7. |
| HU-16.2 | RF-16 | Rechazo formal y disparo de lista de espera asistida | `MS-BEC` | 6, 8, 15 | Parcial: el rechazo y el evento están listos y la promoción es de `MS-POS`; faltan 15.4 (verificación del ciclo de reserva) y 15.7 (borrado del job). |
| HU-17.1 | RF-17 | Carga del documento de compromiso firmado por el becario | `MS-BEC` | 3, 4, 6, 8, 10 | Lista. |
| HU-17.2 | RF-17 | Revisión, validación y habilitación de la compuerta de desembolsos | `MS-BEC` | 3, 4, 6, 8 | Lista. |
| RF-19 Historia 1 | RF-19 | Configuración de reglas de pago según el programa | `MS-BEC` | 16 | No lista: la cobertura del programa se consulta a `MS-PRG`; la regla de pago y su snapshot son de `MS-BEC`. |
| RF-19 Historia 2 | RF-19 | Generación del calendario de desembolsos y validación de límites | `MS-BEC` | 16 | No lista. |
| RF-20 Historia 1 | RF-20 | Registro operativo de la ejecución de desembolsos | `MS-BEC` | 16 | No lista. |
| RF-20 Historia 2 | RF-20 | Gestión de incidencias por desembolsos fallidos o devueltos | `MS-BEC` | 16 | No lista. |
| RF-20 Historia 3 | RF-20 | Confirmación segura de desembolsos mediante OTP | `MS-BEC` | 16 | No lista. |
| RF-21 Historia 1 | RF-21 | Registro de suspensión temporal de desembolsos | `MS-BEC` | 17 | No lista. |
| RF-21 Historia 2 | RF-21 | Notificación al becario de los requerimientos para resolver la suspensión | `MS-BEC` + `MS-PLT` | 17, 21 | No lista. |
| RF-22 Historia 1 | RF-22 | Registro administrativo y justificación de la revocación de beca | `MS-BEC` | 17 | No lista. |
| RF-22 Historia 2 | RF-22 | Reincorporación presupuestaria y liberación de cupo por revocación | `MS-BEC` + `MS-PRG` | 16, 17 | No lista. |
| RF-24 Historia 1 | RF-24 | Carga y registro de reportes académicos por periodo | `MS-BEC` | 18 | No lista. |
| RF-24 Historia 2 | RF-24 | Validación de fechas límite y alertas por retrasos académicos | `MS-BEC` + `MS-PLT` | 18, 21 | No lista. |
| RF-25 Historia 1 | RF-25 | Evaluación del reporte académico contra los umbrales de permanencia | `MS-BEC` | 18 | No lista. |
| RF-25 Historia 2 | RF-25 | Activación de consecuencias por incumplimiento y visibilidad de motivos | `MS-BEC` | 17, 18 | No lista. |
| RF-26 Historia 1 | RF-26 | Solicitud de renovación de beca por el becario | `MS-BEC` | 19 | No lista. |
| RF-26 Historia 2 | RF-26 | Evaluación, aprobación y conclusión automática por expiración de plazo | `MS-BEC` | 9, 19 | No lista. |
| HU-27A | RF-27 | Solicitud de pausa temporal de beca | `MS-BEC` | 20 | No lista. |
| HU-27B | RF-27 | Evaluación y gestión de pausa o aplazamiento | `MS-BEC` | 17, 20 | No lista. |

### Contribuciones y HUs externas

| HU | RF | Historia funcional | Dueño | Sección(es) | Estado actual |
| :--- | :--- | :--- | :--- | :--- | :--- |
| HU-18.1 | RF-18 | Consulta de historial unificado de becas por persona, programa o periodo | `MS-AUD` | 21 | Contribución pendiente: `MS-BEC` aporta `HistorialEstadoBeca` y eventos; no expone la consulta. |
| HU-28A | RF-28 | Consulta personal del historial de beca | `MS-AUD` | 21 | Contribución pendiente. |
| HU-28B | RF-28 | Auditoría del historial integral por la Fundación | `MS-AUD` | 21 | Contribución pendiente. |
| HU-29A | RF-29 | Recepción de notificaciones automáticas por eventos clave | `MS-PLT` | 21 | Contribución pendiente: `MS-BEC` publica los hitos. |
| HU-29B | RF-29 | Procesamiento masivo y asíncrono de notificaciones de ciclo | `MS-PLT` | 21 | Contribución pendiente. |
| HU-30A | RF-30 | Configuración de parámetros y tiempos de recordatorio | `MS-PLT` | 21 | Contribución pendiente. |
| HU-30B | RF-30 | Recepción de recordatorios proactivos de fechas límite | `MS-PLT` | 21 | Contribución pendiente: `MS-BEC` publica los plazos vigilados. |
| RF-23 Historia 1–2 | RF-23 | Panel y exportación del estado financiero por programa | `MS-PRG`/`MS-AUD` | 16 | Externa: `MS-BEC` solo alimenta movimientos contables. |
| RF-33/34/35/36/40 | RF-33, RF-34, RF-35, RF-36, RF-40 | Reportería, auditoría y panel de dirección | `MS-AUD` | — | Fuera de `MS-BEC`; no se planifican aquí. |

## Hallazgos de la auditoría

1. **Contradicción de alcance corregida.** La frase anterior “fuera de alcance” mezclaba el alcance de este cambio con el alcance del microservicio. Se separaron ambos conceptos: desembolsos, seguimiento, renovación y revocación definitiva no están en este cambio, pero sí son responsabilidad de `MS-BEC`.
2. **Cobertura incompleta.** No existían secciones para RF-19, RF-20, RF-21, RF-22, RF-24, RF-25, RF-26 ni RF-27. Se agregaron las secciones 16–20 y la sección 21 para las contribuciones a RF-18, RF-28, RF-29 y RF-30 y para los movimientos que alimentan RF-23.
3. **Deudas de contrato en RF-15/16/17.** La rebanada funcional está entregada, pero el DDD exige `POST /internal/v1/postulaciones/{id}/otorgar`, la consulta de condiciones del programa, el periodo académico actual y el ciclo de vida de la reserva. También falta propagar el OTP de emisión/anulación y cancelar el job de expiración al responder o anular la oferta. Las deudas restantes se registran en 15.1–15.7; la plantilla del compromiso quedó resuelta en 10.6.
4. **Transiciones de dominio ya existentes, pero incompletas.** `Beca.Suspender`, `Beca.Reactivar`, `Beca.Renovar`, `Beca.Concluir` y `Beca.Revocar` ya existen, al igual que `CadenaBecaId`, `BecaAnteriorId`, `PeriodosCursados`, `MontoDesembolsadoPeriodoActual` y `HabilitadaParaDesembolso`. Las secciones 16–20 los reutilizan y los completan; no se reescribe el agregado.
5. **Puertos existentes sin uso completo.** `IOtpClient` está implementado y probado, pero ningún caso de uso lo consume todavía. `IProgramasClient` solo reserva cupo/presupuesto; faltan las condiciones, el ciclo de la reserva y los movimientos presupuestarios.
6. **Modelo faltante.** No existen `CalendarioDesembolso`, `Desembolso`, `DatosPagoBecario`, `IncidenciaDesembolso`, `SuspensionDesembolso`, `ReporteAcademico`, `EvaluacionPermanencia`, `SolicitudRenovacion` ni `PausaAplazamiento`; las secciones 16–20 los incorporan con sus migraciones y pruebas.
7. **Estado de los roadmaps.** RF-15, RF-16 y RF-17 están marcados por la rebanada funcional entregada; las deudas 15.1–15.7 deben completarse antes de declarar el RF cerrado. RF-21, RF-22 y RF-26 tienen solo la transición básica de dominio y no deben marcarse hasta completar las secciones 17 y 19. RF-18 sigue siendo responsabilidad principal de `MS-AUD`; la parte de `MS-BEC` se verifica en la sección 21.
8. **Alineación HU ↔ RF ↔ tarea.** La matriz de HUs identifica que la anulación expresa (`HU-15.3`) no tenía una tarea propia de cierre y que el borrado del job de Hangfire al aceptar, rechazar o anular no estaba cubierto; se agrega la tarea 15.7. Las HUs de RF-19 a RF-27 se mapean a las secciones 16–20, y las contribuciones a RF-18/RF-28/RF-29/RF-30 a la sección 21.

---

## 1. Preparación del entorno y de la solución

> **Trazabilidad:** transversal. Habilita todas las historias de RF-15 a RF-27.

- [x] 1.1 Instalar el SDK de .NET 10 LTS y Docker/Compose; verificar con `dotnet --version`, `docker --version` y `docker compose version`.
- [x] 1.2 Crear `Ascenso.Becas.sln` con `Ascenso.Becas.Domain`, `Ascenso.Becas.Application`, `Ascenso.Becas.Infrastructure` y `Ascenso.Becas.Api`; agregar las referencias permitidas entre capas y verificar que `dotnet build` compila.
- [x] 1.3 Crear `Ascenso.Becas.Domain.Tests`, `Ascenso.Becas.Application.Tests`, `Ascenso.Becas.ArchitectureTests` y `Ascenso.Becas.IntegrationTests`; verificar que `dotnet test` descubre los proyectos.
- [x] 1.4 Agregar `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` y `.gitignore`, con `net10.0`, nullable, warnings-as-errors y versiones centralizadas; verificar `dotnet restore` y `dotnet build -warnaserror`.
- [x] 1.5 Configurar lock files de NuGet y `RestoreLockedMode` para CI; verificar que un segundo restore con `--locked-mode` no modifica dependencias.
- [x] 1.6 Agregar paquetes base (EF Core/Npgsql, MassTransit/RabbitMQ, Hangfire, FluentValidation, Serilog/OpenTelemetry, PdfSharpCore, Google Cloud Storage) y paquetes de prueba (xUnit, FluentAssertions, NSubstitute, NetArchTest, Testcontainers, WireMock.Net, MassTransit.Testing); verificar restore, build y que las licencias de MassTransit y PdfSharpCore son compatibles con el proyecto.
- [x] 1.7 Crear un `README.md` raíz con requisitos, comandos de build/test y estructura de carpetas; verificar que los comandos documentados funcionan en una copia limpia.
- [x] 1.8 Configurar `dotnet format` y análisis de código en el build; verificar que `dotnet format --verify-no-changes` pasa sobre la solución vacía.
- [x] 1.9 Instalar `dotnet-ef` como herramienta global, agregar `~/.dotnet/tools` al `PATH` y verificar `dotnet ef --version`; documentar el comando de instalación en el README.

## 2. Shared kernel de dominio

> **Trazabilidad:** transversal. Define las invariantes y contratos que reutilizan todas las HUs de becas.

- [x] 2.1 Implementar `EntidadAuditable`, `EntidadInmutable`, `AggregateRoot`, `ValueObject` y `DomainEvent` en `Domain/Comun`; verificar con pruebas que la igualdad por identificador y la auditoría funcionan.
- [x] 2.2 Implementar un generador de `UUIDv7` y la abstracción `IClock` (`DateTimeOffset UtcNow`); verificar con pruebas que los identificadores son version 7 y ordenables por tiempo.
- [x] 2.3 Crear el catálogo de excepciones y códigos de error de dominio (`TransicionEstadoInvalidaException`, `OfertaVencidaException`, `DobleBeneficioException`, `JustificacionRequeridaException`, `PostulacionNoAdjudicableException`, `CompromisoNoFirmadoException`); verificar que cada excepción expone su código y estatus HTTP.
- [x] 2.4 Implementar los objetos de valor `PoliticaCobertura` y `CondicionesPermanencia` con validación de porcentajes, montos no negativos y moneda; verificar con pruebas de valores válidos e inválidos.
- [x] 2.5 Implementar las enumeraciones exactas `EstadoBeca` (6 valores), `EstadoOferta` (5), `EstadoCompromiso` (4), `TipoCierreBeca`, `TipoCobertura`, `TipoCuentaBancaria` y `TipoOperacionOTP`; verificar con pruebas que los valores y nombres coinciden literalmente con la especificación.
- [x] 2.6 Crear las tablas de transición de `Beca`, `OfertaBeca` y `CompromisoBecario` como estructuras de dominio testeables; verificar que cada dupla permitida y prohibida de la Tabla 3.2 está cubierta.
- [x] 2.7 Definir los puertos `IBecaRepository`, `IOfertaBecaRepository`, `ICompromisoBecarioRepository`, `IUnitOfWork`, `IClock`, `IAlmacenamientoArchivos`, `ICartaOfertaPdfGenerator`, `IPostulacionesClient`, `IProgramasClient` e `IOtpClient`; verificar que `Domain` y `Application` compilan sin referencias a `Infrastructure`.

## 3. Modelo de dominio de otorgamiento

> **Trazabilidad:** RF-15 (emisión de la oferta y la carta), RF-16 (aceptación, rechazo, anulación y expiración), RF-17 (compromiso del becario). HUs: HU-15.1–HU-15.3, HU-16.1–HU-16.2 y HU-17.1–HU-17.2.

- [x] 3.1 Implementar la creación de `Beca` en estado `OTORGADA` con `cadenaBecaId = id`, `becaAnteriorId = null`, `periodosCursados = 0`, `montoDesembolsadoPeriodoActual = 0`, `tieneRenovacionEnCurso = false`, `costoMatriculaReferenciaSnapshotId`, `montoComprometidoPeriodoActual` y snapshot de `PoliticaCobertura`; verificar con pruebas de fábrica y de campos iniciales.
- [x] 3.2 Implementar `OfertaBeca` con `montoOfertaPorPeriodo`, `montoTotalEstimadoDuracion`, `duracionPeriodos`, `condicionesSnapshot`, `urlCartaPdfFirebase`, `hashCartaSha256`, `fechaVencimientoAceptacion`, `referenciaJobExpiracionHangfireId` y `version`; verificar con pruebas de creación y valores por defecto.
- [x] 3.3 Implementar las transiciones de `Beca` `OTORGADA → ACTIVA`, `OTORGADA → REVOCADA`, `ACTIVA → SUSPENDIDA`, `SUSPENDIDA → ACTIVA`, `ACTIVA → RENOVADA`, `ACTIVA → CONCLUIDA` y las revocaciones permitidas; verificar que las duplas no listadas lanzan `TransicionEstadoInvalidaException`.
- [x] 3.4 Implementar las transiciones de `OfertaBeca` desde `PENDIENTE_RESPUESTA` hacia `ACEPTADA`, `RECHAZADA_POR_POSTULANTE`, `EXPIRADA` y `ANULADA_POR_COORDINACION`; verificar que los estados terminales no admiten nuevas transiciones.
- [x] 3.5 Implementar `CompromisoBecario` con los estados `PENDIENTE_CARGA`, `EN_REVISION`, `APROBADO` y `RECHAZADO`, incremento de `numeroIntentoCarga`, hash, URL, IP y revisión; verificar el ciclo completo con pruebas.
- [x] 3.6 Implementar `HistorialEstadoBeca` como entidad inmutable y la generación automática de una fila por transición, incluyendo actor, justificación, OTP y marca de terminal; verificar que no existe API de modificación o borrado.
- [x] 3.7 Implementar la política de no doble beneficio como servicio de dominio: (a) ninguna otra beca `ACTIVA` con cobertura `TOTAL`, y (b) ninguna otra beca `ACTIVA` en el mismo periodo salvo acumulación en ambos programas; verificar con pruebas de todos los casos.
- [x] 3.8 Implementar el cálculo del monto de oferta por modalidad `TOTAL`, `PARCIAL` y `MONTO_FIJO`, con redondeo a 2 decimales y moneda USD; verificar con pruebas de borde (0%, 100%, montos fijos y matrícula 0).
- [x] 3.9 Implementar los eventos de dominio de emisión, activación, rechazo, anulación, expiración y compromiso; verificar con pruebas que cada transición emite exactamente el evento esperado y que no se emiten eventos en transiciones fallidas.
- [x] 3.10 Implementar la validación de justificación mínima (`Trim().Length >= 10`) para anulación y revocación; verificar que una justificación vacía o corta no cambia el estado.
- [x] 3.11 Agregar pruebas basadas en la tabla completa de estados de la especificación (todas las duplas origen/destino, incluidas las prohibidas); verificar cobertura del 100% de las transiciones de `Beca` y `OfertaBeca`.

## 4. Persistencia y migraciones

> **Trazabilidad:** RF-15, RF-16 y RF-17. Persiste el agregado de otorgamiento y respalda las invariantes de no doble beneficio e historial inmutable.

- [x] 4.1 Implementar `BecasDbContext` con DbSets de `Beca`, `OfertaBeca`, `CompromisoBecario`, `HistorialEstadoBeca`, proyecciones e idempotencia; verificar que el contexto se construye con la cadena de PostgreSQL.
- [x] 4.2 Configurar las entidades con nombres `snake_case`, `uuid`, `timestamptz`, `date`, `numeric(18,2)`, enums como `varchar`, conversiones de objetos de valor y `version` como token de concurrencia; verificar el SQL generado con `dotnet ef migrations script`.
- [x] 4.3 Crear los índices únicos parciales `ux_beca_estudiante_cobertura_total_activa`, `ux_beca_estudiante_periodo_no_acumulable_activa`, `ux_beca_cadena_periodo`, `ux_oferta_beca_beca` y `ux_beca_codigo`; verificar que existen en la migración y que rechazan duplicados en pruebas de base de datos.
- [x] 4.4 Agregar los `CHECK` constraints de estados permitidos, montos no negativos, porcentajes entre 0 y 100 y fechas coherentes; verificar que una inserción inválida es rechazada por PostgreSQL.
- [x] 4.5 Implementar el trigger que bloquea `UPDATE` y `DELETE` sobre `historial_estado_beca`; verificar con pruebas que ambas operaciones fallan y que `INSERT` funciona.
- [x] 4.6 Crear las tablas de proyección `programa_beca_ref`, `costo_matricula_ref` y `convenio_ref` con `ultimo_evento_id` y marcas de actualización; verificar que el upsert es idempotente.
- [x] 4.7 Crear la tabla `idempotency_request` con índice único por `(endpoint, clave)`, hash de payload, estado, respuesta y expiración; verificar que el índice impide duplicados.
- [x] 4.8 Configurar las entidades de Outbox/Inbox de MassTransit en la migración y el esquema `hangfire` en el arranque; verificar que las tablas de Outbox/Inbox y las tablas de Hangfire existen y son utilizables.
- [x] 4.9 Implementar `BecaRepository`, `OfertaBecaRepository`, `CompromisoBecarioRepository` e `IdempotencyRepository` con `Include` de owned types y entidades hijas; verificar con pruebas de integración que reconstruyen el agregado completo.
- [x] 4.10 Implementar los métodos de bloqueo pesimista `ObtenerBecaParaActualizarAsync` (`SELECT ... FOR UPDATE`) y `AdquirirBloqueoEstudianteAsync` (`pg_advisory_xact_lock`); verificar que dos transacciones concurrentes se serializan.
- [x] 4.11 Crear la migración `InitialCreate` y aplicarla sobre una base PostgreSQL limpia; verificar que `dotnet ef database update` termina sin errores y que una segunda ejecución es idempotente.
- [x] 4.12 Agregar una prueba de integración que cree la base desde cero, aplique todas las migraciones y valide tablas, índices, checks y trigger; verificar que pasa en Testcontainers.

## 5. Seguridad, idempotencia y errores transversales

> **Trazabilidad:** transversal. Protege las HUs de otorgamiento y deja preparadas las operaciones sensibles de RF-20 y RF-22 (OTP, 2FA, idempotencia).

- [x] 5.1 Configurar autenticación JWT Bearer contra Keycloak con validación de emisor, audiencia, firma, `MapInboundClaims=false`, roles y `RoleClaimType=roles`; verificar con un token de prueba válido e inválido.
- [x] 5.2 Implementar la política `Requiere2FA` que valida `acr` configurable o `amr` con `otp`/`totp`; verificar que los roles administrativos sin segundo factor reciben `401 SEGUNDO_FACTOR_REQUERIDO`.
- [x] 5.3 Implementar la política de alcance por programa (`programa_ids`) y la autorización de propiedad por `postulante_id`; verificar que un coordinador sin alcance recibe `403` y que un postulante no puede operar sobre una oferta ajena.
- [x] 5.4 Implementar la política de *Service Account* para `/internal/v1/...`, con validación de `client_id` permitido y scope `becas.internal.read`; verificar que una llamada sin token o con scope insuficiente recibe `401`/`403`.
- [x] 5.5 Implementar el middleware de idempotencia con hash canónico del payload, almacenamiento transaccional y TTL de 7 días; verificar los casos de reintento exitoso, payload distinto y solicitud en proceso.
- [x] 5.6 Implementar el middleware de `ProblemDetails` y el mapeo de excepciones de dominio a códigos HTTP; verificar cada mapeo con pruebas de API.
- [x] 5.7 Implementar el middleware de correlación que acepte o genere `X-Correlation-Id` y lo propague a logs, clientes HTTP y eventos; verificar que el identificador se conserva en la respuesta.
- [x] 5.8 Implementar la redacción de datos sensibles en logs (OTP, cuentas, documentos, correos); verificar con pruebas que los valores nunca aparecen en la salida.

## 6. Capa de aplicación y casos de uso

> **Trazabilidad:** RF-15, RF-16 y RF-17. HUs: HU-15.1–HU-15.3 (emitir, anular y expirar), HU-16.1–HU-16.2 (aceptar y rechazar) y HU-17.1–HU-17.2 (cargar y revisar el compromiso).

- [x] 6.1 Implementar `ICommandDispatcher`/`IQueryDispatcher` y el registro por ensamblado de handlers; verificar que un comando de prueba se despacha y que un handler ausente produce un error claro.
- [x] 6.2 Crear los DTOs y validadores de `EmitirOfertaBeca`, `AceptarOfertaBeca`, `RechazarOfertaBeca`, `AnularOfertaBeca`, `CargarCompromisoBecario`, `RevisarCompromisoBecario` y `ConsultarBecasActivas`; verificar con pruebas de validación de campos obligatorios, formatos y longitudes.
- [x] 6.3 Implementar la orquestación de `EmitirOfertaBecaHandler`: validar proyecciones de programa/costo/convenio, consultar adjudicabilidad, calcular montos y solicitar la reserva; verificar con dobles de prueba los caminos exitoso, no adjudicable, configuración faltante y fallo de reserva.
- [x] 6.4 Completar `EmitirOfertaBecaHandler` con la creación del agregado, generación de carta, subida a almacenamiento, persistencia, programación del job y publicación del evento; verificar con una prueba de integración que no queda estado parcial si falla la subida o la persistencia.
- [x] 6.5 Implementar `AceptarOfertaBecaHandler` con transacción `Serializable`, bloqueo por estudiante, bloqueo de fila, validación de vencimiento, no doble beneficio y transición a `ACTIVA`; verificar con pruebas de aceptación exitosa, oferta vencida y doble beneficio.
- [x] 6.6 Implementar `RechazarOfertaBecaHandler` y `AnularOfertaBecaHandler` con transición terminal, `tipoCierre`, liberación por evento y validación de justificación; verificar con pruebas de éxito, estado inválido y justificación insuficiente.
- [x] 6.7 Implementar `ExpirarOfertaBecaHandler` reutilizable por el job y la reconciliación, con relectura y bloqueo de fila; verificar que una oferta ya respondida no cambia y que una pendiente vencida expira.
- [x] 6.8 Implementar `CargarCompromisoBecarioHandler` y `RevisarCompromisoBecarioHandler`, incluyendo validación de PDF, tamaño máximo, hash, intentos y actualización de `habilitadaParaDesembolso`; verificar con pruebas de carga, aprobación, rechazo y nueva carga.
- [x] 6.9 Implementar las consultas `ObtenerOferta`, `ObtenerBeca` y `ConsultarBecasActivas` con DTOs de lectura autorizados; verificar que no exponen datos personales ni bancarios en la respuesta interna.
- [x] 6.10 Integrar la idempotencia en el pipeline de comandos y asegurar que la fila de idempotencia se confirma o revierte junto con el cambio de estado; verificar con pruebas de reintento y de fallo transaccional.
- [x] 6.11 Implementar la coordinación de transacción y Outbox: publicar eventos antes de `SaveChanges` y confirmar estado + mensaje en el mismo commit; verificar con una prueba que un fallo de `SaveChanges` no deja evento publicado.

## 7. Clientes de integración y contratos

> **Trazabilidad:** RF-15, RF-16 y RF-17. HUs: HU-15.1–HU-15.3 (emisión, anulación y expiración) y HU-16.1–HU-16.2 (aceptación y rechazo). Coordinación con `MS-POS` para adjudicabilidad y postulación, con `MS-PRG` para cupo/presupuesto y con `MS-PLT` para OTP. Las deudas de contrato detectadas se cierran en la sección 15.

- [x] 7.1 Implementar `PostulacionesClient` para `GET /internal/v1/postulaciones/{id}/adjudicable` con *Service Token*, timeout de 3 s, propagación de correlación y mapeo de `404`/`false`/timeout; verificar con WireMock.
- [x] 7.2 Implementar `ProgramasClient` para `POST /internal/v1/programas/{id}/reservas` con `Idempotency-Key: oferta-{id}`, timeout de 3 s y mapeo de `409 SIN_CUPO_O_PRESUPUESTO` y `422 CONVENIO_VENCIDO`; verificar con WireMock que la reserva nace en `PENDIENTE`, que el reintento usa la misma clave y que los errores se mapean.
- [x] 7.3 Implementar `OtpClient` para `POST /internal/v1/otp/consumir` con el contrato documentado, sin invocarlo aún desde los casos de uso; verificar con WireMock los códigos de éxito, expiración y bloqueo.
- [x] 7.4 Implementar el proveedor de *Service Tokens* con *client credentials* de Keycloak, caché en memoria y renovación antes de expirar; verificar con un servidor OIDC simulado que no solicita un token nuevo en cada llamada.
- [x] 7.5 Configurar MassTransit con RabbitMQ, exchange/queue naming, reintentos, cola de error, *Transactional Outbox* e *Idempotent Inbox*; verificar con Testcontainers que un mensaje publicado se persiste en Outbox y llega al consumidor.
- [x] 7.6 Implementar los consumidores de `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido` con upsert idempotente y control de `occurredAt`/`ultimo_evento_id`; verificar que un evento duplicado o desactualizado no retrocede la proyección.
- [x] 7.7 Definir los contratos de eventos de salida (`OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado`, `CompromisoBecarioRechazado`) con el sobre `EventoIntegracion<T>`; verificar con pruebas de serialización que no incluyen PII.
- [x] 7.8 Implementar la publicación de los eventos de salida desde los handlers y verificar que cada transición publica exactamente el evento correspondiente.
- [x] 7.9 Implementar una métrica y alerta de configuración faltante (programa, costo o convenio) y documentar el procedimiento para que `MS-PRG` reemita el evento; verificar que la emisión se bloquea con `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`.

## 8. API HTTP

> **Trazabilidad:** RF-15, RF-16 y RF-17. HUs: HU-15.1–HU-15.3, HU-16.1–HU-16.2 y HU-17.1–HU-17.2. Expone las historias de emisión, consulta, respuesta, anulación y compromiso con autorización por rol y alcance.

- [x] 8.1 Implementar `OfertasBecaController` con las rutas `POST /api/v1/becas/ofertas`, `GET /api/v1/becas/ofertas/{id}`, `POST .../aceptar`, `POST .../rechazar` y `POST .../anular`; verificar que OpenAPI las expone con los códigos de respuesta correctos.
- [x] 8.2 Implementar `BecasController` con `GET /api/v1/becas/{id}` aplicando autorización de dueño o coordinador; verificar `200`, `403` y `404`.
- [x] 8.3 Implementar `CompromisosController` con carga multipart y revisión; verificar que acepta solo PDF hasta 5 MB y rechaza otros tipos con `400`.
- [x] 8.4 Implementar `InternalBecasController` con `GET /internal/v1/becas/activas`, validando *Service Token* y devolviendo solo los campos permitidos; verificar que no expone PII.
- [x] 8.5 Configurar `ProblemDetails`, validación automática de DTOs y respuestas consistentes en todos los controladores; verificar con pruebas de API para `400`, `401`, `403`, `409` y `422`.
- [x] 8.6 Configurar OpenAPI/Swagger con esquemas de seguridad, ejemplos de request/response y documentación de `Idempotency-Key`; verificar que `/swagger/v1/swagger.json` es válido y contiene todas las rutas.
- [x] 8.7 Configurar los health checks `/health/live` y `/health/ready` con PostgreSQL, RabbitMQ y migraciones; verificar `200` con dependencias sanas y `503` con PostgreSQL detenido.

## 9. Hangfire y trabajos en segundo plano

> **Trazabilidad:** RF-15 y RF-16. HU-15.2 (expiración de ofertas) y base para RF-26 Historia 2 (conclusión de becas). La conclusión y la reconciliación de renovaciones se completan en la sección 19.

- [x] 9.1 Configurar Hangfire con PostgreSQL, esquema `hangfire`, cola `critical` y dashboard protegido; verificar que el dashboard responde `401/403` sin autorización y `200` con rol administrativo.
- [x] 9.2 Implementar `ExpirarOfertaBecaJob` y programarlo al emitir la oferta, guardando el id en `referenciaJobExpiracionHangfireId`; verificar con una prueba de integración que una oferta pendiente expira y una aceptada no.
- [x] 9.3 Implementar `ReconciliarOfertasPendientesJob` recurrente cada 5 minutos como red de seguridad; verificar que detecta ofertas vencidas sin job o con job perdido.
- [x] 9.4 Implementar los jobs de limpieza de idempotencia y Outbox con retención configurable; verificar que eliminan solo registros vencidos.
- [x] 9.5 Verificar la persistencia de jobs tras reiniciar el contenedor de la API; comprobar que la oferta sigue expirando después del reinicio.

## 10. Archivos, carta y compromiso

> **Trazabilidad:** RF-15 (carta de otorgamiento) y RF-17 (compromiso firmado). HUs: HU-15.1 (carta) y HU-17.1 (plantilla y carga). La plantilla del compromiso se genera al emitir la oferta, se almacena en una ruta inmutable por beca y se expone al becario; solo sus metadatos se persisten.

- [x] 10.1 Implementar `IAlmacenamientoArchivos` con adaptador local para desarrollo y adaptador Firebase/Google Cloud Storage para producción; verificar que ambos calculan SHA-256 y devuelven URL, hash y tamaño.
- [x] 10.2 Implementar `ICartaOfertaPdfGenerator` con PdfSharpCore y una plantilla que incluya fundación, estudiante, programa, periodo, cobertura, monto y vigencia; verificar que el PDF generado tiene cabecera `%PDF` y hash reproducible.
- [x] 10.3 Integrar la generación y subida de la carta en la emisión; verificar que si la subida falla no se persiste la oferta y que si se persiste la URL/hash son los del archivo subido.
- [x] 10.4 Implementar la validación de archivos de compromiso (PDF, 5 MB, hash, intento) y la ruta inmutable en Firebase; verificar con pruebas de archivo válido, tipo inválido, tamaño excedido y reintento tras rechazo.
- [x] 10.5 Verificar que los binarios nunca se guardan en PostgreSQL y que solo se persisten `url`, `hash`, `tamaño` y `tipoMime`.
- [x] 10.6 Generar y almacenar la plantilla del compromiso (`urlPlantillaGeneradaFirebase`) con las condiciones del programa y de la beca; verificar que el becario descarga la plantilla correcta, que la ruta es inmutable y que el binario no se guarda en PostgreSQL.

## 11. Observabilidad y operación

> **Trazabilidad:** transversal. Da soporte a todas las HUs y a la contribución de `MS-BEC` a RF-18/RF-35.

- [x] 11.1 Configurar Serilog con salida JSON, enriquecedores de servicio, correlación y redacción; verificar que un log de prueba incluye `CorrelationId` y no incluye datos sensibles.
- [x] 11.2 Configurar OpenTelemetry para ASP.NET Core, HttpClient, Npgsql, MassTransit y Hangfire, con exportador OTLP configurable; verificar con un exportador de prueba que se generan trazas y métricas.
- [x] 11.3 Agregar métricas de negocio (`ofertas_emitidas`, `ofertas_aceptadas`, `ofertas_rechazadas`, `ofertas_expiradas`, `compromisos_pendientes`, `outbox_pendientes`); verificar que se incrementan tras las operaciones correspondientes.
- [x] 11.4 Configurar logs y health checks para no exponer cadenas de conexión, secretos ni stack traces en producción; verificar con una prueba de configuración `Production`.

## 12. Pruebas de integración y calidad

> **Trazabilidad:** verificación de las HUs implementadas en las secciones 3–10. Las pruebas de las secciones 16–20 se agregan en cada cambio de continuidad.

- [x] 12.1 Ejecutar la suite de dominio y verificar que cubre todas las transiciones, invariantes, snapshots y cálculos de montos con cobertura ≥ 80%.
- [x] 12.2 Ejecutar la suite de aplicación con dobles de repositorios, clientes, reloj y almacenamiento; verificar los caminos felices y los fallos de cada handler.
- [x] 12.3 Ejecutar las pruebas de arquitectura y verificar que `Domain` no referencia infraestructura y que `Application` no referencia `Infrastructure`/`API`.
- [x] 12.4 Ejecutar las pruebas de integración de persistencia con Testcontainers PostgreSQL: migraciones, índices únicos, trigger de historial, concurrencia optimista y bloqueo por estudiante.
- [x] 12.5 Ejecutar una prueba end-to-end de API: emitir oferta con WireMock de POS/PRG, aceptar, consultar beca activa y verificar el estado final y los eventos.
- [x] 12.6 Ejecutar una prueba end-to-end de rechazo, anulación y expiración con Hangfire y verificar que se publica el evento de liberación correspondiente y que la beca queda `REVOCADA` con el `tipoCierre` correcto.
- [x] 12.7 Ejecutar una prueba de concurrencia con dos aceptaciones simultáneas del mismo estudiante y verificar que solo una gana y la otra recibe `409 DOBLE_BENEFICIO`.
- [x] 12.8 Ejecutar una prueba de mensajería con RabbitMQ/Testcontainers: evento en Outbox, entrega, duplicado e Inbox; verificar que no hay efectos duplicados.
- [x] 12.9 Ejecutar pruebas de seguridad: token sin 2FA, sin `postulante_id`, sin `programa_ids`, servicio interno sin token y postulante operando sobre oferta ajena; verificar `401`/`403`.
- [x] 12.10 Ejecutar `dotnet format --verify-no-changes`, `dotnet build -warnaserror` y `dotnet test` sobre toda la solución; verificar que el pipeline local queda verde.
- [x] 12.11 Verificar la cobertura global y el umbral de 80% en `Domain` + `Application`; ajustar pruebas si el reporte queda por debajo.

## 13. Entorno local, contenedores y CI

> **Trazabilidad:** transversal. Es la base de ejecución de todas las HUs y de las pruebas de contrato con los demás microservicios.

- [x] 13.1 Crear `compose.yaml` con PostgreSQL 17, RabbitMQ 4 (management), Keycloak 26 y un perfil opcional para la API; verificar con `docker compose config` y `docker compose up -d` que los healthchecks quedan sanos.
- [x] 13.2 Crear `deploy/keycloak/ascenso-realm.json` con realm, clientes `ms-bec` y `ms-bec-internal`, roles base, usuarios de prueba y mappers de claims; verificar la obtención de un token con `curl` y que incluye `roles`, `postulante_id` y `programa_ids`.
- [x] 13.3 Crear `Makefile` con `up`, `down`, `migrate`, `run`, `test` y `format`, y `.env.example` sin secretos; verificar cada objetivo en una copia limpia.
- [x] 13.4 Crear el `Dockerfile` multi-stage con usuario no root, variables de entorno y `HEALTHCHECK`; verificar `docker build` y un arranque contra el compose local.
- [x] 13.5 Crear el pipeline de GitHub Actions con restore bloqueado, build `-warnaserror`, formato, pruebas unitarias/arquitectura, pruebas de integración con Docker, cobertura y build de imagen; verificar que el workflow es válido y que falla si se rompe una regla de arquitectura.
- [x] 13.6 Crear un script de arranque limpio que cree la base, aplique migraciones y cargue el realm de Keycloak; verificar que un entorno nuevo queda operativo con un solo comando.
- [x] 13.7 Documentar en el README el flujo de desarrollo, las variables de entorno, la obtención de tokens y la solución de problemas comunes; verificar que un integrante nuevo puede levantar el servicio siguiendo solo la documentación.
- [x] 13.8 Crear un servicio `dev-stubs` en Compose (WireMock o API mínima) que simule `MS-POS` y `MS-PRG`, con datos semilla de postulación adjudicable y respuesta de reserva; verificar que el flujo de emisión funciona de punta a punta sin depender de los microservicios reales.
- [x] 13.9 Dejar el almacenamiento local como única opción de desarrollo y CI, retirando el servicio Docker y las pruebas de almacenamiento simulado; verificar que no se realizan llamadas cloud.
- [x] 13.10 Documentar el almacenamiento local predeterminado y la preparación del adaptador Google Cloud Storage sin conexión real, bucket ni credenciales en el repositorio.
- [x] 13.11 Mantener la selección explícita `Local`/`Firebase` por entorno, con `Local` como valor predeterminado; documentar que Cloud Storage for Firebase requiere Blaze con cuotas sin cargo sujetas a límites y facturación habilitada.
- [x] 13.12 Conservar referencias `gs://` estables y la generación de URL firmada cloud de máximo 15 minutos en el adaptador; probarlo con un doble aislado sin contactar servicios reales.
- [x] 13.13 Actualizar las respuestas de cartas y compromisos para resolver enlaces solo después de validar JWT Keycloak y propiedad/alcance; agregar pruebas que aseguren que una petición no autorizada no recibe enlace, y documentar el comportamiento de la API.
- [x] 13.14 Conservar el archivo de Firebase Security Rules para una publicación futura, documentar IAM mínimo y que las reglas no están publicadas; no ejecutar pruebas contra un proyecto real.
- [x] 13.15 Ejecutar `dotnet test` (con build), validar Compose y ejecutar validación estricta OpenSpec con almacenamiento local y pruebas aisladas del adaptador cloud; confirmar que CI no usa credenciales ni contacta proyectos reales.

## 14. Documentación de contratos y cierre del cambio

> **Trazabilidad:** contratos de RF-15/16/17 y transición al plan de continuidad de las secciones 15–22.

- [x] 14.1 Publicar la especificación OpenAPI de los endpoints públicos e internos y los contratos de eventos (AsyncAPI o Markdown) en `docs/`; verificar que coinciden con las rutas y DTOs implementados.
- [ ] 14.2 Documentar el contrato de claims de Keycloak (`roles`, `postulante_id`, `programa_ids`, `acr`/`amr`) y las responsabilidades de `MS-PLT`; verificar que el equipo de plataforma lo revisa.
- [ ] 14.3 Documentar los contratos de `adjudicable` y `reservas` con `MS-POS` y `MS-PRG`, incluyendo ejemplos y códigos de error; verificar que los equipos consumidores los validan.
- [x] 14.4 Ejecutar `openspec validate tech-bootstrap-int4-msbeca --strict` y corregir cualquier observación; verificar que el cambio queda listo para aplicar.
- [x] 14.5 Verificar que este cambio no implemente desembolsos, seguimiento, renovación ni revocación definitiva, y que el alcance completo de `MS-BEC` quede planificado y trazado en las secciones 15–22.

---

# Plan de continuidad de `MS-BEC` (fuera de este cambio)

> Las secciones 15–22 no forman parte de `tech-bootstrap-int4-msbeca`. Se ejecutarán en cambios posteriores y mantienen la misma arquitectura, seguridad, idempotencia, Outbox/Inbox y reglas de prueba.

## 15. Cierre de integraciones del otorgamiento (RF-15, RF-16, RF-17)

> **Trazabilidad:** RF-15, RF-16 y RF-17. HUs: HU-15.1 (emisión, transición de la postulación y OTP), HU-15.2 (expiración y contrato de liberación de la reserva), HU-15.3 (anulación expresa y cancelación del job), HU-16.1 (aceptación con OTP y cancelación del job) y HU-16.2 (rechazo, cancelación del job y promoción externa).
>
> **Mapa de deudas:** tarea 15.1 → HU-15.1; 15.2 → HU-15.1 (condiciones de emisión; prepara RF-26); 15.3 → HU-15.1 (periodo de emisión; prepara RF-24, RF-26 y RF-27); 15.4 → HU-15.1, HU-15.2, HU-15.3, HU-16.1 y HU-16.2; 15.5 → HU-15.1, HU-15.3 y HU-16.1; 15.7 → HU-15.3, HU-16.1 y HU-16.2.
>
> **Adaptación de lo existente:** se reutilizan `EmitirOfertaBecaHandler`, `PostulacionesClient`, `ProgramasClient`, `OtpClient`, `CompromisoBecario` y las proyecciones locales. No se reescribe el flujo: se completan los contratos que el DDD exige y se documentan las equivalencias.

- [ ] 15.1 **[HU-15.1]** Implementar `POST /internal/v1/postulaciones/{postulacionId}/otorgar` en `PostulacionesClient`, con `Idempotency-Key: otorgar-{ofertaId}`, timeout de 3 s, propagación de correlación y mapeo de `409`/`422`/`503`; invocarlo en `EmitirOfertaBecaHandler` dentro de la misma unidad de trabajo lógica que la emisión, con compensación o reintento idempotente si la llamada falla; verificar con WireMock que un fallo no deja una oferta persistida sin la postulación otorgada.
- [ ] 15.2 **[HU-15.1]** Implementar `GET /internal/v1/programas/{id}/condiciones?carreraId=&periodoId=` en `ProgramasClient` y usarlo al emitir ofertas y renovaciones para obtener cobertura, arancel vigente, vigencia del convenio y condiciones de permanencia; mantener las proyecciones locales como caché y verificar con WireMock los casos de arancel vigente, convenio vencido y configuración faltante.
- [ ] 15.3 **[HU-15.1]** Implementar `GET /internal/v1/periodos-academicos/actual` en un cliente de `MS-PLT` y resolver el periodo vigente para emisión, renovación, reportes y pausas; verificar el fallo cerrado `503` y la coherencia con `PeriodoVigenteActualId`.
- [ ] 15.4 **[HU-15.1, HU-15.2, HU-15.3, HU-16.1, HU-16.2]** Cerrar el contrato del ciclo de vida de la reserva: implementar en `ProgramasClient` las operaciones `confirmar`, `activar` y `liberar`; si se mantiene la sustitución por los eventos `OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada` y `OfertaBecaExpirada`, registrar la desviación en `design.md` y probar que el ciclo `PENDIENTE → CONFIRMADA → ACTIVADA → LIBERADA` no deja reservas huérfanas.
- [ ] 15.5 **[HU-15.1, HU-15.3, HU-16.1]** Propagar y consumir la autorización OTP de emisión y anulación según la política del programa: persistir `AutorizacionOtpEmisionId` y, si se adopta OTP de anulación, agregar el campo y la migración correspondientes en `OfertaBeca`; verificar que, cuando el OTP es obligatorio, sin código no hay cambio de estado.
- [ ] 15.6 **[Todas las anteriores]** Agregar pruebas de contrato de las integraciones 15.1–15.5 y actualizar OpenAPI/`docs/`; verificar que `MS-POS`, `MS-PRG` y `MS-PLT` validan los ejemplos y que `dotnet test` cubre los caminos de error.
- [ ] 15.7 **[HU-15.3, HU-16.1, HU-16.2]** Cancelar `referenciaJobExpiracionHangfireId` en `AceptarOfertaBecaHandler`, `RechazarOfertaBecaHandler` y `AnularOfertaBecaHandler`; verificar que no queda un job activo tras la respuesta o anulación y que el job sigue siendo idempotente como red de seguridad si la cancelación falla.

## 16. Desembolsos y datos de pago (RF-19, RF-20; alimenta RF-23)

> **Trazabilidad:** RF-19 (programación de desembolsos), RF-20 (registro y confirmación de pagos con OTP) y los movimientos contables que alimentan el reporte financiero RF-23 de `MS-PRG`/`MS-AUD`. HUs: RF-19 Historia 1–2 y RF-20 Historia 1–3.
>
> **Cobertura de HUs:** RF-19 Historia 1 → 16.1–16.2 y 16.13 (regla de pago y snapshot de la cobertura consultada a `MS-PRG`). RF-19 Historia 2 → 16.1–16.3, 16.7–16.8, 16.11 y 16.13–16.14. RF-20 Historia 1 → 16.3, 16.8, 16.11 y 16.14. RF-20 Historia 2 → 16.5, 16.8, 16.11 y 16.14. RF-20 Historia 3 → 16.9, 16.11 y 16.14.
>
> **Adaptación de lo existente:** se reutilizan `Beca.MontoComprometidoPeriodoActual`, `Beca.MontoDesembolsadoPeriodoActual`, `Beca.HabilitadaParaDesembolso`, `OfertaBeca.MontoOfertaPorPeriodo`, `PoliticaCobertura`, `HistorialEstadoBeca`, `IAlmacenamientoArchivos`, `OtpClient`, `ProgramasClient` y el Outbox. Se agregan los agregados y entidades que faltan, sin duplicar el modelo de otorgamiento.

- [ ] 16.1 Crear las enumeraciones `FrecuenciaDesembolso`, `DestinatarioPago`, `MetodoPago`, `EstadoPlanDesembolso`, `EstadoDatosPago`, `EstadoDesembolso`, `TipoIncidenciaDesembolso` y `EstadoIncidencia` con los valores del DDD/PRD; verificar con pruebas que los códigos persistidos y los contratos coinciden literalmente.
- [ ] 16.2 Implementar `CalendarioDesembolso` como agregado raíz con `periodoAcademicoId`, `frecuencia`, `destinatarioPredeterminado`, `montoMaximoAprobadoPeriodo`, `sumaCuotasProgramadas`, `numeroCuotas`, fechas y `estadoPlan`; validar `sumaCuotasProgramadas <= montoMaximoAprobadoPeriodo` (RN-BEC-03) y fechas coherentes; verificar con pruebas de límites y montos negativos.
- [ ] 16.3 Implementar `Desembolso` como entidad hija con número de cuota, montos programado/ejecutado, fechas, destinatario, `datosPagoBecarioId`, snapshot de cuenta, método, estado, intentos, referencia bancaria, observación, actores, OTP y vínculos a suspensión/pausa/reintento; implementar la máquina `PROGRAMADO → EN_PROCESO → PAGADO_CONFIRMADO / FALLIDO / DEVUELTO`, `SUSPENDIDO` y `CANCELADO`; verificar todas las duplas permitidas y prohibidas.
- [ ] 16.4 Implementar `DatosPagoBecario` como agregado raíz con banco, tipo de cuenta, número cifrado con AES-GCM, documento del titular, estado, actor y OTP de modificación; verificar que el número nunca se persiste en claro, que la clave se obtiene de un secreto y que se conserva el historial de cuentas reemplazadas.
- [ ] 16.5 Implementar `IncidenciaDesembolso` con tipo, motivo, observación bancaria, estado, fechas y actores; exigir observación bancaria en `FALLIDO`/`DEVUELTO` y resolverla antes de reintentar; verificar con pruebas de incidencia abierta, resuelta y reintento.
- [ ] 16.6 Adaptar el agregado `Beca` para exponer la compuerta `RN-BEC-02` (`412 COMPROMISO_NO_FIRMADO`), actualizar `MontoDesembolsadoPeriodoActual` al confirmar y `MontoComprometidoPeriodoActual` al suspender/revocar; verificar que no se desembolsa sin compromiso aprobado y que los montos no superan lo aprobado.
- [ ] 16.7 Persistir `calendario_desembolso`, `desembolso`, `datos_pago_becario` e `incidencia_desembolso` con `snake_case`, `numeric(18,2)`, `uuid`, `timestamptz`, enums `varchar`, `CHECK` de estados/montos, índice único `(calendario_desembolso_id, numero_cuota)` y `version`; crear la migración y verificar que una inserción inválida es rechazada por PostgreSQL.
- [ ] 16.8 Implementar los comandos `ProgramarCalendarioDesembolso`, `ActualizarDatosPagoBecario`, `RegistrarEjecucionDesembolso`, `ConfirmarDesembolso`, `RegistrarIncidenciaDesembolso`, `ResolverIncidenciaDesembolso` y `ReintentarDesembolso`, y las consultas `ObtenerCalendarioDesembolsos` y `ObtenerDesembolsos`; agregar validadores y verificar caminos felices y errores de estado/monto.
- [ ] 16.9 Integrar `IOtpClient` en `ConfirmarDesembolso` (`TipoOperacionOTP.ConfirmarDesembolso`) y en `ActualizarDatosPagoBecario` (`TipoOperacionOTP.ModificarDatosPago`): construir la huella `(usuarioId, operacion, recursoTipo, recursoId, huellaPayload)`, consumir el OTP en la misma transacción lógica y bloquear la fila de `Beca` con `SELECT ... FOR UPDATE` antes de confirmar; verificar con WireMock los casos de éxito, expiración y bloqueo, y la concurrencia con una revocación.
- [ ] 16.10 Implementar en `ProgramasClient` `POST /internal/v1/programas/{id}/movimientos-presupuesto` con `Idempotency-Key` y publicar los movimientos de desembolso confirmado, suspensión, reanudación, revocación y conclusión; verificar con WireMock que un reintento no duplica el movimiento y que un fallo no deja el pago confirmado sin registro contable.
- [ ] 16.11 Exponer los endpoints de calendario, datos de pago, ejecución, confirmación e incidencias con roles `ADMIN_FINANCIERO`/`COORDINADOR_PROGRAMA`, 2FA y autorización por programa; verificar `201`, `200`, `400`, `403`, `409`, `412` y `422` con pruebas de API.
- [ ] 16.12 Definir y publicar los eventos `DesembolsoProgramado`, `DesembolsoConfirmadoConOTP`, `DesembolsoFallido`, `DesembolsoDevuelto`, `DesembolsoReintentado` y `DesembolsoCancelado` con el sobre `EventoIntegracion<T>`; verificar que no incluyen cuentas bancarias, documentos ni montos socioeconómicos.
- [ ] 16.13 Generar el calendario de desembolsos al activarse la beca y al aprobarse una renovación, tomando el monto aprobado y la frecuencia de la configuración del programa; verificar que una beca activa sin calendario se detecta y se corrige de forma idempotente.
- [ ] 16.14 Agregar la prueba end-to-end de desembolsos: programar calendario, ejecutar, fallar, devolver, reintentar, confirmar con OTP, registrar el movimiento en `MS-PRG` y verificar `422 EXCEDE_OFERTA` al superar el techo; verificar también la concurrencia entre confirmación y revocación.

## 17. Suspensión, reanudación y revocación definitiva (RF-21, RF-22)

> **Trazabilidad:** RF-21 (suspensión temporal sin cancelar la beca) y RF-22 (revocación definitiva con OTP y liberación de cupo/presupuesto). HUs: RF-21 Historia 1–2 y RF-22 Historia 1–2.
>
> **Cobertura de HUs:** RF-21 Historia 1 → 17.1–17.3, 17.5, 17.7 y 17.9. RF-21 Historia 2 → 17.2, 17.5 y 21.3–21.4. RF-22 Historia 1 → 17.4–17.7 y 17.9. RF-22 Historia 2 → 17.4, 17.6, 17.9 y 16.10. HU-27B reutiliza 17.2–17.3 para suspender y reactivar la pausa.
>
> **Adaptación de lo existente:** `Beca.Suspender`, `Beca.Reactivar` y `Beca.Revocar` ya existen con transiciones y justificación mínima. Se adaptan para crear `SuspensionDesembolso`, mover cuotas, exigir OTP en la revocación, liberar saldo y emitir los eventos que hoy faltan.

- [ ] 17.1 Crear las enumeraciones `TipoSuspensionBeca` y `EstadoSuspension` e implementar `SuspensionDesembolso` como entidad hija de `Beca` con tipo, motivo, requisitos para reanudar, fecha de inicio, fecha de revisión o `esIndefinida`, estado, coordinador que registra y coordinador que levanta; verificar con pruebas de suspensión con fecha e indefinida.
- [ ] 17.2 Adaptar `Beca.Suspender` para crear la `SuspensionDesembolso`, detener las cuotas `PROGRAMADO → SUSPENDIDO`, transferir el saldo pendiente y el cupo a las bolsas de suspensión y publicar `DesembolsosBecaSuspendidos`; verificar que no se cancelan las cuotas ni se pierde la beca.
- [ ] 17.3 Adaptar `Beca.Reactivar` para restaurar las cuotas `SUSPENDIDO → PROGRAMADO`, devolver el saldo/cupo a activos y publicar `DesembolsosBecaReanudados`; si la suspensión proviene de una pausa, exigir el reporte académico y la evaluación de permanencia de la sección 20.
- [ ] 17.4 Extender `Beca.Revocar` para aceptar `OTORGADA`, `ACTIVA` y `SUSPENDIDA`, exigir `AutorizacionOTP` con `TipoOperacionOTP.RevocarBeca`, fijar `tipoCierre = REVOCACION_DEFINITIVA`, cancelar cuotas futuras `PROGRAMADO`/`SUSPENDIDO` y no generar deuda por lo ya pagado; verificar con pruebas de cada estado de origen y de OTP ausente/inválido.
- [ ] 17.5 Implementar los handlers `SuspenderBeca`, `ReanudarBeca` y `RevocarBecaDefinitivamente` con bloqueo `SELECT ... FOR UPDATE` sobre `Beca` (C4), transacción local, justificación y OTP cuando aplique; verificar la concurrencia entre confirmar un desembolso y revocar la beca.
- [ ] 17.6 Implementar en `ProgramasClient` los movimientos de suspensión, reanudación y revocación (saldo no ejecutado y cupo), y publicar `BecaRevocadaDefinitivamente`; verificar que `MS-PRG` recibe el movimiento y que un reintento es idempotente.
- [ ] 17.7 Exponer los endpoints `POST /api/v1/becas/{id}/suspender`, `POST /api/v1/becas/{id}/reanudar` y `POST /api/v1/becas/{id}/revocar` con roles `COORDINADOR_PROGRAMA`/`DIRECCION`, 2FA y autorización por programa; verificar `200`, `400`, `403`, `409` y `412`.
- [ ] 17.8 Agregar un job de revisión de suspensiones (`RevisarSuspensionesJob`) que alerte las fechas de revisión vencidas y las indefinidas; verificar que no reactiva automáticamente sin decisión del coordinador.
- [ ] 17.9 Agregar las pruebas end-to-end de suspensión, reanudación y revocación: detener cuotas, transferir saldos, confirmar que no se paga una cuota suspendida, revocar con OTP y verificar que las cuotas futuras quedan canceladas y el cupo liberado.

## 18. Seguimiento académico y permanencia (RF-24, RF-25)

> **Trazabilidad:** RF-24 (reportes académicos periódicos) y RF-25 (evaluación de condiciones de permanencia). HUs: RF-24 Historia 1–2 y RF-25 Historia 1–2.
>
> **Cobertura de HUs:** RF-24 Historia 1 → 18.1–18.2, 18.4–18.6 y 18.9–18.11. RF-24 Historia 2 → 18.6–18.7, 18.11 y 21.3. RF-25 Historia 1 → 18.3–18.5, 18.8 y 18.10–18.11. RF-25 Historia 2 → 18.8 y 18.10–18.11.
>
> **Adaptación de lo existente:** `CondicionesPermanencia` y `ConsecuenciaPermanencia` ya existen en el dominio y `OfertaBeca` guarda el snapshot de condiciones. Se reutilizan para comparar el reporte contra la fotografía del programa y para recomendar la consecuencia.

- [ ] 18.1 Crear las enumeraciones `EstadoMatricula`, `FuenteReporte`, `EstadoReporteAcademico`, `TipoCondicionPermanencia`, `ConsecuenciaPermanencia` y `EstadoConsecuencia` con los valores del DDD/PRD; verificar con pruebas que los códigos y nombres son los oficiales.
- [ ] 18.2 Implementar `ReporteAcademico` como agregado raíz con `becaId`, `periodoAcademicoId`, promedio, materias aprobadas/reprobadas/cursando, créditos, estado de matrícula, URL del comprobante, fuente, universidad, fecha de entrega, fecha límite, `entregaTardia`, estado y revisión; validar rangos y coherencia de materias; verificar con pruebas de reporte válido, tardío y con archivo inválido.
- [ ] 18.3 Implementar `EvaluacionPermanencia` y `CondicionPermanenciaEvaluada` para comparar cada condición del snapshot con el reporte, calcular `cumpleCondiciones`, recomendar `LLAMADO_ATENCION`/`SUSPENSION`/`REVOCACION`, registrar `motivoDecision`, `siguientePasoBecario` y la decisión del coordinador; verificar con pruebas de cumplimiento total, parcial y nulo.
- [ ] 18.4 Persistir `reporte_academico`, `evaluacion_permanencia` y `condicion_permanencia_evaluada` con `snake_case`, enums `varchar`, `CHECK` de rangos y `version`; crear la migración y verificar la reconstrucción del agregado con su evaluación.
- [ ] 18.5 Implementar los comandos `RegistrarReporteAcademico`, `EvaluarPermanencia` y `AplicarConsecuenciaPermanencia`, y las consultas `ObtenerReporteAcademico` y `ObtenerEvaluacionPermanencia`; agregar validadores y verificar los caminos de becario, universidad y coordinador.
- [ ] 18.6 Marcar `entregaTardia = true` cuando la fecha de entrega supere `fechaLimitePeriodo`, publicar `ReporteAcademicoEntregado` y generar la alerta para `MS-PLT`/coordinador; verificar que una entrega a tiempo no genera la alerta.
- [ ] 18.7 Resolver la fecha límite y las condiciones desde la configuración del programa (`CalendarioAcademicoPrograma`/eventos de `MS-PRG`); si falta configuración, fallar cerrado con `422` y no permitir una evaluación con condiciones incompletas; verificar con pruebas de configuración faltante.
- [ ] 18.8 Implementar la aplicación de consecuencias: `LLAMADO_ATENCION` sin cambio de estado, `SUSPENSION` creando la suspensión de la sección 17 y `REVOCACION` con OTP y la revocación de la sección 17; verificar que la consecuencia aplicada coincide con la aprobada por el coordinador.
- [ ] 18.9 Exponer la carga de reportes (becario o universidad) y la evaluación (coordinador) con roles `BECARIO`/`ENLACE_UNIVERSIDAD`/`COORDINADOR_PROGRAMA`, autorización de propiedad y validación de PDF/5 MB/hash; verificar `200`, `201`, `400`, `403` y `409`.
- [ ] 18.10 Definir y publicar `ReporteAcademicoEntregado`, `EvaluacionPermanenciaRegistrada` y `ConsecuenciaPermanenciaAplicada` sin PII socioeconómica; verificar la entrega idempotente y la trazabilidad en `HistorialEstadoBeca`.
- [ ] 18.11 Agregar la prueba end-to-end reporte → evaluación → suspensión/revocación y la prueba de entrega tardía; verificar que la beca conserva el estado correcto y que se registran los eventos de auditoría.

## 19. Renovación y conclusión (RF-26)

> **Trazabilidad:** RF-26 (solicitud y evaluación de renovación; cadena de becas) y la conclusión por vencimiento de la ventana. HUs: RF-26 Historia 1–2.
>
> **Cobertura de HUs:** RF-26 Historia 1 → 19.1–19.3 y 19.7–19.9. RF-26 Historia 2 → 19.2–19.6 y 19.8–19.9.
>
> **Adaptación de lo existente:** `Beca.Renovar`, `Beca.Concluir`, `CadenaBecaId`, `BecaAnteriorId` y `PeriodosCursados` ya existen. Se adaptan para crear la beca sucesora, reservar presupuesto, renovar el snapshot de costo y disparar la conclusión automática.

- [ ] 19.1 Crear la enumeración `EstadoRenovacion` (`SOLICITADA_EN_CURSO`, `APROBADA`, `RECHAZADA`, `EXPIRADA_CONCLUIDA`) e implementar `SolicitudRenovacion` como agregado raíz con `becaId`, periodo origen/solicitado, fecha de solicitud, fecha límite de la ventana, cumplimiento académico, presupuesto disponible, monto y reserva, decisión, `referenciaJobVencimientoId` y job de vencimiento; verificar la máquina de estados completa.
- [ ] 19.2 Implementar la elegibilidad de la renovación: ventana del calendario, `cumpleRendimientoAcademico` a partir de la evaluación de permanencia de la sección 18 y presupuesto disponible consultado a `MS-PRG`; verificar que fuera de ventana, sin cumplimiento o sin presupuesto la solicitud se rechaza sin crear la beca sucesora.
- [ ] 19.3 Implementar los comandos `SolicitarRenovacion`, `EvaluarRenovacion` (aprobar/rechazar) y los jobs `ConcluirBecaJob`/`ReconciliarRenovacionesJob`; verificar con pruebas de ventana abierta, cerrada, aprobación, rechazo y vencimiento sin solicitud.
- [ ] 19.4 Implementar la aprobación: `Beca` actual `ACTIVA → RENOVADA`, creación de la `Beca` sucesora en `ACTIVA` con el mismo `cadenaBecaId`, `becaAnteriorId = actual.id`, `periodosCursados + 1`, snapshot del costo vigente y nuevo calendario/reporte; heredar `habilitadaParaDesembolso` del compromiso aprobado de la beca anterior salvo que el programa exija nueva firma, y verificar la transacción y el índice `ux_beca_cadena_periodo`.
- [ ] 19.5 Implementar la reserva de renovación en `MS-PRG` (`referenciaTipo = RENOVACION`) y su confirmación/activación/liberación; verificar que la aprobación no crea la beca sucesora si la reserva falla y que el rechazo libera la reserva.
- [ ] 19.6 Implementar la conclusión por vencimiento de la ventana sin solicitud (`ACTIVA → CONCLUIDA`), la liberación de cupo y la excepción `permiteRenovacionTardiaExcepcional` del programa; verificar que la beca no se renueva automáticamente y que el cupo vuelve a `MS-PRG`.
- [ ] 19.7 Persistir `solicitud_renovacion` con `snake_case`, estados `varchar`, `CHECK` de montos/fechas y `version`; crear la migración y verificar la reconstrucción del agregado y la cadena de becas.
- [ ] 19.8 Exponer `POST /api/v1/becas/{id}/renovaciones`, `POST /api/v1/renovaciones/{id}/evaluacion` y las consultas de renovación; publicar `RenovacionBecaSolicitada`, `RenovacionBecaAprobada`, `BecaActivadaNuevoPeriodo` y `BecaConcluidaPorVencimientoRenovacion`; verificar roles, 2FA y ausencia de PII.
- [ ] 19.9 Agregar la prueba end-to-end de dos periodos: solicitar, aprobar, crear la sucesora, verificar la cadena y los montos; probar también el rechazo y el vencimiento sin solicitud.

## 20. Pausa y aplazamiento (RF-27)

> **Trazabilidad:** RF-27 (pausa o aplazamiento sin perder la beca). HUs: HU-27A y HU-27B.
>
> **Cobertura de HUs:** HU-27A → 20.1–20.7. HU-27B → 20.3–20.7.
>
> **Adaptación de lo existente:** se reutilizan `Beca.Suspender`/`Beca.Reactivar` y la `SuspensionDesembolso` de la sección 17. La pausa agrega su propio agregado, documento de soporte, duración máxima y validación académica de reactivación.

- [ ] 20.1 Crear las enumeraciones `TipoPausa` y `EstadoPausa` con los valores `SOLICITADA`, `EN_REVISION`, `APROBADA_ACTIVA`, `RECHAZADA` y `REACTIVADA`; verificar con pruebas que la máquina de estados coincide con el DDD.
- [ ] 20.2 Implementar `PausaAplazamiento` como agregado raíz con número de solicitud, beca, tipo, motivo, dictamen, fechas, duración solicitada, URL del documento, estado, requisitos de reactivación, reporte/evaluación de reactivación, solicitante y aprobador; validar la duración máxima configurable por programa.
- [ ] 20.3 Implementar los comandos `SolicitarPausa`, `EvaluarPausa` (aprobar/rechazar) y `ReactivarPausa`; al aprobar, llamar a `Beca.Suspender` y crear la `SuspensionDesembolso`; al reactivar, exigir el nuevo `ReporteAcademico` con `EvaluacionPermanencia.cumpleCondiciones == true` y llamar a `Beca.Reactivar`.
- [ ] 20.4 Resolver la duración máxima y los requisitos de reactivación desde la configuración del programa; verificar que una solicitud que excede el máximo se rechaza y que una pausa sin configuración falla cerrada.
- [ ] 20.5 Persistir `pausa_aplazamiento` con `snake_case`, enums `varchar`, `CHECK` de fechas/duración y `version`; crear la migración y verificar la reconstrucción del agregado y su vínculo con la suspensión.
- [ ] 20.6 Exponer `POST /api/v1/becas/{id}/pausas`, `POST /api/v1/pausas/{id}/evaluacion` y `POST /api/v1/pausas/{id}/reactivacion` con roles `BECARIO`/`COORDINADOR_PROGRAMA`, autorización de propiedad y carga de soporte; verificar `200`, `201`, `400`, `403` y `409`.
- [ ] 20.7 Publicar `PausaBecaSolicitada`, `PausaBecaAprobada` y `BecaReactivadaTrasPausa`; verificar que la pausa detiene los desembolsos, que no concluye la beca y que la reactivación exige la validación académica.

## 21. Historial, auditoría y notificaciones (RF-18, RF-28, RF-29, RF-30)

> **Trazabilidad:** RF-18 y RF-28 (historial por persona/programa/periodo y línea de tiempo completa, con `MS-AUD` como dueño principal), y RF-29/RF-30 (notificaciones y recordatorios, con `MS-PLT` como dueño). HUs: HU-18.1, HU-28A, HU-28B, HU-29A, HU-29B, HU-30A y HU-30B. `MS-BEC` aporta el historial append-only, los eventos y los plazos; no expone las consultas ni envía notificaciones.
>
> **Cobertura de HUs:** HU-18.1 → 21.1–21.2 y 21.5–21.6. HU-28A/HU-28B → 21.1–21.3 y 21.5–21.6. HU-29A/HU-29B → 21.3 y 21.6. HU-30A/HU-30B → 21.4 y 21.6.
>
> **Adaptación de lo existente:** `HistorialEstadoBeca`, `TipoEntidadHistorial`, el trigger de inmutabilidad, `EventoIntegracion<T>` y el Outbox ya existen. Se amplían para cubrir desembolsos, reportes, renovaciones, pausas y hitos de notificación.

- [ ] 21.1 Ampliar `TipoEntidadHistorial` y el historial append-only para `CalendarioDesembolso`, `Desembolso`, `DatosPagoBecario`, `SuspensionDesembolso`, `ReporteAcademico`, `EvaluacionPermanencia`, `SolicitudRenovacion` y `PausaAplazamiento`; verificar que cada transición genera una fila inmutable y que `UPDATE`/`DELETE` siguen bloqueados.
- [ ] 21.2 Definir y publicar `EventoHistorialBecario` con `cadenaBecaId`, agregado, estados, actor, justificación, OTP cuando aplique y `occurredAt`; verificar que `MS-AUD` puede reconstruir la línea de tiempo y que no se expone PII.
- [ ] 21.3 Publicar los hitos de `MS-PLT` para RF-29/RF-30: oferta emitida, beca activada, compromiso, desembolso confirmado, suspensión, reanudación, reporte académico, renovación y pausa; verificar que cada evento lleva `eventId`, `correlationId` y la acción esperada.
- [ ] 21.4 Publicar los plazos vigilados (`PlazoVigilado`) de reportes académicos, ventana de renovación y fechas de revisión de suspensiones/pausas; verificar que `MS-PLT` puede recordar antes del vencimiento y que el cierre del plazo es idempotente.
- [ ] 21.5 Verificar la parte de `MS-BEC` en RF-18: consulta por persona/programa/periodo cubierta por los eventos de `MS-AUD` y agrupación por `cadenaBecaId`; dejar constancia de que RF-18 y RF-28 solo se marcan cuando `MS-AUD` consume y expone la información.
- [ ] 21.6 Agregar pruebas de contrato y de idempotencia de los eventos de historial/notificación; verificar que un duplicado no genera dos correos, dos plazos ni dos filas de auditoría.

## 22. Verificación integral del alcance de `MS-BEC`

> **Trazabilidad:** cierre de RF-15 a RF-27, de las contribuciones a RF-18, RF-28, RF-29 y RF-30 y de los movimientos que alimentan RF-23. Solo después de esta sección se actualizan los checkboxes de los roadmaps.

- [ ] 22.1 Ejecutar la suite completa (`Domain`, `Application`, `Architecture`, `Integration`) y una prueba end-to-end del ciclo completo: otorgamiento, compromiso, desembolso, suspensión, reanudación, reporte académico, renovación y pausa; verificar `dotnet build -warnaserror` y `dotnet format --verify-no-changes`.
- [ ] 22.2 Verificar la cobertura ≥ 80% en `Domain` + `Application` y que cada RF de `MS-BEC` tiene al menos una prueba de camino feliz, una de error y una de contrato con sus microservicios colaboradores.
- [ ] 22.3 Revisar seguridad y privacidad: OTP obligatorio en desembolso/revocación/cambio de datos de pago, 2FA en roles administrativos, autorización de propiedad del becario, cifrado AES-GCM y ausencia de PII/cuentas en logs y eventos.
- [ ] 22.4 Actualizar `openspec/specs/ms-beca/spec_beca.md` con los escenarios de las HUs implementadas, publicar OpenAPI/AsyncAPI definitivos y actualizar `docs/`.
- [ ] 22.5 Actualizar los checkboxes de `openspec/roadmaps/por_integrante.md` y `por_microservicio.md` solo para los RF con todas sus HUs cubiertas por tareas `[x]` y evidencia de pruebas; revalidar la marca provisional de RF-15/16/17 al cerrar 15.1–15.7 y dejar RF-18/RF-28 pendientes hasta que `MS-AUD` cierre su parte.
- [ ] 22.6 Ejecutar `openspec validate --strict` sobre el cambio de cierre y archivar los cambios de continuidad ya completados.

## Plan de cambios posteriores

| Cambio propuesto | Secciones | RF principales | HUs principales | Dependencias |
| :--- | :--- | :--- | :--- | :--- |
| `cerrar-integracion-otorgamiento-ms-beca` | 15 | RF-15, RF-16, RF-17 | HU-15.1–HU-15.3, HU-16.1–HU-16.2 | `MS-POS`, `MS-PRG`, `MS-PLT` |
| `implementar-desembolsos-ms-beca` | 16 | RF-19, RF-20; alimenta RF-23 | RF-19 Historia 1–2; RF-20 Historia 1–3 | Sección 15, `MS-PRG`, `MS-PLT` (OTP) |
| `implementar-suspension-revocacion-ms-beca` | 17 | RF-21, RF-22 | RF-21 Historia 1–2; RF-22 Historia 1–2 | Sección 16, `MS-PRG` |
| `implementar-seguimiento-academico-ms-beca` | 18 | RF-24, RF-25 | RF-24 Historia 1–2; RF-25 Historia 1–2 | Sección 17, `MS-PRG`/`MS-PLT` |
| `implementar-renovacion-ms-beca` | 19 | RF-26 | RF-26 Historia 1–2 | Sección 18, `MS-PRG` |
| `implementar-pausa-ms-beca` | 20 | RF-27 | HU-27A, HU-27B | Secciones 17 y 18 |
| `integrar-auditoria-notificaciones-ms-beca` | 21 | RF-18, RF-28, RF-29, RF-30 | HU-18.1, HU-28A/B, HU-29A/B, HU-30A/B | Secciones 16–20, `MS-AUD`, `MS-PLT` |
| `cerrar-alcance-ms-beca` | 22 | Todos | Todas las HUs | Todos los cambios anteriores |

## Criterio de cierre por RF

Un RF de `MS-BEC` solo se marca `[x]` en los roadmaps cuando:

1. Todas las HUs del RF están cubiertas por tareas `[x]` y la matriz HU ↔ RF no tiene filas pendientes.
2. Todas las tareas de sus secciones están `[x]`.
3. Existe una prueba de dominio, una de aplicación y una de integración/contrato que cubra el camino feliz y los errores principales.
4. Los eventos, endpoints y migraciones están documentados y son idempotentes.
5. No quedan deudas de contrato con `MS-POS`, `MS-PRG`, `MS-PLT` o `MS-AUD` sin una decisión registrada.

> **Estado de partida (2026-11-20):** RF-15, RF-16 y RF-17 están marcados en los roadmaps por la rebanada funcional entregada, pero a nivel de HU solo `HU-17.1` y `HU-17.2` están listas. `HU-15.2` está lista del lado `MS-BEC` con dependencias de `MS-PRG`/`MS-POS` y la deuda 15.4 de verificación; `HU-15.1`, `HU-15.3`, `HU-16.1` y `HU-16.2` siguen parciales por las deudas 15.1–15.5 y 15.7. RF-19 a RF-27 no tienen HUs implementadas. RF-18, RF-28, RF-29 y RF-30 son contribuciones a `MS-AUD`/`MS-PLT`, y RF-23 es de `MS-PRG`/`MS-AUD`. No marcar un RF como cerrado hasta que todas sus HUs estén cubiertas por tareas `[x]`.
