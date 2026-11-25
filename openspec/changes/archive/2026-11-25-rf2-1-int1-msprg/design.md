# Design

## Context

Ver `proposal.md` para la motivación. El estado actual relevante es:

- `MS-PRG` ya tiene `ProgramaBeca` con estado `BORRADOR`, `CONFIGURADO`, `ACTIVO` y `CERRADO`, cupos disponibles calculados, presupuesto asignado y convenios/carreras en `UniversidadAliada`.
- No existe ningún agregado `Convocatoria` en el repositorio. `MS-POS` solo referencia `convocatoriaId` en comités y rúbricas, sin contrato de lectura.
- `MS-PRG` no tiene autenticación JWT configurada, aunque sí referencia los paquetes de Hangfire y tiene el `ProgramasDbContext` con esquema `programas`.
- El DDD maestro ubica `Convocatoria` en `MS-PRG` y define `GET /internal/v1/convocatorias/*` como contrato hacia `MS-POS`.
- El periodo académico es responsabilidad de `MS-PLT`; hoy `MS-PRG` solo guarda identificadores como texto.

## Goals / Non-Goals

**Goals:**

- Implementar el ciclo `BORRADOR → PUBLICADA → CERRADA` con validaciones locales de programa, cupos, presupuesto y convenios.
- Exponer un contrato interno estable para que `MS-POS` consulte convocatorias sin compartir base de datos.
- Aplicar PBAC granular en `MS-PRG` sin alterar las políticas por rol existentes de otros servicios.
- Garantizar la zona horaria `America/Caracas` y las restricciones de integridad en PostgreSQL.
- Programar el cierre automático de forma segura ante múltiples instancias.

**Non-Goals:**

- Crear endpoints de postulación, elegibilidad o carga de archivos en `MS-POS`.
- Reservar cupos del programa al publicar; esta HU solo valida y revalida disponibilidad.
- Activar programas ni modificar las reglas de `ProgramaBeca`.
- Actualizar el realm de Keycloak ni migrar las políticas por rol de Rúbricas/Comités a PBAC.

## Decisions

### 1. El agregado vive en `MS-PRG` y encapsula criterios y documentos

`Convocatoria` es la raíz del agregado. `CriteriosElegibilidadCiclo` es un objeto de valor inmutable y `DocumentoRequerido` es una entidad hija. Las listas de universidades y carreras permitidas se guardan como `jsonb`, siguiendo el patrón ya usado por `CriteriosElegibilidadPrograma` en `ProgramaBeca`, para evitar tablas puente sin comportamiento propio.

La raíz expone métodos expresivos (`Crear`, `Actualizar`, `AgregarDocumento`, `QuitarDocumento`, `Publicar`, `Cerrar`) y no setters públicos. Las transiciones inválidas lanzan excepciones de dominio que el `ManejadorExcepcionesDominio` existente traduce a `ProblemDetails`.

**Alternativas descartadas:** crear el agregado en `MS-POS` (contradice el DDD y obliga a duplicar datos de cupos/convenios) o guardar las listas en tablas puente (más joins sin reglas propias).

### 2. Validación local mediante un puerto de lectura dedicado

Para no modificar los repositorios de RF-11 a RF-14, se crea un puerto de aplicación `ICatalogoConvocatoriaReader` implementado en infraestructura con consultas EF sobre las tablas existentes. Devuelve una instantánea del programa, sus cupos disponibles, presupuesto, convenios vigentes y carreras elegibles. El dominio recibe esa instantánea y aplica las reglas sin depender de EF Core.

La creación, edición y publicación se ejecutan dentro de una transacción y bloquean la fila del programa (`SELECT ... FOR UPDATE`) antes de validar cupos. Así dos convocatorias concurrentes no pueden sobreasignar el mismo cupo dentro de `MS-PRG`.

**Alternativas descartadas:** llamar por HTTP a los endpoints públicos de programas (acoplamiento innecesario dentro del mismo servicio) o extender `IProgramaBecaRepository` con métodos de convenios (mezcla responsabilidades de otros RF).

### 3. Validación del periodo académico por HTTP interno

Se define el puerto `IPeriodoAcademicoClient` en la capa de aplicación y un adaptador `PeriodoAcademicoClient` en infraestructura que consulta `GET /internal/v1/periodos-academicos/{id}` en `MS-PLT`. El identificador `{id}` y el campo `id` de la respuesta son UUID (formato de ruta `D`); por ello, `periodo_academico_id` admite hasta 36 caracteres. El cliente consume y valida `id`, `nombre` y `estado`; no consume `fechaInicio` ni `fechaFin`. Un `404` representa un ID inexistente. MS-PRG valida la vigencia con `estado == VIGENTE` y falla cerrado con `503` si MS-PLT no responde o devuelve una respuesta inválida. El mecanismo de autenticación de la ruta interna queda por coordinar con el responsable de MS-PLT y no implica cambios al realm compartido.

**Dependencia:** el endpoint por identificador debe existir en `MS-PLT`; el actual solo expone `/actual`. Si no está disponible en el entorno de integración, la creación de convocatorias queda bloqueada, lo cual es preferible a aceptar un periodo inexistente.

### 4. Fechas normalizadas en `America/Caracas`

Un componente de aplicación `CalendarioConvocatoria` convierte las fechas de entrada a instantes:

- `fechaApertura`: `00:00:00` de Caracas.
- `fechaCierre`: `23:59:59` de Caracas.
- `fechaLimiteSubsanacionDocumentos`: `23:59:59` de Caracas.
- `fechaInicioEvaluacion`: `00:00:00` de Caracas.
- `fechaCierreEvaluacion`: `23:59:59` de Caracas.

Se usa `TimeZoneInfo.FindSystemTimeZoneById("America/Caracas")` y se persiste `DateTimeOffset` en columnas `timestamptz`. Las reglas de cronología se validan en el dominio sobre los instantes ya normalizados, incluyendo la regla corregida: con revisión previa `true`, el inicio de evaluación debe ser posterior o igual al cierre; con `false`, puede iniciar desde la apertura.

### 5. Persistencia con restricciones en PostgreSQL

Tabla `programas.convocatorias`:

- `id uuid` PK, `programa_beca_id uuid` FK a `programas.programas_beca`, `periodo_academico_id varchar(36)` con UUID de MS-PLT.
- `codigo varchar(40)` `NOT NULL` con índice único `ux_convocatoria_codigo`.
- `nombre varchar(200)`, `estado varchar(20)`.
- `fecha_apertura`, `fecha_cierre`, `fecha_limite_subsanacion_documentos`, `fecha_inicio_evaluacion`, `fecha_cierre_evaluacion` como `timestamptz`.
- `requiere_revision_documental_previa boolean NOT NULL DEFAULT true`.
- `cupos_asignados integer`.
- Campos de criterios: `promedio_minimo numeric(5,2)`, `edad_minima`, `edad_maxima`, `tope_ingreso_familiar numeric(18,2)`, `universidades_permitidas_ids jsonb`, `carreras_permitidas_ids jsonb`.
- `fecha_publicacion timestamptz NULL` y campos de auditoría.

Tabla `programas.documentos_requeridos`:

- `id uuid` PK, `convocatoria_id uuid` FK con `ON DELETE CASCADE`.
- `tipo_documento`, `nombre`, `descripcion`, `obligatorio`, `tipos_mime_permitidos jsonb`, `tamanio_maximo_mb`.
- Campos de auditoría.

Restricciones: `cupos_asignados >= 1`, `edad_minima <= edad_maxima`, `promedio_minimo BETWEEN 0 AND 20`, `fecha_cierre > fecha_apertura`, `estado` dentro del catálogo y `requiere_revision_documental_previa` no nulo. La violación de `ux_convocatoria_codigo` se traduce en `ProgramasDbContext` a `409 CODIGO_CONVOCATORIA_DUPLICADO`.

**Alternativa descartada:** almacenar las fechas como `date` (el requisito exige instantes UTC y el cierre a las 23:59:59).

### 6. Seguridad PBAC con proveedor dinámico

Se agrega `Microsoft.AspNetCore.Authentication.JwtBearer` a `MS-PRG` y se configura Keycloak en `Program.cs` con `MapInboundClaims = false`, `RoleClaimType = roles` y validación de emisor/audiencia según configuración. Se registra `PermissionPolicyProvider` antes de `AddAuthorization` para que `GetPolicyAsync` consulte primero las políticas nombradas y, si no existe, construya una política `RequireAuthenticatedUser().RequireClaim("permissions", policyName)`.

El token conserva `roles` y agrega `permissions` como un arreglo JSON real de strings individuales con formato `recurso:accion` (por ejemplo, `["convocatorias:crear", "convocatorias:leer"]`). Cada valor se valida como un claim independiente; no se interpreta JSON ni listas delimitadas dentro de un único string. En esta HU, PBAC aplica únicamente a las políticas `convocatorias:*`; las políticas por roles de otros módulos permanecen sin cambios.

Los endpoints usan `[Authorize(Policy = "convocatorias:crear")]`, etc. Un `ManejadorResultadoAutorizacionConvocatorias` devuelve `403` con `codigo = PERMISO_DENEGADO` cuando la política es de convocatorias. Los tests usan un esquema de autenticación de prueba con claims configurables, sin depender de Keycloak.

**Riesgo de despliegue:** el realm actual no emite `permissions`; hasta que el equipo de identidad agregue el mapper, los endpoints responderán `403` en entornos reales. Se documenta como dependencia y no se modifica el realm de `ms-becas`.

### 7. Cierre automático con Hangfire

Se registra Hangfire con almacenamiento PostgreSQL sobre la misma base de `Programas` y un servidor en `MS-PRG`. El job recurrente `CerrarConvocatoriasVencidas` se ejecuta cada minuto, busca convocatorias `PUBLICADA` con `fecha_cierre <= ahora`, las transiciona a `CERRADA` y publica `ConvocatoriaCerrada`. La operación es idempotente y tolera ejecuciones concurrentes; Hangfire evita solapamientos por identificador de job.

**Alternativa descartada:** `BackgroundService` con temporizador en memoria, porque no sobrevive reinicios ni coordina múltiples instancias.

### 8. Contrato de API y errores

Público:

- `POST /api/v1/convocatorias` → `201`, `convocatorias:crear`.
- `PUT /api/v1/convocatorias/{id}` → `200`, `convocatorias:editar`.
- `GET /api/v1/convocatorias` y `GET /api/v1/convocatorias/{id}` → `200`, `convocatorias:leer`.
- `POST /api/v1/convocatorias/{id}/publicar` → `200`, `convocatorias:publicar`.
- `POST /{id}/documentos-requeridos` y `DELETE /{id}/documentos-requeridos/{documentoId}` → `convocatorias:editar`.

Interno:

- `GET /internal/v1/convocatorias/{id}` y `GET /internal/v1/convocatorias/abiertas` → contrato de solo lectura para `MS-POS`, autenticado con el mismo esquema y permiso `convocatorias:leer`.

Los errores de negocio usan `ProblemDetails` con `codigo` y los estatus definidos en la especificación (`400`, `409`, `422`). La paginación sigue el `PaginadoResponse<T>` existente en `MS-PRG`.

## Risks / Trade-offs

- **Sin reserva de cupos:** dos convocatorias podrían validar el mismo cupo en momentos distintos. Mitigación: bloqueo `FOR UPDATE` del programa durante la validación y revalidación al publicar; una reserva formal queda para una HU posterior.
- **Periodo académico dependiente de `MS-PLT`:** si el endpoint por identificador no existe, no se puede crear la convocatoria. Mitigación: contrato documentado y *fail-closed*; coordinar con el equipo de plataforma antes de la integración.
- **Realm sin `permissions`:** los endpoints PBAC fallarán cerrados en entornos reales. Mitigación: mapper de Keycloak como dependencia y pruebas con esquema de autenticación de prueba.
- **Job de cierre con reloj del servidor:** se usa la hora UTC del servidor y se normaliza contra `America/Caracas`; una deriva del reloj podría cerrar antes o después. Mitigación: comparar instantes UTC y monitorear ejecuciones del job.
- **Propiedad del microservicio:** `MS-PRG` pertenece a otro integrante. Mitigación: cambio limitado a un módulo nuevo, sin modificar RF-11 a RF-14, y coordinación explícita antes de aplicar.

## Migration Plan

1. Aprobar el cambio con el responsable de `MS-PRG` y actualizar el roadmap para reflejar `RF-02` en `MS-PRG` además de `MS-POS`.
2. Agregar el paquete JWT Bearer y la configuración de Keycloak; validar el arranque sin `Authority` (los endpoints protegidos responden `401`).
3. Crear la migración `Convocatorias` y aplicarla en un entorno limpio; verificar índice único y CHECKs con pruebas de integración.
4. Registrar Hangfire y el job recurrente; verificar que una convocatoria vencida pasa a `CERRADA` y que una futura no cambia.
5. Desplegar primero el contrato interno y luego el consumidor de `MS-POS` en su propia rama.
6. Rollback: deshabilitar el job, detener el despliegue de la API y revertir la migración solo si no existen datos productivos; las tablas nuevas no afectan a los módulos existentes.

## Open Questions

- Confirmar con el equipo de identidad el nombre final del claim (`permissions`) y si el mapper se aplicará por rol compuesto o por client scope.
- Confirmar con `MS-PLT` el contrato final y el mecanismo de autenticación de `GET /internal/v1/periodos-academicos/{id}`.
