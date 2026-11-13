## How

Se implementó la Feature 2 del cambio `bootstrap-ms-bec` dentro de `Apps/services/ms-becas/Ascenso.Becas.Domain`, manteniendo el proyecto como C# puro (.NET 10), sin referencias a frameworks de infraestructura.

### Componentes creados

- `Comun/`: `EntidadAuditable`, `EntidadAuditable<TId>`, `EntidadInmutable`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainEvent` y `UuidV7`.
- `Ports/`: `IClock`, repositorios `IBecaRepository`, `IOfertaBecaRepository`, `ICompromisoBecarioRepository`, `IUnitOfWork` y servicios externos de archivos, PDF, postulaciones, programas y OTP.
- `ValueObjects/`: `PoliticaCobertura` y `CondicionesPermanencia`.
- `Enums/`: estados de beca, oferta y compromiso, tipos de cierre, cobertura, cuenta bancaria, operación OTP y consecuencia de permanencia; `CodigoEnum.ACodigo()`.
- `Excepciones/`: `DomainException` y las excepciones `TransicionEstadoInvalida`, `OfertaVencida`, `DobleBeneficio`, `JustificacionRequerida`, `PostulacionNoAdjudicable` y `CompromisoNoFirmado`.
- `Transiciones/`: `TablaTransiciones<TEstado>`, `TransicionesBeca`, `TransicionesOfertaBeca` y `TransicionesCompromisoBecario`.

### Decisiones de diseño

- **Dominio puro:** las abstracciones de infraestructura se expresan como puertos, nunca como referencias directas.
- **UUIDv7:** los identificadores se generan con `Guid.CreateVersion7()` y se valida la versión.
- **Auditoría y concurrencia:** `EntidadAuditable` expone la versión como token de concurrencia optimista y actualiza marcas de creación/modificación.
- **Historial inmutable:** `EntidadInmutable` sirve de base para registros append-only.
- **Excepciones con contrato:** cada excepción lleva código de negocio y estatus HTTP para la futura capa API.
- **Transiciones cerradas:** las tablas de transición concentran las duplas permitidas y los estados terminales.

### Validación ejecutada

```text
dotnet build Ascenso.Becas.sln -warnaserror        → 0 advertencias, 0 errores
dotnet test Ascenso.Becas.Domain.Tests             → 59/59 correctas
dotnet format --verify-no-changes                  → sin cambios
openspec validate bootstrap-ms-bec --strict        → cambio válido
```
