# Bitácora de Ejecución: `rf1-2-int1-mspos` (2026-11-19)

- **Cambio OpenSpec:** `openspec/changes/rf1-2-int1-mspos`
- **Requerimiento:** `RF-01` — Registro de postulantes y perfil socioeconómico (`HU 1.2`)
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones/`)
- **Integrante:** Integrante 1 (`int1`)

## Resumen de Implementación

1. **Dominio (`Ascenso.Postulaciones.Domain`):**
   - Enumeraciones `EstadoCivil`, `Parentesco` y `Ocupacion`, persistidas como códigos `VARCHAR` en mayúsculas.
   - Objeto de valor `DatosMiembroFamiliar` y entidad `MiembroFamiliar` dentro del agregado `Postulante`.
   - Ampliación de `Postulante` con datos socioeconómicos, colección familiar de solo lectura, cálculo de `NumeroIntegrantesFamilia` e `IngresoFamiliarMensualTotal`, transición a `EstadoPerfil.Completo` y actualización de auditoría/versión.
   - Nuevas excepciones de dominio para catálogos, datos socioeconómicos, coherencia laboral y miembros familiares.

2. **Aplicación (`Ascenso.Postulaciones.Application`):**
   - `ActualizarPerfilSocioeconomicoCommand` con handler y validadores FluentValidation.
   - Ampliación de `PostulanteDto`, nuevo `MiembroFamiliarDto` y actualización de `PostulanteMapeador`.
   - Aislamiento por `usuarioId` obtenido del token y reemplazo idempotente de la sección socioeconómica.

3. **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):**
   - Configuración EF Core de `MiembroFamiliar` y ampliación de `PostulanteConfiguracion`.
   - Columnas nuevas en `postulaciones.postulantes` y tabla `postulaciones.miembros_familiares` con FK en cascada, índice y restricciones `CHECK`.
   - Consultas de lectura y actualización con `Include` de familiares.
   - Migración `20261119004820_PerfilSocioeconomicoGrupoFamiliar` aplicada en PostgreSQL local.

4. **API (`Ascenso.Postulaciones.Api`):**
   - `PUT /api/v1/postulantes/mio/socioeconomico` con respuestas `200`, `400`, `401`, `404`, `409` y `422`.
   - `GET /api/v1/postulantes/mio` ampliado con datos socioeconómicos, cálculos y familiares.
   - Swagger documenta el nuevo endpoint y los esquemas de request/response en Development.

## Evidencia Real de Verificación

- **Compilación Release:**
  - Comando: `dotnet build "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release`
  - Resultado: `Compilación correcta. 0 Advertencia(s), 0 Errores`.

- **Suite de pruebas Release:**
  - Comando: `dotnet test "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release --no-build`
  - Resultados:
    - `Ascenso.Postulaciones.Domain.Tests`: **123/123** superadas.
    - `Ascenso.Postulaciones.Application.Tests`: **58/58** superadas.
    - `Ascenso.Postulaciones.ArchitectureTests`: **11/11** superadas.
    - `Ascenso.Postulaciones.IntegrationTests`: **60/60** superadas.
    - **Total: 252/252** superadas (`0` fallidas, `0` omitidas).

- **Formato:**
  - Comando: `dotnet format "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --verify-no-changes --no-restore`
  - Resultado: `exit code 0`, sin cambios pendientes.

- **Restore bloqueado:**
  - Comando: `dotnet restore "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --locked-mode`
  - Resultado: `exit code 0`, 7 proyectos restaurados correctamente.

- **Validación OpenSpec:**
  - Comando: `openspec validate rf1-2-int1-mspos --strict`
  - Resultado: `Change 'rf1-2-int1-mspos' is valid`.
  - Comando: `openspec validate --specs`
  - Resultado: `6 passed, 0 failed`.

- **Migración y esquema en PostgreSQL local (`postgres-mspos`, puerto `5433`):**
  - Migración aplicada: `20261119004820_PerfilSocioeconomicoGrupoFamiliar`.
  - Historial verificado en `__ef_migrations_history`: `20261114210403_RegistroPostulantes` y la nueva migración.
  - Columnas verificadas en `postulaciones.postulantes`: `estado_civil`, `promedio_actual`, `trabaja`, `ocupacion`, `ingreso_mensual_postulante`, `numero_integrantes_familia`, `ingreso_familiar_mensual_total`.
  - Tabla `postulaciones.miembros_familiares` verificada con clave foránea, índice y restricciones `CHECK`.

- **Flujo E2E de aplicación contra PostgreSQL real (tareas 5.2 y 5.3):**
  - Sin familiares: `numeroIntegrantesFamilia = 1`, `ingresoFamiliarMensualTotal = 1200.50`, `miembrosFamiliares = 0`, `estadoPerfil = COMPLETO`.
  - Con dos familiares: `numeroIntegrantesFamilia = 3`, `ingresoFamiliarMensualTotal = 2300.25`, `miembrosFamiliares = 2`, `estadoPerfil = COMPLETO`; 2 filas confirmadas en `miembros_familiares`.
  - Limpieza verificada: postulantes de prueba eliminados y `0` familiares restantes.

- **Flujo HTTP completo contra PostgreSQL efímero real (verificación temporal):**
  - Infraestructura: `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql` con migraciones aplicadas; repositorio, `UnitOfWork`, `DbContext` y PostgreSQL reales.
  - Autenticación JWT y clientes `MS-PRG`/`MS-PLT` sustituidos por dobles de prueba, porque Keycloak y esos servicios no estaban disponibles en el entorno local. No se modificó código productivo para esta verificación.
  - Casos verificados por HTTP:
    - `GET /api/v1/postulantes/mio` sin autenticación: `401 Unauthorized`.
    - `GET /api/v1/postulantes/mio` con usuario sin perfil: `404 Not Found`.
    - `POST /api/v1/postulantes` válido: `201 Created`, perfil `INCOMPLETO`.
    - `POST /api/v1/postulantes` con documento duplicado: `409 Conflict` traducido desde PostgreSQL.
    - `PUT /api/v1/postulantes/mio/socioeconomico` con dos familiares: `200 OK`, `numeroIntegrantesFamilia = 3`, `ingresoFamiliarMensualTotal = 2300.25`, `estadoPerfil = COMPLETO`.
    - `GET /api/v1/postulantes/mio`: `200 OK` con los mismos datos persistidos.
    - `PUT` con promedio fuera de rango (`25`): `400 Bad Request`.
    - `PUT` para usuario sin perfil: `404 Not Found`.
  - Verificación directa en PostgreSQL: 1 postulante con 2 filas en `miembros_familiares` y los cálculos esperados.
  - El test temporal se retiró después de la ejecución y no forma parte del entregable.

## Deuda Técnica Registrada

- **Outbox y evento `PostulanteRegistrado`:** se mantiene la deuda de `DEC-08`; no se publican eventos con información socioeconómica.
- **Integración real con Keycloak, `MS-PRG` y `MS-PLT`:** el flujo HTTP se validó con dobles de esos servicios; la verificación contra dependencias reales queda para el entorno de integración.
- **Frontend y consultas administrativas/evaluadores:** fuera de alcance de esta HU.

## Seguimiento de Workflow

- Archivar el cambio y sincronizar el delta hacia `openspec/specs/ms-pos/postulantes/spec.md`.
- Mantener `RF-01` sin marcar como completo hasta cubrir sus demás HUs.
