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

## Estructura

```text
Apps/
  services/
    ms-becas/
      Ascenso.Becas.sln
      Ascenso.Becas.Domain/
      Ascenso.Becas.Application/
      Ascenso.Becas.Infrastructure/
      Ascenso.Becas.Api/
frontend/                   Aplicación React + Vite
openspec/                   Especificaciones y cambios
```

Los microservicios se ubican en `Apps/services/<nombre-del-servicio>/`. Cada uno mantiene su propia solución .NET y la separación de capas de la arquitectura hexagonal.

## Microservicio ms-becas

```bash
dotnet build Apps/services/ms-becas/Ascenso.Becas.sln
dotnet run --project Apps/services/ms-becas/Ascenso.Becas.Api
```

La API se ejecuta en `http://localhost:5120`.

Capas:

```text
Ascenso.Becas.Domain/          Entidades, objetos de valor, excepciones y repositorios
Ascenso.Becas.Application/     Casos de uso, DTOs, puertos y código común
Ascenso.Becas.Infrastructure/  Persistencia, adaptadores y configuraciones
Ascenso.Becas.Api/             Controladores, Program.cs y configuración
```

El microservicio contiene únicamente el esqueleto de capas; todavía no tiene entidades ni endpoints de negocio.

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
2. Ejecuta el microservicio y el frontend en terminales separadas.
3. Consulta la API en `http://localhost:5120` y la aplicación web en `http://localhost:5173`.
