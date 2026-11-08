# Ascenso

Base del proyecto Ascenso. Incluye el entorno de desarrollo, el frontend en React y los microservicios en .NET organizados bajo `Apps/services`.

## Entorno de desarrollo

| Herramienta | Versión |
| :--- | :--- |
| .NET SDK | 10.0.403 (instalación local en `~/.dotnet`) |
| Entity Framework Core Tools | 10.0.14 |
| Node.js | 24.19.0 |
| npm | 11.17.0 |
| Docker | 29.7.2 |
| Docker Compose | 5.5.0 |

El SDK de .NET y `dotnet-ef` se cargan automáticamente en terminales nuevas mediante `~/.bashrc` y `~/.profile`. La versión del SDK está fijada en `global.json`.

