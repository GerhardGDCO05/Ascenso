# Bitácora de archivo: rf2-1-int1-msprg

**Fecha:** 2026-11-25  
**Microservicio:** MS-PRG  
**Change:** `rf2-1-int1-msprg`

## Resultado

Se implementó la HU 2.1 para creación, configuración, consulta y publicación de convocatorias en MS-PRG, incluyendo documentos requeridos, seguridad PBAC, persistencia, cierre automático y contrato interno de lectura para MS-POS. MS-POS consume el contrato; no duplica el agregado.

La spec delta se sincronizó en `openspec/specs/ms-prg/convocatorias/spec.md`.

## Verificación

- `dotnet build Apps\services\ms-prg\Ascenso.Programas.sln --no-restore`: correcto, 0 errores y 0 advertencias.
- `dotnet test Apps\services\ms-prg\Ascenso.Programas.sln --no-build`: 211 pruebas correctas.
- `openspec validate rf2-1-int1-msprg --strict`: válido.
- `openspec validate --specs`: 9 specs correctas.

El mapper de `permissions` en Keycloak y la autenticación del endpoint interno de MS-PLT quedan como coordinaciones externas; no se modificaron en este cambio.
