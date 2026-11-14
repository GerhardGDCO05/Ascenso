using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Universidades.Entidades;

/// <summary>
/// Carrera elegible ofrecida por una universidad aliada y asociada a su historial de costos de matrícula.
/// </summary>
public sealed class CarreraElegible : EntidadAuditable
{
    private readonly List<CostoMatriculaReferencia> _costosMatricula = [];

    private CarreraElegible()
    {
    }

    private CarreraElegible(
        Guid id,
        Guid universidadAliadaId,
        string codigoCarrera,
        string nombre,
        int duracionSemestres,
        bool activa,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        UniversidadAliadaId = universidadAliadaId;
        CodigoCarrera = codigoCarrera;
        Nombre = nombre;
        DuracionSemestres = duracionSemestres;
        Activa = activa;
    }

    public Guid UniversidadAliadaId { get; private set; }

    public string CodigoCarrera { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public int DuracionSemestres { get; private set; }

    public bool Activa { get; private set; }

    public IReadOnlyCollection<CostoMatriculaReferencia> CostosMatricula => _costosMatricula.AsReadOnly();

    internal static CarreraElegible Crear(
        Guid universidadAliadaId,
        string codigoCarrera,
        string nombre,
        int duracionSemestres,
        Guid usuarioCreacionId,
        DateTimeOffset fechaCreacion)
    {
        if (universidadAliadaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la universidad aliada es obligatorio.", nameof(universidadAliadaId));
        }

        if (string.IsNullOrWhiteSpace(codigoCarrera))
        {
            throw new ArgumentException("El código de la carrera es obligatorio.", nameof(codigoCarrera));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la carrera es obligatorio.", nameof(nombre));
        }

        if (duracionSemestres <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duracionSemestres), "La duración en semestres debe ser mayor a cero.");
        }

        return new CarreraElegible(
            UuidV7.Nuevo(fechaCreacion),
            universidadAliadaId,
            codigoCarrera.Trim().ToUpperInvariant(),
            nombre.Trim(),
            duracionSemestres,
            activa: true,
            fechaCreacion,
            usuarioCreacionId);
    }

    internal CostoMatriculaReferencia PublicarNuevoCosto(
        string periodoAcademicoId,
        decimal montoReferenciaMatricula,
        string moneda,
        Guid usuarioModificacionId,
        DateTimeOffset fechaVigenciaDesde)
    {
        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico es obligatorio.", nameof(periodoAcademicoId));
        }

        var periodoNormalizado = periodoAcademicoId.Trim();
        var fechaEfectiva = fechaVigenciaDesde.ToUniversalTime();

        var costosPeriodo = _costosMatricula
            .Where(costo => string.Equals(costo.PeriodoAcademicoId, periodoNormalizado, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var versionAnteriorVigente = costosPeriodo
            .SingleOrDefault(costo => costo.Estado == EstadoVersionCosto.Vigente);

        versionAnteriorVigente?.MarcarComoHistorico(fechaEfectiva, usuarioModificacionId);

        var siguienteVersion = costosPeriodo.Count == 0
            ? 1
            : costosPeriodo.Max(costo => costo.NumeroVersion) + 1;

        var nuevoCosto = CostoMatriculaReferencia.CrearVigente(
            Id,
            periodoNormalizado,
            siguienteVersion,
            montoReferenciaMatricula,
            moneda,
            fechaEfectiva,
            usuarioModificacionId);

        _costosMatricula.Add(nuevoCosto);
        ActualizarAuditoria(usuarioModificacionId, fechaEfectiva);

        return nuevoCosto;
    }

    internal void Desactivar(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)
    {
        if (!Activa)
        {
            return;
        }

        Activa = false;
        ActualizarAuditoria(usuarioModificacionId, fechaModificacion);
    }
}
