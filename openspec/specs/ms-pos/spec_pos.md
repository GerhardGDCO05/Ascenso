# Specification: MS-POS (Microservicio de Postulaciones y Evaluación)

## Context

Gestión integral del ciclo de vida de la admisión, abarcando desde el registro del perfil socioeconómico y la postulación a convocatorias, hasta el proceso de verificación de elegibilidad, evaluación por comités, puntuación mediante rúbricas y generación del ranking de mérito.

## Invariants

* Un `Postulante` no puede tener más de una `Postulacion` en estado `Borrador`, `Enviada` o `EnEvaluacion` simultáneamente para el mismo `PeriodoAcademico`.
* Una `Postulacion` jamás puede transicionar al estado `EnEvaluacion` sin haber aprobado previamente la `VerificacionElegibilidad` automática.
* Una `Puntuacion` emitida por un `Evaluador` sobre un `Expediente` es inmutable una vez que el estado de esa evaluación se marca como `Confirmada`.
* Las entidades del dominio no deben exponer setters públicos; las transiciones de estado y cálculos de puntaje se realizan mediante métodos de comportamiento que garantizan la integridad de los datos.





\## Scenarios

<!-- OpenCode inyectará los escenarios GIVEN/WHEN/THEN detallados al archivar cada HU -->



## Behaviors / Business Rules

### 1\. Convocatorias y Elegibilidad Básica

* **Ventana de Postulación:** El dominio solo permite la creación y envío de una `Postulacion` si la fecha actual se encuentra dentro del rango `FechaInicio` y `FechaFin` definido en la `Convocatoria` activa.
* **Filtro Automático:** Al momento de enviar la postulación, el sistema ejecuta una `VerificacionElegibilidad`. Si los datos del `PerfilSocioeconomico` no cumplen con los umbrales mínimos definidos en la `Convocatoria`, la `Postulacion` transiciona automáticamente a `RechazadaPorElegibilidad`.
* **Retiro Voluntario:** Un `Postulante` puede invocar el comportamiento de retiro sobre su `Postulacion` en cualquier momento, siempre y cuando su estado sea anterior a `EnEvaluacion` o `Finalizada`.

### 2\. Comités Evaluadores y Conflictos de Interés

* **Declaración de Conflictos:** Si un miembro del `ComiteEvaluador` registra un `ConflictoDeInteres` respecto a un `Expediente` particular, el dominio debe anular cualquier asignación previa de ese expediente a dicho evaluador e impedir que emita calificaciones sobre el mismo.
* **Aislamiento de Calificaciones (Blind Review):** Durante la fase de evaluación, un `Evaluador` solo puede consultar y modificar sus propias calificaciones. El sistema prohíbe el acceso a las `Puntuacion`es parciales emitidas por otros miembros del comité hasta que la etapa de evaluación sea declarada como cerrada.

### 3\. Rúbricas y Registro de Puntuaciones

* **Cálculo de Rúbricas:** La puntuación final de una evaluación no se ingresa de forma arbitraria; el dominio la calcula automáticamente basándose en los niveles seleccionados para cada `CriterioEvaluacion` dentro de la `Rubrica` asociada al programa.
* **Completitud de Evaluación:** Un `Expediente` solo transiciona al estado `EvaluacionCompletada` cuando todos los evaluadores asignados (sin conflicto de interés) han emitido y confirmado sus calificaciones respectivas.

### 4\. Consolidación, Ranking y Empates

* **Generación de Ranking:** Al finalizar la ventana de evaluación de una `Convocatoria`, el dominio consolida las evaluaciones completadas, calcula el puntaje promedio ponderado de cada `Expediente` y genera un `Ranking`.
* **Resolución de Empates:** Si dos o más expedientes tienen el mismo puntaje exacto, el dominio aplica automáticamente una `ReglaResolucionEmpate` basada en la puntuación de un sub-criterio prioritario predefinido (ej. nivel de vulnerabilidad socioeconómica) para ordenar la lista.
* **Lista de Espera:** Aquellos expedientes que aprueban la evaluación pero cuya posición en el `Ranking` supera los cupos disponibles de la `Convocatoria`, adquieren automáticamente el estado `EnListaDeEspera`.

