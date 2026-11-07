## 1. Preparación del entorno y de la solución

- [x] 1.1 Instalar el SDK de .NET 10 LTS y Docker/Compose; verificar con `dotnet --version`, `docker --version` y `docker compose version`.
- [x] 1.2 Crear `Ascenso.Becas.sln` con `Ascenso.Becas.Domain`, `Ascenso.Becas.Application`, `Ascenso.Becas.Infrastructure` y `Ascenso.Becas.Api`; agregar las referencias permitidas entre capas y verificar que `dotnet build` compila.
- [ ] 1.3 Crear `Ascenso.Becas.Domain.Tests`, `Ascenso.Becas.Application.Tests`, `Ascenso.Becas.ArchitectureTests` y `Ascenso.Becas.IntegrationTests`; verificar que `dotnet test` descubre los proyectos.
- [ ] 1.4 Agregar `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` y `.gitignore`, con `net10.0`, nullable, warnings-as-errors y versiones centralizadas; verificar `dotnet restore` y `dotnet build -warnaserror`.
- [ ] 1.5 Configurar lock files de NuGet y `RestoreLockedMode` para CI; verificar que un segundo restore con `--locked-mode` no modifica dependencias.
- [ ] 1.6 Agregar paquetes base (EF Core/Npgsql, MassTransit/RabbitMQ, Hangfire, FluentValidation, Serilog/OpenTelemetry, PdfSharpCore, Google Cloud Storage) y paquetes de prueba (xUnit, FluentAssertions, NSubstitute, NetArchTest, Testcontainers, WireMock.Net, MassTransit.Testing); verificar restore, build y que las licencias de MassTransit y PdfSharpCore son compatibles con el proyecto.
- [ ] 1.7 Crear un `README.md` raíz con requisitos, comandos de build/test y estructura de carpetas; verificar que los comandos documentados funcionan en una copia limpia.
- [ ] 1.8 Configurar `dotnet format` y análisis de código en el build; verificar que `dotnet format --verify-no-changes` pasa sobre la solución vacía.
- [ ] 1.9 Instalar `dotnet-ef` como herramienta global, agregar `~/.dotnet/tools` al `PATH` y verificar `dotnet ef --version`; documentar el comando de instalación en el README.

## 2. Shared kernel de dominio

- [ ] 2.1 Implementar `EntidadAuditable`, `EntidadInmutable`, `AggregateRoot`, `ValueObject` y `DomainEvent` en `Domain/Comun`; verificar con pruebas que la igualdad por identificador y la auditoría funcionan.
- [ ] 2.2 Implementar un generador de `UUIDv7` y la abstracción `IClock` (`DateTimeOffset UtcNow`); verificar con pruebas que los identificadores son version 7 y ordenables por tiempo.
- [ ] 2.3 Crear el catálogo de excepciones y códigos de error de dominio (`TransicionEstadoInvalidaException`, `OfertaVencidaException`, `DobleBeneficioException`, `JustificacionRequeridaException`, `PostulacionNoAdjudicableException`, `CompromisoNoFirmadoException`); verificar que cada excepción expone su código y estatus HTTP.
- [ ] 2.4 Implementar los objetos de valor `PoliticaCobertura` y `CondicionesPermanencia` con validación de porcentajes, montos no negativos y moneda; verificar con pruebas de valores válidos e inválidos.
- [ ] 2.5 Implementar las enumeraciones exactas `EstadoBeca` (6 valores), `EstadoOferta` (5), `EstadoCompromiso` (4), `TipoCierreBeca`, `TipoCobertura`, `TipoCuentaBancaria` y `TipoOperacionOTP`; verificar con pruebas que los valores y nombres coinciden literalmente con la especificación.
- [ ] 2.6 Crear las tablas de transición de `Beca`, `OfertaBeca` y `CompromisoBecario` como estructuras de dominio testeables; verificar que cada dupla permitida y prohibida de la Tabla 3.2 está cubierta.
- [ ] 2.7 Definir los puertos `IBecaRepository`, `IOfertaBecaRepository`, `ICompromisoBecarioRepository`, `IUnitOfWork`, `IClock`, `IAlmacenamientoArchivos`, `ICartaOfertaPdfGenerator`, `IPostulacionesClient`, `IProgramasClient` e `IOtpClient`; verificar que `Domain` y `Application` compilan sin referencias a `Infrastructure`.

## 3. Modelo de dominio de otorgamiento

- [ ] 3.1 Implementar la creación de `Beca` en estado `OTORGADA` con `cadenaBecaId = id`, `becaAnteriorId = null`, `periodosCursados = 0`, `montoDesembolsadoPeriodoActual = 0`, `tieneRenovacionEnCurso = false`, `costoMatriculaReferenciaSnapshotId`, `montoComprometidoPeriodoActual` y snapshot de `PoliticaCobertura`; verificar con pruebas de fábrica y de campos iniciales.
- [ ] 3.2 Implementar `OfertaBeca` con `montoOfertaPorPeriodo`, `montoTotalEstimadoDuracion`, `duracionPeriodos`, `condicionesSnapshot`, `urlCartaPdfFirebase`, `hashCartaSha256`, `fechaVencimientoAceptacion`, `referenciaJobExpiracionHangfireId` y `version`; verificar con pruebas de creación y valores por defecto.
- [ ] 3.3 Implementar las transiciones de `Beca` `OTORGADA → ACTIVA`, `OTORGADA → REVOCADA`, `ACTIVA → SUSPENDIDA`, `SUSPENDIDA → ACTIVA`, `ACTIVA → RENOVADA`, `ACTIVA → CONCLUIDA` y las revocaciones permitidas; verificar que las duplas no listadas lanzan `TransicionEstadoInvalidaException`.
- [ ] 3.4 Implementar las transiciones de `OfertaBeca` desde `PENDIENTE_RESPUESTA` hacia `ACEPTADA`, `RECHAZADA_POR_POSTULANTE`, `EXPIRADA` y `ANULADA_POR_COORDINACION`; verificar que los estados terminales no admiten nuevas transiciones.
- [ ] 3.5 Implementar `CompromisoBecario` con los estados `PENDIENTE_CARGA`, `EN_REVISION`, `APROBADO` y `RECHAZADO`, incremento de `numeroIntentoCarga`, hash, URL, IP y revisión; verificar el ciclo completo con pruebas.
- [ ] 3.6 Implementar `HistorialEstadoBeca` como entidad inmutable y la generación automática de una fila por transición, incluyendo actor, justificación, OTP y marca de terminal; verificar que no existe API de modificación o borrado.
- [ ] 3.7 Implementar la política de no doble beneficio como servicio de dominio: (a) ninguna otra beca `ACTIVA` con cobertura `TOTAL`, y (b) ninguna otra beca `ACTIVA` en el mismo periodo salvo acumulación en ambos programas; verificar con pruebas de todos los casos.
- [ ] 3.8 Implementar el cálculo del monto de oferta por modalidad `TOTAL`, `PARCIAL` y `MONTO_FIJO`, con redondeo a 2 decimales y moneda USD; verificar con pruebas de borde (0%, 100%, montos fijos y matrícula 0).
- [ ] 3.9 Implementar los eventos de dominio de emisión, activación, rechazo, anulación, expiración y compromiso; verificar con pruebas que cada transición emite exactamente el evento esperado y que no se emiten eventos en transiciones fallidas.
- [ ] 3.10 Implementar la validación de justificación mínima (`Trim().Length >= 10`) para anulación y revocación; verificar que una justificación vacía o corta no cambia el estado.
- [ ] 3.11 Agregar pruebas basadas en la tabla completa de estados de la especificación (todas las duplas origen/destino, incluidas las prohibidas); verificar cobertura del 100% de las transiciones de `Beca` y `OfertaBeca`.

## 4. Persistencia y migraciones

- [ ] 4.1 Implementar `BecasDbContext` con DbSets de `Beca`, `OfertaBeca`, `CompromisoBecario`, `HistorialEstadoBeca`, proyecciones e idempotencia; verificar que el contexto se construye con la cadena de PostgreSQL.
- [ ] 4.2 Configurar las entidades con nombres `snake_case`, `uuid`, `timestamptz`, `date`, `numeric(18,2)`, enums como `varchar`, conversiones de objetos de valor y `version` como token de concurrencia; verificar el SQL generado con `dotnet ef migrations script`.
- [ ] 4.3 Crear los índices únicos parciales `ux_beca_estudiante_cobertura_total_activa`, `ux_beca_estudiante_periodo_no_acumulable_activa`, `ux_beca_cadena_periodo`, `ux_oferta_beca_beca` y `ux_beca_codigo`; verificar que existen en la migración y que rechazan duplicados en pruebas de base de datos.
- [ ] 4.4 Agregar los `CHECK` constraints de estados permitidos, montos no negativos, porcentajes entre 0 y 100 y fechas coherentes; verificar que una inserción inválida es rechazada por PostgreSQL.
- [ ] 4.5 Implementar el trigger que bloquea `UPDATE` y `DELETE` sobre `historial_estado_beca`; verificar con pruebas que ambas operaciones fallan y que `INSERT` funciona.
- [ ] 4.6 Crear las tablas de proyección `programa_beca_ref`, `costo_matricula_ref` y `convenio_ref` con `ultimo_evento_id` y marcas de actualización; verificar que el upsert es idempotente.
- [ ] 4.7 Crear la tabla `idempotency_request` con índice único por `(endpoint, clave)`, hash de payload, estado, respuesta y expiración; verificar que el índice impide duplicados.
- [ ] 4.8 Configurar las entidades de Outbox/Inbox de MassTransit en la migración y el esquema `hangfire` en el arranque; verificar que las tablas de Outbox/Inbox y las tablas de Hangfire existen y son utilizables.
- [ ] 4.9 Implementar `BecaRepository`, `OfertaBecaRepository`, `CompromisoBecarioRepository` e `IdempotencyRepository` con `Include` de owned types y entidades hijas; verificar con pruebas de integración que reconstruyen el agregado completo.
- [ ] 4.10 Implementar los métodos de bloqueo pesimista `ObtenerBecaParaActualizarAsync` (`SELECT ... FOR UPDATE`) y `AdquirirBloqueoEstudianteAsync` (`pg_advisory_xact_lock`); verificar que dos transacciones concurrentes se serializan.
- [ ] 4.11 Crear la migración `InitialCreate` y aplicarla sobre una base PostgreSQL limpia; verificar que `dotnet ef database update` termina sin errores y que una segunda ejecución es idempotente.
- [ ] 4.12 Agregar una prueba de integración que cree la base desde cero, aplique todas las migraciones y valide tablas, índices, checks y trigger; verificar que pasa en Testcontainers.

## 5. Seguridad, idempotencia y errores transversales

- [ ] 5.1 Configurar autenticación JWT Bearer contra Keycloak con validación de emisor, audiencia, firma, `MapInboundClaims=false`, roles y `RoleClaimType=roles`; verificar con un token de prueba válido e inválido.
- [ ] 5.2 Implementar la política `Requiere2FA` que valida `acr` configurable o `amr` con `otp`/`totp`; verificar que los roles administrativos sin segundo factor reciben `401 SEGUNDO_FACTOR_REQUERIDO`.
- [ ] 5.3 Implementar la política de alcance por programa (`programa_ids`) y la autorización de propiedad por `postulante_id`; verificar que un coordinador sin alcance recibe `403` y que un postulante no puede operar sobre una oferta ajena.
- [ ] 5.4 Implementar la política de *Service Account* para `/internal/v1/...`, con validación de `client_id` permitido y scope `becas.internal.read`; verificar que una llamada sin token o con scope insuficiente recibe `401`/`403`.
- [ ] 5.5 Implementar el middleware de idempotencia con hash canónico del payload, almacenamiento transaccional y TTL de 7 días; verificar los casos de reintento exitoso, payload distinto y solicitud en proceso.
- [ ] 5.6 Implementar el middleware de `ProblemDetails` y el mapeo de excepciones de dominio a códigos HTTP; verificar cada mapeo con pruebas de API.
- [ ] 5.7 Implementar el middleware de correlación que acepte o genere `X-Correlation-Id` y lo propague a logs, clientes HTTP y eventos; verificar que el identificador se conserva en la respuesta.
- [ ] 5.8 Implementar la redacción de datos sensibles en logs (OTP, cuentas, documentos, correos); verificar con pruebas que los valores nunca aparecen en la salida.

## 6. Capa de aplicación y casos de uso

- [ ] 6.1 Implementar `ICommandDispatcher`/`IQueryDispatcher` y el registro por ensamblado de handlers; verificar que un comando de prueba se despacha y que un handler ausente produce un error claro.
- [ ] 6.2 Crear los DTOs y validadores de `EmitirOfertaBeca`, `AceptarOfertaBeca`, `RechazarOfertaBeca`, `AnularOfertaBeca`, `CargarCompromisoBecario`, `RevisarCompromisoBecario` y `ConsultarBecasActivas`; verificar con pruebas de validación de campos obligatorios, formatos y longitudes.
- [ ] 6.3 Implementar la orquestación de `EmitirOfertaBecaHandler`: validar proyecciones de programa/costo/convenio, consultar adjudicabilidad, calcular montos y solicitar la reserva; verificar con dobles de prueba los caminos exitoso, no adjudicable, configuración faltante y fallo de reserva.
- [ ] 6.4 Completar `EmitirOfertaBecaHandler` con la creación del agregado, generación de carta, subida a almacenamiento, persistencia, programación del job y publicación del evento; verificar con una prueba de integración que no queda estado parcial si falla la subida o la persistencia.
- [ ] 6.5 Implementar `AceptarOfertaBecaHandler` con transacción `Serializable`, bloqueo por estudiante, bloqueo de fila, validación de vencimiento, no doble beneficio y transición a `ACTIVA`; verificar con pruebas de aceptación exitosa, oferta vencida y doble beneficio.
- [ ] 6.6 Implementar `RechazarOfertaBecaHandler` y `AnularOfertaBecaHandler` con transición terminal, `tipoCierre`, liberación por evento y validación de justificación; verificar con pruebas de éxito, estado inválido y justificación insuficiente.
- [ ] 6.7 Implementar `ExpirarOfertaBecaHandler` reutilizable por el job y la reconciliación, con relectura y bloqueo de fila; verificar que una oferta ya respondida no cambia y que una pendiente vencida expira.
- [ ] 6.8 Implementar `CargarCompromisoBecarioHandler` y `RevisarCompromisoBecarioHandler`, incluyendo validación de PDF, tamaño máximo, hash, intentos y actualización de `habilitadaParaDesembolso`; verificar con pruebas de carga, aprobación, rechazo y nueva carga.
- [ ] 6.9 Implementar las consultas `ObtenerOferta`, `ObtenerBeca` y `ConsultarBecasActivas` con DTOs de lectura autorizados; verificar que no exponen datos personales ni bancarios en la respuesta interna.
- [ ] 6.10 Integrar la idempotencia en el pipeline de comandos y asegurar que la fila de idempotencia se confirma o revierte junto con el cambio de estado; verificar con pruebas de reintento y de fallo transaccional.
- [ ] 6.11 Implementar la coordinación de transacción y Outbox: publicar eventos antes de `SaveChanges` y confirmar estado + mensaje en el mismo commit; verificar con una prueba que un fallo de `SaveChanges` no deja evento publicado.

## 7. Clientes de integración y contratos

- [ ] 7.1 Implementar `PostulacionesClient` para `GET /internal/v1/postulaciones/{id}/adjudicable` con *Service Token*, timeout de 3 s, propagación de correlación y mapeo de `404`/`false`/timeout; verificar con WireMock.
- [ ] 7.2 Implementar `ProgramasClient` para `POST /internal/v1/programas/{id}/reservas` con `Idempotency-Key: oferta-{id}`, timeout de 3 s y mapeo de `409 SIN_CUPO_O_PRESUPUESTO` y `422 CONVENIO_VENCIDO`; verificar con WireMock que la reserva nace en `PENDIENTE`, que el reintento usa la misma clave y que los errores se mapean.
- [ ] 7.3 Implementar `OtpClient` para `POST /internal/v1/otp/consumir` con el contrato documentado, sin invocarlo aún desde los casos de uso; verificar con WireMock los códigos de éxito, expiración y bloqueo.
- [ ] 7.4 Implementar el proveedor de *Service Tokens* con *client credentials* de Keycloak, caché en memoria y renovación antes de expirar; verificar con un servidor OIDC simulado que no solicita un token nuevo en cada llamada.
- [ ] 7.5 Configurar MassTransit con RabbitMQ, exchange/queue naming, reintentos, cola de error, *Transactional Outbox* e *Idempotent Inbox*; verificar con Testcontainers que un mensaje publicado se persiste en Outbox y llega al consumidor.
- [ ] 7.6 Implementar los consumidores de `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido` con upsert idempotente y control de `occurredAt`/`ultimo_evento_id`; verificar que un evento duplicado o desactualizado no retrocede la proyección.
- [ ] 7.7 Definir los contratos de eventos de salida (`OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado`, `CompromisoBecarioRechazado`) con el sobre `EventoIntegracion<T>`; verificar con pruebas de serialización que no incluyen PII.
- [ ] 7.8 Implementar la publicación de los eventos de salida desde los handlers y verificar que cada transición publica exactamente el evento correspondiente.
- [ ] 7.9 Implementar una métrica y alerta de configuración faltante (programa, costo o convenio) y documentar el procedimiento para que `MS-PRG` reemita el evento; verificar que la emisión se bloquea con `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`.

## 8. API HTTP

- [ ] 8.1 Implementar `OfertasBecaController` con las rutas `POST /api/v1/becas/ofertas`, `GET /api/v1/becas/ofertas/{id}`, `POST .../aceptar`, `POST .../rechazar` y `POST .../anular`; verificar que OpenAPI las expone con los códigos de respuesta correctos.
- [ ] 8.2 Implementar `BecasController` con `GET /api/v1/becas/{id}` aplicando autorización de dueño o coordinador; verificar `200`, `403` y `404`.
- [ ] 8.3 Implementar `CompromisosController` con carga multipart y revisión; verificar que acepta solo PDF hasta 5 MB y rechaza otros tipos con `400`.
- [ ] 8.4 Implementar `InternalBecasController` con `GET /internal/v1/becas/activas`, validando *Service Token* y devolviendo solo los campos permitidos; verificar que no expone PII.
- [ ] 8.5 Configurar `ProblemDetails`, validación automática de DTOs y respuestas consistentes en todos los controladores; verificar con pruebas de API para `400`, `401`, `403`, `409` y `422`.
- [ ] 8.6 Configurar OpenAPI/Swagger con esquemas de seguridad, ejemplos de request/response y documentación de `Idempotency-Key`; verificar que `/swagger/v1/swagger.json` es válido y contiene todas las rutas.
- [ ] 8.7 Configurar los health checks `/health/live` y `/health/ready` con PostgreSQL, RabbitMQ y migraciones; verificar `200` con dependencias sanas y `503` con PostgreSQL detenido.

## 9. Hangfire y trabajos en segundo plano

- [ ] 9.1 Configurar Hangfire con PostgreSQL, esquema `hangfire`, cola `critical` y dashboard protegido; verificar que el dashboard responde `401/403` sin autorización y `200` con rol administrativo.
- [ ] 9.2 Implementar `ExpirarOfertaBecaJob` y programarlo al emitir la oferta, guardando el id en `referenciaJobExpiracionHangfireId`; verificar con una prueba de integración que una oferta pendiente expira y una aceptada no.
- [ ] 9.3 Implementar `ReconciliarOfertasPendientesJob` recurrente cada 5 minutos como red de seguridad; verificar que detecta ofertas vencidas sin job o con job perdido.
- [ ] 9.4 Implementar los jobs de limpieza de idempotencia y Outbox con retención configurable; verificar que eliminan solo registros vencidos.
- [ ] 9.5 Verificar la persistencia de jobs tras reiniciar el contenedor de la API; comprobar que la oferta sigue expirando después del reinicio.

## 10. Archivos, carta y compromiso

- [ ] 10.1 Implementar `IAlmacenamientoArchivos` con adaptador local para desarrollo y adaptador Firebase/Google Cloud Storage para producción; verificar que ambos calculan SHA-256 y devuelven URL, hash y tamaño.
- [ ] 10.2 Implementar `ICartaOfertaPdfGenerator` con PdfSharpCore y una plantilla que incluya fundación, estudiante, programa, periodo, cobertura, monto y vigencia; verificar que el PDF generado tiene cabecera `%PDF` y hash reproducible.
- [ ] 10.3 Integrar la generación y subida de la carta en la emisión; verificar que si la subida falla no se persiste la oferta y que si se persiste la URL/hash son los del archivo subido.
- [ ] 10.4 Implementar la validación de archivos de compromiso (PDF, 5 MB, hash, intento) y la ruta inmutable en Firebase; verificar con pruebas de archivo válido, tipo inválido, tamaño excedido y reintento tras rechazo.
- [ ] 10.5 Verificar que los binarios nunca se guardan en PostgreSQL y que solo se persisten `url`, `hash`, `tamaño` y `tipoMime`.

## 11. Observabilidad y operación

- [ ] 11.1 Configurar Serilog con salida JSON, enriquecedores de servicio, correlación y redacción; verificar que un log de prueba incluye `CorrelationId` y no incluye datos sensibles.
- [ ] 11.2 Configurar OpenTelemetry para ASP.NET Core, HttpClient, Npgsql, MassTransit y Hangfire, con exportador OTLP configurable; verificar con un exportador de prueba que se generan trazas y métricas.
- [ ] 11.3 Agregar métricas de negocio (`ofertas_emitidas`, `ofertas_aceptadas`, `ofertas_rechazadas`, `ofertas_expiradas`, `compromisos_pendientes`, `outbox_pendientes`); verificar que se incrementan tras las operaciones correspondientes.
- [ ] 11.4 Configurar logs y health checks para no exponer cadenas de conexión, secretos ni stack traces en producción; verificar con una prueba de configuración `Production`.

## 12. Pruebas de integración y calidad

- [ ] 12.1 Ejecutar la suite de dominio y verificar que cubre todas las transiciones, invariantes, snapshots y cálculos de montos con cobertura ≥ 80%.
- [ ] 12.2 Ejecutar la suite de aplicación con dobles de repositorios, clientes, reloj y almacenamiento; verificar los caminos felices y los fallos de cada handler.
- [ ] 12.3 Ejecutar las pruebas de arquitectura y verificar que `Domain` no referencia infraestructura y que `Application` no referencia `Infrastructure`/`API`.
- [ ] 12.4 Ejecutar las pruebas de integración de persistencia con Testcontainers PostgreSQL: migraciones, índices únicos, trigger de historial, concurrencia optimista y bloqueo por estudiante.
- [ ] 12.5 Ejecutar una prueba end-to-end de API: emitir oferta con WireMock de POS/PRG, aceptar, consultar beca activa y verificar el estado final y los eventos.
- [ ] 12.6 Ejecutar una prueba end-to-end de rechazo, anulación y expiración con Hangfire y verificar que se publica el evento de liberación correspondiente y que la beca queda `REVOCADA` con el `tipoCierre` correcto.
- [ ] 12.7 Ejecutar una prueba de concurrencia con dos aceptaciones simultáneas del mismo estudiante y verificar que solo una gana y la otra recibe `409 DOBLE_BENEFICIO`.
- [ ] 12.8 Ejecutar una prueba de mensajería con RabbitMQ/Testcontainers: evento en Outbox, entrega, duplicado e Inbox; verificar que no hay efectos duplicados.
- [ ] 12.9 Ejecutar pruebas de seguridad: token sin 2FA, sin `postulante_id`, sin `programa_ids`, servicio interno sin token y postulante operando sobre oferta ajena; verificar `401`/`403`.
- [ ] 12.10 Ejecutar `dotnet format --verify-no-changes`, `dotnet build -warnaserror` y `dotnet test` sobre toda la solución; verificar que el pipeline local queda verde.
- [ ] 12.11 Verificar la cobertura global y el umbral de 80% en `Domain` + `Application`; ajustar pruebas si el reporte queda por debajo.

## 13. Entorno local, contenedores y CI

- [ ] 13.1 Crear `compose.yaml` con PostgreSQL 17, RabbitMQ 4 (management), Keycloak 26 y un perfil opcional para la API; verificar con `docker compose config` y `docker compose up -d` que los healthchecks quedan sanos.
- [ ] 13.2 Crear `deploy/keycloak/ascenso-realm.json` con realm, clientes `ms-bec` y `ms-bec-internal`, roles base, usuarios de prueba y mappers de claims; verificar la obtención de un token con `curl` y que incluye `roles`, `postulante_id` y `programa_ids`.
- [ ] 13.3 Crear `Makefile` con `up`, `down`, `migrate`, `run`, `test` y `format`, y `.env.example` sin secretos; verificar cada objetivo en una copia limpia.
- [ ] 13.4 Crear el `Dockerfile` multi-stage con usuario no root, variables de entorno y `HEALTHCHECK`; verificar `docker build` y un arranque contra el compose local.
- [ ] 13.5 Crear el pipeline de GitHub Actions con restore bloqueado, build `-warnaserror`, formato, pruebas unitarias/arquitectura, pruebas de integración con Docker, cobertura y build de imagen; verificar que el workflow es válido y que falla si se rompe una regla de arquitectura.
- [ ] 13.6 Crear un script de arranque limpio que cree la base, aplique migraciones y cargue el realm de Keycloak; verificar que un entorno nuevo queda operativo con un solo comando.
- [ ] 13.7 Documentar en el README el flujo de desarrollo, las variables de entorno, la obtención de tokens y la solución de problemas comunes; verificar que un integrante nuevo puede levantar el servicio siguiendo solo la documentación.
- [ ] 13.8 Crear un servicio `dev-stubs` en Compose (WireMock o API mínima) que simule `MS-POS` y `MS-PRG`, con datos semilla de postulación adjudicable y respuesta de reserva; verificar que el flujo de emisión funciona de punta a punta sin depender de los microservicios reales.
- [ ] 13.9 Configurar el almacenamiento local como modo por defecto en desarrollo (`Storage:Provider=Local`), con un volumen para los archivos; verificar que las cartas y compromisos se guardan y se leen sin Firebase.
- [ ] 13.10 Documentar y provisionar el bucket de Firebase y el service account para el entorno de integración, manteniendo las credenciales fuera del repositorio; verificar una subida real con el adaptador Firebase.

## 14. Documentación de contratos y cierre del cambio

- [ ] 14.1 Publicar la especificación OpenAPI de los endpoints públicos e internos y los contratos de eventos (AsyncAPI o Markdown) en `docs/`; verificar que coinciden con las rutas y DTOs implementados.
- [ ] 14.2 Documentar el contrato de claims de Keycloak (`roles`, `postulante_id`, `programa_ids`, `acr`/`amr`) y las responsabilidades de `MS-PLT`; verificar que el equipo de plataforma lo revisa.
- [ ] 14.3 Documentar los contratos de `adjudicable` y `reservas` con `MS-POS` y `MS-PRG`, incluyendo ejemplos y códigos de error; verificar que los equipos consumidores los validan.
- [ ] 14.4 Ejecutar `openspec validate bootstrap-ms-bec --strict` y corregir cualquier observación; verificar que el cambio queda listo para aplicar.
- [ ] 14.5 Revisar que el alcance no incluya desembolsos, seguimiento, renovación ni revocación definitiva; registrar los cambios posteriores necesarios en el roadmap.

---

**Siguientes cambios planificados (fuera de este cambio):**

- `implementar-desembolsos-ms-bec`: `CalendarioDesembolso`, `Desembolso`, `DatosPagoBecario` con AES-GCM, OTP, incidencias, suspensión y revocación definitiva.
- `implementar-seguimiento-academico-ms-bec`: `ReporteAcademico`, `EvaluacionPermanencia`, alertas de entrega tardía y consecuencias de permanencia.
- `implementar-renovacion-y-pausa-ms-bec`: `SolicitudRenovacion`, creación de la beca sucesora y `PausaAplazamiento`.
- `integrar-auditoria-ms-bec`: publicación de eventos de historial y proyecciones para `MS-AUD`.
