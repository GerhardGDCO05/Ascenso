# Proposal: Registro de Datos Personales y Académicos Base (`rf1-1-int1-mspos`)

## Why

El microservicio `MS-POS` (`Ascenso.Postulaciones`) ya cuenta con su fundación técnica (arquitectura hexagonal, Shared Kernel, persistencia y contrato de errores), pero aún no posee el agregado `Postulante` que da inicio al ciclo de admisión. Sin este registro, el postulante no puede iniciar su perfil, y los procesos posteriores de completitud de perfil (`RN-POS-01`), postulación a convocatorias (`RF-02`) y verificación de elegibilidad (`RF-04`) no tienen sobre qué operar.

El `RF-01` del PRD agrupa varias historias de usuario. Este cambio cubre exclusivamente la **HU 1.1: Registro de Datos Personales y Académicos Base**, dejando la información socioeconómica y la composición familiar para las HUs siguientes.

## What Changes

- **Nuevo agregado `Postulante`** en `MS-POS`, creado a partir de la identidad autenticada del usuario (`sub` del JWT de Keycloak), con:
  - Identificación (`numeroDocumentoIdentidad`), nombres y apellidos.
  - Datos de contacto (correo electrónico y teléfono) como objeto de valor inmutable `DatosContacto`.
  - Datos académicos base: `universidadAliadaId`, `carreraId`, `periodoActualId` y `condicionAcademica` (`ADMITIDO` o `MATRICULADO`).
  - Aceptación obligatoria del tratamiento de datos personales.
  - `estadoPerfil` que nace en `INCOMPLETO` y `fechaRegistro` generada con el reloj del sistema.
- **Unicidad de identidad y de perfil:** un `numeroDocumentoIdentidad` solo puede registrarse una vez (`409 IDENTIDAD_DUPLICADA`) y cada `usuarioId` autenticado puede tener un único perfil (`409 PERFIL_YA_REGISTRADO`).
- **Validación de datos académicos contra `MS-PRG`:** la universidad debe existir, estar activa y tener convenio vigente, y la carrera debe pertenecer a esa universidad y estar activa (`422 UNIVERSIDAD_NO_ELEGIBLE` / `CARRERA_NO_ELEGIBLE`). Si el catálogo no responde, la operación falla cerrada con `503 DEPENDENCIA_NO_DISPONIBLE`.
- **Validación del periodo académico actual:** se consulta al servicio de periodos y se rechaza un periodo que no corresponda al vigente (`422 PERIODO_ACADEMICO_INVALIDO`); si la dependencia no responde, `503 DEPENDENCIA_NO_DISPONIBLE`. En `Development` se admite un periodo de respaldo configurado mientras `MS-PLT` no exista.
- **Seguridad perimetral:** los endpoints públicos de postulantes exigen JWT válido de Keycloak y no confían en identificadores de usuario enviados por el cliente.
- **Consultas:** el postulante autenticado puede consultar su propio perfil; si aún no existe, `404 POSTULANTE_NO_ENCONTRADO`.
- **Persistencia y migración:** tabla `postulantes` en el esquema `postulaciones`, con índices únicos, restricciones `CHECK` y traducción de violaciones de unicidad a excepciones de dominio con código estable.

**Fuera de alcance de este cambio:**

- Datos socioeconómicos (`promedioActual`, `estadoCivil`, `numeroIntegrantesFamilia`, `ingresoFamiliarMensualTotal`) y `MiembroFamiliar`, que pertenecen a las HUs siguientes del `RF-01`.
- Transición del perfil a `COMPLETO` (`RN-POS-01`), que requiere datos académicos válidos y al menos un miembro familiar.
- `BorradorPostulacion`, `Postulacion`, convocatorias y verificación de elegibilidad (`RF-02`, `RF-03`, `RF-04`).
- Publicación de eventos de integración y `Transactional Outbox`. **Deuda técnica registrada:** implementar `PostulanteRegistrado` y el Outbox cuando se defina el contrato con `MS-AUD`/`MS-PLT`.
- Implementación real de los endpoints internos de periodos académicos (`MS-PLT`, `RF-38`) y su migración a `/internal/v1/...`.

## Capabilities

### New Capabilities

- `ms-pos/postulantes`: registro, unicidad, validación académica y consulta del perfil base del postulante en `MS-POS` (`RF-01`, HU 1.1), incluyendo la vinculación con la identidad autenticada y las dependencias de catálogo y periodo académico.

### Modified Capabilities

<!-- Ninguna capacidad existente modifica sus requisitos a nivel de contrato en este cambio. -->

## Impact

- **Especificaciones OpenSpec:** se añade el delta `openspec/changes/rf1-1-int1-mspos/specs/ms-pos/postulantes/spec.md`, que al archivarse se sincronizará hacia `openspec/specs/ms-pos/postulantes/spec.md`.
- **Dominio (`Ascenso.Postulaciones.Domain`):** nueva carpeta `Postulantes/` con el agregado `Postulante`, el objeto de valor `DatosContacto` y los contratos de consulta externa; nuevas enumeraciones `CondicionAcademicaIngreso` y `EstadoPerfil`; nuevas excepciones de negocio; nuevos puertos `IPostulanteRepository`, `IPeriodoAcademicoClient` y `ICatalogoAcademicoClient` en `Interfaces/`.
- **Aplicación (`Ascenso.Postulaciones.Application`):** comandos, consultas, DTOs, mapeador, validador FluentValidation, puerto `IUsuarioActual` y registro de handlers.
- **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):** configuración EF Core de `postulantes`, `PostulanteRepository`, traducción de violaciones de unicidad PostgreSQL, clientes HTTP de periodo y catálogo académico, y migración `RegistroPostulantes`.
- **API (`Ascenso.Postulaciones.Api`):** `PostulantesController` bajo `/api/v1/postulantes`, autenticación JWT Bearer de Keycloak y adaptador `IUsuarioActual` basado en el claim `sub`. Nueva dependencia `Microsoft.AspNetCore.Authentication.JwtBearer`.
- **Pruebas:** unitarias de dominio y aplicación, pruebas de modelo de persistencia y pruebas de endpoints con esquema de autenticación de prueba; sin dependencia de PostgreSQL ni Keycloak reales en CI.
- **Tooling:** manifiesto local de herramientas con `dotnet-ef` para generar la migración, y actualización de lock files por el nuevo paquete de autenticación.
- **Dependencias entre servicios:** `MS-PRG` para universidades/carreras (ruta pública actual, preparada para migrar a `/internal/v1/...`) y `MS-PLT` para el periodo académico vigente (contrato pendiente de implementación).
- **Documentación:** actualización de `docs/Explicacion_del_DDD_V7.md` (sub-modelo 2.3.A) para incorporar el objeto de valor de contacto en `Postulante`.
- **Roadmaps:** al completar la implementación se marcará el avance correspondiente del `RF-01` en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`, sin marcar el RF completo hasta cubrir sus demás HUs.
