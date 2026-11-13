## Why

El microservicio `ms-becas` necesitaba una base de entorno reproducible: SDK fijado, configuración centralizada, versiones de paquetes administradas, lock files, análisis de código, formato, README con comandos verificables y `dotnet-ef` documentado. Sin esa base, cada proyecto podía divergir en versiones y el CI no podría restaurar de forma determinista.

## What Changes

- Crear `Directory.Build.props`, `Directory.Packages.props` y `.editorconfig` dentro de `Apps/services/ms-becas/`.
- Fijar el SDK en `global.json` y centralizar las versiones de los 33 paquetes.
- Generar los ocho `packages.lock.json` y verificar `dotnet restore --locked-mode`.
- Actualizar el workflow con restore bloqueado, build con `--warnaserror` y `dotnet format --verify-no-changes`.
- Documentar en el `README.md` requisitos, estructura, comandos, lock files, licencias e instalación de `dotnet-ef`.
- Corregir `docs/organizacion_repositorio.md` y el nombre del paquete de pruebas de MassTransit en `design.md`.
- Marcar las tareas `1.4` a `1.9` de `bootstrap-ms-beca`.

**Fuera de alcance:** imponer configuración, versiones o reglas a `ms-pos` o `ms-plt`.

## Capabilities

### New Capabilities

- Ninguna. Es preparación de entorno y cadena de build.

### Modified Capabilities

- Ninguna.

## Impact

- **Configuración:** `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` y ocho `packages.lock.json`.
- **CI:** restore con `--locked-mode`, build con `--warnaserror` y verificación de formato.
- **Documentación:** `README.md` y `docs/organizacion_repositorio.md`.
- **Validación:** 67/67 pruebas, restore bloqueado estable, build sin advertencias, formato correcto y validación OpenSpec.
