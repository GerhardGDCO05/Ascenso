# Especificación de dominio — MS-POSTULACIONES (estructura de carpetas)

Este archivo registra que el microservicio `ms-postulaciones` cuenta con su esqueleto de carpetas y proyectos .NET listo, replicando la arquitectura hexagonal de `ms-becas`.

## Alcance

- Estructura de carpetas y archivos de proyecto únicamente.
- No incluye entidades, agregados, objetos de valor, excepciones, puertos, repositorios ni casos de uso.

## Convenciones respetadas

- Carpeta de microservicio en `Apps/services/ms-postulaciones/`.
- Cuatro proyectos obligatorios: `Domain`, `Application`, `Infrastructure`, `Api`.
- Proyecto de pruebas en `tests/Ascenso.Postulaciones.Domain.Tests/`.
- Nombres de capa y carpeta en PascalCase.
- Target framework `net10.0`.

## Pendiente (cambios futuros)

- Modelo de dominio de postulaciones (Postulante, Postulacion, EstadoPostulacion, etc.).
- Casos de uso de aplicación.
- Adaptadores de infraestructura (EF Core, clientes HTTP, mensajería).
- Controladores de API pública e interna.
