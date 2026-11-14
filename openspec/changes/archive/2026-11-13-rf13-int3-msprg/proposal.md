# Proposal: Alta y Gestión de Donantes y Aportes Financieros (`rf13-int3-msprg`)

## Why

El microservicio `MS-PRG` (`Ascenso.Programas`) es la fuente de verdad transaccional del catálogo de programas de beca, fondos de donantes, universidades aliadas y convocatorias dentro de la Plataforma Ascenso. Tras completar el andamiaje fundacional (`tech-setup-int3-msprg`), el primer requerimiento funcional en la ruta crítica del Integrante 3 (`int3`) es el **RF-13: Alta y Gestión de Donantes y Aportes Financieros**.

Los programas de beca con financiamiento externo (`MecenazgoExterno` o `Mixto`) requieren trazabilidad contable estricta sobre quién aporta los fondos (`Donante`), bajo qué comprobante bancario verificable se recibió cada ingreso (`AporteDonante`) y cuál es el saldo disponible de cada donante (`saldoDisponibleAsignacion`) antes de poder comprometer recursos en programas específicos (`RF-01`). Implementar el agregado `Donante` y sus invariantes (`RN-PRG-04`) garantiza la integridad financiera desde el origen de los fondos, previniendo duplicidad de identificaciones fiscales o referencias bancarias y bloqueando la baja de donantes con recursos comprometidos en programas vigentes.

## What Changes

Se diseñan e implementan los componentes de dominio, aplicación, infraestructura, exposición REST y pruebas automatizadas para cubrir las dos Historias de Usuario del **RF-13** en `Apps/services/ms-prg/`:

- **[HU-13.1] Registro y Gestión del Perfil Institucional/Personal del Donante:**
  - Creación del agregado raíz `Donante` (`EntidadAuditable<Guid>`) con `Guid.CreateVersion7()`, encapsulando su clasificación (`TipoDonante`: `Empresa`, `Organizacion`, `EgresadoPersona`), `razonSocialNombre`, `identificacionFiscal` unívoca (`RIF` o Cédula), datos institucionales (`email`, `telefono`, `direccionFiscal`), objeto de valor `ContactoDonante` (`nombreRepresentante`, `cargo`, `emailContacto`, `telefonoContacto`), estado operativo inicial `EstadoDonante.Activo` (`ACTIVO`) y saldos iniciales en cero (`saldoTotalHistorico = 0.00m`, `saldoDisponibleAsignacion = 0.00m`, `moneda = "USD"`).
  - Operaciones de negocio para actualizar datos de contacto (`ActualizarContacto`), reactivar un donante inactivo (`Reactivar`) y desactivar un donante (`Desactivar`) validando la invariante `RN-PRG-04` (bloqueo con `409 Conflict` / `DONANTE_CON_FONDOS` si mantiene fondos comprometidos en programas vigentes).
  - Casos de uso CQRS-Lite (`RegistrarDonanteCommand`, `ActualizarContactoDonanteCommand`, `DesactivarDonanteCommand`, `ReactivarDonanteCommand`, `ConsultarDonantesQuery`, `ObtenerDonantePorIdQuery`) con validadores FluentValidation.

- **[HU-13.2] Registro y Control Transaccional de Aportes Financieros:**
  - Creación de la entidad interna inmutable `AporteDonante` (`EntidadInmutable<Guid>`) vinculada al agregado `Donante`, registrando `montoAporte` (`> 0`), `moneda` (`USD`), `periodoAcademicoId` (ej. `2026-1`), `fechaAporte`, `bancoEmisor`, `numeroReferenciaBancaria` unívoca, `urlComprobanteFirebase` obligatoria (ruta en Firebase Storage `/donantes/{donanteId}/aportes/{uuid}.pdf`), `estadoAporte` (`EstadoAporte.Confirmado`), `observaciones` y `registradoPorUsuarioId`.
  - Método de dominio `Donante.RegistrarAporte(...)` que verifica que el donante esté `Activo`, valida el monto positivo y la presencia del comprobante en Firebase (`RN-PRG-04`), incorpora el aporte confirmado, incrementa atómicamente `saldoTotalHistorico` y `saldoDisponibleAsignacion`, y emite el evento `AporteDonanteRegistradoDominioEvent`.
  - Casos de uso CQRS-Lite (`RegistrarAporteDonanteCommand`, `ConsultarHistorialAportesQuery`) con rechazo ante `numeroReferenciaBancaria` duplicada (`409 Conflict` / `REFERENCIA_DUPLICADA`) sin alterar los saldos del donante.

- **Infraestructura, Persistencia (`ProgramasDbContext`) y API REST (`DonantesController`):**
  - Implementación de `ProgramasDbContext` en esquema PostgreSQL `programas` con soporte para `Transactional Outbox` de MassTransit (`inbox_state`, `outbox_state`, `outbox_message`), concurrencia optimista (`Version`), conversión de enumeraciones a `VARCHAR` en `MAYUSCULAS_CON_GUIONES` (`CodigoEnum`), índices únicos (`ux_donante_identificacion_fiscal`, `ux_aporte_donante_referencia_bancaria`), restricciones `CHECK` de no negatividad de saldos y repositorio `DonanteRepository`.
  - Publicación del evento de integración `AporteDonanteRegistrado` vía MassTransit Outbox.
  - Controlador `DonantesController` en `/api/v1/donantes` con manejo estandarizado de respuestas `ProblemDetails` (`201 Created`, `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict`).
  - Suite de pruebas unitarias (`Domain.Tests`, `Application.Tests`) y pruebas de arquitectura (`ArchitectureTests`).

## Capabilities

### New Capabilities
- `ms-prg/donantes`: Gestión del ciclo de vida de donantes institucionales y personales (`Donante`, `ContactoDonante`) y control transaccional de sus aportes financieros (`AporteDonante`) con actualización atómica de saldos y trazabilidad documental (`RF-13`: `HU-13.1`, `HU-13.2`, `RN-PRG-04`).

### Modified Capabilities
<!-- Ninguna capacidad existente modifica sus requisitos a nivel de contrato en este cambio. -->

## Impact

- ** Especificaciones OpenSpec:** Se añade la especificación delta `openspec/changes/rf13-int3-msprg/specs/ms-prg/donantes/spec.md` que al archivarse enriquecerá el catálogo de capacidades de `ms-prg`.
- **Microservicio `MS-PRG` (`Apps/services/ms-prg/`):**
  - `Ascenso.Programas.Domain`: Nuevas carpetas `Donantes/` (agregado `Donante`, entidad `AporteDonante`, VO `ContactoDonante`, eventos), `Enums/` (`TipoDonante`, `EstadoDonante`, `EstadoAporte`, `CodigoEnum`), `Interfaces/` (`IDonanteRepository`, `IUnitOfWork`) y `Excepciones/` específicas de donantes.
  - `Ascenso.Programas.Application`: Nueva carpeta `Donantes/` con comandos, consultas, handlers, validadores FluentValidation, DTOs y contrato de evento de integración `AporteDonanteRegistrado` más registro de servicios en `DependencyInjection.cs`.
  - `Ascenso.Programas.Infrastructure`: Implementación de `Persistence/ProgramasDbContext.cs`, `Configuraciones/ConfiguracionPersistencia.cs`, `Configuraciones/DonanteConfiguracion.cs`, `Configuraciones/AporteDonanteConfiguracion.cs`, `Repositorios/DonanteRepository.cs` y `DependencyInjection.cs`.
  - `Ascenso.Programas.Api`: Implementación de `Controllers/DonantesController.cs`, middleware/mapeo de excepciones de dominio a códigos HTTP (`400`, `404`, `409`) y registro en `Program.cs`.
  - Proyectos de prueba (`Domain.Tests`, `Application.Tests`, `ArchitectureTests`): Cobertura unitaria de invariantes de `Donante`/`AporteDonante`, handlers de comandos/consultas y cumplimiento de reglas de arquitectura hexagonal.
- **Roadmaps del proyecto:** Al finalizar la implementación en `/opsx:apply`, se actualizarán `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` marcando `[x] RF-13`.

