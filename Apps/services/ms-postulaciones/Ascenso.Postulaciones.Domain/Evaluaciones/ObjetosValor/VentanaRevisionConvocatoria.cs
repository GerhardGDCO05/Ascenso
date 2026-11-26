using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que encapsula la ventana formal de revisión de una convocatoria.
/// </summary>
public sealed record class VentanaRevisionConvocatoria : ValueObject
{
    public VentanaRevisionConvocatoria(
        Guid convocatoriaId,
        EstadoConvocatoriaExterna estadoConvocatoria,
        DateTimeOffset fechaInicioEvaluacion,
        DateTimeOffset fechaCierreEvaluacion)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (!Enum.IsDefined(estadoConvocatoria))
        {
            throw new ArgumentException(
                "El estado externo de la convocatoria no es válido.",
                nameof(estadoConvocatoria));
        }

        if (fechaInicioEvaluacion == default)
        {
            throw new ArgumentException(
                "La fecha de inicio de evaluación es obligatoria.",
                nameof(fechaInicioEvaluacion));
        }

        if (fechaCierreEvaluacion == default)
        {
            throw new ArgumentException(
                "La fecha de cierre de evaluación es obligatoria.",
                nameof(fechaCierreEvaluacion));
        }

        var fechaInicioUtc = fechaInicioEvaluacion.ToUniversalTime();
        var fechaCierreUtc = fechaCierreEvaluacion.ToUniversalTime();

        if (fechaCierreUtc <= fechaInicioUtc)
        {
            throw new ArgumentException(
                "La fecha de cierre de evaluación debe ser estrictamente posterior a la fecha de inicio.",
                nameof(fechaCierreEvaluacion));
        }

        ConvocatoriaId = convocatoriaId;
        EstadoConvocatoria = estadoConvocatoria;
        FechaInicioEvaluacion = fechaInicioUtc;
        FechaCierreEvaluacion = fechaCierreUtc;
    }

    public Guid ConvocatoriaId { get; }

    public EstadoConvocatoriaExterna EstadoConvocatoria { get; }

    public DateTimeOffset FechaInicioEvaluacion { get; }

    public DateTimeOffset FechaCierreEvaluacion { get; }

    public EstadoPeriodoRevision ObtenerEstadoPeriodoRevision(DateTimeOffset ahora)
    {
        if (ahora == default)
        {
            throw new ArgumentException(
                "El instante actual de evaluación es obligatorio.",
                nameof(ahora));
        }

        if (EstadoConvocatoria == EstadoConvocatoriaExterna.Cancelada)
        {
            return EstadoPeriodoRevision.Cancelado;
        }

        if (EstadoConvocatoria == EstadoConvocatoriaExterna.Borrador)
        {
            return EstadoPeriodoRevision.NoIniciado;
        }

        var ahoraUtc = ahora.ToUniversalTime();

        if (ahoraUtc < FechaInicioEvaluacion)
        {
            return EstadoPeriodoRevision.NoIniciado;
        }

        if (ahoraUtc <= FechaCierreEvaluacion)
        {
            return EstadoPeriodoRevision.Abierto;
        }

        return EstadoPeriodoRevision.Cerrado;
    }

    public void AsegurarPeriodoAbiertoParaEvaluacion(DateTimeOffset ahora)
    {
        switch (ObtenerEstadoPeriodoRevision(ahora))
        {
            case EstadoPeriodoRevision.Abierto:
                return;
            case EstadoPeriodoRevision.NoIniciado:
                throw new PeriodoRevisionNoIniciadoException();
            case EstadoPeriodoRevision.Cerrado:
                throw new PeriodoRevisionCerradoException();
            case EstadoPeriodoRevision.Cancelado:
                throw new ConvocatoriaCanceladaException();
            default:
                throw new InvalidOperationException("Estado del periodo de revisión no soportado.");
        }
    }

    public void AsegurarPeriodoCerradoParaVisibilidadCoevaluadores(DateTimeOffset ahora)
    {
        switch (ObtenerEstadoPeriodoRevision(ahora))
        {
            case EstadoPeriodoRevision.Cerrado:
                return;
            case EstadoPeriodoRevision.NoIniciado:
            case EstadoPeriodoRevision.Abierto:
                throw new PeriodoRevisionAbiertoException();
            case EstadoPeriodoRevision.Cancelado:
                throw new ConvocatoriaCanceladaException();
            default:
                throw new InvalidOperationException("Estado del periodo de revisión no soportado.");
        }
    }
}
