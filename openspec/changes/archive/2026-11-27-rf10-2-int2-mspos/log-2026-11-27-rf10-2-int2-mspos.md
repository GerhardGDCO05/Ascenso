# Bitácora de ejecución: `rf10-2-int2-mspos` (HU 10.2 — Lista de espera y reasignación por vacante)

- **Fecha:** 2026-11-27
- **Cambio OpenSpec:** `rf10-2-int2-mspos` (cierra **RF-10** al 100% en `MS-POS`)
- **Microservicio:** `Apps/services/ms-postulaciones` (`Ascenso.Postulaciones.sln`)
- **Tareas completadas:** `24/24`

---

## 1. Evidencia real de verificaciones

1. **Restore con lockfile (`--locked-mode`):**
   - Comando: `dotnet restore Ascenso.Postulaciones.sln --locked-mode` (desde `Apps/services/ms-postulaciones`)
   - Resultado: código de salida `0`; los 8 proyectos restaurados sin modificaciones en `packages.lock.json`.
2. **Compilación Release:**
   - Comando: `dotnet build Ascenso.Postulaciones.sln --configuration Release --no-restore`
   - Resultado: código de salida `0`, `0 Advertencia(s)`, `0 Errores` (`17.05 s`).
3. **Suite completa de pruebas (incluye Testcontainers PostgreSQL y `ArchitectureTests` sin modificaciones):**
   - Comando: `dotnet test Ascenso.Postulaciones.sln`
   - Resultado: **1,607 pruebas superadas, 0 fallidas, 0 omitidas** (las 1,514 pruebas previas intactas + 93 nuevas pruebas de HU 10.2):
     - `Ascenso.Postulaciones.Domain.Tests`: `652` superadas, `0` fallidas, `0` omitidas (`303 ms`)
     - `Ascenso.Postulaciones.Application.Tests`: `481` superadas, `0` fallidas, `0` omitidas (`447 ms`)
     - `Ascenso.Postulaciones.ArchitectureTests`: `11` superadas, `0` fallidas, `0` omitidas (`599 ms`)
     - `Ascenso.Postulaciones.IntegrationTests`: `463` superadas, `0` fallidas, `0` omitidas (`13 s`) — incluye `ListaEsperaPostgreSqlTests` contra PostgreSQL real (generación automática de lista de espera al confirmar ranking, registro de vacante liberada, promoción manual validando `MS-PRG`, actualización real de `GET /internal/v1/postulaciones/{postulacionId}/adjudicable`, violaciones reales de índices únicos y restricciones `CHECK`, y concurrencia real `C6` entre dos `DbContext` en paralelo).
4. **Formato estricto sobre archivos `.cs` nuevos o modificados respecto a `develop` (`--include`):**
   - Comando: `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <rutas-relativas-de-git-diff>`
   - Resultado: código de salida `0` sin observaciones ni archivos ajenos modificados.
5. **Modelo EF Core sin cambios pendientes:**
   - Comando: `dotnet ef migrations has-pending-model-changes --project Ascenso.Postulaciones.Infrastructure --startup-project Ascenso.Postulaciones.Api --no-build`
   - Resultado: código de salida `0` (`No changes have been made to the model since the last migration.`).
6. **Validación OpenSpec:**
   - `openspec validate rf10-2-int2-mspos --strict` → `Change 'rf10-2-int2-mspos' is valid` (código `0`).
   - `openspec validate --specs` (previo al sync) → `11 passed, 0 failed`.
   - `openspec validate --specs` (posterior al sync de `ms-pos/lista-espera` y `ms-pos/consolidacion` al archivar) → `12 passed, 0 failed`.

---

## 2. Ajustes mínimos de constructores y DI en pruebas previas (0 aserciones alteradas)

Para incorporar los nuevos puertos ISP/OCP sin modificar ninguna aserción existente en las suites previas:

- **Pruebas unitarias de Aplicación (parámetro de constructor):**
  - `tests/Ascenso.Postulaciones.Application.Tests/Consolidacion/Comandos/ConfirmarRankingConvocatoriaCommandHandlerTests.cs`: incorporación del sustituto `IGeneradorListaEsperaRanking` en la instanciación de `ConfirmarRankingConvocatoriaCommandHandler`.
  - `tests/Ascenso.Postulaciones.Application.Tests/Consolidacion/Comandos/ConfirmarRankingConCasoLimiteTests.cs`: incorporación del sustituto `IGeneradorListaEsperaRanking` en la instanciación de `ConfirmarRankingConvocatoriaCommandHandler`.
  - `tests/Ascenso.Postulaciones.Application.Tests/Consolidacion/Consultas/ConsultarAdjudicabilidadPostulacionQueryHandlerTests.cs`: incorporación del sustituto `IEntradaListaEsperaRepository` en `CrearHandler()` para `ConsultarAdjudicabilidadPostulacionQueryHandler`.
- **Pruebas de composición DI en Aplicación:**
  - `tests/Ascenso.Postulaciones.Application.Tests/Configuracion/InyeccionDependenciasTests.cs`: registro de sustitutos para `IEntradaListaEsperaRepository` e `IGestorConcurrenciaListaEspera` y verificación de resolución de los 4 handlers nuevos y sus validadores.
- **Pruebas PostgreSQL de Integración (parámetro de constructor):**
  - `tests/Ascenso.Postulaciones.IntegrationTests/Persistencia/RankingPostgreSqlTests.cs`: paso de `IGeneradorListaEsperaRanking` al construir `ConfirmarRankingConvocatoriaCommandHandler`.
  - `tests/Ascenso.Postulaciones.IntegrationTests/Persistencia/CasosLimitePostgreSqlTests.cs`: paso de `IGeneradorListaEsperaRanking` al construir `ConfirmarRankingConvocatoriaCommandHandler`.
- **20 fábricas `WebApplicationFactory` en `IntegrationTests` (dobles mínimos de DI para `IEntradaListaEsperaRepository` e `IGestorConcurrenciaListaEspera`):**
  1. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/AsignacionesEvaluacionControllerTests.cs`
  2. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ComitesEvaluadoresControllerTests.cs`
  3. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/EvaluacionesControllerTests.cs`
  4. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ExcepcionEvaluadorUnicoControllerTests.cs`
  5. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulacionesInternasControllerTests.cs`
  6. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulantesControllerTests.cs`
  7. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RankingsControllerTests.cs`
  8. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ResolucionCasosLimiteControllerTests.cs`
  9. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ResultadosConsolidadosControllerTests.cs`
  10. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RubricasControllerTests.cs`
  11. `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/SwaggerApiTests.cs`
  12. `tests/Ascenso.Postulaciones.IntegrationTests/EnsambladoApiTests.cs`
  13. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionAsignacionesTests.cs`
  14. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionCasosLimiteTests.cs`
  15. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionComitesTests.cs`
  16. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionConsolidacionTests.cs`
  17. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionEvaluacionesTests.cs`
  18. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionExcepcionEvaluadorUnicoTests.cs`
  19. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRankingTests.cs`
  20. `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRubricasTests.cs`

---

## 3. Resumen de decisiones de diseño aplicadas

- **Migración EF Core:** `20261128004340_ListaEsperaPostulaciones` sobre `20261127012016_ResolucionCasosLimiteRanking`, creando `postulaciones.entradas_lista_espera` con índices (`ux_entradas_lista_espera_postulacion`, `ux_entradas_lista_espera_convocatoria_posicion`, `ix_entradas_lista_espera_convocatoria_estado_posicion`) y 4 restricciones `CHECK` (`ck_entradas_lista_espera_estado_valido`, `ck_entradas_lista_espera_posicion_positiva`, `ck_entradas_lista_espera_monto_positivo`, `ck_entradas_lista_espera_estado_coherente` con guarda para orden alfabético y lógica trivaluada `NULL`).
- **Agregado raíz `EntradaListaEspera` y máquina de estados:** `{EN_ESPERA, ELEGIBLE_POR_VACANTE, PROMOVIDA}` (`EstadoListaEspera` serializado con `CodigoEnum`), invariantes de monto snapshot, posición `>= 1`, fechas UTC y `Version` optimista (`C1`).
- **Concurrencia real `C6` en `GestorConcurrenciaListaEsperaPostgreSql`:** transacción PostgreSQL con `SELECT pg_advisory_xact_lock(hashtextextended(@convocatoriaIdText, 1002))` seguido de `FromSqlInterpolated` con `SELECT * FROM postulaciones.entradas_lista_espera WHERE convocatoria_id = @convocatoriaId ORDER BY posicion_orden ASC FOR UPDATE` directo sin subconsulta y control de `Version` al persistir.
- **Integración OCP:**
  - `ConfirmarRankingConvocatoriaCommandHandler` invoca `IGeneradorListaEsperaRanking` (`GeneradorListaEsperaRankingAdapter`) antes del único `IUnitOfWork.GuardarCambiosAsync`.
  - `ConsultarAdjudicabilidadPostulacionQueryHandler` consulta `IEntradaListaEsperaRepository` para postulaciones en `LISTA_DE_ESPERA`, habilitando `adjudicable = true` en `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` cuando la entrada está en `PROMOVIDA` y ampliando `AdjudicabilidadPostulacionDto` con parámetros opcionales al final.
- **Controladores y seguridad dedicados:** `ListaEsperaController` (`api/v1/lista-espera`) y `ListaEsperaInternaController` (`internal/v1/lista-espera`) con políticas aditivas `ConsultaListaEspera`, `GestionListaEspera` y `RegistroVacanteListaEsperaInterna` en `PoliticasAutorizacionListaEspera` y `ManejadorResultadoAutorizacionRubricas` (`403 PERMISO_DENEGADO`), manteniendo 100% intactos los controladores previos.

---

## 4. Preguntas abiertas heredadas de `design.md` (fuera de alcance de HU 10.2)

1. **Integración asíncrona con `MS-BEC` vía RabbitMQ/MassTransit:** al habilitar mensajería en `MS-POS`, definir los contratos de consumo `OfertaBecaRechazada` / `OfertaBecaExpirada` / `OfertaBecaAnulada` (con *Idempotent Inbox* por `eventId`) que invoquen directamente `RegistrarVacanteLiberadaCommandHandler`, y el evento de integración `CandidatoListaEsperaElegible` / `CandidatoListaEsperaPromovido` hacia `MS-PLT` y `MS-AUD`.
2. **Descarte o cierre masivo de entradas pendientes al cerrar una convocatoria:** confirmar con negocio si una HU futura requerirá transicionar entradas `EN_ESPERA`/`ELEGIBLE_POR_VACANTE` remanentes a un estado terminal de cierre (`EXPIRADA`/`DESCARTADA`) al finalizar el periodo académico.
3. **Idempotencia HTTP (`Idempotency-Key`) en `POST /internal/v1/lista-espera/convocatorias/{convocatoriaId}/vacantes-liberadas`:** cuando `MS-BEC` consuma este endpoint síncronamente con reintentos (antes de migrar a eventos), acordar con el equipo de `MS-BEC` una clave de idempotencia por `ofertaId` para evitar marcar dos candidatos ante un reintento de red.
