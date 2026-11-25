using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;

namespace Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;

/// <summary>Rendimiento informado para una beca en un periodo académico.</summary>
public sealed class ReporteAcademico : AggregateRoot
{
    private const long TamanioMaximoComprobante = 5 * 1024 * 1024;

    private ReporteAcademico()
    {
    }

    private ReporteAcademico(DatosReporteAcademico datos, ContextoOperacion contexto)
        : base(Guid.CreateVersion7(), contexto.Fecha, contexto.UsuarioAutorId)
    {
        BecaId = datos.BecaId;
        PeriodoAcademicoId = datos.PeriodoAcademicoId;
        PromedioPeriodo = datos.PromedioPeriodo;
        MateriasAprobadas = datos.MateriasAprobadas;
        MateriasReprobadas = datos.MateriasReprobadas;
        MateriasCursando = datos.MateriasCursando;
        CreditosAprobados = datos.CreditosAprobados;
        EstadoMatricula = datos.EstadoMatricula;
        UrlComprobanteNotasFirebase = datos.UrlComprobanteNotasFirebase.Trim();
        HashComprobanteSha256 = datos.HashComprobanteSha256.ToUpperInvariant();
        TamanioComprobanteBytes = datos.TamanioComprobanteBytes;
        TipoMimeComprobante = datos.TipoMimeComprobante;
        FuenteEntrega = datos.FuenteEntrega;
        UniversidadAliadaId = datos.UniversidadAliadaId;
        FechaEntrega = contexto.Fecha;
        FechaLimitePeriodo = datos.FechaLimitePeriodo;
        EntregaTardia = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime) > datos.FechaLimitePeriodo;
        Estado = EstadoReporteAcademico.Entregado;
    }

    public Guid BecaId { get; private set; }
    public Guid PeriodoAcademicoId { get; private set; }
    public decimal PromedioPeriodo { get; private set; }
    public int MateriasAprobadas { get; private set; }
    public int MateriasReprobadas { get; private set; }
    public int MateriasCursando { get; private set; }
    public int CreditosAprobados { get; private set; }
    public EstadoMatricula EstadoMatricula { get; private set; }
    public string UrlComprobanteNotasFirebase { get; private set; } = string.Empty;
    public string HashComprobanteSha256 { get; private set; } = string.Empty;
    public long TamanioComprobanteBytes { get; private set; }
    public string TipoMimeComprobante { get; private set; } = string.Empty;
    public FuenteReporte FuenteEntrega { get; private set; }
    public Guid UniversidadAliadaId { get; private set; }
    public DateTimeOffset FechaEntrega { get; private set; }
    public DateOnly FechaLimitePeriodo { get; private set; }
    public bool EntregaTardia { get; private set; }
    public EstadoReporteAcademico Estado { get; private set; }
    public Guid? RevisadoPorUsuarioId { get; private set; }
    public DateTimeOffset? FechaRevision { get; private set; }

    public static ReporteAcademico Registrar(DatosReporteAcademico datos,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(contexto);
        Validar(datos);
        return new ReporteAcademico(datos, contexto);
    }

    public void IniciarRevision(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (Estado != EstadoReporteAcademico.Entregado)
            throw new InvalidOperationException("Solo un reporte entregado puede iniciar revisión.");
        Estado = EstadoReporteAcademico.EnRevision;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public void ResolverRevision(bool aprobado, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (Estado != EstadoReporteAcademico.EnRevision)
            throw new InvalidOperationException("El reporte debe estar en revisión.");
        Estado = aprobado ? EstadoReporteAcademico.Aprobado : EstadoReporteAcademico.Rechazado;
        RevisadoPorUsuarioId = contexto.UsuarioAutorId;
        FechaRevision = contexto.Fecha;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    private static void Validar(DatosReporteAcademico datos)
    {
        if (datos.BecaId == Guid.Empty || datos.PeriodoAcademicoId == Guid.Empty
            || datos.UniversidadAliadaId == Guid.Empty || datos.FechaLimitePeriodo == default
            || !Enum.IsDefined(datos.EstadoMatricula) || !Enum.IsDefined(datos.FuenteEntrega))
            throw new ArgumentException("Los identificadores y catálogos del reporte son obligatorios.", nameof(datos));

        if (datos.PromedioPeriodo is < 0m or > 20m
            || decimal.Round(datos.PromedioPeriodo, 2) != datos.PromedioPeriodo
            || datos.MateriasAprobadas < 0 || datos.MateriasReprobadas < 0
            || datos.MateriasCursando < 0 || datos.CreditosAprobados < 0
            || (datos.CreditosAprobados > 0 && datos.MateriasAprobadas == 0)
            || (datos.EstadoMatricula != EstadoMatricula.Matriculado
                && datos.MateriasCursando > 0))
            throw new ArgumentException("El rendimiento o la matrícula del reporte son incoherentes.", nameof(datos));

        if (string.IsNullOrWhiteSpace(datos.UrlComprobanteNotasFirebase)
            || !Uri.TryCreate(datos.UrlComprobanteNotasFirebase, UriKind.Absolute,
                out var referencia)
            || referencia.Scheme != "gs" || string.IsNullOrWhiteSpace(referencia.Host)
            || referencia.AbsolutePath is "" or "/"
            || string.IsNullOrWhiteSpace(datos.HashComprobanteSha256)
            || datos.HashComprobanteSha256.Length != 64
            || !datos.HashComprobanteSha256.All(Uri.IsHexDigit)
            || datos.TamanioComprobanteBytes is <= 0 or > TamanioMaximoComprobante
            || datos.TipoMimeComprobante != "application/pdf")
            throw new ArgumentException("El comprobante PDF del reporte es inválido.", nameof(datos));
    }
}
