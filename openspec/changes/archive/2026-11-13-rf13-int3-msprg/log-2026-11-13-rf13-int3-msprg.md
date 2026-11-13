# Registro de Auditoría — Cambio: rf13-int3-msprg

- **Fecha de ejecución:** 13 de noviembre de 2026 (UTC-4)
- **Cambio:** `rf13-int3-msprg`
- **Requerimiento Funcional:** `RF-13` — Alta y Gestión de Donantes y Aportes Financieros (`[HU-13.1]`, `[HU-13.2]`, `RN-PRG-02`, `RN-PRG-04`)
- **Integrante:** 3 (`int3`)
- **Microservicio:** `ms-prg` (`MS-PRG` · `Ascenso.Programas`)

## Resumen de Actividades por Capa

1. **Capa de Dominio (`Ascenso.Programas.Domain`):**
   - `Enums/`: `TipoDonante` (`Empresa`, `Organizacion`, `EgresadoPersona`), `EstadoDonante` (`Activo`, `Inactivo`), `EstadoAporte` (`PendienteVerificacion`, `Confirmado`, `Anulado`) y `CodigoEnum` (`ACodigo()` / `DesdeCodigo<TEnum>()`).
   - `Donantes/ObjetosValor/ContactoDonante.cs`: Objeto de valor inmutable con validación al instanciar.
   - `Donantes/Entidades/AporteDonante.cs`: Entidad inmutable (`EntidadInmutable`, `Guid.CreateVersion7()`) validando `montoAporte > 0`, `moneda == "USD"` y `urlComprobanteFirebase` obligatoria bajo `/donantes/{donanteId}/aportes/`.
   - `Donantes/Agregados/Donante.cs`: Agregado raíz (`AggregateRoot`, sin setters públicos en propiedades mutables) con `Crear` (estado `Activo`, saldos en `0.00 USD`), `ActualizarContacto`, `Desactivar` (bloqueo `RN-PRG-04`), `Reactivar` y `RegistrarAporte` (incremento atómico de `SaldoTotalHistorico` y `SaldoDisponibleAsignacion`).
   - `Donantes/Eventos/`: `DonanteRegistradoDominioEvent`, `AporteDonanteRegistradoDominioEvent`.
   - `Excepciones/`: `IdentificacionFiscalDuplicadaException` (`409`), `DonanteConFondosComprometidosException` (`409`, `DONANTE_CON_FONDOS`), `DonanteNoEncontradoException` (`404`), `DonanteInactivoException` (`409`), `ReferenciaBancariaDuplicadaException` (`409`, `REFERENCIA_DUPLICADA`), `ComprobanteAporteRequeridoException` (`400`, `COMPROBANTE_REQUERIDO`), `MontoAporteInvalidoException` (`400`) y `ExcepcionConflictoConcurrencia` (`409`).
   - `Interfaces/`: `IDonanteRepository` e `IUnitOfWork` con propagación de `CancellationToken`.

2. **Capa de Aplicación (`Ascenso.Programas.Application`):**
   - `DTOs/`, `Abstracciones/IIntegrationEventPublisher.cs`, `EventosIntegracion/AporteDonanteRegistrado.cs`.
   - Comandos CQRS-Lite + validadores FluentValidation (`[HU-13.1]` y `[HU-13.2]`): `RegistrarDonanteCommand`, `ActualizarContactoDonanteCommand`, `DesactivarDonanteCommand`, `ReactivarDonanteCommand`, `RegistrarAporteDonanteCommand`.
   - Consultas CQRS-Lite: `ConsultarDonantesQuery`, `ObtenerDonantePorIdQuery`, `ConsultarHistorialAportesQuery`.
   - `DependencyInjection.AgregarAplicacionProgramas`.

3. **Capa de Infraestructura (`Ascenso.Programas.Infrastructure`):**
   - `Persistence/ProgramasDbContext.cs` (`Esquema = "programas"`, implementación de `IUnitOfWork`, traducción de concurrencia optimista y constraints únicos PostgreSQL `23505`, y entidades MassTransit Outbox `inbox_state`, `outbox_state`, `outbox_message`) y `ProgramasDbContextFactory.cs`.
   - `Persistence/Configuraciones/`: `ConfiguracionPersistencia.cs`, `DonanteConfiguracion.cs` (`ux_donante_identificacion_fiscal`, `OwnsOne` `ContactoPrincipal`, restricciones `CHECK` y `Version` token de concurrencia) y `AporteDonanteConfiguracion.cs` (`ux_aporte_donante_referencia_bancaria`, `ix_aporte_donante_donante_periodo` y `CHECK` de monto positivo).
   - `Persistence/Repositorios/DonanteRepository.cs`, `Mensajeria/MassTransitIntegrationEventPublisher.cs` y `DependencyInjection.AgregarInfraestructuraProgramas`.

4. **Capa API (`Ascenso.Programas.Api`):**
   - `Controllers/v1/DonantesController.cs` en `/api/v1/donantes` (8 endpoints para `[HU-13.1]` y `[HU-13.2]`).
   - `Middlewares/ManejadorExcepcionesDominio.cs` (`IExceptionHandler` -> `ProblemDetails`) y registro en `Program.cs`.

5. **Pruebas y Gobernanza:**
   - `Domain.Tests/Donantes/`: `ContactoDonanteTests.cs`, `DonanteTests.cs`.
   - `Application.Tests/Donantes/`: `RegistrarDonanteCommandHandlerTests.cs`, `GestionEstadoYContactoDonanteCommandHandlerTests.cs`, `RegistrarAporteDonanteCommandHandlerTests.cs`, `ConsultasDonantesQueryHandlerTests.cs`.
   - `ArchitectureTests/Arquitectura/DomainIndependenceTests.cs`: ampliado para inspeccionar `EntidadAuditable<Guid>` y `ValueObject` sin setters públicos.
   - `IntegrationTests/Persistencia/ModeloPersistenciaProgramasTests.cs`: validación completa del modelo EF Core de `ProgramasDbContext`.
   - Roadmaps actualizados con `[x] RF-13` en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`.

## Validación Ejecutada (Salidas Reales)

### 1. Restauración y verificación de lockfiles (`dotnet restore` y `--locked-mode`)
```text
> dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln
  Se ha restaurado ...\Ascenso.Programas.Application.Tests.csproj (en 1,24 s).
  Se ha restaurado ...\Ascenso.Programas.Application.csproj (en 1,23 s).
  Se ha restaurado ...\Ascenso.Programas.Api.csproj (en 1,35 s).
  Se ha restaurado ...\Ascenso.Programas.Domain.Tests.csproj (en 34 ms).
  Se ha restaurado ...\Ascenso.Programas.ArchitectureTests.csproj (en 1,37 s).
  Se ha restaurado ...\Ascenso.Programas.Domain.csproj (en 45 ms).
  Se ha restaurado ...\Ascenso.Programas.Infrastructure.csproj (en 338 ms).
  Se ha restaurado ...\Ascenso.Programas.IntegrationTests.csproj (en 617 ms).

> dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln --locked-mode
  Exit code 0 (los 8 proyectos restaurados en modo bloqueado).
```

### 2. Compilación en Release (`dotnet build --configuration Release --no-restore`)
```text
Compilación correcta.
    0 Advertencia(s)
    0 Errores

Tiempo transcurrido 00:00:17.76
```

### 3. Ejecución integral de pruebas (`dotnet test --configuration Release --no-build`)
```text
Correctas! - Con error:     0, Superado:    39, Omitido:     0, Total:    39, Duración: 150 ms - Ascenso.Programas.Domain.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:    13, Omitido:     0, Total:    13, Duración: 417 ms - Ascenso.Programas.Application.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:     9, Omitido:     0, Total:     9, Duración: 481 ms - Ascenso.Programas.ArchitectureTests.dll (net10.0)
Correctas! - Con error:     0, Superado:    28, Omitido:     0, Total:    28, Duración: 1 s - Ascenso.Programas.IntegrationTests.dll (net10.0)
```
Total: **89/89 pruebas superadas**, 0 errores, 0 omitidas.

### 4. Corrección de concurrencia y tracking en registro de aportes (`[HU-13.2]`)
- Se configuró `.ValueGeneratedNever()` en `AporteDonanteConfiguracion.Id` y en `ConfiguracionPersistencia.ConfigurarEntidadAuditable` (`Id` y `Version` junto con `.IsConcurrencyToken()`), evitando que EF Core marque entidades `AporteDonante` nuevas con UUIDv7 como `Modified` (`UPDATE`) en lugar de `Added` (`INSERT`).
- Se verificó `dotnet ef migrations has-pending-model-changes`: `No changes have been made to the model since the last migration.`

## Estado

Todas las tareas (`1.1` a `5.3`) han sido completadas y verificadas en verde. El cambio `rf13-int3-msprg` está listo para revisión y posterior archivado con `/opsx:archive rf13-int3-msprg`.

