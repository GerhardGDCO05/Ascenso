# Registro de Auditoría — Organización general del repositorio por microservicio

- **Fecha y hora de ejecución:** 8 de noviembre de 2026, 16:46 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** ajuste de la organización general para que la estructura sirva a todos los microservicios, manteniendo `ms-beca` como área de trabajo activa.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.

## 1. Aclaración atendida

La organización del repositorio debe ser general y separar las partes que corresponden a cada microservicio. `ms-beca` es el microservicio donde trabaja el equipo, pero la estructura, las convenciones y las reglas deben poder reutilizarse por los demás equipos sin mezclar código ni especificaciones.

## 2. Documento general de organización

Se creó `docs/organizacion_repositorio.md` con:

- Estructura de alto nivel del repositorio (`Apps/services/`, `docs/`, `openspec/`, `frontend/`, `.github/`).
- Tabla de los cinco microservicios de la plataforma y sus rutas de código y especificación.
- Ubicación de especificaciones, cambios, históricos, roadmaps y pipelines.
- Reglas de aislamiento por microservicio y de no duplicación documental.
- Procedimiento para incorporar un nuevo microservicio.
- Situación actual de `ms-beca` como área de trabajo activa.

## 3. Convenciones generalizadas

En `docs/convenciones_c_y_ddd.md`:

- La estructura de proyectos dejó de estar fijada solo a `ms-becas`.
- Ahora se muestra `Apps/services/` con `ms-becas` como ejemplo implementado y `ms-<otro>/` como plantilla para los demás equipos.
- Se aclara que los nombres `Ascenso.Becas.*` corresponden a `ms-beca` y que cada equipo reemplaza `Becas` por el contexto de su microservicio.

## 4. Configuración de OpenSpec

En `openspec/config.yaml`:

- Se agregó `docs/organizacion_repositorio.md` como contexto general.
- La estructura hexagonal se describió de forma general: `Apps/services/<microservicio>/` y `Ascenso.<Contexto>.*`.
- Se agregó la sección **6. Organización por Microservicio** con las reglas de aislamiento, el foco en `ms-beca` y el archivado de históricos.
- Se corrigieron las referencias a los roadmaps: `por_integrante.md` y `por_microservicio.md`.
- Se añadió una regla de especificaciones para mantener una carpeta por microservicio y no modificar las de otros equipos.

## 5. README

Se agregó una sección de organización del repositorio con las rutas principales y un enlace a `docs/organizacion_repositorio.md`, indicando que el trabajo activo está en `ms-beca` y que `ms-pos` y `ms-plt` son contexto de otros equipos.

## 6. Validación ejecutada

```text
python3 (yaml.safe_load openspec/config.yaml)          → YAML OK
python3 (yaml.safe_load .github/workflows/ci-cd.yml)   → YAML OK
openspec validate bootstrap-ms-beca --strict           → cambio válido
dotnet test ... --configuration Release                → 59/59 pruebas correctas
git diff --check                                       → sin errores de espacio
```

## 7. Estado local

- Cambios locales sin commit y sin `git push`.
- `ms-pos` y `ms-plt` permanecen intactos.
- La documentación general vive en `docs/`; las especificaciones por microservicio en `openspec/specs/ms-*`.
- El cambio activo sigue siendo `bootstrap-ms-beca` y el trabajo funcional se concentra en `ms-beca`.
