# Bitácora de Ejecución: `rf1-1-int1-mspos` (2026-11-15)

- **Cambio OpenSpec:** `openspec/changes/rf1-1-int1-mspos`
- **Requerimiento:** `RF-01` — Registro de datos personales y académicos base (`HU 1.1`)
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones/`)
- **Integrante:** Integrante 1 (`int1`)

## Resumen de Implementación

1. **Dominio (`Ascenso.Postulaciones.Domain`):**
   - Enumeraciones `CondicionAcademicaIngreso`, `EstadoPerfil`, `EstadoPeriodoAcademico` y `EstadoUniversidadAliada`.
   - Objeto de valor `DatosContacto` con normalización y validación.
   - Agregado `Postulante` con `UuidV7`, estado inicial `INCOMPLETO`, validaciones de datos personales y aceptación del tratamiento de datos.
   - Excepciones de negocio con código estable y estatus HTTP, puertos de repositorio y clientes externos.

2. **Aplicación (`Ascenso.Postulaciones.Application`):**
   - `IUsuarioActual`, DTOs y `PostulanteMapeador`.
   - `RegistrarPostulanteCommandHandler` con validación FluentValidation, verificación de duplicados, validación de periodo/catálogo y confirmación transaccional.
   - `ObtenerMiPerfilQueryHandler` con `404 POSTULANTE_NO_ENCONTRADO`.

3. **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):**
   - `PostulanteConfiguracion`, `DbSet<Postulante>`, índices únicos y restricciones `CHECK`.
   - `PostulanteRepository` y `TraductorExcepcionesPersistencia` para SQLSTATE `23505`.
   - `PeriodoAcademicoClient` y `CatalogoAcademicoClient` fail-closed, con timeout y respaldo de periodo solo en Development.
   - Migración `RegistroPostulantes` aplicada en PostgreSQL local.

4. **API y seguridad (`Ascenso.Postulaciones.Api`):**
   - JWT Bearer de Keycloak con `MapInboundClaims=false` y `RoleClaimType=roles`.
   - `UsuarioActualJwt` para resolver el `sub`.
   - `PostulantesController` con `POST /api/v1/postulantes` y `GET /api/v1/postulantes/mio`.
   - Swagger UI habilitado únicamente en Development.

## Evidencia Real de Verificación

- **Compilación Release:**
  - Comando: `dotnet build "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release`
  - Resultado: `Compilación correcta. 0 Advertencia(s), 0 Errores`.

- **Suite de pruebas Release:**
  - Comando: `dotnet test "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release --no-build`
  - Resultados:
    - `Ascenso.Postulaciones.Domain.Tests`: **76/76** superadas.
    - `Ascenso.Postulaciones.Application.Tests`: **21/21** superadas.
    - `Ascenso.Postulaciones.ArchitectureTests`: **10/10** superadas.
    - `Ascenso.Postulaciones.IntegrationTests`: **44/44** superadas.
    - **Total: 151/151** superadas (`0` fallidas, `0` omitidas).

- **Formato:**
  - Comando: `dotnet format "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --verify-no-changes --no-restore`
  - Resultado: `exit code 0`, sin cambios pendientes.

- **Restore bloqueado:**
  - Comando: `dotnet restore "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --locked-mode`
  - Resultado: `exit code 0`, 7 proyectos restaurados correctamente.

- **Validación OpenSpec:**
  - Comando: `openspec validate rf1-1-int1-mspos --strict`
  - Resultado: `Change 'rf1-1-int1-mspos' is valid`.
  - Comando: `openspec validate --specs`
  - Resultado: `4 passed, 0 failed`.

- **Validación manual del usuario:**
  - Se probaron los casos borde y el registro normal desde Swagger con un entorno temporal de mocks.
  - Se verificó la inserción real en PostgreSQL (`postulaciones.postulantes`).
  - Los mocks temporales de autenticación y clientes HTTP fueron retirados después de la validación; no forman parte del entregable.

## Deuda Técnica Registrada

- **Outbox y evento `PostulanteRegistrado`:** no se implementan en la HU 1.1. Quedan como seguimiento explícito para un cambio posterior cuando se defina el contrato de integración con `MS-AUD`/`MS-PLT`, conforme a `DEC-08`.
- **Prueba E2E con dependencias reales:** la validación local se realizó con mocks temporales. La verificación contra Keycloak, `MS-PRG` y `MS-PLT` reales queda para el entorno de integración.

## Seguimiento de Workflow

- Archivar el cambio y sincronizar el delta hacia `openspec/specs/ms-pos/postulantes/spec.md`.
- Marcar el avance de la HU 1.1 en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`, sin marcar `RF-01` como completo.
