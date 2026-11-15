# Registro de Auditoría — Sección 6: capa de aplicación y casos de uso

- **Fecha y hora de ejecución:** 14 de noviembre de 2026
- **Rama:** `Gerhard`
- **Commit de referencia:** `1d3b616` (`update historico seguridad`); los cambios de la sección 6 aún no estaban comprometidos al cierre.
- **Alcance:** tareas `6.1` a `6.11` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; `ms-pos`, `ms-plt` y los demás servicios quedaron intactos.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.
- **Estado del cambio activo:** 60/114 tareas; secciones 7–14 pendientes. El cambio **no** se movió a `archive/`.

## 1. Objetivo

Implementar la capa de aplicación del otorgamiento con CQRS-Lite, comandos y consultas, validación previa, transacciones, idempotencia reutilizada, Outbox por puerto y pruebas con dobles para las dependencias que se implementarán en las secciones 7–10.

## 2. Cambios realizados

### 2.1 Despacho y registro (6.1)

- Contratos `ICommand`, `ICommand<T>`, `ICommandHandler<>`, `ICommandHandler<,>`, `IQuery<T>` e `IQueryHandler<,>`.
- `CommandDispatcher` y `QueryDispatcher` con validación FluentValidation antes del handler y error claro `HANDLER_NO_ENCONTRADO`.
- `RegistroAplicacion.AgregarAplicacion()` registra validadores, despachadores y handlers por ensamblado.

### 2.2 DTOs y validadores (6.2)

- DTOs de emisión, respuesta, carga y revisión de compromiso, oferta, beca, beca activa, historial, cobertura y condiciones.
- Validadores para los comandos de emisión, aceptación, rechazo, anulación, expiración, carga y revisión de compromiso, y para las consultas de oferta, beca y becas activas.
- El validador de compromiso tolera contenido nulo, vacío, extensión y MIME inválidos sin lanzar `NullReferenceException`.

### 2.3 Emisión de oferta (6.3 y 6.4)

- `EmitirOfertaBecaHandler` consulta adjudicabilidad a `MS-POS`, valida las proyecciones locales de programa, costo y convenio, calcula montos y solicita la reserva a `MS-PRG` con referencia `OFERTA_BECA`.
- Solo acepta reservas `PENDIENTE` o `CONFIRMADA`, con identificador válido, monto suficiente y al menos un cupo.
- Genera y sube la carta PDF, programa la expiración, crea el agregado y persiste con Outbox.
- Si falla la programación del job, la subida o la persistencia, aplica compensación best-effort sobre archivo y job.
- Las pruebas verifican camino exitoso, no adjudicable, configuración faltante, proyección no vigente, reserva rechazada, caídas de `MS-POS`/`MS-PRG` y compensaciones.

### 2.4 Aceptación, rechazo, anulación y expiración (6.5–6.7)

- `AceptarOfertaBecaHandler` usa transacción `Serializable`, bloqueo por estudiante, bloqueo de fila, vencimiento y política de no doble beneficio; activa la beca y encola `BecaActivadaEvento`.
- `RechazarOfertaBecaHandler` y `AnularOfertaBecaHandler` cierran la oferta y la beca, registran `tipoCierre` y validan la justificación mínima.
- `ExpirarOfertaBecaHandler` relee y bloquea la fila, no modifica ofertas ya respondidas y expira únicamente ofertas pendientes vencidas.

### 2.5 Compromiso del becario (6.8)

- `CargarCompromisoBecarioHandler` valida MIME PDF, extensión, bytes mágicos `%PDF-`, tamaño máximo de 5 MB, hash SHA-256 e intento.
- Sube el archivo antes de la transacción local; si falla el hash o la persistencia, elimina el archivo como compensación.
- `RevisarCompromisoBecarioHandler` aprueba o rechaza bajo bloqueo y actualiza `habilitadaParaDesembolso`.

### 2.6 Consultas internas (6.9)

- `ObtenerOfertaHandler`, `ObtenerBecaHandler` y `ConsultarBecasActivasHandler` usan el modelo de lectura.
- La consulta interna devuelve solo identificadores, estados, cobertura y `permiteAcumulacion`; una prueba verifica que no expone nombres, documentos, cuentas, bancos, correos ni teléfonos.

### 2.7 Idempotencia, transacción y Outbox (6.10 y 6.11)

- `UnidadDeTrabajo` reutiliza la transacción ambiental abierta por el middleware de idempotencia; solo abre una nueva cuando no existe y usa `Serializable` cuando se solicita.
- Los handlers encolan los eventos antes de `GuardarCambiosAsync`; el Outbox de prueba confirma al commit y descarta al revertir.
- Pruebas de integración verifican commit, rollback, nivel `ReadCommitted` por defecto, nivel `Serializable`, reutilización de la transacción de idempotencia y reversión conjunta de la fila idempotente y el cambio de estado.

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Application/Despacho/`: contratos, despachadores y `HandlerNoEncontradoException`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/`: comandos, consultas, DTOs, puertos y validadores.
- `Apps/services/ms-becas/Ascenso.Becas.Application/RegistroAplicacion.cs`, `Comun/`, `Correlacion/CorrelacionOperacion.cs` y `Excepciones/`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/UnidadDeTrabajo/UnidadDeTrabajo.cs`, `Consultas/ConsultasBeca.cs` y `Proyecciones/ProyeccionesOtorgamiento.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Tiempo/RelojSistema.cs` y `Persistence/Repositorios/BecaRepository.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Contexto/ContextoOperacionHttp.cs`, `Program.cs` y mapeo de errores.
- `Apps/services/ms-becas/tests/Ascenso.Becas.Application.Tests/` y `tests/Ascenso.Becas.IntegrationTests/Persistencia/UnidadDeTrabajoTests.cs`.

## 4. Pruebas incorporadas

- `Application.Tests/Otorgamiento/`: emisión, aceptación, rechazo, anulación, expiración, carga y revisión de compromiso, consultas y validadores.
- `Application.Tests/Despacho/`: despacho de comandos y consultas, validación previa y handler ausente.
- `Application.Tests/Configuracion/`: registro por ensamblado de validadores, despachadores y handlers.
- `IntegrationTests/Persistencia/UnidadDeTrabajoTests.cs`: 6 pruebas con PostgreSQL/Testcontainers.

**Suite completa:** 441/441 correctas (Domain 136, Application 77, Integration 220, Architecture 8).

## 5. Verificación ejecutada

- `dotnet build Ascenso.Becas.sln -c Release --warnaserror`: correcto, 0 errores.
- `dotnet test Ascenso.Becas.sln -c Release`: 441/441 correctas.
- `dotnet restore Ascenso.Becas.sln --locked-mode`: correcto.
- `dotnet format Ascenso.Becas.sln --verify-no-changes`: limpio.
- `dotnet ef migrations has-pending-model-changes`: sin cambios de modelo.
- `git diff --check`: limpio.
- `openspec validate --all --strict`: correcto.

## 6. Análisis de HUs del roadmap

No se marcó ningún RF de `MS-BEC` en `openspec/roadmaps/por_microservicio.md` ni en `openspec/roadmaps/por_integrante.md`:

- **RF-15 (emisión de carta/oferta):** la lógica de aplicación y la compensación existen, pero faltan los clientes HTTP reales de `MS-POS`/`MS-PRG`, el almacenamiento/PDF real, el job de expiración y la publicación Outbox de las secciones 7–10.
- **RF-16 (aceptación o rechazo formal):** la máquina de estados y los handlers existen, pero faltan los controladores HTTP, la autorización de dueño/coordinador y las integraciones reales de secciones 8–10.
- **RF-17 (firma de compromiso):** la carga, validación y revisión existen a nivel de aplicación, pero faltan la API multipart, el almacenamiento real y la compuerta completa de desembolsos.

Las tareas `6.1` a `6.11` quedaron marcadas porque su implementación y verificación con dobles o pruebas de persistencia están completas dentro del alcance de la sección 6. Los RF permanecen pendientes hasta cerrar las secciones 7–10.

## 7. Sincronización de specs

Este histórico no incluye deltas de specs ni sincroniza `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` continúan en el cambio activo `tech-bootstrap-int4-msbeca`.
