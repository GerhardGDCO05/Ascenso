using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.Entidades;
using Ascenso.Programas.Domain.Programas.ObjetosValor;

namespace Ascenso.Programas.Application.Programas;

/// <summary>
/// Mapeador de entidades del módulo de Programas de Becas hacia DTOs de respuesta.
/// </summary>
internal static class ProgramaBecaMapeador
{
    public static ProgramaBecaResumenDto AResumenDto(ProgramaBeca programa)
        => new(
            programa.Id,
            programa.Codigo,
            programa.Nombre,
            programa.PoliticaCobertura.Modalidad.ACodigo(),
            programa.CupoTotal,
            programa.CuposOcupados,
            programa.PeriodoInicialId,
            programa.PresupuestoTotalAsignado,
            programa.Moneda,
            programa.Estado.ACodigo(),
            programa.VigenciaDesde,
            programa.VigenciaHasta);

    public static ProgramaBecaDetalleDto ADetalleDto(ProgramaBeca programa)
        => new(
            programa.Id,
            programa.Codigo,
            programa.Nombre,
            programa.Descripcion,
            ADto(programa.PoliticaCobertura),
            programa.CupoTotal,
            programa.CuposOcupados,
            programa.DuracionPeriodos,
            programa.PeriodoInicialId,
            programa.VigenciaDesde,
            programa.VigenciaHasta,
            programa.PermiteAcumulacion,
            programa.RequiereCompromisoFirmado,
            programa.TerminosCompromisoEspecial,
            programa.PresupuestoTotalAsignado,
            programa.Moneda,
            programa.Estado.ACodigo(),
            programa.CriteriosElegibilidad is null ? null : ADto(programa.CriteriosElegibilidad),
            programa.CondicionesPermanencia is null ? null : ADto(programa.CondicionesPermanencia),
            programa.AsignacionesFondo
                .OrderByDescending(asignacion => asignacion.FechaAsignacion)
                .Select(ADto)
                .ToArray(),
            programa.FechaCreacion,
            programa.FechaUltimaModificacion);

    private static PoliticaCoberturaDto ADto(PoliticaCobertura politica)
        => new(
            politica.Modalidad.ACodigo(),
            politica.PorcentajeCobertura,
            politica.MontoFijoPeriodo,
            politica.Moneda);

    private static CriteriosElegibilidadProgramaDto ADto(CriteriosElegibilidadPrograma criterios)
        => new(
            criterios.PromedioMinimoIngreso,
            criterios.EdadMinima,
            criterios.EdadMaxima,
            criterios.TopeIngresoFamiliarMensual,
            criterios.UniversidadesElegiblesIds,
            criterios.CarrerasElegiblesIds);

    private static CondicionesPermanenciaDto ADto(CondicionesPermanencia condiciones)
        => new(
            condiciones.PromedioMinimoRenovacion,
            condiciones.MateriasMinimasAprobadas,
            condiciones.MaximoMateriasReprobadas,
            condiciones.HorasServicioPorPeriodo,
            condiciones.FrecuenciaReportes.ACodigo(),
            condiciones.ConsecuenciaIncumplimiento.ACodigo(),
            condiciones.MargenRiesgoPromedio,
            condiciones.DuracionMaximaPausaPeriodos,
            condiciones.PermiteRenovacionTardiaExcepcional);

    private static AsignacionFondoProgramaDto ADto(AsignacionFondoPrograma asignacion)
        => new(
            asignacion.Id,
            asignacion.DonanteId,
            asignacion.PeriodoAcademicoId,
            asignacion.MontoAsignado,
            asignacion.Estado.ACodigo(),
            asignacion.FechaAsignacion,
            asignacion.AutorizadoPorUsuarioId);
}
