# Registro de Auditoría — Verificación de `tasks.md` de MS-BECA

- **Fecha y hora de ejecución:** 10 de noviembre de 2026, 23:48 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** analizar `openspec/changes/bootstrap-ms-beca/tasks.md` y marcar únicamente las tareas cuyo criterio de verificación ya está cumplido.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.

## 1. Criterio aplicado

No se marcó una tarea por el simple hecho de tener código relacionado. Se exigió que el entregable y la verificación descritos en la propia tarea estuvieran presentes y ejecutados.

## 2. Tareas marcadas en esta sesión

### `12.10` — Validaciones locales de calidad

> Ejecutar `dotnet format --verify-no-changes`, `dotnet build -warnaserror` y `dotnet test` sobre toda la solución; verificar que el pipeline local queda verde.

Verificado sobre `Apps/services/ms-becas/Ascenso.Becas.sln`:

```text
dotnet build --configuration Release --warnaserror  → 0 advertencias, 0 errores
dotnet test --configuration Release                → 59/59 pruebas correctas
dotnet format --verify-no-changes                  → correcto
```

### `14.4` — Validación de OpenSpec

> Ejecutar `openspec validate bootstrap-ms-beca --strict` y corregir cualquier observación; verificar que el cambio queda listo para aplicar.

```text
openspec validate bootstrap-ms-beca --strict → Change 'bootstrap-ms-beca' is valid
```

## 3. Tareas revisadas y no marcadas

- `1.3`: solo existe `Ascenso.Becas.Domain.Tests`; faltan `Application.Tests`, `ArchitectureTests` e `IntegrationTests`.
- `1.4`: faltan `Directory.Build.props`, `Directory.Packages.props` y `.editorconfig`; no hay versiones centralizadas.
- `1.5`: no hay lock files ni `RestoreLockedMode`.
- `1.6`: no están agregados los paquetes base ni la mayoría de paquetes de prueba.
- `1.7`: el README no documenta comandos de build/test ni la verificación en copia limpia.
- `1.8`: `dotnet format` pasa, pero no está configurado como parte del build ni hay análisis de código configurado.
- `1.9`: `dotnet-ef` está instalado, pero el README no documenta el comando de instalación.
- `3.1` a `3.11`: no existen los agregados `Beca`, `OfertaBeca`, `CompromisoBecario` ni `HistorialEstadoBeca`.
- `4.x` a `11.x`: persistencia, seguridad, aplicación, integraciones, API, Hangfire y observabilidad siguen pendientes.
- `12.1` a `12.9` y `12.11`: dependen de suites y funcionalidades que aún no existen.
- `13.x`: no hay compose, Keycloak, Makefile, Dockerfile ni stubs.
- `14.1` a `14.3` y `14.5`: no hay contratos publicados ni revisión de alcance con otros equipos.

## 4. Resultado

- **Antes:** 9 de 114 tareas completadas.
- **Después:** 11 de 114 tareas completadas.
- **Nuevas marcas:** `12.10` y `14.4`.
- Ningún RF de MS-BEC se marcó en los roadmaps porque siguen sin estar funcionalmente completos.

## 5. Validación posterior a la edición

```text
dotnet build ... --configuration Release --warnaserror     → 0 advertencias, 0 errores
dotnet test ... --configuration Release                    → 59/59 pruebas correctas
dotnet format ... --verify-no-changes                      → correcto
openspec validate bootstrap-ms-beca --strict               → cambio válido
```

## 6. Estado local

- Único archivo modificado: `openspec/changes/bootstrap-ms-beca/tasks.md`.
- Sin commit y sin `git push`.
- `ms-pos` y `ms-plt` intactos.
