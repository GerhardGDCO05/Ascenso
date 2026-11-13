# Design

## Context

De acuerdo con `docs/Explicacion_del_DDD_V7.md`, `docs/organizacion_repositorio.md` y `docs/convenciones_c_y_ddd.md`, cada uno de los 5 microservicios de Ascenso vive en su propia carpeta bajo `Apps/services/<microservicio>/`, mantiene su propia especificación duradera en `openspec/specs/<microservicio>/spec_<código>.md` y aplica Arquitectura Hexagonal estricta en .NET 10 (`global.json` SDK `10.0.403`).

Actualmente `ms-becas` cuenta con la estructura de referencia completa (`Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, 4 proyectos de capas y 4 proyectos de prueba). El microservicio `MS-PRG` (`Apps/services/ms-prg/`, solución `Ascenso.Programas.sln`), asignado en sus requerimientos nucleares (`RF-11`, `RF-12`, `RF-13`, `RF-14`) al **Integrante 3 (`int3`)**, necesita su especificación duradera (`openspec/specs/ms-prg/spec_prg.md`), su andamiaje hexagonal completo, su *Shared Kernel* en `Domain/Comun/` y sus pruebas de arquitectura automatizadas antes de implementar los agregados de negocio.

## Goals / Non-Goals

**Goals:**
- Crear la especificación duradera `openspec/specs/ms-prg/spec_prg.md` alineada con `openspec/specs/ms-beca/spec_beca.md` y `docs/Explicacion_del_DDD_V7.md` (Sección 2.2, Reglas `RN-PRG-01` a `RN-PRG-05` y Concurrencia `C1`/`C2`).
- Inicializar `Apps/services/ms-prg/Ascenso.Programas.sln` replicando la configuración de compilación determinista y gestión centralizada de paquetes de `ms-becas` (`Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`).
- Estructurar los 4 proyectos principales (`Domain`, `Application`, `Infrastructure`, `Api`) y los 4 proyectos de pruebas (`Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests`) con sus referencias de proyecto y archivos `packages.lock.json`.
- Implementar en `Ascenso.Programas.Domain/Comun/` y `Ascenso.Programas.Domain/Excepciones/` el *Shared Kernel* táctico (`EntidadAuditable`, `EntidadInmutable`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainEvent` / `EventoDominio`, `UuidV7`, `Result`, `DomainException`).
- Implementar en `Ascenso.Programas.ArchitectureTests` las pruebas con `NetArchTest.Rules` y reflexión que garanticen: (a) cero dependencias externas en `Domain`, (b) dirección estricta de dependencias entre capas y (c) ausencia de setters públicos en entidades mutables de `Domain`.

**Non-Goals:**
- Implementar en este cambio los agregados de negocio (`ProgramaBeca`, `BalancePresupuestarioPrograma`, `Donante`, `UniversidadAliada`, `Convocatoria`), los cuales se desarrollarán en los cambios correspondientes a `RF-11`..`RF-14`.
- Configurar `ProgramasDbContext`, migraciones de PostgreSQL o adaptadores de MassTransit/Keycloak en `Infrastructure` (se reservan para el cambio de persistencia e infraestructura de `MS-PRG`).

## Decisions

### 1. Diseño de la Especificación Duradera (`openspec/specs/ms-prg/spec_prg.md`)

Siguiendo la convención de `openspec/specs/ms-beca/spec_beca.md`, `openspec/specs/ms-prg/spec_prg.md` estructurará las invariantes y reglas del negocio en 6 bloques cohesivos:

1. **Invariantes Globales del Contexto `MS-PRG`:**
   - Identificadores `UUIDv7` (`Guid.CreateVersion7()`), montos monetarios `Decimal(18,2)` en `USD`, fechas de negocio `DateOnly`, marcas de tiempo `DateTimeOffset` en UTC y prohibición absoluta de setters públicos en entidades mutables.
   - Composición sobre herencia en `ProgramaBeca` mediante los Objetos de Valor inmutables `PoliticaCobertura`, `CriteriosElegibilidadPrograma` y `CondicionesPermanencia`.
2. **Programas de Becas y Configuración (`ProgramaBeca`, `CalendarioAcademicoPrograma` — RF-11, `RN-PRG-01`):**
   - `ProgramaBeca` nace en `BORRADOR` con presupuesto y cupos ocupados en `0`.
   - Solo transiciona a `CONFIGURADO` si `presupuestoTotalAsignado > 0`, `cupoTotal > 0` y todas sus universidades elegibles poseen un `Convenio` en estado `VIGENTE` (`422 PROGRAMA_NO_CONFIGURABLE`).
3. **Control Presupuestario, Cupos y Reserva Atómica (`BalancePresupuestarioPrograma`, `ReservaPresupuestoCupo`, `MovimientoPresupuesto` — RF-12, RF-23, `RN-PRG-03`, `C2`):**
   - Desacoplamiento entre la configuración de `ProgramaBeca` y los acumuladores financieros/cupos en `BalancePresupuestarioPrograma`.
   - **Fórmula única de disponibilidad en tiempo real:**
     $$\text{presupuestoDisponibleActual} = \text{presupuestoTotalAsignado} - \text{montoDesembolsadoEjecutado} - \text{comprometidoOfertasPendientes} - \text{comprometidoBecasActivas} - \text{comprometidoBecasSuspendidas} - \text{reservasRenovacionEnCurso}$$
     $$\text{cuposDisponibles} = \text{cupoTotal} - \text{cuposOcupadosActivos} - \text{cuposSuspendidos} - \text{cuposRetenidosEnOfertas}$$
   - **Invariante de no sobregiro y no doble conteo:** `presupuestoDisponibleActual >= 0` y `cuposDisponibles >= 0` en todo instante. Al confirmarse un desembolso (`RF-20`), el monto pasa de `comprometidoBecasActivas` a `montoDesembolsadoEjecutado` sin alterar `presupuestoDisponibleActual`. Al suspenderse/reanudarse una beca (`ACTIVA ↔ SUSPENDIDA`), su saldo y cupo se transfieren entre las bolsas activas y suspendidas sin liberarse como disponibles.
   - **Estrategia de concurrencia `C2` (`ExecuteUpdateAsync`):** La reserva del último cupo/presupuesto (`POST /internal/v1/programas/{id}/reservas` con `Idempotency-Key`) ejecuta una sentencia `UPDATE` atómica condicional en PostgreSQL verificando `cupos_disponibles >= 1 AND presupuesto_disponible_actual >= @monto`; si `filasAfectadas == 0`, rechaza con `409 SIN_CUPO_O_PRESUPUESTO`. Cada cambio genera un `MovimientoPresupuesto` inmutable (*append-only*).
4. **Donantes, Aportes y Asignación Multi-Donante (`Donante`, `ContactoDonante`, `AporteDonante`, `AsignacionFondoPrograma` — RF-13, `RN-PRG-02`, `RN-PRG-04`):**
   - Todo `AporteDonante` exige `numeroReferenciaBancaria` único y `urlComprobanteFirebase` obligatorio (`400 COMPROBANTE_REQUERIDO` / `409 REFERENCIA_DUPLICADA`).
   - Asignar fondos (`AsignacionFondoPrograma`) exige `Donante.estado == ACTIVO` y `montoAsignado <= Donante.saldoDisponibleAsignacion`, deduciendo el saldo del donante e incrementando `BalancePresupuestarioPrograma.presupuestoTotalAsignado` atómicamente (`409 SALDO_DONANTE_INSUFICIENTE`).
   - Se prohíbe pasar un `Donante` a `INACTIVO` si tiene fondos comprometidos en programas activos (`409 DONANTE_CON_FONDOS`) y se prohíbe reducir fondos de un programa `ACTIVO` con convocatoria en curso (`409 PRESUPUESTO_EN_EJECUCION`).
5. **Universidades Aliadas, Convenios y Versionado de Matrículas (`UniversidadAliada`, `Convenio`, `CarreraElegible`, `CostoMatriculaReferencia` — RF-14, `RN-PRG-05`):**
   - Si un `Convenio` vence (`VENCIDO`), las becas en estado `ACTIVA` continúan sin interrupción, pero se bloquea toda nueva postulación u oferta (`422 CONVENIO_VENCIDO`).
   - Al publicar un nuevo `CostoMatriculaReferencia` (`montoReferenciaMatricula > 0`), la versión anterior pasa a `HISTORICO` inmutable y se crea la versión `N+1` como `VIGENTE`. Las becas y ofertas ya emitidas conservan su fotografía (`costoMatriculaReferenciaSnapshotId`) sin alteración retroactiva.
6. **Convocatorias y Documentos Requeridos (`Convocatoria`, `ReglaElegibilidad`, `DocumentoRequerido` — RF-02, RF-03):**
   - Parametrización por ciclo académico de ventanas (`fechaApertura`, `fechaCierre`, `fechaLimiteSubsanacionDocumentos`), `cupoConvocatoria`, `ReglaElegibilidad` y `DocumentoRequerido` (tipos MIME y tamaño máximo en MB), publicando `ConvocatoriaPublicada` hacia `MS-POS`.

---

### 2. Estructura de Carpetas y Proyectos en `Apps/services/ms-prg/`

```text
Apps/services/ms-prg/
├── .editorconfig
├── Directory.Build.props
├── Directory.Packages.props
├── Ascenso.Programas.sln
├── Ascenso.Programas.Domain/
│   ├── Ascenso.Programas.Domain.csproj
│   ├── Comun/
│   │   ├── AggregateRoot.cs
│   │   ├── DomainEvent.cs
│   │   ├── EntidadAuditable.cs
│   │   ├── EntidadInmutable.cs
│   │   ├── EventoDominio.cs
│   │   ├── IDomainEvent.cs
│   │   ├── Result.cs
│   │   ├── UuidV7.cs
│   │   └── ValueObject.cs
│   ├── Enums/
│   │   └── .gitkeep
│   ├── Excepciones/
│   │   └── DomainException.cs
│   ├── Ports/
│   │   └── .gitkeep
│   ├── Transiciones/
│   │   └── .gitkeep
│   └── ValueObjects/
│       └── .gitkeep
├── Ascenso.Programas.Application/
│   ├── Ascenso.Programas.Application.csproj
│   ├── Common/.gitkeep
│   ├── DTOs/.gitkeep
│   ├── Interfaces/.gitkeep
│   └── UseCases/.gitkeep
├── Ascenso.Programas.Infrastructure/
│   ├── Ascenso.Programas.Infrastructure.csproj
│   ├── Adapters/.gitkeep
│   ├── Configurations/.gitkeep
│   └── Persistence/.gitkeep
├── Ascenso.Programas.Api/
│   ├── Ascenso.Programas.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/.gitkeep
│   └── Properties/launchSettings.json
└── tests/
    ├── Ascenso.Programas.Domain.Tests/
    │   ├── Ascenso.Programas.Domain.Tests.csproj
    │   ├── Comun/
    │   │   ├── AggregateRootTests.cs
    │   │   ├── DomainEventTests.cs
    │   │   ├── EntidadAuditableTests.cs
    │   │   ├── EntidadInmutableTests.cs
    │   │   ├── ResultTests.cs
    │   │   └── UuidV7Tests.cs
    │   └── Excepciones/
    │       └── DomainExceptionTests.cs
    ├── Ascenso.Programas.Application.Tests/
    │   ├── Ascenso.Programas.Application.Tests.csproj
    │   └── Configuracion/
    │       └── EnsambladoAplicacionTests.cs
    ├── Ascenso.Programas.ArchitectureTests/
    │   ├── Ascenso.Programas.ArchitectureTests.csproj
    │   └── Arquitectura/
    │       ├── DependenciasEntreCapasTests.cs
    │       └── DomainIndependenceTests.cs
    └── Ascenso.Programas.IntegrationTests/
        ├── Ascenso.Programas.IntegrationTests.csproj
        └── Configuracion/
            └── EnsambladoApiTests.cs
```

### 3. Matriz de Referencias y Paquetes por Proyecto

- **`Ascenso.Programas.Domain`**: Sin `<PackageReference>` ni `<ProjectReference>`. C# 100 % puro.
- **`Ascenso.Programas.Application`**: Referencia únicamente a `Ascenso.Programas.Domain` + paquetes `FluentValidation` y `FluentValidation.DependencyInjectionExtensions`.
- **`Ascenso.Programas.Infrastructure`**: Referencia a `Ascenso.Programas.Domain` y `Ascenso.Programas.Application` + paquetes `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `MassTransit`, `MassTransit.EntityFrameworkCore`, `MassTransit.RabbitMQ`, `Hangfire`, `Hangfire.PostgreSql`, `Google.Cloud.Storage.V1`.
- **`Ascenso.Programas.Api`**: Referencia a `Application`, `Infrastructure` y `Domain` + paquetes `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Hangfire.AspNetCore`, `Serilog.AspNetCore`, `Serilog.Sinks.Console` y `OpenTelemetry.*`.
- **`Ascenso.Programas.ArchitectureTests`**: Referencia a los 4 proyectos principales (`Domain`, `Application`, `Infrastructure`, `Api`) + `NetArchTest.Rules`, `xunit`, `FluentAssertions`.

### 4. Diseño del Shared Kernel (`Domain/Comun/` y `Domain/Excepciones/`) y Reglas de Arquitectura

- **`EntidadAuditable` / `EntidadAuditable<TId>`:** Propiedades con `protected set` (`Id`, `FechaCreacion`, `UsuarioCreacionId`, `FechaUltimaModificacion`, `UsuarioModificacionId`, `Version`), método `ActualizarAuditoria(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)` que convierte a UTC e incrementa `Version`, e igualdad por tipo e `Id`.
- **`EntidadInmutable`:** Propiedades de solo lectura (`Id`, `FechaCreacion`, `UsuarioAutorId`, `CorrelacionId`) sin métodos de mutación (*append-only*).
- **`AggregateRoot` / `AggregateRoot<TId>`:** Hereda de `EntidadAuditable<TId>` y encapsula la lista privada `_eventosDominio` expuesta como `IReadOnlyCollection<IDomainEvent> EventosDominio`, con `RegistrarEventoDominio` y `LimpiarEventosDominio`.
- **`IDomainEvent`, `DomainEvent` y `EventoDominio`:** Registro base inmutable con `EventId` (`UuidV7.Nuevo()`), `OccurredAt` (en UTC) y `CorrelationId`. Se provee `EventoDominio` (heredando o como alias de `DomainEvent`) para mantener compatibilidad total tanto con `ms-becas` como con la nomenclatura en español.
- **`Result` y `Result<TValue>`:** Tipo de retorno funcional inmutable (`EsExitoso`, `EsFallo`, `CodigoError`, `MensajeError`, `Valor`, métodos de fábrica `Exito` y `Fallo`) en `Ascenso.Programas.Domain.Comun`.
- **`DomainException`:** Excepción base abstracta en `Ascenso.Programas.Domain.Excepciones` con `Codigo` (obligatorio) y `EstadoHttp` (`400..599`).
- **Pruebas de Arquitectura (`Ascenso.Programas.ArchitectureTests`):**
  1. `DependenciasEntreCapasTests`: Usa `NetArchTest.Rules` (`Types.InAssembly(...)`) y reflexión de ensamblados para verificar que `Domain` no dependa de `Application`, `Infrastructure` ni `Api`; que `Application` no dependa de `Infrastructure` ni `Api`; y que `Infrastructure` no dependa de `Api`.
  2. `DomainIndependenceTests`: Verifica con `NetArchTest.Rules` y reflexión que `Ascenso.Programas.Domain` no referencie `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `MassTransit`, `Firebase`, `Keycloak` ni `Npgsql`, y que ninguna clase que herede de `EntidadAuditable` o `AggregateRoot` en `Ascenso.Programas.Domain` tenga propiedades con `SetMethod?.IsPublic == true`.

## Risks / Trade-offs

- **[Riesgo] Advertencias de compilación tratadas como errores (`TreatWarningsAsErrors=true`):** Con `<GenerateDocumentationFile>true</GenerateDocumentationFile>` y `<AnalysisMode>Recommended</AnalysisMode>` heredados de `Directory.Build.props`, reglas de analizadores como `CA1032`, `CA1716` o `CA1812` pueden romper el build si no se diseñan adecuadamente las clases base o el `.editorconfig`.
  - **Mitigación:** Copiar íntegramente `.editorconfig` y `Directory.Build.props` desde `Apps/services/ms-becas/` y validar con `dotnet build --warnaserror` en cada paso.
- **[Riesgo] Desfase de paquetes con `RestoreLockedMode` en CI:** El workflow `.github/workflows/ci-cd.yml` ejecuta `dotnet restore` verificando los lockfiles cuando `RestorePackagesWithLockFile=true`.
  - **Mitigación:** Ejecutar `dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln` para generar los 8 archivos `packages.lock.json` y comprobar `dotnet restore --locked-mode`.

