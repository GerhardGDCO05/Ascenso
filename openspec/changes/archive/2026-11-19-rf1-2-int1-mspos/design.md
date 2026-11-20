# Design: Perfil Socioeconómico y Grupo Familiar (`rf1-2-int1-mspos`)

## Context

Ver `proposal.md` para la motivación. La HU 1.1 ya dejó implementados el agregado `Postulante`, la autenticación JWT, el repositorio EF Core, la traducción de errores de dominio y las pruebas de endpoint. El perfil nace en `INCOMPLETO` y hoy no tiene datos socioeconómicos ni composición familiar.

Restricciones relevantes:

- `Domain` debe permanecer puro, sin EF Core, ASP.NET Core ni mensajería.
- No se permiten setters públicos en propiedades mutables del dominio.
- Los enums se persisten como códigos `VARCHAR` en mayúsculas mediante `CodigoEnum`.
- La versión optimista existente (`Version`) debe proteger las actualizaciones concurrentes.
- No hay que modificar otros microservicios ni introducir dependencias externas nuevas.
- La regla `RN-POS-01` del DDD debe ajustarse para admitir composición familiar vacía.

## Goals / Non-Goals

**Goals:**

- Ampliar el agregado `Postulante` con el perfil socioeconómico y la composición familiar.
- Calcular en el servidor la cantidad de integrantes y el ingreso familiar mensual total.
- Permitir lista familiar vacía y completar el perfil con una sección socioeconómica válida.
- Persistir los cambios con migración, restricciones e índices.
- Exponer un `PUT` idempotente y ampliar la consulta del perfil propio.
- Mantener la confidencialidad y el aislamiento por usuario.

**Non-Goals:**

- Implementar el frontend, endpoints para evaluadores o reportes administrativos.
- Implementar `RF-04`, ranking o snapshots de postulación.
- Publicar eventos de integración o implementar Outbox.
- Cambiar la autenticación, el catálogo académico o el calendario de `MS-PLT`.

## Decisions

### 1. Modelo de dominio

- `Postulante` conserva los datos de HU 1.1 y agrega, con setters privados:
  - `EstadoCivil?`, `PromedioActual?`, `Trabaja?`, `Ocupacion?`, `IngresoMensualPostulante?`.
  - `NumeroIntegrantesFamilia?`, `IngresoFamiliarMensualTotal?`.
  - `IReadOnlyCollection<MiembroFamiliar> MiembrosFamiliares`.
- Los campos socioeconómicos son nullable para representar perfiles de HU 1.1 que aún no completaron la sección. Un perfil `COMPLETO` debe tenerlos todos no nulos, excepto que la lista familiar puede ser vacía.
- `MiembroFamiliar` es una entidad dentro del agregado `Postulante`, no un agregado independiente. Contiene `Id`, `PostulanteId`, `Parentesco`, `Edad`, `Ocupacion`, `IngresoMensual` y los campos de auditoría del Shared Kernel.
- Se crea `DatosMiembroFamiliar` como objeto de valor inmutable para validar y transportar los datos de cada familiar antes de construir la entidad.
- Método de dominio `RegistrarPerfilSocioeconomico(...)`:
  1. Valida enums, rangos y coherencia laboral.
  2. Reemplaza la colección de familiares.
  3. Calcula `NumeroIntegrantesFamilia = MiembrosFamiliares.Count + 1`.
  4. Calcula `IngresoFamiliarMensualTotal = IngresoMensualPostulante + suma(IngresoMensual de familiares)`.
  5. Actualiza la auditoría, incrementa `Version` y establece `EstadoPerfil = Completo`.
- El cálculo monetario se redondea a dos decimales con `MidpointRounding.AwayFromZero`; los valores de entrada deben tener como máximo dos decimales.
- La lista familiar se expone como solo lectura. La mutación ocurre únicamente mediante el método de dominio.

**Alternativas descartadas:**

- **Agregado `GrupoFamiliar` separado:** obligaría a coordinar dos transacciones para mantener la consistencia del perfil y complicaría la transición a `COMPLETO`. El DDD ya modela `MiembroFamiliar` dentro de `Postulante`.
- **Campos socioeconómicos no nullable con valores por defecto:** haría parecer datos válidos donde no los hay y rompería la semántica de `INCOMPLETO`.
- **`Ocupacion` como texto libre:** el catálogo cerrado evita valores inconsistentes y facilita reportes futuros.

### 2. Catálogos y validaciones

- `EstadoCivil`: `SOLTERO`, `CASADO`, `DIVORCIADO`, `VIUDO`.
- `Parentesco`: `PADRE`, `MADRE`, `HERMANO`, `HIJO`, `PAREJA`, `OTRO`.
- `Ocupacion`: `EMPLEADO`, `INDEPENDIENTE`, `ESTUDIANTE`, `LABORES_HOGAR`, `JUBILADO`, `NO_TRABAJA`, `OTRO`.
- Descripciones de `Ocupacion` para documentación y frontend:
  - `EMPLEADO`: trabajador asalariado o dependiente, sector público o privado.
  - `INDEPENDIENTE`: trabajador por cuenta propia, comerciante o economía informal.
  - `ESTUDIANTE`: menor en edad escolar o familiar cursando estudios.
  - `LABORES_HOGAR`: persona dedicada al cuidado del hogar.
  - `JUBILADO`: persona retirada o pensionada.
  - `NO_TRABAJA`: desempleado, niño pequeño o familiar sin actividad laboral.
  - `OTRO`: cualquier otra ocupación no contemplada.
- `PromedioActual`: `0.00–20.00`.
- `Edad` de familiar: `0–120`.
- Ingresos: `>= 0`, máximo dos decimales.
- `Ocupacion` obligatoria para el postulante y para cada familiar.
- Coherencia laboral:
  - `Trabaja = true` permite `EMPLEADO`, `INDEPENDIENTE` u `OTRO`.
  - `Trabaja = false` permite `ESTUDIANTE`, `LABORES_HOGAR`, `JUBILADO`, `NO_TRABAJA` u `OTRO`.
  - El ingreso no se fuerza a cero cuando `Trabaja = false`, para admitir pensiones, rentas o apoyo económico.
- La validación de entrada se implementa con FluentValidation en `Application`; el dominio repite las invariantes críticas para no depender del transporte.

### 3. Aplicación y contrato de actualización

- Nuevo `ActualizarPerfilSocioeconomicoCommand` con:
  - `EstadoCivil`, `PromedioActual`, `Trabaja`, `Ocupacion`, `IngresoMensualPostulante`.
  - `IReadOnlyList<DatosMiembroFamiliarCommand> MiembrosFamiliares`.
- El comando no incluye `NumeroIntegrantesFamilia` ni `IngresoFamiliarMensualTotal`; si el cliente los envía, se ignoran.
- `ActualizarPerfilSocioeconomicoCommandHandler`:
  1. Obtiene el usuario autenticado con `IUsuarioActual`.
  2. Carga el perfil con una consulta de actualización con tracking.
  3. Si no existe, lanza `PostulanteNoEncontradoException`.
  4. Convierte los códigos de catálogo y construye los objetos de valor.
  5. Invoca `Postulante.RegistrarPerfilSocioeconomico(...)`.
  6. Guarda con `IUnitOfWork.GuardarCambiosAsync(cancellationToken)`.
  7. Devuelve `PostulanteDto`.
- Se amplía `PostulanteDto` con campos nullable y `MiembrosFamiliares`; el mapeador expone los enums como códigos y los cálculos persistidos.
- Se agrega `MiembroFamiliarDto` con `Id`, `Parentesco`, `Edad`, `Ocupacion` e `IngresoMensual`.
- El endpoint es `PUT /api/v1/postulantes/mio/socioeconomico`; semánticamente reemplaza la sección completa y devuelve `200 OK`. No se crea un endpoint separado de lectura porque `GET /mio` ya devuelve el perfil completo.

**Alternativas descartadas:**

- **CRUD separado de familiares:** genera más endpoints y estados intermedios; un formulario de perfil se guarda mejor como una sección completa.
- **Aceptar el total desde el cliente:** permitiría inconsistencias contables y contradice `RN-POS-01`.

### 4. Persistencia y migración

- `postulantes` agrega columnas nullable:
  - `estado_civil varchar(20)`, `promedio_actual numeric(5,2)`, `trabaja boolean`.
  - `ocupacion varchar(30)`, `ingreso_mensual_postulante numeric(18,2)`.
  - `numero_integrantes_familia integer`, `ingreso_familiar_mensual_total numeric(18,2)`.
- Nueva tabla `miembros_familiares`:
  - `id uuid` PK, `postulante_id uuid` FK a `postulantes` con borrado en cascada.
  - `parentesco varchar(20)`, `edad integer`, `ocupacion varchar(30)`, `ingreso_mensual numeric(18,2)`.
  - Campos de auditoría y `version` mediante `ConfiguracionPersistencia.ConfigurarEntidadAuditable`.
  - Índice `ix_miembros_familiares_postulante_id`.
- `CHECK` constraints:
  - Catálogos válidos de estado civil, parentesco y ocupación.
  - Promedio entre `0` y `20`; edad entre `0` y `120`; ingresos no negativos.
  - `numero_integrantes_familia >= 1`.
  - Un perfil `COMPLETO` debe tener todos los campos socioeconómicos no nulos.
- La relación uno-a-muchos se configura con `HasMany`/`WithOne`, campo de respaldo y `PropertyAccessMode.Field`.
- Las consultas de lectura incluyen `MiembrosFamiliares` y usan `AsNoTracking()`. La consulta de actualización usa tracking e incluye la colección.
- La migración `PerfilSocioeconomicoGrupoFamiliar` se genera con `dotnet-ef` y no se aplica automáticamente al arrancar.
- No se hace backfill de filas existentes: permanecen con campos nulos y `estado_perfil = INCOMPLETO`.

**Alternativas descartadas:**

- **Tabla JSON para la composición familiar:** impide restricciones relacionales y complica los cálculos y reportes.
- **Campos no nullable con default `0`:** distorsiona los perfiles incompletos.

### 5. API, seguridad y confidencialidad

- `PostulantesController` agrega la acción `PUT mio/socioeconomico` con `[Authorize]` y respuestas documentadas `200`, `400`, `401`, `404`, `409`, `422`.
- El `usuarioId` se obtiene del token; el cliente no puede indicar el propietario del perfil.
- `GET /mio` devuelve datos socioeconómicos y familiares solo al propietario.
- No se almacenan nombres de familiares; la respuesta incluye únicamente parentesco, edad, ocupación e ingreso.
- No se registran payloads socioeconómicos en logs ni se incluyen en eventos de integración.
- La concurrencia se resuelve con el `Version` existente: si dos actualizaciones compiten, una recibe `409 Conflict` mediante `ExcepcionConflictoConcurrencia`.

### 6. Pruebas

- **Dominio:** creación de `DatosMiembroFamiliar`, cálculo con lista vacía y con familiares, transición a `COMPLETO`, coherencia laboral, rangos, catálogos y ausencia de setters públicos.
- **Aplicación:** éxito con lista vacía y con familiares, perfil inexistente, validaciones, mapeo de DTO y persistencia del total.
- **Persistencia:** modelo EF, columnas nullable, tabla, FK, índices, `CHECK` y migración.
- **Integración:** `PUT` y `GET`, códigos `200/400/401/404/409/422`, aislamiento por usuario y respuesta con cálculos.
- **Verificación:** build Release, pruebas, formato, restore bloqueado y validación OpenSpec.

## Risks / Trade-offs

- **[Riesgo] La lista familiar vacía contradice la redacción actual de `RN-POS-01`.** → Mitigación: actualizar explícitamente `docs/Explicacion_del_DDD_V7.md` y la spec antes de archivar; dejar la decisión registrada en este diseño.
- **[Riesgo] Las filas de HU 1.1 no tienen datos socioeconómicos.** → Mitigación: columnas nullable, sin backfill y restricción de completitud solo para `estado_perfil = COMPLETO`.
- **[Riesgo] `Trabaja` y `Ocupacion` pueden contradecirse.** → Mitigación: matriz de coherencia validada en aplicación y dominio.
- **[Riesgo] El catálogo cerrado de ocupación puede quedarse corto.** → Mitigación: `OTRO` cubre casos no previstos; ampliar el enum requiere una migración de catálogo y un cambio de spec.
- **[Riesgo] Pérdida de identidad de familiares al reemplazar la lista.** → Mitigación: los familiares no se referencian desde otros agregados; si en el futuro se requiere historial, se modelará un cambio específico.
- **[Riesgo] Datos sensibles expuestos en logs o eventos.** → Mitigación: no se registran payloads, no se publican eventos con PII y solo el propietario accede.
- **[Riesgo] Conflictos de concurrencia en actualizaciones simultáneas.** → Mitigación: `Version` como token de concurrencia y traducción a `409`.
- **[Riesgo] `promedioActual` en escala 0–20 no aplica a todos los programas.** → Mitigación: se mantiene como decimal; una escala distinta requerirá una regla de configuración futura.

## Migration Plan

1. Implementar enums, objeto de valor, entidad y método de dominio.
2. Implementar comando, handler, validador y DTOs.
3. Configurar EF Core para las nuevas columnas y la tabla familiar.
4. Generar la migración `PerfilSocioeconomicoGrupoFamiliar` con `dotnet-ef`.
5. Aplicar la migración en el entorno correspondiente. Rollback: `dotnet ef database update <migración-anterior>`; elimina la tabla y las columnas nuevas.
6. Verificar un flujo real contra PostgreSQL local: registrar perfil base, actualizar sección socioeconómica con y sin familiares, consultar perfil y comprobar cálculos.
7. Actualizar documentación y roadmaps; no marcar `RF-01` completo.

## Open Questions

- Ninguna pregunta bloqueante. La escala de `promedioActual` se fija en `0.00–20.00` y los catálogos quedan cerrados según la decisión de negocio.
