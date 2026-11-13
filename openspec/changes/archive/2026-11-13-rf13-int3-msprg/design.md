# Design: Alta y Gestión de Donantes y Aportes Financieros (`rf13-int3-msprg`)

## Context

El microservicio `MS-PRG` (`Ascenso.Programas`) ya cuenta con su estructura hexagonal base (`Domain`, `Application`, `Infrastructure`, `Api` y 4 proyectos de prueba) establecida en el cambio `tech-setup-int3-msprg`, junto con el Shared Kernel en `Ascenso.Programas.Domain/Comun/` (`EntidadAuditable<TId>`, `EntidadInmutable<TId>`, `EventoDominio`, `Result`) y excepciones base (`ExcepcionDominio`, `ExcepcionReglaNegocio`, `ExcepcionEntidadNoEncontrada`, `ExcepcionConflictoConcurrencia`).

Para implementar el **RF-13** (`[HU-13.1]` y `[HU-13.2]`) respetando `openspec/specs/ms-prg/spec_prg.md` (`RN-PRG-04`) y `docs/convenciones_c_y_ddd.md`, se requiere introducir el primer agregado persistente del microservicio (`Donante`) y configurar la infraestructura base de Entity Framework Core (`ProgramasDbContext`) siguiendo el mismo patrón arquitectónico de `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/` (`BecasDbContext`, `ConfiguracionPersistencia`, conversión de enumeraciones `CodigoEnum` a `VARCHAR` en `MAYUSCULAS_CON_GUIONES` y tablas de `Transactional Outbox` de MassTransit).

## Goals / Non-Goals

**Goals:**
- Modelar en `Ascenso.Programas.Domain` el agregado raíz `Donante`, la entidad interna inmutable `AporteDonante`, el objeto de valor `ContactoDonante`, las enumeraciones (`TipoDonante`, `EstadoDonante`, `EstadoAporte`, `CodigoEnum`), los eventos de dominio (`DonanteRegistradoDominioEvent`, `AporteDonanteRegistradoDominioEvent`), las excepciones específicas y los puertos `IDonanteRepository` e `IUnitOfWork` sin ningún setter público en propiedades mutables y utilizando `Guid.CreateVersion7()`.
- Implementar en `Ascenso.Programas.Application` los comandos y consultas CQRS-Lite para `[HU-13.1]` y `[HU-13.2]`, sus validadores FluentValidation, DTOs y la publicación del evento de integración `AporteDonanteRegistrado`.
- Configurar en `Ascenso.Programas.Infrastructure` el `ProgramasDbContext` sobre el esquema PostgreSQL `programas`, `ConfiguracionPersistencia`, `DonanteConfiguracion`, `AporteDonanteConfiguracion`, `DonanteRepository` y el `Transactional Outbox` de MassTransit.
- Exponer en `Ascenso.Programas.Api` el controlador `DonantesController` bajo `/api/v1/donantes` con manejo determinista de errores de dominio mediante `ProblemDetails` (`400`, `404`, `409`).
- Garantizar cobertura de pruebas unitarias (`Domain.Tests`, `Application.Tests`) y cumplimiento de pruebas de arquitectura (`ArchitectureTests`).

**Non-Goals:**
- Implementar la asignación o consumo de fondos del donante hacia programas de beca (`AsignacionFondoPrograma` / `Donante.ComprometerFondo(...)` aplicados a un `ProgramaBeca`), lo cual corresponde al alcance del **RF-01** (`rf01-int3-msprg`). No obstante, el agregado `Donante` y el puerto `IDonanteRepository.TieneFondosComprometidosEnProgramaVigenteAsync` dejarán preparado el contrato para la regla `RN-PRG-04`.
- Subida directa de archivos binarios PDF desde `MS-PRG` hacia Firebase Storage; conforme a la arquitectura del sistema y `[HU-13.2]`, el cliente/gateway sube previamente el archivo a `/donantes/{donanteId}/aportes/{uuid}.pdf` y envía la `urlComprobanteFirebase` resultante.

## Decisions

### 1. Modelado del Agregado `Donante`, `ContactoDonante` y `AporteDonante` (`Ascenso.Programas.Domain`)

- **Decisión:**
  - `Donante : EntidadAuditable<Guid>` actúa como raíz de agregado en `Ascenso.Programas.Domain/Donantes/Agregados/Donante.cs`.
  - Propiedades del agregado (todas con `{ get; private set; }` o colecciones de solo lectura `IReadOnlyCollection<AporteDonante>` expuestas desde `private readonly List<AporteDonante> _aportes = [];`):
    - `TipoDonante` (`enum TipoDonante`: `Empresa`, `Organizacion`, `EgresadoPersona`)
    - `RazonSocialNombre` (`string`)
    - `IdentificacionFiscal` (`string`, normalizada con `.Trim().ToUpperInvariant()`)
    - `Email` (`string`), `Telefono` (`string`), `DireccionFiscal` (`string`)
    - `ContactoPrincipal` (`ContactoDonante`, `sealed record` inmutable en `Domain/Donantes/ObjetosValor/ContactoDonante.cs` con `NombreRepresentante`, `Cargo`, `EmailContacto`, `TelefonoContacto`)
    - `EstadoDonante` (`enum EstadoDonante`: `Activo`, `Inactivo`)
    - `Moneda` (`string`, constante `"USD"` en `RF-13`)
    - `SaldoTotalHistorico` (`decimal`) y `SaldoDisponibleAsignacion` (`decimal`), inicializados en `0.00m`.
  - Métodos de dominio expresivos en `Donante`:
    - `Donante.Crear(...)`: Fábrica estática que valida invariantes, asigna `Guid.CreateVersion7()`, inicializa `EstadoDonante.Activo` y saldos en `0.00m`, y registra `DonanteRegistradoDominioEvent`.
    - `ActualizarContacto( string email, string telefono, string direccionFiscal, ContactoDonante contactoPrincipal, Guid? usuarioModificacionId = null)`: Actualiza la información institucional y el `ContactoDonante`, llamando a `MarcarComoModificado(usuarioModificacionId)`.
    - `Desactivar(bool tieneFondosComprometidosEnProgramaVigente, Guid? usuarioModificacionId = null)`: Verifica `RN-PRG-04`; si `tieneFondosComprometidosEnProgramaVigente` es `true` o si `SaldoTotalHistorico > SaldoDisponibleAsignacion` (fondos actualmente comprometidos), lanza `DonanteConFondosComprometidosException` (`DONANTE_CON_FONDOS`). En caso contrario, cambia `EstadoDonante` a `EstadoDonante.Inactivo`.
    - `Reactivar(Guid? usuarioModificacionId = null)`: Transiciona `EstadoDonante` a `EstadoDonante.Activo`.
    - `RegistrarAporte(decimal montoAporte, string moneda, string periodoAcademicoId, DateTimeOffset fechaAporte, string bancoEmisor, string numeroReferenciaBancaria, string urlComprobanteFirebase, Guid registradoPorUsuarioId, string? observaciones = null)`: Verifica que el donante esté `Activo`, que `montoAporte > 0`, que `moneda == "USD"` y que `urlComprobanteFirebase` no sea nula ni vacía (`ComprobanteAporteRequeridoException`, `RN-PRG-04`). Crea la entidad inmutable `AporteDonante` en `EstadoAporte.Confirmado`, la añade a `_aportes`, incrementa atómicamente:
      - `SaldoTotalHistorico += montoAporte`
      - `SaldoDisponibleAsignacion += montoAporte`
      marca auditoría (`MarcarComoModificado(registradoPorUsuarioId)`) y emite `AporteDonanteRegistradoDominioEvent`.
- **Alternativas consideradas:**
  - Modelar `AporteDonante` como un agregado independiente y actualizar los saldos del `Donante` mediante eventos eventuales. Se descartó porque `spec_prg.md` y `[HU-13.2]` exigen que el incremento de `saldoTotalHistorico` y `saldoDisponibleAsignacion` ocurra de forma estrictamente atómica al registrar el aporte confirmado.

### 2. Mapeo de Enumeraciones a `VARCHAR` en `MAYUSCULAS_CON_GUIONES` (`CodigoEnum`)

- **Decisión:** Siguiendo exactamente `Apps/services/ms-becas/Ascenso.Becas.Domain/Enums/CodigoEnum.cs` y `ConfiguracionPersistencia.cs`:
  - `TipoDonante` se mapea a `"EMPRESA"`, `"ORGANIZACION"`, `"EGRESADO_PERSONA"`.
  - `EstadoDonante` se mapea a `"ACTIVO"`, `"INACTIVO"`.
  - `EstadoAporte` se mapea a `"PENDIENTE_VERIFICACION"`, `"CONFIRMADO"`, `"ANULADO"`.
  - En `ConfiguracionPersistencia`, el método de extensión `PropertyBuilder<TEnum>.ComoCodigoEnum()` configura `.HasConversion(valor => valor.ACodigo(), codigo => CodigoEnum.DesdeCodigo<TEnum>(codigo))` en columnas `varchar` de longitud acotada.

### 3. Persistencia en `ProgramasDbContext`, Índices Únicos y `Transactional Outbox`

- **Decisión:**
  - `ProgramasDbContext` define `public const string Esquema = "programas";`, aplica todas las `IEntityTypeConfiguration` del ensamblado e incluye las entidades de MassTransit (`AddInboxStateEntity`, `AddOutboxStateEntity`, `AddOutboxMessageEntity`) con nombres de tabla `inbox_state`, `outbox_state` y `outbox_message`, idéntico a `BecasDbContext`.
  - `DonanteConfiguracion` mapea la tabla `donantes` en el esquema `programas`:
    - Propiedad `ContactoPrincipal` configurada mediante `OwnsOne(d => d.ContactoPrincipal, ...)` en columnas `contacto_nombre_representante`, `contacto_cargo`, `contacto_email` y `contacto_telefono`.
    - Índice único `ux_donante_identificacion_fiscal` sobre `identificacion_fiscal`.
    - Precisión `numeric(18,2)` en `saldo_total_historico` y `saldo_disponible_asignacion`.
    - Restricciones `CHECK`: `ck_donante_saldos_no_negativos` (`saldo_total_historico >= 0 AND saldo_disponible_asignacion >= 0 AND saldo_disponible_asignacion <= saldo_total_historico`).
    - Concurrencia optimista configurada sobre `Version` (`version`) con `.IsConcurrencyToken().ValueGeneratedNever()` (el dominio incrementa `Version` manualmente en `ActualizarAuditoria`), e `Id` configurado con `.ValueGeneratedNever()` en `ConfiguracionPersistencia.ConfigurarEntidadAuditable` al generarse como UUIDv7 en dominio.
  - `AporteDonanteConfiguracion` mapea la tabla `aportes_donante` en el esquema `programas`:
    - Clave primaria `Id` configurada con `.ValueGeneratedNever()` (UUIDv7 asignado en el constructor de dominio) para que EF Core inserte (`Added` / `INSERT`) los nuevos aportes añadidos a `Donante.Aportes` en lugar de asumir `Modified` (`UPDATE`).
    - Clave foránea `donante_id` hacia `donantes(id)` con `DeleteBehavior.Restrict`.
    - `DonanteRepository.ObtenerPorIdAsync` carga el agregado raíz con tracking e `Include(d => d.Aportes)` (sin `AsNoTracking()` ni `Update()`), permitiendo que el Change Tracker distinga entre el agregado raíz modificado y las nuevas entidades hijas `AporteDonante`.
    - Índice único `ux_aporte_donante_referencia_bancaria` sobre `numero_referencia_bancaria`.
    - Índice compuesto `ix_aporte_donante_donante_periodo` sobre `(donante_id, periodo_academico_id, fecha_aporte DESC)` para optimizar `ConsultarHistorialAportesQuery`.
    - Restricción `CHECK`: `ck_aporte_donante_monto_positivo` (`monto_aporte > 0`).
  - Al ejecutar `RegistrarAporteDonanteCommandHandler`, el handler valida previamente que no exista `numeroReferenciaBancaria` (`ExisteReferenciaBancariaAsync`), invoca `donante.RegistrarAporte(...)`, publica el mensaje de integración `AporteDonanteRegistrado` a través de `IPublishEndpoint` (o puerto `IIntegrationEventPublisher` respaldado por MassTransit Outbox) y confirma con `IUnitOfWork.SaveChangesAsync(cancellationToken)`. También captura cualquier `DbUpdateException` por violación de constraint único concurrente para traducirla de forma determinista a `409 Conflict`.

### 4. Diseño de Endpoints REST (`DonantesController`) y Códigos HTTP

- **Decisión:** Ubicado en `Ascenso.Programas.Api/Controllers/v1/DonantesController.cs` con ruta base `[Route("api/v1/donantes")]`:
  1. `POST /api/v1/donantes` → `RegistrarDonanteCommand` → `201 Created` (`CreatedAtAction` hacia `ObtenerPorId`), `400 Bad Request` (validación), `409 Conflict` (`IDENTIFICACION_FISCAL_DUPLICADA`).
  2. `GET /api/v1/donantes` → `ConsultarDonantesQuery` (`pagina`, `tamanoPagina`, `tipoDonante`, `estadoDonante`, `busqueda`) → `200 OK` (`PaginadoResponse<DonanteResumenDto>`).
  3. `GET /api/v1/donantes/{donanteId:guid}` → `ObtenerDonantePorIdQuery` → `200 OK` (`DonanteDetalleDto`) o `404 Not Found`.
  4. `PUT /api/v1/donantes/{donanteId:guid}/contacto` → `ActualizarContactoDonanteCommand` → `200 OK` (`DonanteDetalleDto`) o `404 Not Found`.
  5. `POST /api/v1/donantes/{donanteId:guid}/desactivar` → `DesactivarDonanteCommand` → `200 OK`, `404 Not Found` o `409 Conflict` (`DONANTE_CON_FONDOS`).
  6. `POST /api/v1/donantes/{donanteId:guid}/reactivar` → `ReactivarDonanteCommand` → `200 OK` o `404 Not Found`.
  7. `POST /api/v1/donantes/{donanteId:guid}/aportes` → `RegistrarAporteDonanteCommand` → `201 Created` (`AporteDonanteDto`), `400 Bad Request` (`COMPROBANTE_REQUERIDO` / monto inválido), `404 Not Found` o `409 Conflict` (`REFERENCIA_DUPLICADA` / donante inactivo).
  8. `GET /api/v1/donantes/{donanteId:guid}/aportes` → `ConsultarHistorialAportesQuery` (`periodoAcademicoId` opcional) → `200 OK` (`IReadOnlyList<AporteDonanteDto>`) o `404 Not Found`.

## Risks / Trade-offs

- **[Risk] Condición de carrera al registrar dos aportes simultáneos con el mismo `numeroReferenciaBancaria` o dos donantes con la misma `identificacionFiscal`** → **Mitigación:** Además de la verificación previa en aplicación (`ExisteIdentificacionFiscalAsync` / `ExisteReferenciaBancariaAsync`), se definen los índices únicos `ux_donante_identificacion_fiscal` y `ux_aporte_donante_referencia_bancaria` en PostgreSQL y se traduce cualquier `UniqueConstraintViolation` en `DonanteRepository` / `UnitOfWork` a las excepciones de dominio `IdentificacionFiscalDuplicadaException` y `ReferenciaBancariaDuplicadaException` (`409 Conflict`).
- **[Risk] Modificación concurrente de saldos del `Donante` al registrar aportes simultáneos** → **Mitigación:** `Donante` hereda la propiedad `Version` configurada como `.IsConcurrencyToken()` en EF Core; ante colisión concurrente, EF Core lanza `DbUpdateConcurrencyException`, que se traduce a `ExcepcionConflictoConcurrencia` (`409 Conflict`).
- **[Risk] Verificación de fondos comprometidos en programas vigentes antes de que exista la tabla `asignaciones_fondo_programa` (RF-01)** → **Mitigación:** En `Donante.Desactivar(...)` se valida que `SaldoDisponibleAsignacion == SaldoTotalHistorico` (ningún dólar comprometido) además del parámetro booleano `tieneFondosComprometidosEnProgramaVigente` consultado por `IDonanteRepository.TieneFondosComprometidosEnProgramaVigenteAsync(...)`. Cuando se implemente el `RF-01`, dicho método del repositorio consultará también la tabla `asignaciones_fondo_programa` vinculada a programas activos.

## Migration Plan

1. Implementar las clases de `Domain`, `Application`, `Infrastructure` y `Api` siguiendo el orden de `tasks.md`.
2. Verificar que `ArchitectureTests` valide las nuevas entidades (`Donante`, `AporteDonante`), objetos de valor (`ContactoDonante`), handlers y repositorios.
3. Ejecutar `dotnet build --configuration Release` (0 warnings, 0 errors) y `dotnet test --configuration Release` verificando el 100% de pruebas en verde.

