# Tasks: Registro de Datos Personales y Académicos Base (`rf1-1-int1-mspos`)

## 1. Dependencias y tooling

- [x] 1.1 Añadir `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.15 a `Directory.Packages.props` y referenciarlo en `Ascenso.Postulaciones.Api.csproj`; verificar con `dotnet restore` y con `dotnet restore --locked-mode` que los lock files quedan consistentes.
- [x] 1.2 Crear `Apps/services/ms-postulaciones/.config/dotnet-tools.json` con `dotnet-ef` 10.0.15 y ejecutar `dotnet tool restore`; verificar con `dotnet ef --version` que la herramienta responde.

## 2. Dominio y documentación del modelo (`Ascenso.Postulaciones.Domain`, `Domain.Tests`)

- [x] 2.1 Crear `CondicionAcademicaIngreso` (`Admitido`, `Matriculado`) y `EstadoPerfil` (`Incompleto`, `Completo`) en `Domain/Enums/`; verificar con pruebas de `CodigoEnum` que serializan como `ADMITIDO`, `MATRICULADO`, `INCOMPLETO` y `COMPLETO`.
- [x] 2.2 Implementar `DatosContacto : ValueObject` en `Domain/Postulantes/ObjetosValor/` con normalización y validación de correo y teléfono; verificar con pruebas unitarias de valores válidos e inválidos.
- [x] 2.3 Crear las excepciones de dominio de la HU 1.1 en `Domain/Excepciones/` con sus códigos y estatus HTTP (`IDENTIDAD_DUPLICADA`, `PERFIL_YA_REGISTRADO`, `POSTULANTE_NO_ENCONTRADO`, `DATOS_PERSONALES_INVALIDOS`, `CONDICION_ACADEMICA_INVALIDA`, `UNIVERSIDAD_NO_ELEGIBLE`, `CARRERA_NO_ELEGIBLE`, `PERIODO_ACADEMICO_INVALIDO`, `DEPENDENCIA_NO_DISPONIBLE`, `NO_AUTENTICADO`); verificar con pruebas que exponen `Codigo` y `EstadoHttp`.
- [x] 2.4 Implementar `Postulante : AggregateRoot` en `Domain/Postulantes/Agregados/Postulante.cs` con factory `Crear(...)`, `EstadoPerfil.Incompleto`, `UuidV7.Nuevo()`, sin setters públicos y con validaciones de datos personales y aceptación de tratamiento; verificar con pruebas unitarias de creación exitosa y rechazos esperados.
- [x] 2.5 Crear los puertos y contratos de lectura en `Domain/Interfaces/` (`IPostulanteRepository`, `IPeriodoAcademicoClient`, `ICatalogoAcademicoClient`, `PeriodoAcademicoActual`, `UniversidadAliadaCatalogo`, `CarreraCatalogo`); verificar que `Domain` compila sin dependencias externas y que `ArchitectureTests` sigue en verde.
- [x] 2.6 Actualizar `docs/Explicacion_del_DDD_V7.md` (sub-modelo 2.3.A) para documentar `DatosContacto` y el estado `INCOMPLETO` del perfil en la HU 1.1; verificar que la sección coincide con las propiedades y códigos implementados.

## 3. Aplicación y pruebas (`Ascenso.Postulaciones.Application`, `Application.Tests`)

- [x] 3.1 Crear `IUsuarioActual` en `Application/Interfaces/` y los DTOs/mapeador de postulante (`PostulanteDto`, `DatosContactoDto`, `PostulanteMapeador`); verificar que `Application` compila sin referencias a ASP.NET Core ni EF Core.
- [x] 3.2 Implementar `RegistrarPostulanteCommand`, su handler y validador FluentValidation: resolver usuario, verificar duplicados, validar periodo y catálogo, crear el agregado y confirmar con `IUnitOfWork.GuardarCambiosAsync(cancellationToken)` usando `TimeProvider`; verificar con pruebas unitarias los caminos exitoso, duplicado, periodo inválido/no disponible y universidad/carrera no elegible.
- [x] 3.3 Implementar `ObtenerMiPerfilQuery` y su handler, incluyendo `404 POSTULANTE_NO_ENCONTRADO`; verificar con pruebas unitarias de perfil existente e inexistente.
- [x] 3.4 Registrar los handlers en `Application/DependencyInjection.cs`; verificar con una prueba que resuelve ambos handlers desde el contenedor y que los validadores se descubren por ensamblado.

## 4. Persistencia e integración externa (`Ascenso.Postulaciones.Infrastructure`, `IntegrationTests`)

- [x] 4.1 Implementar `PostulanteConfiguracion` en `Persistence/Configuraciones/` (tabla `postulantes`, `OwnsOne` de contacto, enums como código, índices únicos y `CHECK`) y registrar `DbSet<Postulante>` en `PostulacionesDbContext`; verificar con `ModeloPersistenciaPostulacionesTests` el esquema, `ValueGenerated.Never()` y las restricciones.
- [x] 4.2 Implementar `PostulanteRepository` en `Persistence/Repositorios/` con lectura `AsNoTracking()` y escritura con tracking; verificar con pruebas unitarias de repositorio o de modelo que las consultas por identificador y por usuario están configuradas.
- [x] 4.3 Traducir violaciones de unicidad PostgreSQL (`23505`) en `GuardarCambiosAsync` a `IdentidadDuplicadaException`, `PerfilYaRegistradoException` o `ExcepcionConflictoConcurrencia` con código `RESTRICCION_UNICIDAD`; verificar con pruebas del traductor usando excepciones simuladas.
- [x] 4.4 Implementar `PeriodoAcademicoClient` y `CatalogoAcademicoClient` con timeout de 3 segundos y política fail-closed, incluido el respaldo de periodo solo en `Development`; verificar con pruebas unitarias de respuesta válida, `404`, timeout y `5xx`.
- [x] 4.5 Registrar repositorio y clientes en `Infrastructure/DependencyInjection.cs`, añadir las claves de configuración a `appsettings.json` y generar la migración `RegistroPostulantes`; verificar con `dotnet ef migrations has-pending-model-changes` y con una prueba de que el script de creación contiene la tabla y los índices.

## 5. API y seguridad (`Ascenso.Postulaciones.Api`, `IntegrationTests`)

- [x] 5.1 Configurar JWT Bearer de Keycloak en `Program.cs` (`Authority`, `Audience`, `MapInboundClaims=false`, `RoleClaimType=roles`, `UseAuthentication()` antes de `UseAuthorization()`) y añadir `UsuarioActualJwt` en `Api/Seguridad/`; verificar con pruebas de `IUsuarioActual` para token con `sub`, sin `sub` y sin autenticación.
- [x] 5.2 Implementar `PostulantesController` con `[Authorize]` y los endpoints `POST /api/v1/postulantes` y `GET /api/v1/postulantes/mio`; verificar con pruebas de endpoint usando un esquema de autenticación de prueba y repositorio/clientes simulados los códigos `201`, `200`, `400`, `401`, `404`, `409`, `422` y `503`.
- [x] 5.3 Verificar que el `usuarioId` se toma del token y que un identificador enviado por el cliente se ignora; cubrir con una prueba de endpoint que el perfil creado pertenece al sujeto autenticado.

## 6. Verificación integral

- [x] 6.1 Ejecutar `dotnet build "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release` y `dotnet test` de la misma solución, verificando `0` advertencias y todas las pruebas en verde.
- [x] 6.2 Ejecutar `dotnet format "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --verify-no-changes --no-restore` y `dotnet restore --locked-mode`, verificando salida limpia.
- [x] 6.3 Ejecutar `openspec validate rf1-1-int1-mspos --strict` y `openspec validate --specs`, verificando que no hay hallazgos.
- [x] 6.4 Redactar la bitácora `openspec/changes/rf1-1-int1-mspos/log-YYYY-MM-DD-rf1-1-int1-mspos.md` con la evidencia real de comandos y resultados, y registrar la deuda técnica de Outbox/`PostulanteRegistrado` como seguimiento explícito.

## Workflow follow-up

- Archivar el cambio cuando la revisión del equipo esté aprobada y todas las tareas estén en `[x]`.
- Sincronizar el delta hacia `openspec/specs/ms-pos/postulantes/spec.md` al archivar.
- Marcar el avance de la HU 1.1 en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` sin marcar el `RF-01` completo hasta cubrir sus demás HUs.
