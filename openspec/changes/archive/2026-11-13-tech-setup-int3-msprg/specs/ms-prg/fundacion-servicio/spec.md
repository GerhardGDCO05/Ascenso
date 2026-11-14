# Spec Delta: MS-PRG Fundación del Servicio y Reglas Base del Dominio

## Purpose

Establecer la fundación arquitectónica hexagonal, el *Shared Kernel* táctico de dominio y las invariantes fundamentales de negocio del microservicio `MS-PRG` (`Ascenso.Programas`) para la gestión de programas de becas, control presupuestario y de cupos, donantes, convenios universitarios y convocatorias.

## ADDED Requirements

### Requirement: Aislamiento de la arquitectura hexagonal en MS-PRG

La solución `Ascenso.Programas.sln` SHALL estructurarse en cuatro capas con dependencias estrictamente hacia adentro: `Domain` no SHALL referenciar frameworks externos (`EntityFrameworkCore`, `AspNetCore`, `MassTransit`, `Npgsql`, `Firebase`, `Keycloak`), `Application` SHALL depender únicamente de `Domain`, `Infrastructure` SHALL implementar los puertos de `Domain` y `Application`, y `Api` SHALL actuar como punto de composición.

#### Scenario: Verificación de independencia del dominio y dirección de dependencias

- **GIVEN** los ensamblados compilados de `Ascenso.Programas.Domain`, `Application`, `Infrastructure` y `Api`
- **WHEN** se ejecuta la suite de pruebas de arquitectura (`Ascenso.Programas.ArchitectureTests`)
- **THEN** se comprueba que `Domain` no referencia ningún paquete de infraestructura ni capas externas
- **AND** se comprueba que `Application` no referencia `Infrastructure` ni `Api`, y que `Infrastructure` no referencia `Api`

### Requirement: Encapsulamiento estricto y Shared Kernel táctico del dominio

Toda entidad mutable de `MS-PRG` SHALL heredar de `EntidadAuditable` o `AggregateRoot` con identificador `UUIDv7` y token de concurrencia optimista (`Version`), todo registro histórico SHALL heredar de `EntidadInmutable` (*append-only*), y ninguna propiedad en `Ascenso.Programas.Domain` SHALL exponer un `set` público.

#### Scenario: Auditoría automática y bloqueo de setters públicos en entidades

- **GIVEN** las clases de entidad y raíces de agregado definidas en `Ascenso.Programas.Domain`
- **WHEN** se inspeccionan sus propiedades mediante las pruebas de arquitectura y unitarias del *Shared Kernel*
- **THEN** ninguna propiedad mutable posee un método `set` público
- **AND** al actualizar la auditoría de una `EntidadAuditable` se incrementa `Version` y se registra la marca de tiempo en UTC

### Requirement: Configuración e invariantes de ProgramaBeca (RN-PRG-01)

Un `ProgramaBeca` SHALL crearse en estado `BORRADOR` con presupuesto y cupos ocupados en cero, componiendo `PoliticaCobertura`, `CriteriosElegibilidadPrograma` y `CondicionesPermanencia` como objetos de valor inmutables. Solo SHALL transicionar a `CONFIGURADO` cuando `presupuestoTotalAsignado > 0`, `cupoTotal > 0` y todas sus universidades elegibles posean un `Convenio` en estado `VIGENTE`.

#### Scenario: Transición exitosa de programa a estado CONFIGURADO

- **GIVEN** un `ProgramaBeca` en estado `BORRADOR` con `cupoTotal > 0`, `presupuestoTotalAsignado > 0` y universidades elegibles con `Convenio` en estado `VIGENTE`
- **WHEN** el coordinador solicita configurar el programa
- **THEN** el `ProgramaBeca` transiciona a `CONFIGURADO` y emite el evento `ProgramaBecaConfigurado`

#### Scenario: Rechazo de configuración por falta de presupuesto o convenio vencido

- **GIVEN** un `ProgramaBeca` en estado `BORRADOR` sin presupuesto asignado o con alguna universidad elegible cuyo `Convenio` no está `VIGENTE`
- **WHEN** se intenta transicionar el programa a `CONFIGURADO`
- **THEN** el dominio rechaza la operación con error `422 PROGRAMA_NO_CONFIGURABLE`

### Requirement: Fórmula de disponibilidad y reserva atómica de presupuesto y cupos (RN-PRG-03 y C2)

`BalancePresupuestarioPrograma` SHALL mantener en todo momento `presupuestoDisponibleActual >= 0` y `cuposDisponibles >= 0` aplicando la fórmula oficial de disponibilidad sin doble conteo entre montos comprometidos y desembolsados. Toda reserva de cupo y presupuesto SHALL ejecutarse de forma atómica condicional (`ExecuteUpdateAsync`) registrando `ReservaPresupuestoCupo` y `MovimientoPresupuesto` inmutable.

#### Scenario: Reserva atómica concurrente por el último cupo disponible

- **GIVEN** un `BalancePresupuestarioPrograma` con `cuposDisponibles == 1` y presupuesto suficiente para una sola beca
- **WHEN** dos solicitudes concurrentes intentan reservar un cupo y presupuesto para el mismo programa
- **THEN** exactamente una solicitud confirma la `ReservaPresupuestoCupo` decrementando `cuposDisponibles` y `presupuestoDisponibleActual`
- **AND** la segunda solicitud recibe `409 SIN_CUPO_O_PRESUPUESTO` sin producir sobregiro

#### Scenario: Transferencia contable sin doble conteo al confirmar desembolso o suspender beca

- **GIVEN** un `BalancePresupuestarioPrograma` con saldo en `comprometidoBecasActivas`
- **WHEN** se registra un desembolso confirmado o la suspensión temporal de una beca
- **THEN** el monto se transfiere de `comprometidoBecasActivas` a `montoDesembolsadoEjecutado` (o a `comprometidoBecasSuspendidas` junto con su cupo) sin alterar `presupuestoDisponibleActual`

### Requirement: Gestión de donantes, aportes inmutables y asignación atómica de fondos (RN-PRG-02 y RN-PRG-04)

Todo `AporteDonante` SHALL exigir `numeroReferenciaBancaria` único y `urlComprobanteFirebase` obligatorio. La asignación de fondos (`AsignacionFondoPrograma`) a un `ProgramaBeca` SHALL exigir `Donante.estado == ACTIVO` y `montoAsignado <= Donante.saldoDisponibleAsignacion`, deduciendo el saldo del donante e incrementando el presupuesto del programa en una sola transacción.

#### Scenario: Asignación válida de fondos de donante a un programa

- **GIVEN** un `Donante` en estado `ACTIVO` con `saldoDisponibleAsignacion` mayor o igual al monto a asignar
- **WHEN** se autoriza la `AsignacionFondoPrograma` hacia un `ProgramaBeca`
- **THEN** se deduce `montoAsignado` de `Donante.saldoDisponibleAsignacion` y se incrementa `BalancePresupuestarioPrograma.presupuestoTotalAsignado`

#### Scenario: Prohibición de desactivar donante o reducir fondos en ejecución

- **GIVEN** un `Donante` con fondos comprometidos en un programa activo o un `ProgramaBeca` con convocatoria en curso
- **WHEN** se intenta pasar el donante a `INACTIVO` o reducir los fondos asignados al programa
- **THEN** el dominio rechaza la operación con `409 DONANTE_CON_FONDOS` o `409 PRESUPUESTO_EN_EJECUCION` respectivamente

### Requirement: Convenios universitarios y versionado histórico de costos de matrícula (RN-PRG-05)

Cada `UniversidadAliada` SHALL gestionar sus `Convenio` y `CarreraElegible`. Al registrar un nuevo `CostoMatriculaReferencia` (`montoReferenciaMatricula > 0`) para una carrera y periodo, la versión anterior SHALL pasar a `HISTORICO` inmutable y la nueva versión `N+1` SHALL quedar como `VIGENTE` sin alterar retroactivamente las becas u ofertas ya emitidas.

#### Scenario: Publicación de nueva versión de costo de matrícula

- **GIVEN** una `CarreraElegible` con un `CostoMatriculaReferencia` en versión `N` y estado `VIGENTE`
- **WHEN** se publica un nuevo arancel de matrícula para la carrera
- **THEN** la versión `N` transiciona a `HISTORICO` inmutable, se crea la versión `N+1` en estado `VIGENTE` y se emite `CostoMatriculaActualizado`

#### Scenario: Vencimiento de convenio universitario

- **GIVEN** una `UniversidadAliada` cuyo `Convenio` supera su `fechaFin`
- **WHEN** el convenio transiciona a `VENCIDO`
- **THEN** se bloquean nuevas postulaciones y ofertas para esa universidad (`422 CONVENIO_VENCIDO`) sin interrumpir las becas que ya se encuentran en estado `ACTIVA`

### Requirement: Configuración y ciclo de vida de Convocatoria y DocumentoRequerido (RF-02 y RF-03)

Una `Convocatoria` SHALL asociarse a un `ProgramaBeca` en estado `CONFIGURADO` o `ACTIVO` y a un `PeriodoAcademico`, parametrizando sus `ReglaElegibilidad` y al menos un `DocumentoRequerido` con tipos MIME permitidos y tamaño máximo en MB antes de poder publicarse.

#### Scenario: Publicación de convocatoria con reglas y documentos requeridos

- **GIVEN** una `Convocatoria` con ventana de fechas válida (`fechaApertura < fechaCierre`), `cupoConvocatoria > 0` y catálogo de `DocumentoRequerido` configurado
- **WHEN** el coordinador publica la convocatoria
- **THEN** la `Convocatoria` transiciona a `PUBLICADA` y emite el evento de integración `ConvocatoriaPublicada` con las reglas y documentos requeridos

