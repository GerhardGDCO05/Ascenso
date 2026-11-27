# Bitácora de Cierre — `rf10-1-int2-mspos` (HU 10.1 / RF-10)

- **Fecha:** 2026-11-26
- **Cambio OpenSpec:** `rf10-1-int2-mspos`
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones`)
- **Alcance:** Implementación completa de **HU 10.1** (resolución manual y auditada de empates en la línea de corte `EMPATE_POR_CUPO` / `EMPATE_POR_PRESUPUESTO` y casos límite presupuestarios sin empate `CASO_LIMITE_PRESUPUESTARIO`, con justificación obligatoria de 10 a 1000 caracteres, inalterabilidad `409 CASO_LIMITE_YA_RESUELTO`, autorización por `COORDINADOR_PROGRAMA` o jefatura activa del comité, desbloqueo/re-bloqueo de confirmación `PROVISIONAL → CONFIRMADO` y puerto OCP para RF-39).
- **Roadmaps:** `HU 10.1` marcada `[x]` en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`; `RF-10` permanece en `[ ]` al quedar pendiente `HU 10.2` (lista de espera y promoción por vacante).

---

## 1. Evidencia real de verificación

Todos los comandos se ejecutaron desde `Apps/services/ms-postulaciones/` (y la raíz del repositorio para `openspec`):

| Verificación | Comando | Resultado observado |
|---|---|---|
| **6.1 Restore** | `dotnet restore Ascenso.Postulaciones.sln --locked-mode` | Código `0`; todos los proyectos restaurados sin modificaciones en `packages.lock.json`. |
| **6.2 Build Release** | `dotnet build Ascenso.Postulaciones.sln --configuration Release --no-restore` | Código `0`; `0 Advertencia(s)`, `0 Errores` (`13.94 s`). |
| **6.3 Test Suite** | `dotnet test Ascenso.Postulaciones.sln` | Código `0`; **1,514 pruebas** superadas, **0** fallidas, **0** omitidas: `Domain.Tests` (`623`, `189 ms`), `Application.Tests` (`452`, `378 ms`), `ArchitectureTests` (`11`, `582 ms`), `IntegrationTests` (`428`, `11 s`, incluyendo Testcontainers PostgreSQL real con resolución por coordinador y jefatura activa, caso límite presupuestario, inalterabilidad, desbloqueo/re-bloqueo, nuevas `CHECK` y concurrencia optimista `C1`). |
| **6.4 Format** | `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <rutas de git diff --name-only develop...HEAD -- "*.cs">` | Código `0`; salida limpia sin cambios pendientes sobre los archivos `.cs` nuevos o modificados respecto a `develop`. |
| **6.5 EF Core** | `dotnet ef migrations has-pending-model-changes --project Ascenso.Postulaciones.Infrastructure --startup-project Ascenso.Postulaciones.Api --no-build` | Código `0`; `No changes have been made to the model since the last migration.` |
| **6.5 OpenSpec (cambio)** | `openspec validate rf10-1-int2-mspos --strict` | Código `0`; `Change 'rf10-1-int2-mspos' is valid`. |
| **6.5 OpenSpec (specs)** | `openspec validate --specs` | Código `0`; `Totals: 11 passed, 0 failed (11 items)`. |

---

## 2. Ajustes mínimos de DI en pruebas previas y `WebApplicationFactory`

Al incorporar los nuevos puertos ISP (`IRolesUsuarioActual`, `IConsultaJefaturaComiteRepository` e `IVerificadorConflictoInteresDecisor`) consumidos por `ResolverCasoLimiteRankingCommandHandler` y `RankingsController`, se realizaron únicamente registros aditivos de sustitutos (`Substitute.For<...>`) en las pruebas existentes, **sin eliminar pruebas ni alterar aserciones previas**:

1. **`Application.Tests` (1 archivo):**
   - `tests/Ascenso.Postulaciones.Application.Tests/Configuracion/InyeccionDependenciasTests.cs`: registro de `IRolesUsuarioActual`, `IConsultaJefaturaComiteRepository` e `IVerificadorConflictoInteresDecisor`, más verificación aditiva de resolución DI para `ResolverCasoLimiteRankingCommandHandler` y `IValidator<ResolverCasoLimiteRankingCommand>`.
2. **`IntegrationTests` — 18 fábricas `WebApplicationFactory` ajustadas de forma aditiva (`RemoveAll` + `AddSingleton(Substitute.For<...>())` para `IConsultaJefaturaComiteRepository` e `IVerificadorConflictoInteresDecisor`):**
   - `tests/Ascenso.Postulaciones.IntegrationTests/EnsambladoApiTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/SwaggerApiTests.cs` *(además de verificar el endpoint nuevo en Swagger)*
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/AsignacionesEvaluacionControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ComitesEvaluadoresControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/EvaluacionesControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ExcepcionEvaluadorUnicoControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulacionesInternasControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulantesControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RankingsControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ResultadosConsolidadosControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RubricasControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionAsignacionesTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionComitesTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionConsolidacionTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionEvaluacionesTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionExcepcionEvaluadorUnicoTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRankingTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRubricasTests.cs`

---

## 3. Resumen de decisiones de diseño aplicadas

- **Migración EF Core:** `20261127012016_ResolucionCasosLimiteRanking` sobre `20261126194657_ConfirmacionRankingConvocatoria`, añadiendo únicamente 5 columnas anulables (`tipo_caso_limite`, `seleccionado_en_caso_limite`, `justificacion_desempate_caso_limite`, `decidido_por_usuario_id`, `fecha_decision_caso_limite`) y 2 restricciones `CHECK` sin modificar ninguna de las 10 `CHECK` previas.
- **Excepción de inalterabilidad:** `CasoLimiteYaResueltoException : ExcepcionReglaNegocio` con código `CASO_LIMITE_YA_RESUELTO` y estado HTTP `409`, traduciéndose automáticamente a `ProblemDetails` en `ManejadorExcepcionesDominio`.
- **Guarda SQL trivaluada en PostgreSQL:** en `ck_resultados_consolidados_caso_limite_coherente` se incluyó la guarda `OR (tipo_caso_limite IS NOT NULL AND tipo_caso_limite NOT IN ('EMPATE_POR_CUPO', 'EMPATE_POR_PRESUPUESTO', 'CASO_LIMITE_PRESUPUESTARIO'))` para evitar que, por orden alfabético de evaluación `CHECK` en PostgreSQL, una fila con `tipo_caso_limite` inválido sea interceptada antes de `ck_resultados_consolidados_tipo_caso_limite_valido` (y manteniendo ambas `CHECK` en `TRUE` cuando las 5 columnas son `NULL`).
- **Autorización en dos capas:** política ASP.NET `PoliticasAutorizacionConsolidacion.ResolucionCasosLimiteRanking` (`COORDINADOR_PROGRAMA` o `MIEMBRO_COMITE`, con `ManejadorResultadoAutorizacionRubricas` emitiendo `403 PERMISO_DENEGADO`) combinada con verificación en servidor dentro de `ResolverCasoLimiteRankingCommandHandler` mediante `IConsultaJefaturaComiteRepository` (`ComiteEvaluador.JefaturaUsuarioId` activa en la convocatoria) antes de consultar `MS-PRG`.
- **Extensibilidad OCP para RF-39 y HU 10.2:** integración del puerto `IVerificadorConflictoInteresDecisor` (implementado por `VerificadorConflictoInteresDecisorSinOperacion` en HU 10.1) listo para sustituirse en RF-39, y preservación de `PosicionRanking` y `SeleccionadoEnCasoLimite` en `LISTA_DE_ESPERA` para construir `EntradaListaEspera` en HU 10.2.

---

## 4. Preguntas abiertas heredadas de `design.md`

1. **Lectura dedicada para la Jefatura del comité antes de resolver un caso límite:** actualmente `GET /api/v1/rankings/convocatorias/{convocatoriaId}` exige `ConsultaConsolidacion` (`COORDINADOR_PROGRAMA` o `DIRECCION`). Confirmar con negocio si en una iteración posterior conviene exponer un endpoint de lectura acotado exclusivamente al caso límite activo para la `Jefatura del comité` sin debilitar `ConsultaConsolidacion`.
2. **Procedimiento administrativo ante caída de disponibilidad en `MS-PRG` posterior a una decisión inalterable:** en HU 10.1, si una decisión ya registrada deja de caber tras reducirse cupos o presupuesto en `MS-PRG`, el sistema bloquea *fail-closed* la confirmación (`422`) respetando simultáneamente la inalterabilidad (Criterio 4) y el no sobregiro (`RN-PRG-03`). Confirmar con negocio si ante ese escenario excepcional se requerirá en el futuro un evento formal de anulación auditada por Dirección o si basta ajustar disponibilidad en `MS-PRG`.
3. **Delegación automática ante conflicto de interés del decisor (RF-39):** al implementarse `ConflictoInteres`, confirmar con negocio si `DIRECCION` actuará como delegada de resolución en `ResolucionCasosLimiteRanking` cuando exista un registro `ConflictoInteres` con `tipoDelegado = DIRECCION` (`RN-POS-06`).
4. **Publicación de evento de auditoría hacia `MS-AUD` (`RF-35`):** una vez habilitada la mensajería RabbitMQ/Outbox en `MS-POS`, definir el contrato del evento de integración emitido al resolverse un empate o caso límite con los 5 campos auditables ya persistidos en `ResultadoConsolidadoRanking`.
