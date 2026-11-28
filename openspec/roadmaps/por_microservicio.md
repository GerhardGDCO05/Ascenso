# Roadmap por Microservicios (Arquitectura DDD)

## ⚙️ MS-PLT (Plataforma y Base)

*Gestión transversal de usuarios, identidad, calendario y comunicaciones.*

- [ ] **RF-29**: Notificaciones automáticas de hitos clave
- [ ] **RF-30**: Recordatorios preventivos antes de un vencimiento
- [ ] **RF-31**: Comunicación al comité evaluador
- [ ] **RF-32**: Mensajería de postulantes hacia la Fundación
- [ ] **RF-37**: Gestión de usuarios internos y permisos RBAC
- [ ] **RF-38**: Gestión del calendario académico institucional

## 📝 MS-POS (Postulaciones y Evaluación)

*Postulación, elegibilidad, comité y selección. MS-POS consume el contrato interno de lectura de convocatorias publicado por MS-PRG; no crea ni duplica el agregado.*

- [x] **RF-01**: Registro de postulantes y perfil socioeconómico
  - [x] HU 1.1: Registro de datos personales y académicos base (2026-11-15)
  - [x] HU 1.2: Registro del perfil socioeconómico y grupo familiar (2026-11-19)
- [ ] **RF-03**: Carga de documentos de soporte
- [ ] **RF-04**: Verificación automática de elegibilidad básica
- [ ] **RF-05**: Retiro voluntario de postulación
- [x] **RF-06**: Conformación de comités evaluadores
  - [x] HU 6.1: Conformación del comité evaluador por programa y convocatoria (2026-11-22)
  - [x] HU 6.2: Asignación de postulaciones a evaluadores del comité (2026-11-24)
  - [x] HU 6.3: Autorización de excepción de evaluador único por el coordinador (2026-11-24)
- [ ] **RF-07**: Configuración de rúbricas de evaluación
  - [x] HU 7.1: Gestión de criterios, pesos porcentuales y publicación de rúbricas (2026-11-21)
  - [x] HU 7.2: Bloqueo de rúbricas publicadas al iniciar evaluación o primera calificación (2026-11-22)
- [x] **RF-08**: Registro de puntuaciones y comentarios
  - [x] HU 8.1: Registro de puntuaciones y comentarios de evaluadores sobre postulaciones asignadas (2026-11-25)
  - [x] HU 8.2: Visibilidad post-cierre entre coevaluadores y validación del cierre del periodo de revisión (2026-11-25)
- [x] **RF-09**: Consolidación y ranking de postulantes
  - [x] HU 9.1: Consolidación automática de puntuaciones definitivas, alertas de discrepancia y excepción de evaluador único (2026-11-26)
  - [x] HU 9.2: Ranking de postulantes, línea de corte por cupo/presupuesto y confirmación final (2026-11-26)
- [x] **RF-10**: Resolución de empates y casos límite / Lista de espera
  - [x] HU 10.1: Resolución de empates y casos límite con justificación obligatoria (2026-11-26)
  - [x] HU 10.2: Gestión de lista de espera y reasignación por vacante (2026-11-27)
- [ ] **RF-39**: Registro de conflictos de interés

## 💼 MS-PRG (Programas y Presupuesto)

*Programas, fondos, donantes, convenios, control presupuestario y convocatorias.*

- [x] **RF-02**: Creación, configuración y publicación de convocatorias (HU 2.1; contrato de lectura consumido por MS-POS)
- [x] **RF-11**: Registro y configuración de programas de becas
- [x] **RF-12**: Gestión de presupuesto y disponibilidad de cupos
- [x] **RF-13**: Alta y gestión de donantes / aportes
- [x] **RF-14**: Convenios con universidades aliadas y aranceles

## 🎓 MS-BEC (Becas y Seguimiento)

*Otorgamiento, ciclo de vida de la beca, desembolsos y seguimiento académico.*

- [ ] **RF-15**: Emisión de carta de otorgamiento / Oferta (flujo base implementado; integraciones pendientes)
- [ ] **RF-16**: Aceptación o rechazo formal de la beca (flujo base implementado; cierre de reserva pendiente)
- [x] **RF-17**: Firma de compromiso del becario
- [ ] **RF-19**: Programación de calendarios de desembolsos
- [ ] **RF-20**: Registro y confirmación de pagos con OTP
- [ ] **RF-21**: Suspensión temporal de desembolsos
- [ ] **RF-22**: Revocación definitiva de una beca
- [ ] **RF-24**: Registro de reportes académicos periódicos
- [ ] **RF-25**: Evaluación de cumplimiento de condiciones de permanencia
- [ ] **RF-26**: Solicitud y evaluación de renovación / Cadena de becas
- [ ] **RF-27**: Solicitud de pausa o aplazamiento

## 📊 MS-AUD (Auditoría y Reportería CQRS)

*Auditoría inmutable, historial unificado y reportería institucional.*

- [ ] **RF-18**: Historial de becas por persona
- [ ] **RF-23**: Reporte de estado financiero por programa
- [ ] **RF-28**: Historial completo y consolidado del becario
- [ ] **RF-33**: Reporte de impacto anonimizado para donantes
- [ ] **RF-34**: Reporte consolidado institucional
- [ ] **RF-35**: Trazabilidad inmutable y auditoría de decisiones
- [ ] **RF-36**: Exportación de paquetes para auditorías externas
- [ ] **RF-40**: Panel de indicadores para dirección en tiempo real
