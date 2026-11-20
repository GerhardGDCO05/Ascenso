# Proposal: Registro del Perfil Socioeconómico y Grupo Familiar (`rf1-2-int1-mspos`)

## Why

La HU 1.1 dejó el perfil del postulante en estado `INCOMPLETO`: aún no existe información socioeconómica ni composición familiar, por lo que no puede aplicarse `RN-POS-01` ni alimentarse la futura verificación de elegibilidad de `RF-04`. La HU 1.2 completa esa brecha dentro de `RF-01`, permitiendo que el postulante declare su situación laboral, sus ingresos y la composición de su hogar.

El PRD exige que el perfil socioeconómico sea necesario para evaluar elegibilidad y que los datos de ingresos y composición familiar se traten como información confidencial. Este cambio incorpora esa sección al agregado `Postulante` y define la transición a `COMPLETO`.

## What Changes

- **Perfil socioeconómico del postulante:** se agregan `estadoCivil`, `promedioActual`, `trabaja`, `ocupacion` e `ingresoMensualPostulante`.
- **Grupo familiar:** se agrega la entidad `MiembroFamiliar` dentro del agregado `Postulante`, con `parentesco`, `edad`, `ocupacion` e `ingresoMensual`.
- **Lista familiar opcional:** `miembrosFamiliares` puede ser vacía para postulantes que viven solos o son foráneos. La sola declaración válida de la sección socioeconómica completa el perfil.
- **Cálculos automáticos:** `numeroIntegrantesFamilia` se deriva como `miembrosFamiliares.Count + 1` e `ingresoFamiliarMensualTotal` como la suma del ingreso del postulante y los ingresos de sus familiares. El cliente no envía ninguno de estos valores.
- **Catálogos cerrados:** `EstadoCivil`, `Parentesco` y `Ocupacion` se modelan como enumeraciones persistidas como códigos en mayúsculas.
- **Actualización idempotente:** nuevo `PUT /api/v1/postulantes/mio/socioeconomico` que reemplaza la sección socioeconómica completa y devuelve el perfil actualizado.
- **Transición a `COMPLETO`:** un guardado válido del perfil socioeconómico cambia `estadoPerfil` a `COMPLETO` y actualiza la auditoría y la versión optimista.
- **Consulta ampliada:** `GET /api/v1/postulantes/mio` devuelve también los datos socioeconómicos, los cálculos y la composición familiar.
- **Ajuste explícito de `RN-POS-01`:** la completitud dejará de exigir al menos un `MiembroFamiliar`; exigirá datos académicos válidos y una sección socioeconómica válida, con composición familiar declarada aunque sea vacía.
- **Confidencialidad:** solo el propietario autenticado accede a estos datos; no se almacenan nombres de familiares y no se publican eventos con información socioeconómica.

**Fuera de alcance:**

- Implementación de frontend, endpoints para evaluadores o consultas administrativas.
- Verificación automática de elegibilidad (`RF-04`) y uso del perfil en ranking.
- Publicación de eventos de integración y `Transactional Outbox`; se mantiene la deuda registrada en `DEC-08`.
- Modificación de `MS-PRG`, `MS-PLT` o contratos de otros microservicios.

## Capabilities

### New Capabilities

<!-- Ninguna capacidad nueva: el comportamiento pertenece a la capacidad existente del perfil del postulante. -->

### Modified Capabilities

- `ms-pos/postulantes`: se agrega el registro y actualización del perfil socioeconómico y grupo familiar, el cálculo del ingreso total y la transición a `COMPLETO`; también se amplía la consulta del perfil propio.

## Impact

- **Especificaciones OpenSpec:** delta en `openspec/changes/rf1-2-int1-mspos/specs/ms-pos/postulantes/spec.md`; al archivar se sincroniza con `openspec/specs/ms-pos/postulantes/spec.md`. Se actualizará `docs/Explicacion_del_DDD_V7.md` para reflejar la nueva redacción de `RN-POS-01` y los datos laborales del postulante.
- **Dominio (`Ascenso.Postulaciones.Domain`):** nuevos enums `EstadoCivil`, `Parentesco` y `Ocupacion`; nueva entidad `MiembroFamiliar`; ampliación de `Postulante` con campos, colección familiar, método de registro socioeconómico y transición a `EstadoPerfil.Completo`; nuevas excepciones de dominio.
- **Aplicación (`Ascenso.Postulaciones.Application`):** nuevo comando `ActualizarPerfilSocioeconomicoCommand` con handler y validador FluentValidation; ampliación de `PostulanteDto`, nuevo DTO de familiar y actualización del mapeador.
- **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):** columnas nuevas en `postulantes`, tabla `miembros_familiares` con FK y borrado en cascada, configuración EF Core, `CHECK` constraints, consultas con `Include` y consulta de actualización con tracking; migración correspondiente.
- **API (`Ascenso.Postulaciones.Api`):** nueva acción `PUT /api/v1/postulantes/mio/socioeconomico` en `PostulantesController`, con respuestas `200`, `400`, `401`, `404`, `409` y `422`; `GET /mio` devuelve la sección ampliada.
- **Pruebas:** unitarias de dominio y aplicación, pruebas de modelo de persistencia, pruebas de repositorio y pruebas de endpoint con autenticación de prueba; sin dependencia obligatoria de PostgreSQL o Keycloak reales en CI.
- **Dependencias entre servicios:** ninguna nueva. No se modifica `MS-PRG`, `MS-PLT` ni `MS-BEC`.
- **Roadmaps:** al completar la implementación se marcará la HU 1.2 en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`, sin marcar `RF-01` como completo.
