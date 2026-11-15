# Design: Registro de Datos Personales y Académicos Base (`rf1-1-int1-mspos`)

## Context

El microservicio `MS-POS` (`Ascenso.Postulaciones`) dispone de la fundación archivada en `tech-bootstrap-int1-mspos`: Shared Kernel (`EntidadAuditable`, `AggregateRoot`, excepciones de dominio, `CodigoEnum`, `Result`), `PostulacionesDbContext` con esquema `postulaciones`, `ConfiguracionPersistencia`, middleware `ManejadorExcepcionesDominio` con `ProblemDetails` y cuatro proyectos de prueba. No existe todavía ningún agregado de negocio, migración, autenticación JWT ni registro de handlers de aplicación.

Restricciones relevantes:

- `Directory.Build.props` trata advertencias como errores, usa lock files y exige formato LF/UTF-8.
- El paquete `Microsoft.AspNetCore.Authentication.JwtBearer` no está en `Directory.Packages.props`.
- No hay manifiesto de herramientas `dotnet-ef` en el repositorio, por lo que la migración no puede generarse todavía.
- `MS-PLT` no existe en `Apps/services/`, por lo que el endpoint real de periodos académicos aún no está disponible.
- `MS-PRG` expone `GET /api/v1/universidades/{id}` con `Estado` como `"ACTIVA"`/`"INACTIVA"`, `TieneConvenioVigente` y `Carreras[].Activa`.

## Goals / Non-Goals

**Goals:**

- Modelar el agregado `Postulante` de la HU 1.1 y su objeto de valor `DatosContacto`, sin setters públicos.
- Asociar el perfil al sujeto autenticado (`sub`) del JWT de Keycloak.
- Validar universidad/carrera y periodo vigente con política fail-closed.
- Persistir en el esquema `postulaciones` con índices únicos, `CHECK` y traducción de unicidad a excepciones de dominio.
- Mantener las pruebas sin dependencia de PostgreSQL o Keycloak reales.

**Non-Goals:**

- Datos socioeconómicos, `MiembroFamiliar` y transición a `EstadoPerfil.COMPLETO`.
- `BorradorPostulacion`, `Postulacion`, convocatorias y elegibilidad.
- Eventos de integración y `Transactional Outbox` (deuda técnica registrada).
- Implementar `MS-PLT` o cambiar los contratos actuales de `MS-PRG`.
- Actualización (`PUT`) del perfil base.

## Decisions

### 1. Modelo de dominio

- `Postulante : AggregateRoot` en `Ascenso.Postulaciones.Domain/Postulantes/Agregados/Postulante.cs`, con propiedades `{ get; private set; }`:
  - `PersonaId` (`Guid?`, nulo mientras `MS-PLT` no exista), `UsuarioId` (`Guid`), `NumeroDocumentoIdentidad`, `Nombres`, `Apellidos`.
  - `DatosContacto` (`DatosContacto`), `UniversidadAliadaId`, `CarreraId`, `PeriodoActualId`.
  - `CondicionAcademicaIngreso`, `AceptaTratamientoDatos`, `EstadoPerfil`, `FechaRegistro`.
  - `NombreCompleto` calculado, sin persistencia.
- Factory `Crear(...)` que normaliza la identificación (`Trim`) y valida: identificación, nombres y apellidos no vacíos y con longitud máxima; `DatosContacto` válido; `aceptaTratamientoDatos == true`; condición académica en `ADMITIDO`/`MATRICULADO`. El perfil nace en `EstadoPerfil.Incompleto`, con `UuidV7.Nuevo()` y `fechaRegistro` recibida del reloj.
- `DatosContacto` como `sealed record class : ValueObject` en `Domain/Postulantes/ObjetosValor/DatosContacto.cs`: `CorreoElectronico` y `Telefono` normalizados, con validación en el constructor (formato de correo y teléfono de 7 a 15 dígitos, permitiendo `+` y separadores).
- Enums en `Domain/Enums/`: `CondicionAcademicaIngreso` (`Admitido`, `Matriculado`) y `EstadoPerfil` (`Incompleto`, `Completo`); se serializan con `ACodigo()` como `ADMITIDO`, `MATRICULADO`, `INCOMPLETO`, `COMPLETO`.
- Excepciones en `Domain/Excepciones/`: `IdentidadDuplicadaException` (409 `IDENTIDAD_DUPLICADA`), `PerfilYaRegistradoException` (409 `PERFIL_YA_REGISTRADO`), `PostulanteNoEncontradoException` (404 `POSTULANTE_NO_ENCONTRADO`), `DatosPersonalesInvalidosException` (400 `DATOS_PERSONALES_INVALIDOS`), `CondicionAcademicaInvalidaException` (422 `CONDICION_ACADEMICA_INVALIDA`), `UniversidadNoElegibleException` (422 `UNIVERSIDAD_NO_ELEGIBLE`), `CarreraNoElegibleException` (422 `CARRERA_NO_ELEGIBLE`), `PeriodoAcademicoInvalidoException` (422 `PERIODO_ACADEMICO_INVALIDO`), `DependenciaNoDisponibleException` (503 `DEPENDENCIA_NO_DISPONIBLE`), `NoAutenticadoException` (401 `NO_AUTENTICADO`). Todas heredan de `DomainException` vía `ExcepcionReglaNegocio` cuando aplica.
- Puertos en `Domain/Interfaces/`: `IPostulanteRepository` (`ObtenerPorIdAsync`, `ObtenerPorUsuarioIdAsync`, `ExistePorNumeroDocumentoAsync`, `AgregarAsync`), `IPeriodoAcademicoClient` (`ObtenerPeriodoActualAsync`), `ICatalogoAcademicoClient` (`ObtenerUniversidadAsync`), y contratos de lectura `PeriodoAcademicoActual`, `UniversidadAliadaCatalogo` y `CarreraCatalogo` en `Domain/Interfaces/Contratos/`.

**Alternativas descartadas:** modelar `DatosPersonales` como objeto de valor separado (el DDD 2.3.A define esos campos directamente en `Postulante`) y usar un `ValueObject` genérico con reflexión (el Shared Kernel ya expone un marcador `record class`).

### 2. Identidad autenticada

- `IUsuarioActual` en `Application/Interfaces/` expone `UsuarioId` y `EstaAutenticado`; el adaptador `UsuarioActualJwt` en `Api/Seguridad/` lee el claim `sub` del `HttpContext.User`.
- `Program.cs` configura `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` con `Authority` y `Audience` desde `Keycloak:Authority` y `Keycloak:Audience`, `MapInboundClaims = false`, `RoleClaimType = "roles"` y `RequireHttpsMetadata` desde configuración (falso solo en desarrollo). Si no hay `Authority` configurada, la aplicación arranca igual y los endpoints protegidos responden `401`.
- `PostulantesController` usa `[Authorize]`; el handler obtiene el `usuarioId` del puerto y lanza `NoAutenticadoException` si no hay sujeto válido. El cliente nunca envía `usuarioId`.
- **Alternativas descartadas:** recibir `usuarioId` en el cuerpo (permite suplantación) y `X-Usuario-Id` (convención no autenticada).
- Se añade `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.15 a `Directory.Packages.props` y al proyecto `Api`; el lock file se regenera con `dotnet restore`.

### 3. Casos de uso y validaciones externas

- `RegistrarPostulanteCommand` (+ handler y validador FluentValidation) en `Application/Postulantes/Comandos/RegistrarPostulante/`:
  1. Resolver el usuario autenticado.
  2. Verificar duplicados con `IPostulanteRepository` (`usuarioId` y `numeroDocumentoIdentidad`) para dar errores tempranos.
  3. Consultar el periodo vigente con `IPeriodoAcademicoClient` y exigir coincidencia con `periodoAcademicoId`; cualquier fallo de transporte devuelve `DEPENDENCIA_NO_DISPONIBLE`.
  4. Consultar la universidad con `ICatalogoAcademicoClient`; validar `Estado == "ACTIVA"`, `TieneConvenioVigente == true` y que la carrera exista con `Activa == true`.
  5. Crear el agregado, agregarlo al repositorio y confirmar con `IUnitOfWork.GuardarCambiosAsync(cancellationToken)`.
  6. Devolver `PostulanteDto`.
- `ObtenerMiPerfilQuery` (+ handler) busca por `usuarioId` y devuelve `PostulanteDto` o lanza `PostulanteNoEncontradoException`.
- `TimeProvider` (inyectado como `TimeProvider.System`) sustituye a una abstracción propia de reloj y permite pruebas deterministas sin paquetes adicionales.
- DTOs en `Application/Postulantes/DTOs/`: `PostulanteDto`, `DatosContactoDto` y `PostulanteMapeador`.

### 4. Adaptadores de integración

- `PeriodoAcademicoClient` en `Infrastructure/Adapters/`:
  - `GET {ServiciosExternos:Plataforma:UrlBase}/internal/v1/periodos-academicos/actual`.
  - `HttpClient` con `Timeout` de 3 segundos; timeout, `5xx`, `404` o JSON inválido se traducen a `DependenciaNoDisponibleException` (fail-closed).
  - En `Development`, si la dependencia falla y `PeriodoActual:FallbackId` está configurado, se usa ese identificador; en otros entornos no hay respaldo.
- `CatalogoAcademicoClient` en `Infrastructure/Adapters/`:
  - `GET {ServiciosExternos:Programas:UrlBase}/api/v1/universidades/{id}`.
  - Mismo timeout y política fail-closed. `404` de la universidad se traduce a `UniversidadNoElegibleException`; una carrera ausente o inactiva a `CarreraNoElegibleException`.
  - Queda preparado para migrar a `/internal/v1/...` cuando se acuerde el contrato interno con `MS-PRG`; no se modifica `ms-prg`.
- Registro con `AddHttpClient<IPeriodoAcademicoClient, PeriodoAcademicoClient>` y su equivalente de catálogo en `Infrastructure/DependencyInjection.cs`. Si falta la URL base, el adaptador falla cerrado en la primera llamada.
- Configuración nueva en `appsettings.json`:
  - `Keycloak:Authority`, `Keycloak:Audience`, `Keycloak:RequireHttpsMetadata`.
  - `ServiciosExternos:Plataforma:UrlBase`, `ServiciosExternos:Programas:UrlBase`, `ServiciosExternos:TimeoutSegundos`.
  - `PeriodoActual:FallbackId` (vacío por defecto).

### 5. Persistencia y migración

- `PostulanteConfiguracion` en `Infrastructure/Persistence/Configuraciones/`:
  - Tabla `postulantes` (esquema `postulaciones`), usando `ConfiguracionPersistencia.ConfigurarEntidadAuditable`.
  - `DatosContacto` con `OwnsOne` a columnas `correo_electronico` y `telefono`.
  - Enums con `ComoCodigoEnum` y `CHECK` con `ConfiguracionPersistencia.ValoresSql<TEnum>()`.
  - Índices únicos `ux_postulante_numero_documento_identidad` y `ux_postulante_usuario_id`.
  - `CHECK`: `acepta_tratamiento_datos = true`, longitud mínima de identificación/nombres y condición académica válida.
- `PostulanteRepository` en `Infrastructure/Persistence/Repositorios/`: lectura con `AsNoTracking()` y escritura con tracking.
- `PostulacionesDbContext`: `DbSet<Postulante> Postulantes` y traducción en `GuardarCambiosAsync` de `DbUpdateException` cuyo `PostgresException.SqlState == "23505"`:
  - `ux_postulante_numero_documento_identidad` → `IdentidadDuplicadaException`.
  - `ux_postulante_usuario_id` → `PerfilYaRegistradoException`.
  - Cualquier otra unicidad → `ExcepcionConflictoConcurrencia` con código `RESTRICCION_UNICIDAD` (409).
- Migración `RegistroPostulantes` generada con `dotnet-ef`. Como no hay manifiesto local, se crea `Apps/services/ms-postulaciones/.config/dotnet-tools.json` con `dotnet-ef` 10.0.15, se ejecuta `dotnet tool restore` y luego `dotnet ef migrations add RegistroPostulantes --project Ascenso.Postulaciones.Infrastructure --startup-project Ascenso.Postulaciones.Api`.
- La migración no se aplica automáticamente al arrancar (la fundación no lo hace) para permitir iniciar la API sin PostgreSQL; se aplica en despliegue o con `dotnet ef database update`.

### 6. API

- `Api/Controllers/v1/PostulantesController.cs` con `[Route("api/v1/postulantes")]` y `[Authorize]`:
  - `POST /api/v1/postulantes` → `RegistrarPostulanteCommand` → `201 Created` (`PostulanteDto`) con `Location`; errores `400`, `401`, `409`, `422`, `503`.
  - `GET /api/v1/postulantes/mio` → `ObtenerMiPerfilQuery` → `200 OK` (`PostulanteDto`) o `404 Not Found`.
- `Program.cs` añade `UseAuthentication()` antes de `UseAuthorization()`.
- Registro de handlers en `Application/DependencyInjection.cs` (`AddScoped<RegistrarPostulanteCommandHandler>()` y `AddScoped<ObtenerMiPerfilQueryHandler>()`).

## Risks / Trade-offs

- **[Riesgo] El catálogo de `MS-PRG` usa una ruta pública que puede cambiar a `/internal/v1`.** → Mitigación: el adaptador encapsula la ruta y la configuración de URL base; solo se modifica el adaptador cuando se acuerde el contrato interno.
- **[Riesgo] `MS-PLT` no existe y el periodo real no puede validarse.** → Mitigación: fail-closed en todos los entornos y respaldo explícito solo en `Development`; la deuda queda documentada.
- **[Riesgo] Duplicados concurrentes que superan la verificación previa.** → Mitigación: los índices únicos de PostgreSQL son la autoridad final y se traducen a excepciones de dominio con códigos estables.
- **[Riesgo] Keycloak no está disponible en local ni en CI.** → Mitigación: las pruebas de endpoints usan un esquema de autenticación de prueba y el arranque no exige `Authority`.
- **[Riesgo] El nuevo paquete de autenticación rompe el restore en modo bloqueado.** → Mitigación: actualizar `Directory.Packages.props`, regenerar los lock files y verificar con `dotnet restore --locked-mode` después.
- **[Riesgo] Falta de auditoría/eventos en el registro.** → Deuda registrada para un cambio posterior que implemente Outbox y `PostulanteRegistrado`; no se bloquea la HU 1.1.

## Migration Plan

1. Añadir el paquete JWT y regenerar lock files.
2. Crear el manifiesto de `dotnet-ef`, generar la migración `RegistroPostulantes` y verificar el modelo con `dotnet ef migrations has-pending-model-changes`.
3. Desplegar la migración en el entorno correspondiente (`postulaciones`); rollback con la migración descendente o `dotnet ef database update <migración-anterior>`.
4. Configurar `Keycloak`, `ServiciosExternos` y (solo en desarrollo) `PeriodoActual:FallbackId`.

## Open Questions

- El contrato definitivo del periodo académico vigente con `MS-PLT` (nombre y forma del identificador) se confirmará cuando ese microservicio exista; el puerto ya aísla el cambio.
- El claim de rol que habilita el registro de postulantes se definirá con el equipo de identidad; por ahora el endpoint solo exige autenticación, no un rol específico.
