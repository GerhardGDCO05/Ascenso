using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Universidades.Entidades;

/// <summary>
/// Versión histórica de costo de matrícula de referencia para una carrera y período académico.
/// </summary>
public sealed class CostoMatriculaReferencia : EntidadAuditable
{
    private CostoMatriculaReferencia()
    {
    }

    private CostoMatriculaReferencia(
        Guid id,
        Guid carreraElegibleId,
        string periodoAcademicoId,
        int numeroVersion,
        decimal montoReferenciaMatricula,
        string moneda,
        DateTimeOffset fechaVigenciaDesde,
        DateTimeOffset? fechaVigenciaHasta,
        EstadoVersionCosto estado,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        CarreraElegibleId = carreraElegibleId;
        PeriodoAcademicoId = periodoAcademicoId;
        NumeroVersion = numeroVersion;
        MontoReferenciaMatricula = montoReferenciaMatricula;
        Moneda = moneda;
        FechaVigenciaDesde = fechaVigenciaDesde;
        FechaVigenciaHasta = fechaVigenciaHasta;
        Estado = estado;
    }

    public Guid CarreraElegibleId { get; private set; }

    public string PeriodoAcademicoId { get; private set; } = string.Empty;

    public int NumeroVersion { get; private set; }

    public decimal MontoReferenciaMatricula { get; private set; }

    public string Moneda { get; private set; } = "USD";

    public DateTimeOffset FechaVigenciaDesde { get; private set; }

    public DateTimeOffset? FechaVigenciaHasta { get; private set; }

    public EstadoVersionCosto Estado { get; private set; }

    internal static CostoMatriculaReferencia CrearVigente(
        Guid carreraElegibleId,
        string periodoAcademicoId,
        int numeroVersion,
        decimal montoReferenciaMatricula,
        string moneda,
        DateTimeOffset fechaVigenciaDesde,
        Guid usuarioCreacionId)
    {
        if (carreraElegibleId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la carrera elegible es obligatorio.", nameof(carreraElegibleId));
        }

        if (string.IsNullOrWhiteSpace(periodoAcademicoId))
        {
            throw new ArgumentException("El período académico es obligatorio.", nameof(periodoAcademicoId));
        }

        if (numeroVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroVersion), "El número de versión debe ser mayor a cero.");
        }

        if (montoReferenciaMatricula <= 0 || decimal.Round(montoReferenciaMatricula, 2) != montoReferenciaMatricula)
        {
            throw new MontoMatriculaInvalidoException(montoReferenciaMatricula);
        }

        if (!string.Equals(moneda?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La moneda admitida para el costo de matrícula de referencia es USD.", nameof(moneda));
        }

        var fechaEfectiva = fechaVigenciaDesde.ToUniversalTime();

        return new CostoMatriculaReferencia(
            UuidV7.Nuevo(fechaEfectiva),
            carreraElegibleId,
            periodoAcademicoId.Trim(),
            numeroVersion,
            montoReferenciaMatricula,
            "USD",
            fechaEfectiva,
            fechaVigenciaHasta: null,
            EstadoVersionCosto.Vigente,
            fechaEfectiva,
            usuarioCreacionId);
    }

    internal void MarcarComoHistorico(DateTimeOffset fechaVigenciaHasta, Guid usuarioModificacionId)
    {
        if (Estado == EstadoVersionCosto.Historico)
        {
            return;
        }

        Estado = EstadoVersionCosto.Historico;
        FechaVigenciaHasta = fechaVigenciaHasta.ToUniversalTime();
        ActualizarAuditoria(usuarioModificacionId, fechaVigenciaHasta);
    }
}
