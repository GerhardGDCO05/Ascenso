# Ascenso

Base del proyecto Ascenso. Incluye el entorno de desarrollo, una API en .NET y una aplicación web en React, preparados para incorporar nuevas funcionalidades y microservicios.

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

## Estructura

```text
backend/
  Ascenso.slnx              Solución .NET
  src/Ascenso.Api/          API mínima con OpenAPI y health checks
  tests/Ascenso.Api.Tests/  Pruebas de la API
frontend/                   Aplicación React + Vite
```

## Backend

```bash
dotnet restore backend/Ascenso.slnx
dotnet build backend/Ascenso.slnx
dotnet test backend/Ascenso.slnx
dotnet run --project backend/src/Ascenso.Api
```

La API se ejecuta en `http://localhost:5122`. Endpoints disponibles:

| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| GET | `/` | Información básica de la API |
| GET | `/health` | Estado del servicio |
| GET | `/openapi/v1.json` | Documento OpenAPI (solo en Development) |

El backend no contiene lógica de negocio. Los microservicios se agregarán como proyectos independientes dentro de `backend/src` cuando inicie su implementación.

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Verificaciones:

```bash
npm run build
npm run lint
```

La aplicación de desarrollo se publica en `http://localhost:5173`.

## Uso

1. Instala las dependencias del frontend con `npm install` dentro de `frontend/`.
2. Ejecuta el backend y el frontend en terminales separadas.
3. Consulta la API en `http://localhost:5122` y la aplicación web en `http://localhost:5173`.
