using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Application.Desembolsos;

/// <summary>Construye el calendario inicial solo con datos oficiales del periodo.</summary>
public static class PlanificadorCalendarioInicial
{
    public static async Task<CalendarioDesembolso> CrearAsync(Beca beca,
        IPlanDesembolsoPrograma planes, ContextoOperacion contexto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beca);
        ArgumentNullException.ThrowIfNull(planes);
        ArgumentNullException.ThrowIfNull(contexto);

        var plan = await planes.ObtenerAsync(beca.ProgramaBecaId,
            beca.PeriodoVigenteActualId, cancellationToken);
        return CrearDesdePlan(beca, plan, contexto);
    }

    public static CalendarioDesembolso CrearDesdePlan(Beca beca,
        PlanDesembolsoPrograma? plan, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(beca);
        ArgumentNullException.ThrowIfNull(contexto);
        if (plan is null || plan.ProgramaBecaId != beca.ProgramaBecaId
            || plan.PeriodoAcademicoId != beca.PeriodoVigenteActualId
            || plan.Cuotas is null)
            throw new ConfiguracionProgramaNoDisponibleException(
                beca.ProgramaBecaId,
                "Falta el plan oficial de desembolsos del periodo.");

        var monto = beca.Oferta?.MontoOfertaPorPeriodo
            ?? (beca.BecaAnteriorId is not null
                ? beca.MontoComprometidoPeriodoActual : 0m);
        if (monto <= 0m)
            throw new ConfiguracionProgramaNoDisponibleException(
                beca.ProgramaBecaId,
                "La beca no tiene un monto aprobado verificable para el periodo.");

        try
        {
            return CalendarioDesembolso.Programar(beca.Id,
                beca.PeriodoVigenteActualId, plan.Frecuencia,
                plan.Destinatario, monto, plan.FechaInicio,
                plan.FechaFin, plan.Cuotas, contexto);
        }
        catch (ArgumentException excepcion)
        {
            throw new ConfiguracionProgramaNoDisponibleException(
                beca.ProgramaBecaId,
                $"El plan oficial de desembolsos es inválido: {excepcion.Message}");
        }
    }
}
