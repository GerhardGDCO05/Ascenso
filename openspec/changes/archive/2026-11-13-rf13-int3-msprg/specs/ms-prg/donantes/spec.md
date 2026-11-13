# Spec Delta: Alta y Gestión de Donantes y Aportes Financieros (`ms-prg/donantes`)

## Purpose

Especificar el contrato de comportamiento del módulo de Donantes (`RF-13`) en el microservicio `MS-PRG` (`Ascenso.Programas`), abarcando el registro y gestión del perfil institucional o personal del `Donante` (`[HU-13.1]`) y el registro y control transaccional de sus aportes financieros `AporteDonante` con actualización atómica de saldos y trazabilidad documental (`[HU-13.2]`, `RN-PRG-04`).

## ADDED Requirements

### Requirement: [HU-13.1] Registro y Gestión del Perfil Institucional o Personal del Donante
El sistema `MS-PRG` SHALL permitir al Administrador/a Financiero/a registrar y gestionar el perfil de un `Donante` (`Empresa`, `Organizacion` o `EgresadoPersona`) con `identificacionFiscal` unívoca, datos institucionales y `ContactoDonante`, iniciando en estado `ACTIVO` con `saldoTotalHistorico = 0.00` y `saldoDisponibleAsignacion = 0.00` en `USD`, y MUST impedir su desactivación (`RN-PRG-04`) si mantiene fondos comprometidos en programas vigentes.

#### Scenario: [HU-13.1] Registro exitoso de perfil de donante con saldos iniciales en cero
- **GIVEN** un Administrador/a Financiero/a autenticado que envía una solicitud `POST /api/v1/donantes` con `tipoDonante` válido (`EMPRESA`, `ORGANIZACION` o `EGRESADO_PERSONA`), `razonSocialNombre`, `identificacionFiscal` no registrada previamente, `email`, `telefono`, `direccionFiscal` y datos completos de `contactoPrincipal` (`ContactoDonante`)
- **WHEN** el sistema procesa el comando `RegistrarDonanteCommand`
- **THEN** crea el agregado `Donante` con identificador UUIDv7, `estadoDonante = "ACTIVO"`, `moneda = "USD"`, `saldoTotalHistorico = 0.00` y `saldoDisponibleAsignacion = 0.00`
- **AND** emite el evento de dominio `DonanteRegistradoDominioEvent` y responde `201 Created` con la representación del donante creado.

#### Scenario: [HU-13.1] Rechazo de registro de donante por identificación fiscal duplicada
- **GIVEN** que ya existe un `Donante` registrado en `MS-PRG` con la misma `identificacionFiscal` (normalizada sin espacios extremos y en mayúsculas)
- **WHEN** el Administrador/a Financiero/a intenta registrar un nuevo `Donante` con dicha `identificacionFiscal`
- **THEN** el sistema rechaza la operación con código HTTP `409 Conflict` (`IDENTIFICACION_FISCAL_DUPLICADA`)
- **AND** no persiste ningún registro duplicado.

#### Scenario: [HU-13.1] Actualización de datos de contacto y reactivación de donante
- **GIVEN** un `Donante` existente en el catálogo de `MS-PRG`
- **WHEN** el Administrador/a Financiero/a actualiza sus datos de contacto e institucionales (`PUT /api/v1/donantes/{donanteId}/contacto`) o solicita su reactivación desde el estado `INACTIVO` (`POST /api/v1/donantes/{donanteId}/reactivar`)
- **THEN** el agregado `Donante` actualiza su objeto de valor `ContactoDonante` y metadatos de auditoría o transiciona a `estadoDonante = "ACTIVO"`
- **AND** el sistema incrementa la `Version` de concurrencia optimista y responde `200 OK`.

#### Scenario: [HU-13.1] Bloqueo de desactivación de donante con fondos comprometidos en programas vigentes (RN-PRG-04)
- **GIVEN** un `Donante` en estado `ACTIVO` que mantiene fondos comprometidos en al menos un programa vigente o que conserva compromisos financieros activos (`tieneFondosComprometidosEnProgramaVigente == true`)
- **WHEN** el Administrador/a Financiero/a solicita desactivar al donante (`POST /api/v1/donantes/{donanteId}/desactivar`)
- **THEN** el dominio rechaza la transición invocando la regla `RN-PRG-04`
- **AND** el sistema responde `409 Conflict` con código de error `DONANTE_CON_FONDOS` manteniendo al donante en estado `ACTIVO`.

#### Scenario: [HU-13.1] Desactivación exitosa de donante sin fondos comprometidos
- **GIVEN** un `Donante` en estado `ACTIVO` que no mantiene fondos comprometidos en ningún programa vigente
- **WHEN** el Administrador/a Financiero/a solicita desactivar al donante (`POST /api/v1/donantes/{donanteId}/desactivar`)
- **THEN** el agregado `Donante` cambia su estado a `INACTIVO`
- **AND** el sistema persiste el cambio y responde `200 OK`.

### Requirement: [HU-13.1] Consulta Paginada, Filtrado y Detalle de Donantes
El sistema `MS-PRG` SHALL proveer consultas de lectura sin seguimiento para listar donantes de forma paginada con filtros opcionales por `tipoDonante`, `estadoDonante` y término de búsqueda (`razonSocialNombre` o `identificacionFiscal`), así como para obtener el detalle completo de un `Donante` por su `donanteId`.

#### Scenario: [HU-13.1] Consulta paginada de donantes con filtros opcionales y detalle por ID
- **GIVEN** múltiples registros de `Donante` almacenados en el esquema `programas`
- **WHEN** el Administrador/a Financiero/a consulta `GET /api/v1/donantes` indicando `pagina`, `tamanoPagina` y filtros opcionales (`tipoDonante`, `estadoDonante`, `busqueda`), o consulta `GET /api/v1/donantes/{donanteId}`
- **THEN** el sistema retorna `200 OK` con la lista paginada de donantes y el total de registros coincidentes, o el detalle completo del `Donante` incluyendo su `ContactoDonante` y saldos actuales (`404 Not Found` si el `donanteId` no existe).

### Requirement: [HU-13.2] Registro y Control Transaccional de Aportes Financieros
El sistema `MS-PRG` SHALL registrar cada ingreso financiero de un `Donante` activo como una entidad inmutable `AporteDonante` en estado `CONFIRMADO`, exigiendo `montoAporte > 0`, `moneda = 'USD'`, `numeroReferenciaBancaria` unívoca y `urlComprobanteFirebase` obligatoria (`RN-PRG-04`), e incrementando atómicamente `saldoTotalHistorico` y `saldoDisponibleAsignacion` junto con la publicación del evento `AporteDonanteRegistrado`.

#### Scenario: [HU-13.2] Registro exitoso de aporte financiero incrementando saldos atómicamente
- **GIVEN** un `Donante` en estado `ACTIVO` y un comprobante PDF previamente cargado en Firebase Storage bajo la ruta `/donantes/{donanteId}/aportes/{uuid}.pdf`
- **WHEN** el Administrador/a Financiero/a envía `POST /api/v1/donantes/{donanteId}/aportes` con `montoAporte > 0`, `moneda = "USD"`, `periodoAcademicoId` (ej. `2026-1`), `fechaAporte`, `bancoEmisor`, `numeroReferenciaBancaria` no utilizada previamente y `urlComprobanteFirebase` válida
- **THEN** el sistema crea la entidad inmutable `AporteDonante` en estado `CONFIRMADO` e incrementa atómicamente `saldoTotalHistorico` y `saldoDisponibleAsignacion` del `Donante` en una única transacción de base de datos
- **AND** encola el evento de integración `AporteDonanteRegistrado` en el `Transactional Outbox` y responde `201 Created`.

#### Scenario: [HU-13.2] Registro consecutivo de múltiples aportes financieros sobre un mismo donante
- **GIVEN** un `Donante` existente en estado `ACTIVO` con `version` inicial y uno o más aportes previos registrados
- **WHEN** el Administrador/a Financiero/a registra múltiples aportes consecutivos mediante `POST /api/v1/donantes/{donanteId}/aportes` con referencias bancarias distintas
- **THEN** el sistema inserta cada nueva entidad `AporteDonante` (`INSERT`) sin lanzar `DbUpdateConcurrencyException`, acumula correctamente `saldoTotalHistorico` y `saldoDisponibleAsignacion`, e incrementa la `version` del `Donante` en cada transacción.

#### Scenario: [HU-13.2] Rechazo de aporte financiero por número de referencia bancaria duplicado (RN-PRG-04)
- **GIVEN** que ya existe un `AporteDonante` registrado en el sistema con el mismo `numeroReferenciaBancaria`
- **WHEN** el Administrador/a Financiero/a intenta registrar un nuevo aporte con dicho `numeroReferenciaBancaria`
- **THEN** el sistema rechaza la transacción con código HTTP `409 Conflict` (`REFERENCIA_DUPLICADA`)
- **AND** tanto `saldoTotalHistorico` como `saldoDisponibleAsignacion` del `Donante` permanecen inalterados.

#### Scenario: [HU-13.2] Rechazo de aporte financiero por ausencia de comprobante en Firebase o monto inválido (RN-PRG-04)
- **GIVEN** una solicitud de registro de aporte para un `Donante` donde `urlComprobanteFirebase` está vacía o no corresponde a la ruta del donante, o donde `montoAporte <= 0`
- **WHEN** el sistema valida la solicitud en la capa de aplicación o en el método de dominio `Donante.RegistrarAporte(...)`
- **THEN** rechaza la operación con código HTTP `400 Bad Request` (`COMPROBANTE_REQUERIDO` o error de validación)
- **AND** no persiste ningún `AporteDonante` ni modifica los saldos del `Donante`.

### Requirement: [HU-13.2] Consulta de Historial de Aportes Financieros por Donante
El sistema `MS-PRG` SHALL permitir consultar el historial de aportes financieros (`AporteDonante`) asociados a un `Donante` específico, soportando el filtrado opcional por `periodoAcademicoId` y ordenando los registros de forma descendente por `fechaAporte`.

#### Scenario: [HU-13.2] Consulta de historial de aportes por donante y período académico
- **GIVEN** un `Donante` existente con uno o más registros de `AporteDonante` asociados a distintos períodos académicos
- **WHEN** el Administrador/a Financiero/a consulta `GET /api/v1/donantes/{donanteId}/aportes` con filtro opcional `periodoAcademicoId` (ej. `2026-1`)
- **THEN** el sistema responde `200 OK` devolviendo los aportes del donante que coinciden con el filtro (incluyendo `montoAporte`, `moneda`, `periodoAcademicoId`, `fechaAporte`, `bancoEmisor`, `numeroReferenciaBancaria`, `urlComprobanteFirebase` y `estadoAporte`), ordenados del más reciente al más antiguo.

