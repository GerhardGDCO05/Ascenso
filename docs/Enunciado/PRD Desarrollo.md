# UCAB
## UNIVERSIDAD CATÓLICA ANDRÉS BELLO
### ESCUELA DE INGENIERÍA INFORMÁTICA

**Documento de Requerimientos del Producto (PRD)**

# ASCENSO
## Plataforma de Becas y Ayudas Estudiantiles

**Materia:** Desarrollo de Software
**Profesor:** Bismarck Ponce
**Escuela:** Ingeniería Informática
**Fecha:** 17 de septiembre de 2026

---

*Fundación Ascenso necesita una plataforma que gestione el ciclo completo de becas y ayudas estudiantiles, desde la postulación hasta la renovación. Este documento describe qué necesita el negocio, en su propio lenguaje, para que cada equipo defina cómo construirlo.*

---

## Contenido

## Sobre este Documento

Este documento es el PRD (Product Requirements Document) del proyecto semestral. Está escrito como lo escribiría Fundación Ascenso — el "cliente" de este proyecto — sin arquitectura, patrones ni tecnologías sugeridas.

**Reglas de uso para los equipos:**

* Cada equipo debe traducir estos requerimientos en historias de usuario, un modelo de dominio y una arquitectura propios. Ese análisis es parte de la evaluación, no un dato ya resuelto aquí.
* El catálogo tiene 40 requerimientos funcionales (RF-01 a RF-40), organizados en 7 áreas de negocio.
* Equipos de 5 personas: cada integrante puede tomar entre 6 y 8 requerimientos durante el semestre.
* Cada requerimiento tiene una prioridad: **Debe tener** (núcleo del proyecto), **Debería tener** (importante, no bloqueante) o **Podría tener** (opcional si el tiempo alcanza).
* Ningún requerimiento dicta una solución técnica. Si algo parece implicar una tecnología específica, es una decisión que le corresponde tomar al equipo.

## Contexto y Antecedentes

Fundación Ascenso administra 12 programas de becas activos, con más de 800 becarios distribuidos en 6 universidades aliadas. Un equipo de 9 personas coordina todo el proceso hoy usando hojas de cálculo y correo electrónico.

Esto genera problemas concretos:

* Preparar un reporte de impacto para un donante toma varios días de trabajo manual.
* No hay una vista consolidada del presupuesto disponible por programa en un momento dado.
* Las decisiones sobre postulaciones (aprobación, rechazo, suspensión) no siempre quedan documentadas con su justificación.
* El seguimiento académico y las renovaciones dependen de que alguien recuerde hacerlo manualmente — algunos casos se pierden.

La Fundación planea duplicar su número de programas en los próximos dos años, sin duplicar el equipo administrativo. La plataforma debe sostener ese crecimiento.

## Objetivos del Proyecto

1. Reducir el tiempo de evaluación de una postulación de varias semanas a pocos días.
2. Tener visibilidad en tiempo real del presupuesto disponible por programa.
3. Producir un reporte de impacto para un donante en minutos, no en días.
4. Garantizar trazabilidad completa de cada decisión que afecte a un postulante o becario.
5. Escalar de 12 a más de 25 programas en dos años sin crecer el equipo administrativo en la misma proporción.

## Alcance del Proyecto

### En alcance
* Todo el ciclo de vida de una beca: postulación, evaluación, otorgamiento, desembolso, seguimiento académico y renovación.
* Gestión de programas de becas, fondos, donantes y universidades aliadas.
* Reportería interna y reportes para donantes.
* Comunicaciones y notificaciones asociadas a cada etapa del proceso.

### Fuera de alcance (por ahora)
* Procesamiento de pagos en línea. Los desembolsos se registran en la plataforma; la transferencia bancaria se sigue ejecutando por los canales financieros actuales de la Fundación.
* Portal de autoservicio para donantes. Los reportes se comparten directamente por el equipo de la Fundación; un portal propio podría evaluarse en una fase futura.
* Integración automática en tiempo real con los sistemas académicos de cada universidad aliada. El intercambio de información académica se hace por los medios que la Fundación defina.
* Aplicación móvil nativa.

## Restricciones Técnicas del Cliente

La Fundación ya tiene compromisos y políticas vigentes que el proyecto debe respetar, sin que esto determine cómo construir el resto de la plataforma:

* **Almacenamiento de documentos.** La Fundación ya usa Firebase para alojar archivos (documentos de postulantes, cartas, comprobantes). El nuevo sistema debe apoyarse en esa misma cuenta, no crear un almacenamiento paralelo.
* **Verificación en dos pasos.** Por política de seguridad, cualquier persona con acceso a información financiera o de decisión (coordinadores, finanzas, dirección, comité) debe verificar su identidad con un segundo factor además de su contraseña al iniciar sesión.
* **Código de un solo uso para operaciones sensibles.** Antes de confirmar una acción que mueve dinero o cambia el estado legal de una beca —desembolsar (RF-20), revocar una beca (RF-22), o modificar los datos de pago de un becario— la persona debe ingresar un código de verificación de un solo uso (OTP) enviado en el momento.

## Actores y Usuarios del Sistema

* **Postulante.** Estudiante que aplica a un programa de beca. Le importa entender si califica, qué documentos le faltan, y en qué estado está su postulación.
* **Becario o becaria.** Postulante que ya recibió una beca activa. Necesita ver el estado de sus desembolsos, cumplir sus condiciones de permanencia, y solicitar la renovación a tiempo.
* **Coordinador o coordinadora de programa.** Personal de la Fundación responsable de uno o varios programas: define criterios, gestiona el ciclo de postulación, y decide sobre casos límite.
* **Miembro del comité evaluador.** Persona interna o externa que revisa y puntúa postulaciones asignadas. Le importa tener criterios claros y evitar conflictos de interés.
* **Administrador o administradora financiera.** Registra y controla los desembolsos, y necesita que ningún pago exceda el presupuesto aprobado de un programa.
* **Enlace de universidad aliada.** Contacto en cada universidad que certifica la matrícula y el rendimiento académico de los becarios.
* **Donante o patrocinador.** Empresa, egresado o persona que financia uno o más programas. Necesita saber cómo se usan sus fondos y qué impacto están teniendo.
* **Dirección de la Fundación.** Supervisa el conjunto de programas, aprueba configuraciones y resuelve excepciones que el coordinador no puede decidir solo.

## Glosario de Negocio

| Término | Definición |
| :--- | :--- |
| **Postulante** | Persona que aplica a un programa de beca; aún no ha sido evaluada. |
| **Becario/a** | Postulante aprobado, con una beca activa. |
| **Programa de beca (o fondo)** | Iniciativa de financiamiento con presupuesto, criterios de elegibilidad y cupo propios. |
| **Donante / patrocinador** | Empresa, egresado o persona que aporta los fondos de uno o más programas. |
| **Universidad aliada** | Institución con la que la Fundación tiene un convenio de colaboración. |
| **Comité evaluador** | Grupo responsable de revisar y puntuar postulaciones de un programa. |
| **Ciclo de postulación** | Periodo en el que un programa recibe postulaciones. |
| **Desembolso** | Pago realizado a un becario o a su universidad en su nombre. |
| **Renovación** | Solicitud de un becario activo para continuar recibiendo la beca el siguiente periodo. |
| **Estatus académico** | Información de rendimiento de un becario (promedio, materias aprobadas, etc.). |
| **Suspensión** | Pausa temporal del beneficio, con causa documentada. |
| **Revocación** | Cancelación definitiva de una beca. |
| **Lista de espera** | Postulantes elegibles no otorgados por falta de cupo o presupuesto, en espera de disponibilidad. |

## Reglas de Negocio Transversales

* Un postulante no puede tener dos becas de cobertura completa activas y simultáneas en programas distintos de la Fundación.
* Cualquier persona con poder de decisión sobre una postulación (evaluador, coordinador) debe declarar un conflicto de interés antes de participar en su evaluación.
* Todo cambio de estatus de una beca (otorgada, suspendida, revocada, renovada, concluida) debe quedar justificado y registrado.
* Ningún desembolso puede exceder el presupuesto aprobado del programa correspondiente.
* Una postulación solo puede estar en uno de estos estados a la vez: Recibida, En verificación, En evaluación, No elegible, Otorgada, Rechazada, Retirada. No puede saltarse etapas: no puede pasar de "Recibida" a "Otorgada" sin pasar por evaluación.
* Una beca solo puede estar en uno de estos estados a la vez: Otorgada, Activa, Suspendida, Renovada, Concluida, Revocada. Las únicas transiciones permitidas son: Otorgada → Activa; Activa → Suspendida; Suspendida → Activa o → Revocada; Activa → Concluida o → Renovada; y, por incumplimiento grave, cualquier estado activo → Revocada directamente.
* Ninguna beca puede reactivarse después de estar Concluida o Revocada; si la persona quiere continuar, debe postular de nuevo como un caso nuevo.

## Requerimientos No Funcionales

* **Confidencialidad.** La información socioeconómica de los postulantes (ingresos, composición familiar) solo debe ser visible para quienes la necesiten para evaluar o decidir.
* **Disponibilidad en periodos críticos.** La plataforma debe funcionar de forma confiable durante las ventanas de postulación y evaluación, que son cortas y no se pueden extender.
* **Trazabilidad.** Toda acción relevante sobre una postulación o beca debe quedar registrada de forma permanente, sin posibilidad de edición retroactiva silenciosa.
* **Accesibilidad.** Postulantes con conectividad limitada deben poder completar su postulación sin fricción innecesaria.
* **Escalabilidad de uso.** La plataforma debe sostener el crecimiento previsto (de 12 a más de 25 programas) sin degradar la experiencia.
* **Protección de datos personales.** El manejo de información sensible debe cumplir con buenas prácticas de protección de datos personales.
* **Actualizaciones en tiempo real.** Ciertas pantallas (mensajería, panel de indicadores) deben reflejar cambios al instante, sin que la persona tenga que recargar la página manualmente.
* **Procesamiento en segundo plano.** Operaciones pesadas (notificaciones masivas, reportes grandes) no deben bloquear ni ralentizar el uso normal del resto de la plataforma mientras se ejecutan.
* **Consistencia bajo concurrencia.** Dos personas actuando al mismo tiempo sobre el mismo caso (por ejemplo, dos evaluadores calificando la misma postulación, o finanzas confirmando un desembolso mientras se revoca esa misma beca) no deben producir datos contradictorios.

## Supuestos

* Las universidades aliadas están dispuestas a compartir información académica de sus becarios, aunque el mecanismo exacto (manual o automatizado) no está definido todavía.
* Los donantes aceptan reportes de impacto agregados o anonimizados como parte de su acuerdo de patrocinio.
* La evaluación de mérito sigue siendo un proceso humano; la Fundación no busca automatizar el juicio del comité.

## Riesgos

* **Estacionalidad alta.** La mayor parte del uso se concentra en ventanas cortas: apertura de convocatorias y cierre de evaluación.
* **Migración de historial.** Parte de la información de becas y desembolsos existe hoy solo en hojas de cálculo; su incorporación a la nueva plataforma está por definir.
* **Dependencia de terceros.** La exactitud del seguimiento académico depende de que las universidades aliadas colaboren a tiempo.

---

## Catálogo de Requerimientos Funcionales

Los 40 requerimientos están agrupados en 7 áreas de negocio. Cada uno indica su prioridad: **Debe tener**, **Debería tener** o **Podría tener**.

### Área 1 — Postulación y Documentación

**RF-01 — Registro de postulantes · *Debe tener***
Un postulante debe poder crear un perfil con su información personal, académica y socioeconómica, necesaria para evaluar su elegibilidad.
*Consideraciones:*
* Debe estar matriculado o admitido en una universidad aliada.
* Campos mínimos: identificación, contacto, universidad, carrera, periodo académico actual, ingreso familiar declarado, composición familiar.
* No puede postular a un programa si ya tiene una beca activa en el mismo periodo, salvo que ese programa permita acumulación.

**RF-02 — Publicación de convocatorias · *Debe tener***
La Fundación debe poder publicar el ciclo de postulación de cada programa: fechas, criterios de elegibilidad, documentos requeridos y cupo disponible.
*Consideraciones:*
* Cada ciclo tiene fecha de apertura y de cierre; no se aceptan postulaciones fuera de esa ventana.
* Cada programa puede definir criterios adicionales a los generales (promedio mínimo, carrera específica, umbral de ingreso).

**RF-03 — Carga de documentos de soporte · *Debe tener***
El postulante debe poder adjuntar los documentos que pide el programa (notas, constancia de ingresos, identificación, cartas de recomendación, etc.).
*Consideraciones:*
* Cada programa puede pedir un conjunto distinto de documentos.
* Una postulación no está "completa" para evaluación hasta que todos los documentos obligatorios estén cargados.
* Un documento rechazado por el equipo debe indicar el motivo y permitir volver a subirlo antes de una fecha límite.
* Los documentos se guardan en el mismo servicio de archivos que ya usa la Fundación (ver Restricciones Técnicas del Cliente).

**RF-04 — Verificación automática de elegibilidad básica · *Debe tener***
Antes de entrar a evaluación, cada postulación debe pasar una verificación automática de las reglas básicas de elegibilidad.
*Consideraciones:*
* Las que no cumplen quedan marcadas como "No elegible" con el motivo, sin ocupar tiempo del comité.
* El postulante debe poder ver el motivo del rechazo en esta etapa.
* El coordinador de programa puede anular este resultado en casos límite.

**RF-05 — Retiro voluntario de una postulación · *Debería tener***
Un postulante debe poder retirar su postulación antes de que comience la evaluación.
*Consideraciones:*
* No se puede retirar una vez que la evaluación ya empezó.
* El retiro queda en el historial para efectos de reportería.

### Área 2 — Evaluación y Comité

**RF-06 — Conformación de comités evaluadores por programa · *Debe tener***
Cada programa debe poder tener un comité asignado, compuesto por uno o más evaluadores internos o externos.
*Consideraciones:*
* Cada postulación debe recibir al menos dos evaluaciones independientes, salvo que el coordinador autorice una excepción.
* Un evaluador no puede revisar una postulación si declara un conflicto de interés con esa persona.

**RF-07 — Rúbrica de evaluación configurable por programa · *Debe tener***
Cada programa debe poder definir sus propios criterios de evaluación y el peso de cada uno (mérito académico, necesidad socioeconómica, ensayo, etc.).
*Consideraciones:*
* Los pesos de todos los criterios del programa deben sumar 100%.
* La rúbrica no puede modificarse una vez que la evaluación de un ciclo ya comenzó.

**RF-08 — Registro de puntuaciones y comentarios de evaluadores · *Debe tener***
Un evaluador debe poder puntuar y comentar cada postulación que tiene asignada.
*Consideraciones:*
* Cada puntuación debe respetar el rango definido para ese criterio.
* Un evaluador no puede enviar su evaluación sin puntuar todos los criterios de la rúbrica.
* Las puntuaciones de un evaluador no son visibles para otros evaluadores hasta que cierra el periodo de revisión.

**RF-09 — Consolidación y ranking de postulantes · *Debe tener***
Una vez que todos los evaluadores asignados terminan, el sistema debe calcular una puntuación consolidada para ordenar a los postulantes elegibles de un programa.
*Consideraciones:*
* El método de consolidación (por ejemplo, promedio de evaluadores) debe quedar definido.
* Una diferencia grande entre evaluadores debería marcarse para una revisión de desempate.
* El ranking debe respetar el cupo del programa según el presupuesto disponible.

**RF-10 — Resolución de empates y casos límite · *Debe tener***
Cuando hay un empate en el límite del cupo, o el presupuesto no alcanza para otorgar todo lo que el ranking sugiere, debe existir un proceso definido de resolución.
*Consideraciones:*
* El coordinador o la jefatura del comité tiene la decisión final, con justificación registrada.
* Los postulantes elegibles no otorgados por falta de cupo deben quedar en lista de espera por si se libera disponibilidad.

### Área 3 — Programas, Fondos y Otorgamiento de Becas

**RF-11 — Registro y configuración de programas de becas · *Debe tener***
La Fundación debe poder crear y configurar un programa de becas: nombre, donante(s), presupuesto total, tipo de cobertura (total, parcial, monto fijo), cupo de becarios, universidades/carreras elegibles, duración y condiciones de renovación.
*Consideraciones:*
* El total otorgado en un programa no puede superar su presupuesto asignado.
* Un programa puede tener más de un donante, con el aporte de cada uno registrado por separado.

**RF-12 — Gestión de presupuesto y disponibilidad de cupos · *Debe tener***
La Fundación debe poder ver, en cualquier momento, cuánto presupuesto y cupo le queda disponible a un programa.
*Consideraciones:*
* Otorgar una nueva beca debe verificar presupuesto y cupo suficientes antes de confirmarse.
* El presupuesto comprometido en renovaciones pendientes del ciclo actual debe contarse como no disponible, no solo lo ya desembolsado.

**RF-13 — Alta y gestión de donantes o patrocinadores · *Debe tener***
La Fundación debe poder registrar organizaciones o personas donantes: datos de contacto, historial de aportes, y a qué programa(s) financian.
*Consideraciones:*
* Un donante puede financiar varios programas; un programa puede tener varios donantes.
* Debe conservarse el historial de aportes por donante y por periodo, para efectos legales o de reportería.

**RF-14 — Convenios con universidades aliadas · *Debe tener***
La Fundación debe poder registrar universidades aliadas: términos del convenio, carreras elegibles, costo de referencia de matrícula, y contacto designado.
*Consideraciones:*
* Un programa puede restringir la elegibilidad a universidades o carreras específicas.
* El costo de matrícula de referencia cambia cada periodo; debe poder actualizarse sin perder los valores usados en cálculos anteriores.

**RF-15 — Emisión de carta de otorgamiento · *Debe tener***
Cuando se selecciona a un postulante, debe generarse y comunicarse una oferta formal con el monto, la duración y las condiciones de la beca.
*Consideraciones:*
* La oferta debe incluir las condiciones de permanencia (promedio mínimo, horas de servicio si aplica, reportes exigidos).
* El postulante debe aceptar la oferta antes de una fecha límite; si no, la oferta expira y se considera al siguiente en la lista de espera.

**RF-16 — Aceptación o rechazo formal de la beca · *Debe tener***
El postulante seleccionado debe poder aceptar o rechazar formalmente la oferta.
*Consideraciones:*
* Al aceptar, pasa a ser becario/a del programa y periodo correspondiente.
* Si rechaza o la oferta expira, el cupo vuelve a estar disponible considerando la lista de espera.

**RF-17 — Firma de compromiso del becario · *Debe tener***
Algunos programas deben poder exigir un compromiso firmado con las obligaciones del becario antes de iniciar los desembolsos.
*Consideraciones:*
* Si el programa lo exige, los desembolsos no pueden iniciar sin ese compromiso registrado.
* Las condiciones del compromiso pueden variar según el programa.

**RF-18 — Historial de becas por postulante o becario · *Debe tener***
Debe existir un historial completo de todas las becas que ha recibido una persona, en cualquier programa y periodo.
*Consideraciones:*
* Debe poder consultarse por persona, por programa o por periodo.
* Sirve para verificar la regla de "no doble beneficio" del RF-01.

### Área 4 — Desembolsos y Pagos

**RF-19 — Programación de desembolsos por becario · *Debe tener***
Para cada becario activo, debe definirse el calendario de desembolsos (mensual, por semestre, o pago directo a la universidad).
*Consideraciones:*
* El calendario y los montos dependen de la configuración del programa (cobertura total, parcial, o monto fijo mensual).
* El total desembolsado a un becario en un periodo no puede superar lo aprobado en su oferta.

**RF-20 — Registro y confirmación de desembolsos realizados · *Debe tener***
El equipo financiero debe poder registrar cuándo se hizo efectivamente un desembolso: fecha, monto, método y destinatario (becario o universidad).
*Consideraciones:*
* Debe distinguirse si el pago fue directo al becario o a su universidad.
* Un desembolso fallido o devuelto debe quedar marcado y resuelto antes de considerarse pagado.
* Confirmar el desembolso requiere el código de verificación de un solo uso descrito en Restricciones Técnicas del Cliente.

**RF-21 — Suspensión temporal de desembolsos · *Debe tener***
La Fundación debe poder pausar los desembolsos de un becario sin cancelar la beca por completo.
*Consideraciones:*
* El motivo de la suspensión debe quedar registrado.
* El becario debe ser notificado con el motivo y lo que necesita resolver para reanudar.
* Toda suspensión debe tener una fecha de revisión o quedar marcada como indefinida hasta resolución manual.

**RF-22 — Revocación de una beca · *Debe tener***
Una beca debe poder cancelarse de forma definitiva (retiro de la universidad, información falsa, incumplimiento grave).
*Consideraciones:*
* El motivo debe quedar documentado.
* El presupuesto y cupo liberados deben volver a estar disponibles en el programa.
* Los montos ya desembolsados antes de la revocación no se reclaman automáticamente.
* Revocar una beca requiere el mismo código de verificación de un solo uso que un desembolso.

**RF-23 — Reporte de estado financiero por programa · *Debería tener***
La Fundación debe poder ver, por programa, el presupuesto comprometido, desembolsado y disponible.
*Consideraciones:*
* Debe reflejar el estado real considerando becas pendientes, suspendidas y revocadas.
* Debe poder compartirse o exportarse para reportar a un donante.

### Área 5 — Seguimiento Académico y Renovación

**RF-24 — Registro de reportes académicos periódicos · *Debe tener***
Los becarios (o su universidad) deben poder entregar información de rendimiento académico cada periodo: promedio, materias aprobadas/reprobadas, estado de matrícula.
*Consideraciones:*
* Existe una fecha límite de entrega por periodo; una entrega tardía debe generar una alerta.
* Si hay un convenio formal de intercambio de datos con la universidad, la información puede venir directamente de ella en vez del becario.

**RF-25 — Evaluación de cumplimiento de condiciones de permanencia · *Debe tener***
Tras recibir el reporte académico, debe evaluarse si el becario sigue cumpliendo las condiciones para conservar la beca.
*Consideraciones:*
* Si no las cumple, debe activarse una consecuencia según la política del programa: llamado de atención, suspensión o revocación.
* El becario debe poder ver con claridad qué condición no cumplió y cuál es el siguiente paso.

**RF-26 — Solicitud y evaluación de renovación · *Debe tener***
Antes de cada nuevo periodo académico, un becario activo que quiera continuar debe solicitar la renovación, y la Fundación debe evaluarla.
*Consideraciones:*
* La renovación no es automática; existe una ventana de tiempo para solicitarla.
* La aprobación depende del cumplimiento académico (RF-25) y de que el programa aún tenga presupuesto disponible.
* Si el becario no solicita la renovación a tiempo, la beca se da por concluida en ese programa, salvo excepción definida por el programa.

**RF-27 — Solicitud de pausa o aplazamiento · *Debería tener***
Un becario debe poder solicitar una pausa temporal de su beca (licencia médica, retiro temporal autorizado por su universidad) sin perderla por completo.
*Consideraciones:*
* La duración máxima de una pausa debe ser configurable por programa.
* Durante la pausa se detienen los desembolsos, pero la beca no se considera concluida.
* Reactivar la beca tras una pausa requiere verificar de nuevo el estatus académico.

**RF-28 — Historial completo del becario · *Debe tener***
El becario y el equipo de la Fundación deben poder ver una línea de tiempo completa de la beca: otorgamiento, desembolsos, reportes académicos, renovaciones y suspensiones.

### Área 6 — Comunicaciones, Reportería y Cumplimiento

**RF-29 — Notificaciones automáticas de hitos clave · *Debería tener***
Postulantes y becarios deben recibir notificación automática en momentos clave: postulación recibida, documentos faltantes, resultado de evaluación, oferta para aceptar, desembolso realizado, suspensión, entre otros.
*Consideraciones:*
* Cada notificación debe indicar qué ocurrió y qué acción, si alguna, se espera de la persona.
* El canal mínimo es correo electrónico; otros canales pueden agregarse según el programa.
* Enviar notificaciones a todo un ciclo (potencialmente miles de personas a la vez) no debe ralentizar la plataforma para el resto de los usuarios mientras se procesa.

**RF-30 — Recordatorios antes de un vencimiento · *Debería tener***
El sistema debe recordar proactivamente a postulantes y becarios sobre plazos próximos (documentos, solicitud de renovación, reporte académico) antes de que venzan, no solo después.
*Consideraciones:*
* El momento del recordatorio debe ser configurable (por ejemplo, 7 y 2 días antes).

**RF-31 — Comunicación al comité evaluador · *Podría tener***
Los evaluadores deben ser notificados cuando se les asignan nuevas postulaciones y recordados antes de que cierre el periodo de evaluación.

**RF-32 — Mensajería de postulantes y becarios hacia la Fundación · *Debería tener***
Postulantes y becarios deben poder hacer una consulta relacionada con su postulación o beca, y el equipo de la Fundación debe poder responderla y darle seguimiento.
*Consideraciones:*
* Cada consulta debe quedar asociada a la postulación o beca específica.
* Debe tener un estado (abierta, respondida, cerrada) para seguimiento del equipo.
* Los mensajes deben verse casi al instante por ambas partes, sin recargar la página.

**RF-33 — Reporte de impacto por programa o donante · *Debe tener***
Cada donante debe poder recibir un reporte periódico sobre el uso de su aporte: número de becarios, perfil general, ejecución presupuestaria y resultados académicos agregados.
*Consideraciones:*
* No debe exponerse información sensible individual (como el ingreso exacto de una familia); solo datos agregados o anonimizados, salvo que el becario haya aceptado explícitamente ser presentado de forma individual a su donante.

**RF-34 — Reporte consolidado institucional · *Debe tener***
La dirección de la Fundación debe poder ver una vista general de todos los programas: total de postulantes, becarios activos, presupuesto gestionado y desembolsado, por periodo, universidad y carrera.
*Consideraciones:*
* Consultar este reporte no debe afectar el rendimiento de las operaciones diarias (postulaciones, evaluaciones, desembolsos), incluso cuando el volumen de datos crezca.

**RF-35 — Trazabilidad y auditoría de decisiones · *Debe tener***
Toda decisión que afecte a un postulante o becario (aprobación, rechazo, suspensión, revocación, renovación) debe quedar registrada: quién la tomó, cuándo y por qué.
*Consideraciones:*
* Este historial no debe poder editarse ni eliminarse después de registrado.

**RF-36 — Exportación de información para auditorías externas · *Podría tener***
La Fundación, como organización sin fines de lucro, debe poder generar información para entes reguladores o auditorías externas sobre el uso de los fondos.

### Área 7 — Administración General

**RF-37 — Gestión de usuarios internos y sus permisos · *Debe tener***
El personal de la Fundación (coordinadores, evaluadores, finanzas, dirección) debe tener acceso limitado a lo que corresponde a su función.
*Consideraciones:*
* Un evaluador no debe ver las puntuaciones de otros evaluadores.
* El equipo financiero no debe poder modificar rúbricas de evaluación.
* La Fundación puede necesitar agregar nuevos tipos de usuario interno con el tiempo.

**RF-38 — Gestión del calendario académico institucional · *Debe tener***
La Fundación debe poder definir la secuencia de periodos académicos (por ejemplo, "2026-1", "2026-2") a los que se ligan programas, ciclos y renovaciones.
*Consideraciones:*
* Solo un periodo puede ser el "actual" a la vez, pero los periodos anteriores deben seguir siendo consultables.
* Pasar a un nuevo periodo no debe interrumpir renovaciones del periodo anterior que sigan en curso.

**RF-39 — Registro de conflictos de interés · *Debería tener***
Cualquier persona del equipo (evaluador, coordinador) debe poder declarar un conflicto de interés con un postulante específico, lo que restringe su participación en ese caso.

**RF-40 — Panel de indicadores para dirección · *Podría tener***
La dirección de la Fundación debe tener una vista resumida de indicadores clave: postulantes activos por ciclo, tasa de aprobación, presupuesto ejecutado versus disponible, y becarios en riesgo de incumplimiento.
*Consideraciones:*
* Los indicadores deben actualizarse a medida que ocurren los eventos (nueva postulación, desembolso, cambio de estatus), sin que dirección tenga que recargar la página.
* Consultar el panel no debe competir por recursos con las operaciones transaccionales del día a día.