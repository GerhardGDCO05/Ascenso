## Tasks

- [x] Crear `Directory.Build.props` con target framework, nullable, warnings-as-errors y análisis de código.
- [x] Crear `Directory.Packages.props` con las versiones centralizadas de los 33 paquetes.
- [x] Crear `.editorconfig` con estilo, nombres y severidad de analizadores.
- [x] Fijar el SDK `10.0.403` en `global.json`.
- [x] Generar los ocho `packages.lock.json` y verificar `dotnet restore --locked-mode`.
- [x] Actualizar el workflow con restore bloqueado, build con `--warnaserror` y verificación de formato.
- [x] Documentar requisitos, comandos, lock files, licencias y `dotnet-ef` en el `README.md`.
- [x] Corregir `docs/organizacion_repositorio.md` y el nombre `MassTransit.TestFramework` en `design.md`.
- [x] Marcar las tareas `1.4` a `1.9` del cambio `bootstrap-ms-beca`.
- [x] Verificar build, pruebas, formato, copia limpia, `actionlint` y validación OpenSpec.
