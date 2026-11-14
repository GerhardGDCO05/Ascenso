# Spec Delta: Convenios con Universidades Aliadas y Versionado de Costos (`ms-prg/universidades`)

## Purpose

Especificar el contrato de comportamiento del módulo de Universidades Aliadas (`RF-14`) en el microservicio `MS-PRG` (`Ascenso.Programas`), abarcando el registro de instituciones educativas aliadas (`UniversidadAliada`) y sus convenios respaldados en Firebase Storage (`Convenio`, `[HU-14.1]`), así como la administración de carreras elegibles (`CarreraElegible`) y el versionado histórico inmutable de costos de matrícula por período académico (`CostoMatriculaReferencia`, `[HU-14.2]`, `RN-PRG-05`).

## ADDED Requirements

### Requirement: [HU-14.1] Registro de Universidad Aliada y Carga de Convenios en Firebase Storage
El sistema `MS-PRG` SHALL permitir registrar una `UniversidadAliada` con `codigoInstitucional` unívoco, `ContactoEnlace`, `CuentaBancariaInstitucional` opcional y un `Convenio` inicial en estado `VIGENTE` respaldado por un archivo PDF en Firebase Storage (`fechaFin > fechaInicio`), y MUST emitir el evento `ConvenioUniversidadRegistrado`.

#### Scenario: [HU-14.1] Registro exitoso de universidad aliada con convenio inicial vigente
- **GIVEN** un operador autorizado que envía una solicitud `POST /api/v1/universidades` con `codigoInstitucional` no registrado previamente, `nombre`, `direccion`, `contactoEnlace` (`nombre`, `email`, `telefono`), `cuentaBancariaInstitucional` opcional y datos de convenio con `fechaFin > fechaInicio` y `urlDocumentoPdfFirebase` con extensión `.pdf`
- **WHEN** el sistema procesa el comando `RegistrarUniversidadConvenioCommand`
- **THEN** crea el agregado `UniversidadAliada` en estado `ACTIVA` junto con su entidad `Convenio` en estado `VIGENTE`
- **AND** encola el evento de integración `ConvenioUniversidadRegistrado` en el `Transactional Outbox` y responde `201 Created`.

#### Scenario: [HU-14.1] Rechazo de registro de universidad por código institucional duplicado
- **GIVEN** que ya existe una `UniversidadAliada` registrada en `MS-PRG` con el mismo `codigoInstitucional` (normalizado en mayúsculas sin espacios extremos)
- **WHEN** se intenta registrar una nueva `UniversidadAliada` con dicho `codigoInstitucional`
- **THEN** el sistema rechaza la operación con código HTTP `409 Conflict` (`CODIGO_INSTITUCIONAL_DUPLICADO`)
- **AND** no persiste ningún registro duplicado.

#### Scenario: [HU-14.1] Rechazo de convenio por rango de fechas inválido o documento que no es PDF
- **GIVEN** una solicitud de registro de universidad o de nuevo convenio donde `fechaFin <= fechaInicio` o donde `urlDocumentoPdfFirebase` está vacía o no finaliza en `.pdf`
- **WHEN** el sistema valida la solicitud en la capa de aplicación o en el dominio
- **THEN** rechaza la operación con código HTTP `400 Bad Request` (`FECHAS_CONVENIO_INVALIDAS` o `DOCUMENTO_CONVENIO_PDF_REQUERIDO`)
- **AND** no registra el `Convenio` ni publica eventos de integración.

### Requirement: [HU-14.1] Vencimiento de Convenios y Continuidad de Becas Activas (RN-PRG-05)
El sistema `MS-PRG` SHALL permitir transicionar un `Convenio` al estado `VENCIDO` (manualmente o por expiración de `fechaFin`) emitiendo `ConvenioUniversidadVencido`, y MUST bloquear cualquier nueva postulación u oferta vinculada a una universidad sin convenio `VIGENTE` (`422 CONVENIO_VENCIDO`) sin interrumpir las becas que ya se encuentren en estado `ACTIVA`.

#### Scenario: [HU-14.1] Transición de convenio a estado vencido y bloqueo de nuevas postulaciones (RN-PRG-05)
- **GIVEN** una `UniversidadAliada` cuyo `Convenio` activo ha alcanzado su `fechaFin` o es marcado como vencido mediante `POST /api/v1/universidades/{universidadId}/convenios/{convenioId}/vencer`
- **WHEN** el sistema procesa `MarcarConvenioVencidoCommand` y posteriormente se valida la elegibilidad de la universidad para una nueva postulación u oferta sin otro convenio `VIGENTE`
- **THEN** el `Convenio` cambia su estado a `VENCIDO`, se encola el evento `ConvenioUniversidadVencido` y las becas en estado `ACTIVA` continúan sin interrupción
- **AND** cualquier validación para nuevas postulaciones u ofertas en dicha universidad es rechazada con código HTTP `422 Unprocessable Entity` (`CONVENIO_VENCIDO`).

### Requirement: [HU-14.2] Gestión del Catálogo de Carreras Elegibles por Universidad
El sistema `MS-PRG` SHALL permitir agregar y administrar entidades `CarreraElegible` dentro de una `UniversidadAliada` exigiendo `codigoCarrera` único por institución, `nombre` y `duracionSemestres > 0`.

#### Scenario: [HU-14.2] Registro exitoso de carrera elegible y rechazo por código duplicado en la misma universidad
- **GIVEN** una `UniversidadAliada` existente en el catálogo de `MS-PRG`
- **WHEN** se envía `POST /api/v1/universidades/{universidadId}/carreras` con `codigoCarrera`, `nombre` y `duracionSemestres > 0`
- **THEN** el sistema crea la `CarreraElegible` activa con `201 Created` si el `codigoCarrera` no existe en esa universidad
- **AND** rechaza con `409 Conflict` (`CODIGO_CARRERA_DUPLICADO`) si ya existe otra carrera con el mismo `codigoCarrera` dentro de la misma `UniversidadAliada`.

### Requirement: [HU-14.2] Versionado Histórico Inmutable de Costos de Matrícula (RN-PRG-05)
El sistema `MS-PRG` SHALL gestionar los aranceles por carrera y `periodoAcademicoId` mediante `CostoMatriculaReferencia` (`montoReferenciaMatricula > 0`, `moneda = 'USD'`), y al publicar un nuevo arancel MUST transicionar la versión `VIGENTE` anterior a `HISTORICO` inmutable, crear la versión `N+1` como `VIGENTE` y emitir `CostoMatriculaActualizado`.

#### Scenario: [HU-14.2] Publicación inicial y actualización versionada de costo de matrícula preservando snapshots (RN-PRG-05)
- **GIVEN** una `CarreraElegible` activa que posee un `CostoMatriculaReferencia` en `numeroVersion = N` y estado `VIGENTE` para el período académico `2026-1`
- **WHEN** se publica un nuevo arancel mediante `POST /api/v1/universidades/{universidadId}/carreras/{carreraId}/costos-matricula` con `montoReferenciaMatricula > 0` y `moneda = "USD"` para el mismo `periodoAcademicoId`
- **THEN** la versión `N` transiciona a estado `HISTORICO` inmutable registrando su `fechaVigenciaHasta`, y se crea la versión `N+1` en estado `VIGENTE` sin alterar los montos históricos ni las becas en curso
- **AND** el sistema encola el evento `CostoMatriculaActualizado` en el `Transactional Outbox` y responde `201 Created`.

#### Scenario: [HU-14.2] Rechazo de actualización de costo de matrícula por monto no positivo
- **GIVEN** una solicitud para registrar o actualizar el `CostoMatriculaReferencia` de una `CarreraElegible` con `montoReferenciaMatricula <= 0`
- **WHEN** el sistema valida el comando `ActualizarCostoMatriculaCommand`
- **THEN** rechaza la operación con código HTTP `400 Bad Request` (`MONTO_MATRICULA_INVALIDO`)
- **AND** mantiene intacta la versión `VIGENTE` actual sin generar una nueva versión.

### Requirement: [HU-14.2] Consultas de Universidades Aliadas e Historial de Costos de Matrícula
El sistema `MS-PRG` SHALL proveer consultas de lectura para listar `UniversidadAliada` de forma paginada, obtener su detalle completo (incluyendo convenios y carreras elegibles) y consultar el historial cronológico de `CostoMatriculaReferencia` de una carrera.

#### Scenario: [HU-14.2] Consulta paginada de universidades, detalle por ID e historial cronológico de aranceles
- **GIVEN** universidades aliadas registradas con convenios, carreras elegibles y múltiples versiones de `CostoMatriculaReferencia` (`VIGENTE` e `HISTORICO`)
- **WHEN** se consulta `GET /api/v1/universidades`, `GET /api/v1/universidades/{universidadId}` o `GET /api/v1/universidades/{universidadId}/carreras/{carreraId}/costos-matricula`
- **THEN** el sistema responde `200 OK` devolviendo el listado paginado, el detalle de la universidad con sus convenios y carreras, o la serie cronológica de versiones de costos ordenada por `periodoAcademicoId` y `numeroVersion` descendente.
