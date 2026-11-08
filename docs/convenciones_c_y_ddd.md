# Guía Estándar de Convenciones C#, DDD y Arquitectura Hexagonal — Plataforma Ascenso

Este documento define el estándar estricto de codificación, nombrado y diseño técnico que debe seguir todo agente (OpenCode / DeepSeek) y desarrollador al escribir código C# (.NET 10) en la solución `Ascenso.Becas` y demás microservicios.

## 1. Convenciones Estructurales y de Proyectos (Arquitectura Hexagonal)

La solución debe mantener cuatro proyectos con inversión de dependencias estricta:

```
src/
├── Ascenso.Becas.Domain/            # C# Puro (Sin dependencias externas)
├── Ascenso.Becas.Application/       # Casos de Uso, DTOs, Puertos de Aplicación
├── Ascenso.Becas.Infrastructure/    # Adaptadores EF Core, MassTransit, Keycloak, etc.
└── Ascenso.Becas.API/               # Controllers, Middlewares, Dependency Injection Setup
```

### 1.1 Estructura de Carpetas y Namespaces

* **Carpetas (`PascalCase`):** Representan conceptos de dominio o capas.
* **Namespaces (`PascalCase`):** Reflejan la ruta exacta de la carpeta raíz.
  * *Ejemplo:* `namespace Ascenso.Becas.Domain.Otorgamiento.Agregados;`
  * Utilizar **File-Scoped Namespaces** (sintaxis C# 10+) en todos los archivos.

---

## 2. Convenciones Generales de Código C#

| Elemento | Convención C# | Ejemplo |
| :--- | :--- | :--- |
| **Clases / Structs / Records** | `PascalCase` | `Beca`, `OfertaBeca`, `DatosPagoBecario` |
| **Interfaces** | `IPascalCase` | `IBecaRepository`, `IClock`, `IOtpClient` |
| **Métodos y Propiedades** | `PascalCase` | `Transicionar()`, `MontoComprometido`, `FechaCreacion` |
| **Campos Privados** | `_camelCase` | `_dbContext`, `_logger`, `_becaRepository` |
| **Parámetros de Métodos** | `camelCase` | `postulanteId`, `motivoJustificado`, `fechaCorte` |
| **Variables Locales** | `camelCase` | `becaExistente`, `presupuestoDisponible` |
| **Constantes / Enums** | `PascalCase` | `EstadoBeca.Activa`, `TipoCobertura.Total` |
| **Tipos Genéricos** | `T` o `TPascalCase` | `TResult`, `TEvent`, `TEntity` |

---

## 3. Convenciones para Domain-Driven Design (DDD)

### 3.1 Nomenclatura del Dominio en Español

Los nombres de conceptos de negocio (**Agregados, Entidades, Objetos de Valor, Enumeraciones, Eventos de Dominio y Excepciones**) **DEBEN escribirse literalmente en español** como están definidos en la Especificación Maestra (`Explicacion_del_DDD_V7.md`). Las abstracciones de código o infraestructura se escriben en inglés o según el patrón técnico.

### 3.2 Agregados Raíz y Entidades (`Domain/`)

1. **Ubicación:** `Domain/{Modulo}/Agregados/` o `Domain/{Modulo}/Entidades/`.
2. **Identificador:** Toda entidad hereda de `EntidadAuditable` o `EntidadInmutable` y usa identificador `UUIDv7` (tipo `Guid`).
3. **Encapsulamiento de Estado (Setters Privados):** Ninguna propiedad mutable tiene `set` público. La modificación del estado ocurre únicamente mediante métodos de dominio con significación de negocio.

```csharp
namespace Ascenso.Becas.Domain.Otorgamiento.Agregados;

public sealed class Beca : EntidadAuditable
{
    // Propiedades con get público y set privado
    public Guid CadenaBecaId { get; private set; }
    public Guid? BecaAnteriorId { get; private set; }
    public string CodigoBeca { get; private set; } = string.Empty;
    public EstadoBeca Estado { get; private set; }
    public PoliticaCobertura CoberturaAplicada { get; private set; } = null!;

    // Constructor privado para EF Core / Factories de dominio
    private Beca() { }

    // Método de Fábrica de Dominio
    public static Beca CrearOtorgada(
        Guid postulanteEstudianteId,
        Guid postulacionOrigenId,
        PoliticaCobertura cobertura)
    {
        return new Beca
        {
            Id = Guid.CreateVersion7(),
            Estado = EstadoBeca.Otorgada,
            CoberturaAplicada = cobertura
        };
    }

    // Método de Dominio expresivo (no setter directo)
    public void Activar(Guid usuarioAutorId)
    {
        if (Estado != EstadoBeca.Otorgada)
            throw new TransicionEstadoInvalidaException(Estado, EstadoBeca.Activa);

        Estado = EstadoBeca.Activa;
        ActualizarAuditoria(usuarioAutorId);
        
        RegistrarEventoDominio(new BecaActivadaDominioEvent(Id, usuarioAutorId));
    }
}
```

### 3.3 Objetos de Valor (Value Objects) (`Domain/`)

1. **Definición:** Implementados como `readonly record struct` o `sealed record class` inmutables.
2. **Validación:** Se validan completamente en la instanciación o fábrica estática.

```csharp
namespace Ascenso.Becas.Domain.Otorgamiento.ObjetosValor;

public sealed record class PoliticaCobertura
{
    public TipoCobertura Modalidad {; }
    public decimal PorcentajeCobertura {; }
    public decimal MontoFijoPeriodo {; }
    public string Moneda {; }

    public PoliticaCobertura(TipoCobertura modalidad, decimal porcentaje, decimal montoFijo, string moneda)
    {
        if (porcentaje is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(porcentaje), "El porcentaje debe estar entre 0 y 100.");

        Modalidad = modalidad;
        PorcentajeCobertura = porcentaje;
        MontoFijoPeriodo = montoFijo;
        Moneda = moneda;
    }
}
```

### 3.4 Enumeraciones y Máquinas de Estado (`Domain/`)

1. **Nombres de Enums:** `PascalCase` en singular (`EstadoBeca`, `EstadoOferta`, `TipoCierreBeca`).
2. **Valores:** `PascalCase` (`Otorgada`, `Activa`, `Suspendida`, `Renovada`, `Concluida`, `Revocada`).
3. **Persistencia:** En base de datos se mapean a cadenas `VARCHAR` exactas en MAYÚSCULAS (`OTORGADA`, `ACTIVA`).

### 3.5 Puertos de Salida e Interfaces (`Domain/` y `Application/`)

1. **Prefijo `I` obligatorio:** `IBecaRepository`, `IOfertaBecaRepository`, `IUnitOfWork`.
2. **Sufijo `Async` en Métodos Asíncronos:**
   * `Task<Beca?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);`
   * `Task GuardarAsync(Beca beca, CancellationToken cancellationToken = default);`

---

## 4. Convenciones de la Capa de Aplicación (`Application/`)

### 4.1 Comandos, Consultas y Handlers (CQRS / CQRS-Lite)

* **Comando (Escritura):** Sufijo `Command` (ej. `AceptarOfertaBecaCommand`).
* **Consulta (Lectura):** Sufijo `Query` (ej. `ObtenerBecaPorIdQuery`).
* **Manejador:** Sufijo `CommandHandler` o `QueryHandler`.
* **DTOs de Respuesta:** Sufijo `Dto` o `Response`.

```csharp
namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOferta;

public sealed record AceptarOfertaBecaCommand(
    Guid OfertaId,
    Guid PostulanteEstudianteId
) : ICommand<Result>;

public sealed class AceptarOfertaBecaCommandHandler : ICommandHandler<AceptarOfertaBecaCommand, Result>
{
    private readonly IBecaRepository _becaRepository;

    public AceptarOfertaBecaCommandHandler(IBecaRepository becaRepository)
    {
        _becaRepository = becaRepository;
    }

    public async Task<Result> Handle(AceptarOfertaBecaCommand command, CancellationToken cancellationToken)
    {
        // Lógica de orquestación
    }
}
```

---

## 5. Convenciones para Infraestructura y API (`Infrastructure/` y `API/`)

### 5.1 Adaptadores y Implementaciones de Repositorio

* **Sufijo de Repositorio:** `BecaRepository`, `OfertaBecaRepository`.
* **DbContext:** `BecasDbContext`.
* **Clientes HTTP Internos:** Sufijo `Client` (`PostulacionesClient`, `ProgramasClient`).

### 5.2 Controladores API

* **Ubicación:** `API/Controllers/v1/` o `API/Controllers/Internal/v1/`.
* **Sufijo de Clase:** `Controller` (`OfertasBecaController`, `BecasController`).
* **Rutas REST:** `kebab-case` en plural.
  * `/api/v1/becas/ofertas`
  * `/internal/v1/becas/activas`