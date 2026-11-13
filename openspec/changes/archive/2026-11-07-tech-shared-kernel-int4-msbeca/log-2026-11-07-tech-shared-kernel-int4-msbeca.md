# Registro de Auditoría — Feature 2: Shared Kernel del Dominio

> **Nota de reorganización (2026-11-08):** el cambio se renombró de `bootstrap-ms-bec` a `bootstrap-ms-beca` y su carpeta de especificaciones de `specs/ms-bec/` a `specs/ms-beca/`. Este registro se conserva como evidencia de la sesión del 2026-11-07.

- **Fecha y hora de ejecución:** 7 de noviembre de 2026, 13:48 (UTC-4)
- **Cambio OpenSpec:** `bootstrap-ms-bec`
- **Feature:** 2 — Shared Kernel del Dominio
- **Rama asociada:** `Core`
- **Alcance:** Implementación local, sin `git push` ni modificación de ramas remotas.

> **Nota de rama:** el registro se asocia a la rama `Core` según lo indicado por el responsable. Al momento de esta ejecución, el checkout local permanece en la rama `Gerhard` y no existe una rama `Core` local ni remota; no se realizaron cambios de rama.

## Resumen del cambio

Se implementó la Feature 2 (Shared Kernel del Dominio) del cambio `bootstrap-ms-bec`: las clases base tácticas, los identificadores UUIDv7, las abstracciones temporales, el catálogo de excepciones, los objetos de valor, las enumeraciones del dominio, las tablas de transición de estados y los puertos de repositorio/servicios. El proyecto `Ascenso.Becas.Domain` se mantiene como C# puro (.NET 10), sin referencias a EF Core, ASP.NET Core, MassTransit, Firebase, Keycloak ni Npgsql.

## Componentes creados

### Clases base e interfaces tácticas — `Ascenso.Becas.Domain/Comun/`

- `EntidadAuditable` y `EntidadAuditable<TId>`: auditoría de creación/modificación, `Version` como token de concurrencia optimista e igualdad por identificador.
- `EntidadInmutable`: registro base *append-only* sin operaciones de modificación ni borrado.
- `AggregateRoot<TId>` y `AggregateRoot`: acumulación y limpieza de eventos de dominio.
- `ValueObject`: base inmutable para objetos de valor.
- `IDomainEvent` y `DomainEvent`: sobre del evento con `EventId`, `OccurredAt` en UTC y `CorrelationId`.
- `UuidV7`: generador de identificadores UUIDv7 con `Guid.CreateVersion7()` y verificación de versión.

### Abstracciones temporales — `Ascenso.Becas.Domain/Ports/`

- `IClock` con `DateTimeOffset UtcNow`.

### Objetos de valor — `Ascenso.Becas.Domain/ValueObjects/`

- `PoliticaCobertura`: modalidad (`TipoCobertura`), porcentaje `0.00–100.00`, monto fijo no negativo, moneda `USD` y máximo dos decimales.
- `CondicionesPermanencia`: promedio mínimo de renovación, materias mínimas aprobadas, máximo de materias reprobadas, horas de servicio por periodo y consecuencia de incumplimiento.

### Enumeraciones del dominio — `Ascenso.Becas.Domain/Enums/`

- `EstadoBeca` (6 valores), `EstadoOferta` (5), `EstadoCompromiso` (4), `TipoCierreBeca`, `TipoCobertura`, `TipoCuentaBancaria`, `TipoOperacionOTP` y `ConsecuenciaPermanencia`.
- `CodigoEnum.ACodigo()`: traduce los nombres C# en PascalCase al código literal en MAYÚSCULAS usado por la base de datos y los contratos.

### Excepciones de dominio — `Ascenso.Becas.Domain/Excepciones/`

- `DomainException`: base con `Codigo` y `EstadoHttp`.
- `TransicionEstadoInvalidaException` → `TRANSICION_INVALIDA`, HTTP 409.
- `OfertaVencidaException` → `OFERTA_VENCIDA`, HTTP 409.
- `DobleBeneficioException` → `DOBLE_BENEFICIO`, HTTP 409.
- `JustificacionRequeridaException` → `JUSTIFICACION_REQUERIDA`, HTTP 400.
- `PostulacionNoAdjudicableException` → `POSTULACION_NO_ADJUDICABLE`, HTTP 422.
- `CompromisoNoFirmadoException` → `COMPROMISO_NO_FIRMADO`, HTTP 412.

### Puertos de repositorio y servicios — `Ascenso.Becas.Domain/Ports/`

- `IBecaRepository`, `IOfertaBecaRepository`, `ICompromisoBecarioRepository`, `IUnitOfWork`.
- `IAlmacenamientoArchivos`, `ICartaOfertaPdfGenerator`, `IPostulacionesClient`, `IProgramasClient`, `IOtpClient`.
- Contratos auxiliares: `ArchivoAlmacenado`, `DatosCartaOferta`, `ResultadoAdjudicabilidad`, `SolicitudReservaPresupuesto`, `ResultadoReservaPresupuesto` y `ResultadoConsumoOtp`.

### Tablas de transición — `Ascenso.Becas.Domain/Transiciones/`

- `TablaTransiciones<TEstado>`: validación de duplas permitidas y estados terminales.
- `TransicionesBeca`, `TransicionesOfertaBeca` y `TransicionesCompromisoBecario`, alineadas con la Tabla 3.2 de la especificación maestra.

### Pruebas — `tests/Ascenso.Becas.Domain.Tests/`

- Pruebas de entidades auditables e inmutables, igualdad por identificador y estructural, eventos de dominio y UUIDv7.
- Pruebas de excepciones, códigos y estatus HTTP.
- Pruebas de objetos de valor y validaciones.
- Pruebas de enumeraciones y códigos literales.
- Pruebas de tablas de transición.
- Prueba de independencia del dominio frente a frameworks de infraestructura.

## Estado de la suite de pruebas

- Proyecto: `Ascenso.Becas.Domain.Tests`.
- Resultado: **59 de 59 pruebas correctas** (`Correctas! - Con error: 0, Superado: 59, Omitido: 0, Total: 59`).
- Compilación de la solución: `dotnet build Ascenso.Becas.sln -warnaserror` → 0 advertencias, 0 errores.
- Formato: `dotnet format --verify-no-changes` → sin cambios requeridos.
- Validación OpenSpec: `openspec validate bootstrap-ms-bec --strict` → cambio válido.

## Tareas completadas

Se marcaron como completadas las tareas `2.1` a `2.7` en `openspec/changes/bootstrap-ms-bec/tasks.md`. El progreso del cambio queda en **9/114 tareas**.

## Estado local

- Cambios sin commit y sin `git push`.
- Proyecto de pruebas agregado a `Ascenso.Becas.sln`.
- Sin modificaciones en ramas remotas.
