# Proposal

## Why

Tras completar RF-06 (comités, asignaciones y excepción de evaluador único), RF-07 (rúbricas publicadas y bloqueables) y RF-08 (registro de puntuaciones, envío definitivo con `puntajeTotalPonderado` y visibilidad post-cierre entre coevaluadores), `MS-POS` ya almacena las evaluaciones definitivas por postulación, pero aún no consolida una puntuación única por postulante ni detecta diferencias significativas entre evaluadores. La **HU 9.1** (primera historia de **RF-09**) es indispensable para calcular automáticamente el puntaje consolidado determinista (`ResultadoConsolidadoRanking`) al completarse las evaluaciones requeridas —incluyendo el soporte trazable de la excepción autorizada de evaluador único (`RN-POS-04`)—, levantar y auditar la revisión de alertas de discrepancia cuando la diferencia entre evaluadores supere el umbral configurado (DDD V7 §2.3.B, concurrencia `C5`) y dejar preparada la base arquitectónica y persistente sobre la que **HU 9.2** construirá el ranking ordenado, la línea de corte por cupo/presupuesto con `MS-PRG` y la confirmación final.

## What Changes

**Alcance: SOLO la HU 9.1 dentro de RF-09** (Integrante 2, `MS-POS`).

- **Nuevo agregado raíz `ResultadoConsolidadoRanking` y servicio de dominio puro `ServicioCalculoConsolidacion`** (vocabulario ubicuo de DDD V7 §2.3.B):
  - Calcula de forma determinista el `puntajeConsolidado` (`numeric(5,2)` ∈ `[0.00, 100.00]`) como promedio aritmético redondeado a 2 decimales (`MidpointRounding.AwayFromZero`) de las evaluaciones `ENVIADA_DEFINITIVA` de la postulación, la `diferenciaEntreEvaluadores` (`max - min`) y la `alertaDiscrepancia` (`diferencia > umbralDiscrepanciaPuntos`).
  - Exige al menos `evaluadoresMinimosRequeridos` evaluaciones definitivas (por defecto `2`, `RN-POS-04`), salvo que exista una `AutorizacionExcepcionEvaluadorUnico` (HU 6.3) para la postulación, en cuyo caso permite consolidar con `1` única evaluación definitiva registrando `excepcionEvaluadorUnico = true`, `justificacionEvaluadorUnico`, `autorizadorExcepcionId`, `diferenciaEntreEvaluadores = 0.00` y `alertaDiscrepancia = false`.
  - Declara el enum `EstadoRanking` (`PROVISIONAL`, `CONFIRMADO`, persistido con `CodigoEnum`) naciendo siempre en `PROVISIONAL` en HU 9.1 y rechazando mutaciones/recálculos si estuviera en `CONFIRMADO` (`409 RANKING_YA_CONFIRMADO`), preparando por OCP la confirmación final de HU 9.2 sin introducir todavía lógica de ranking.
  - Encapsula la revisión de discrepancia (`MarcarDiscrepanciaRevisada`) exigiendo `alertaDiscrepancia == true`, estado `PROVISIONAL`, comentario obligatorio (10–1000 caracteres) y no haber sido revisada previamente, registrando `revisadoPorUsuarioId` (del token) y `fechaRevisionDiscrepancia` (UTC del servidor).
- **Disparo automático e idempotente bajo concurrencia `C5`:**
  - Nuevo puerto/adaptador desacoplado en Aplicación `IConsolidadorPostulacionEvaluacion` → `ConsolidadorPostulacionEvaluacionAdapter` (patrón idéntico a `IBloqueadorRubricaEvaluacion` en HU 8.1), integrado en `EnviarEvaluacionDefinitivaCommandHandler` (tras persistir el envío definitivo) y en `AutorizarExcepcionEvaluadorUnicoCommandHandler` (para cubrir el caso en que la excepción se autoriza cuando ya existía 1 evaluación definitiva completada).
  - Cuando todas las asignaciones vigentes (`Estado != REASIGNADA_POR_CONFLICTO`) están en `COMPLETADA` y se alcanza el mínimo efectivo de evaluaciones definitivas, se consolida (o recalcula de forma idempotente) el `ResultadoConsolidadoRanking`, resolviendo carreras `C5` de dos evaluadores enviando en simultáneo sin error para ambos y con una única fila persistida.
- **Casos de uso CQRS (sin MediatR ni Hangfire):**
  - Comandos: `ConsolidarPostulacionCommand` (usado por el disparo automático con actor `UsuarioSistema` y expuesto para consolidación/recálculo manual del coordinador) y `RevisarDiscrepanciaConsolidacionCommand` (marcar discrepancia revisada con comentario obligatorio).
  - Consultas: `ObtenerResultadoConsolidadoPostulacionQuery` (resultado consolidado de una postulación junto con sus evaluaciones `ENVIADA_DEFINITIVA` individuales asociadas) y `ListarResultadosConsolidadosConvocatoriaQuery` (listado paginado por convocatoria con filtros opcionales `alertaDiscrepancia` y `discrepanciaRevisada`).
- **Desacoplamiento OCP de parámetros de consolidación (`evaluadoresMinimosRequeridos` y `umbralDiscrepanciaPuntos`):**
  - Verificado en `develop` que `RubricaEvaluacion` (HU 7.1/7.2) aún no define esas columnas y no debe modificarse en este cambio. Se introduce el objeto de valor `ParametrosConsolidacion` y el puerto ISP `IProveedorParametrosConsolidacion` respaldado por configuración (`ConsolidacionOptions` en `appsettings.json`, valores por defecto `EvaluadoresMinimosRequeridos = 2` y `UmbralDiscrepanciaPuntos = 20.00`), permitiendo que una futura extensión de RF-07 sustituya el adaptador para leerlos desde `rubricas` sin alterar dominio, casos de uso ni API.
- **Persistencia en el esquema `postulaciones`:**
  - Nueva tabla `postulaciones.resultados_consolidados_ranking` (`timestamptz`, `numeric(5,2)`, índice único 1:1 en `postulacion_id`, índices compuestos por convocatoria/puntaje y convocatoria/discrepancia, restricciones `CHECK` exhaustivas de invariantes y enums con `CodigoEnum`), traducción de unicidad en `TraductorExcepcionesPersistencia` y migración EF Core nueva en tarea separada antes de pruebas con PostgreSQL real (Testcontainers).
- **API REST v1 y seguridad:**
  - Nuevo controlador `ResultadosConsolidadosController` (`api/v1/resultados-consolidados`) con `ProblemDetails` y Swagger.
  - Nuevas políticas aditivas en `PoliticasAutorizacionConsolidacion`, `Program.cs` y `ManejadorResultadoAutorizacionRubricas`:
    - Lectura (`ConsultaConsolidacion`): `COORDINADOR_PROGRAMA` y `DIRECCION`.
    - Escritura (`GestionConsolidacion` — revisión de discrepancias y consolidación manual): `COORDINADOR_PROGRAMA`.
    - `MIEMBRO_COMITE`, `ADMIN_FINANCIERO` y cualquier otro rol reciben siempre `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails`; sin token responde `401 Unauthorized`.

**Fuera de alcance:**

- **HU 9.2 (segunda parte de RF-09):** ordenamiento de ranking con `posicionRanking`, integración HTTP con `MS-PRG` para disponibilidad de cupo y presupuesto (`GET /internal/v1/programas/{id}/disponibilidad`), `montoEstimadoBeca`, trazado de línea de corte (`ClasificacionRanking`: `DENTRO_DE_CUPO` / `LISTA_DE_ESPERA`) y confirmación final de ranking (`PROVISIONAL → CONFIRMADO`). Al concluir HU 9.1, `RF-09` permanece en `[ ]` en ambos roadmaps.
- **RF-10:** resolución de empates y casos límite (`TipoCasoLimite`, `justificacionDesempateCasoLimite`, `decididoPorUsuarioId`) y agregado `EntradaListaEspera`.
- **RF-39:** declaración de `ConflictoInteres` (`RN-POS-06`, invalidación de evaluaciones y delegación de facultades).
- **Entidad `Postulacion`** (Integrante 1; no existe aún en `develop`, por lo que `postulacionId` permanece como UUID opaco sin FK).
- **Otros microservicios y componentes prohibidos:** cero modificaciones en `MS-PRG`, `MS-BEC`, `MS-PLT`, `MS-AUD`, `Postulantes`, `Rubricas`, `Comites`, `AsignacionesEvaluacionController`, `Domain/Comun`, ni en puertos previos con pruebas de contrato por reflexión (`IEvaluacionRepository`, `IAsignacionEvaluacionRepository`, `IRubricaRepository`, `IComiteEvaluadorRepository`, `IExcepcionEvaluadorUnicoRepository`, `IConvocatoriaEvaluacionClient`, `IConsultaEvaluacionesCoevaluadoresRepository`). Sin MediatR, Outbox ni Hangfire.

## Capabilities

### New Capabilities

- `ms-pos/consolidacion`: consolidación automática, determinista e idempotente de evaluaciones definitivas por postulación (`ResultadoConsolidadoRanking`), soporte de excepción autorizada de evaluador único, detección y revisión trazable de alertas de discrepancia entre evaluadores, consultas de resultados consolidados con notas individuales y control RBAC por rol.

### Modified Capabilities

<!-- Ninguna: los requisitos externos de `ms-pos/evaluaciones`, `ms-pos/comites`, `ms-pos/rubricas` y `ms-pos/postulantes` permanecen intactos; la consolidación automática se engancha como reacción desacoplada tras persistir el envío definitivo o la autorización de excepción. -->

## Impact

- **Especificaciones OpenSpec:** nueva capacidad en `openspec/changes/rf9-1-int2-mspos/specs/ms-pos/consolidacion/spec.md`; al archivar se sincroniza en `openspec/specs/ms-pos/consolidacion/spec.md`.
- **Dominio (`Ascenso.Postulaciones.Domain`):** `Consolidacion/Agregados/ResultadoConsolidadoRanking`, `Consolidacion/ObjetosValor/ParametrosConsolidacion`, `Consolidacion/ObjetosValor/ResultadoCalculoConsolidacion`, `Consolidacion/Servicios/ServicioCalculoConsolidacion`, `Enums/EstadoRanking`, nuevas excepciones en `Excepciones/`, y puertos ISP nuevos (`IResultadoConsolidadoRepository`, `IConsultaDatosConsolidacionRepository`, `IProveedorParametrosConsolidacion`) en `Interfaces/`. Sin cambios en `Domain/Comun` ni en agregados o puertos previos.
- **Aplicación (`Ascenso.Postulaciones.Application`):** `Consolidacion/Comandos/` (`ConsolidarPostulacion`, `RevisarDiscrepanciaConsolidacion`), `Consolidacion/Consultas/` (`ObtenerResultadoConsolidadoPostulacion`, `ListarResultadosConsolidadosConvocatoria`), `Consolidacion/DTOs/`, `Consolidacion/Interfaces/IConsolidadorPostulacionEvaluacion`, `Consolidacion/Servicios/ConsolidadorPostulacionEvaluacionAdapter`, inyección desacoplada de `IConsolidadorPostulacionEvaluacion` en `EnviarEvaluacionDefinitivaCommandHandler` y `AutorizarExcepcionEvaluadorUnicoCommandHandler`, y registro `AddScoped` en `Application/DependencyInjection.cs`.
- **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):** `ResultadoConsolidadoRankingConfiguracion`, `ResultadoConsolidadoRepository`, `ConsultaDatosConsolidacionRepository`, `ConsolidacionOptions`, `ProveedorParametrosConsolidacionConfigurado`, `DbSet<ResultadoConsolidadoRanking>` en `PostulacionesDbContext`, traducción de unicidad en `TraductorExcepcionesPersistencia`, registro en `Infrastructure/DependencyInjection.cs` y nueva migración EF Core `ResultadosConsolidadosRanking`.
- **API (`Ascenso.Postulaciones.Api`):** `ResultadosConsolidadosController` (`api/v1/resultados-consolidados`), `PoliticasAutorizacionConsolidacion`, ampliación estrictamente aditiva de `ManejadorResultadoAutorizacionRubricas`, `Program.cs` y `appsettings.json` (`Consolidacion`), y documentación Swagger. Sin cambios en controladores previos.
- **Pruebas:** nuevas suites TDD en `Domain.Tests`, `Application.Tests` e `IntegrationTests` (modelo EF, migración, controladores, autorización, y PostgreSQL real con Testcontainers incluyendo prueba de concurrencia `C5`); ajuste mínimo y documentado en constructores/fábricas de tests de `EnviarEvaluacionDefinitivaCommandHandler`, `AutorizarExcepcionEvaluadorUnicoCommandHandler` y `WebApplicationFactory`, manteniendo `ArchitectureTests` y aserciones previas intactas.
- **Roadmaps:** al concluir la implementación, agregar y marcar únicamente `HU 9.1` anidada bajo `RF-09` (con fecha `2026-11-26`) en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`, manteniendo `RF-09` en `[ ]`.
