# Registro de auditoría — avances de MS-BEC en las secciones 15–19

- **Fecha:** 26 de noviembre de 2026 (America/Caracas).
- **Rama local:** `feature/seguimiento-renovacion-pausa-historial-ms-beca`.
- **Cambio de origen:** `tech-bootstrap-int4-msbeca`, todavía activo.
- **Alcance de este registro:** 24 tareas verificadas y marcadas `[x]` de 50 tareas en las secciones 15–19: 2 de la sección 15, 10 de la 16, 6 de la 17, 4 de la 18 y 2 de la 19.
- **Estado global del plan al registrar:** 142/189 tareas completas; 47 pendientes.
- **Estado de Git al preparar el histórico:** parte del avance ya estaba en commits de MS-BEC y parte aún estaba en el árbol local. Su incorporación posterior queda registrada por el historial de Git de esta rama.

## 1. Alcance registrado

### Sección 15: integraciones de otorgamiento

Se conservan como cerradas 15.5 y 15.7: la decisión sobre OTP adicional en emisión/anulación y la cancelación de la referencia del job de expiración al aceptar la oferta. Las demás tareas de la sección continúan abiertas, incluidos los contratos y la coordinación con servicios propietarios.

### Sección 16: desembolsos

Se conservan 16.1–16.9 y 16.12: modelo, persistencia, casos de uso locales, autorización OTP y contratos de eventos de MS-BEC. Permanecen abiertas 16.10, 16.11, 16.13 y 16.14 por los criterios aún no satisfechos en el plan activo. El cierre integral requiere el movimiento presupuestario acordado con MS-PRG, el calendario oficial y el recorrido extremo a extremo.

### Sección 17: suspensión y revocación

Se conservan 17.1–17.5 y 17.8: modelo y transiciones locales, handlers y job de revisión. Permanecen abiertas 17.6, 17.7 y 17.9. La transferencia de saldo/cupo y los recorridos completos requieren el contrato financiero de MS-PRG; se mantienen los criterios de API y pruebas pendientes tal como están en el plan activo.

### Sección 18: seguimiento académico

Se conservan 18.1–18.4: enumeraciones, agregado `ReporteAcademico`, evaluación de permanencia y persistencia. Las tareas 18.5–18.11 siguen sin marcar aunque contienen avances locales; este histórico no los presenta como terminados. La fecha límite y el calendario académico oficiales dependen de MS-PRG.

### Sección 19: renovación

Se conservan 19.1 y 19.7: modelo de `SolicitudRenovacion`, máquina de estados y persistencia de la solicitud y cadena. Las tareas 19.2–19.6 y 19.8–19.9 permanecen abiertas. Hay pruebas locales de solicitud, rechazo, fallo cerrado de aprobación sin reserva y vencimiento sin solicitud; la aprobación completa de dos periodos sigue pendiente de la reserva de renovación de MS-PRG y de los datos oficiales del nuevo periodo.

## 2. Verificación y límites

- `openspec list --json` indicó 142 tareas completas de 189 en el cambio activo antes de crear este histórico.
- `openspec status --change tech-bootstrap-int4-msbeca --json` mostró completos los artefactos de planificación, pero no las tareas de implementación.
- Las pruebas específicas de renovación y calendario ejecutadas en el trabajo local previo pasaron; no equivalen a la prueba completa de aprobación por API exigida en 19.9.
- No se sincronizan especificaciones principales ni se mueve `tech-bootstrap-int4-msbeca` porque conserva 47 tareas pendientes y deltas activos.
- Este registro no modifica otros microservicios ni afirma la aceptación de contratos por sus equipos propietarios.

## 3. Continuidad

`openspec/changes/tech-bootstrap-int4-msbeca/tasks.md` sigue siendo el plan completo. Las tareas abiertas de 15–19 y la planificación de 20–22 permanecen allí para su implementación y verificación conforme al PRD y al DDD.
