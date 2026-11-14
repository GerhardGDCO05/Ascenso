# Proposal: Convenios con Universidades Aliadas, Carreras Elegibles y Versionado Histórico de Costos de Matrícula (`rf14-int3-msprg`)

## Why

El microservicio `MS-PRG` (`Ascenso.Programas`) centraliza el catálogo transaccional de programas de beca, donantes, universidades aliadas y convocatorias de la Plataforma Ascenso. Tras completar el alta y control transaccional de donantes y aportes financieros (`rf13-int3-msprg`), el siguiente requerimiento de la ruta crítica asignado al Integrante 3 (`int3`) es el **RF-14: Convenios con Universidades Aliadas, Carreras Elegibles y Versionado Histórico de Costos de Matrícula**.

Para que un `ProgramaBeca` pueda transicionar al estado `Configurado` (`RN-PRG-01`) y para que las postulaciones (`MS-POS`) y otorgamientos/desembolsos (`MS-BEC`) calculen coberturas sobre matrículas verificables, el sistema requiere un catálogo gobernado de instituciones educativas (`UniversidadAliada`) respaldado por convenios interinstitucionales digitalizados en Firebase Storage (`Convenio`) y una oferta académica (`CarreraElegible`) cuyos aranceles por período académico se versionen de manera histórica e inmutable (`CostoMatriculaReferencia`, `RN-PRG-05`). Implementar este módulo asegura que el vencimiento de un convenio bloquee nuevas postulaciones u ofertas (`422 CONVENIO_VENCIDO`) sin interrumpir a los becarios ya activos, y que cualquier actualización arancelaria preserve intactas las fotografías (*snapshots*) financieras de períodos pasados y becas en curso.

## What Changes

Se diseñan e implementan los componentes de dominio, aplicación, infraestructura, exposición REST y pruebas automatizadas para cubrir las dos Historias de Usuario del **RF-14** en `Apps/services/ms-prg/`:

- **[HU-14.1] Registro de Universidad Aliada y Carga de Convenios en Firebase Storage (`RN-PRG-05`):**
  - Creación del agregado raíz `UniversidadAliada` (`AggregateRoot` / `EntidadAuditable<Guid>`) con `Guid.CreateVersion7()`, encapsulando `codigoInstitucional` unívoco (`409 Conflict` ante duplicados), `nombre`, `direccion`, objeto de valor inmutable `ContactoEnlace` (`nombre`, `email`, `telefono`), objeto de valor inmutable opcional `CuentaBancariaInstitucional` (`banco`, `tipoCuenta`, `numeroCuentaCifrado`, `rifTitular`), `usuarioEnlaceId` opcional (`Guid?`) y `estado` (`EstadoUniversidad.Activa`, `EstadoUniversidad.Inactiva`).
  - Creación de la entidad `Convenio` (`EntidadAuditable<Guid>`) dentro del agregado `UniversidadAliada`, con `numeroConvenio`, `terminosResumen`, `fechaInicio` y `fechaFin` (`DateOnly`, validando `fechaFin > fechaInicio` → `400 Bad Request`), `urlDocumentoPdfFirebase` obligatoria (validando extensión `.pdf` → `400 Bad Request`), `permiteIntercambioAutomaticoDatos` y `estado` (`EstadoConvenio.Vigente`, `EstadoConvenio.Vencido`, `EstadoConvenio.Suspendido`).
  - Operaciones de negocio en `UniversidadAliada` para registrar la universidad con su convenio inicial vigente (`CrearConConvenioInicial`), registrar nuevos convenios (`RegistrarNuevoConvenio`) y transicionar un convenio a `EstadoConvenio.Vencido` (`MarcarConvenioVencido`, manual o por expiración de `fechaFin`), además de validar si la universidad es elegible para nuevas postulaciones/ofertas (`ValidarConvenioVigenteParaNuevasPostulaciones` → lanza excepción mapeada a `422 Unprocessable Entity` con código `CONVENIO_VENCIDO`, protegiendo las becas ya `Activa` conforme a `RN-PRG-05`).
  - Emisión de los eventos de integración `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido` a través del Transactional Outbox de MassTransit.

- **[HU-14.2] Gestión de Carreras Elegibles y Versionado Histórico de Costos de Matrícula (`RN-PRG-05`):**
  - Creación de la entidad `CarreraElegible` (`EntidadAuditable<Guid>`) vinculada a `UniversidadAliada`, con `codigoCarrera` único dentro de la universidad (`409 Conflict` ante duplicados en la misma institución), `nombre`, `duracionSemestres` (`> 0`) y `activa` (`bool`).
  - Creación de la entidad `CostoMatriculaReferencia` (`EntidadAuditable<Guid>`) asociada a `CarreraElegible`, con `periodoAcademicoId` (ej. `2026-1`), `numeroVersion` (inicia en `1` y autoincrementa `N+1` por carrera y período), `montoReferenciaMatricula` (`> 0` en `Decimal(18,2)` → `400 Bad Request` si `<= 0`), `moneda` (`"USD"`), `fechaVigenciaDesde`, `fechaVigenciaHasta` (`DateTimeOffset?`) y `estado` (`EstadoVersionCosto.Vigente`, `EstadoVersionCosto.Historico`).
  - Regla de versionado histórico (`RN-PRG-05`): al publicar un nuevo costo de matrícula para una `CarreraElegible` y `periodoAcademicoId`, la versión `Vigente` previa (si existe) transiciona atómicamente a `EstadoVersionCosto.Historico` cerrando su `fechaVigenciaHasta`, y se inserta la nueva versión `N+1` en estado `EstadoVersionCosto.Vigente` preservando inalteradas las fotografías (*snapshots*) históricas para auditorías y becas en curso, publicando el evento de integración `CostoMatriculaActualizado` en el Outbox.
  - Casos de uso CQRS-Lite (`RegistrarUniversidadConvenioCommand`, `RegistrarNuevoConvenioCommand`, `MarcarConvenioVencidoCommand`, `AgregarCarreraElegibleCommand`, `ActualizarCostoMatriculaCommand`, `ConsultarUniversidadesQuery`, `ObtenerUniversidadPorIdQuery`, `ConsultarHistorialCostosCarreraQuery`) con validadores FluentValidation y DTOs.

- **Infraestructura, Persistencia (`ProgramasDbContext`) y API REST (`UniversidadesController`):**
  - Configuraciones EF Core (`UniversidadAliadaConfiguracion`, `ConvenioConfiguracion`, `CarreraElegibleConfiguracion`, `CostoMatriculaReferenciaConfiguracion`) en el esquema `programas` respetando `.ValueGeneratedNever()` en todos los `Id` UUIDv7 generados en dominio y en `Version` (`.IsConcurrencyToken().ValueGeneratedNever()`), mapeo de enums a `VARCHAR` en `MAYUSCULAS_CON_GUIONES` (`CodigoEnum`), índices únicos y migración EF Core `ConveniosUniversidadesYCostos`.
  - Implementación de `UniversidadAliadaRepository` (cargando con tracking en operaciones de escritura y `AsNoTracking()` en consultas) y controlador `UniversidadesController` en `/api/v1/universidades` con soporte para códigos HTTP `201`, `200`, `400`, `404`, `409` y `422` (`CONVENIO_VENCIDO`).

## Capabilities

### New Capabilities
- `ms-prg/universidades`: Gestión de universidades aliadas (`UniversidadAliada`), convenios interinstitucionales respaldados en Firebase Storage (`Convenio`), catálogo de carreras elegibles (`CarreraElegible`) y versionado histórico inmutable de costos de matrícula por período académico (`CostoMatriculaReferencia`) (`RF-14`: `HU-14.1`, `HU-14.2`, `RN-PRG-05`).

### Modified Capabilities
<!-- Ninguna capacidad existente modifica sus requisitos a nivel de contrato en este cambio. -->

## Impact

- **Especificaciones OpenSpec:** Se añade la especificación delta `openspec/changes/rf14-int3-msprg/specs/ms-prg/universidades/spec.md` que al archivarse se sincronizará hacia `openspec/specs/ms-prg/universidades/spec.md`.
- **Microservicio `MS-PRG` (`Apps/services/ms-prg/`):**
  - `Ascenso.Programas.Domain`: Nueva carpeta `Universidades/` (`Agregados/UniversidadAliada.cs`, `Entidades/Convenio.cs`, `Entidades/CarreraElegible.cs`, `Entidades/CostoMatriculaReferencia.cs`, `ObjetosValor/ContactoEnlace.cs`, `ObjetosValor/CuentaBancariaInstitucional.cs`, `Eventos/`), nuevas enumeraciones en `Enums/` (`EstadoUniversidad`, `EstadoConvenio`, `EstadoVersionCosto`), nuevas excepciones de dominio en `Excepciones/` (incluyendo soporte HTTP `422` para `CONVENIO_VENCIDO`) y puerto `IUniversidadAliadaRepository` en `Interfaces/`.
  - `Ascenso.Programas.Application`: Nueva carpeta `Universidades/` con comandos, consultas, handlers, validadores FluentValidation, DTOs y eventos de integración (`ConvenioUniversidadRegistrado`, `ConvenioUniversidadVencido`, `CostoMatriculaActualizado`), más su registro en `DependencyInjection.cs`.
  - `Ascenso.Programas.Infrastructure`: Nuevas configuraciones EF Core en `Persistence/Configuraciones/`, `DbSet`s en `ProgramasDbContext.cs`, nueva migración en `Persistence/Migrations/`, `UniversidadAliadaRepository.cs` en `Persistence/Repositorios/` y registro en `DependencyInjection.cs`.
  - `Ascenso.Programas.Api`: Nuevo controlador `Controllers/v1/UniversidadesController.cs` bajo `/api/v1/universidades`.
  - Proyectos de prueba (`Domain.Tests`, `Application.Tests`, `IntegrationTests`, `ArchitectureTests`): Pruebas unitarias para `[HU-14.1]` y `[HU-14.2]`, pruebas del modelo EF Core (`ValueGenerated.Never`, cascada de estados `Vigente -> Historico` y detección `Added` en entidades hijas) y verificación de reglas arquitectónicas y `dotnet format`.
- **Roadmaps del proyecto:** Al finalizar la implementación en `/opsx:apply`, se actualizarán `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` marcando `[x] RF-14`.
