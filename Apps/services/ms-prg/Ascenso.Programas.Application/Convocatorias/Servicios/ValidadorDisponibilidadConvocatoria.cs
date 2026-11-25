using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Application.Convocatorias.Servicios;

/// <summary>
/// Concentra las validaciones de disponibilidad que comparten la creación, la edición
/// y la publicación de una convocatoria.
/// </summary>
public sealed class ValidadorDisponibilidadConvocatoria
{
    private const string EstadoPeriodoVigente = "VIGENTE";

    private readonly ICatalogoConvocatoriaReader _catalogoReader;
    private readonly IPeriodoAcademicoClient _periodoAcademicoClient;
    private readonly IRelojConvocatorias _reloj;

    public ValidadorDisponibilidadConvocatoria(
        ICatalogoConvocatoriaReader catalogoReader,
        IPeriodoAcademicoClient periodoAcademicoClient,
        IRelojConvocatorias reloj)
    {
        _catalogoReader = catalogoReader ?? throw new ArgumentNullException(nameof(catalogoReader));
        _periodoAcademicoClient = periodoAcademicoClient ?? throw new ArgumentNullException(nameof(periodoAcademicoClient));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task ValidarAsync(
        Guid programaBecaId,
        string periodoAcademicoId,
        int cuposAsignados,
        CriteriosElegibilidadCiclo criterios,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criterios);

        await ValidarPeriodoAsync(periodoAcademicoId, cancellationToken);
        await ValidarProgramaAsync(programaBecaId, cuposAsignados, criterios, cancellationToken);
    }

    public async Task ValidarPeriodoAsync(
        string periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        var periodo = await _periodoAcademicoClient.ObtenerPorIdAsync(periodoAcademicoId, cancellationToken);
        if (periodo is null)
        {
            throw new PeriodoAcademicoNoVigenteException(periodoAcademicoId, "no existe en el calendario institucional.");
        }

        if (!string.Equals(periodo.Estado, EstadoPeriodoVigente, StringComparison.OrdinalIgnoreCase))
        {
            throw new PeriodoAcademicoNoVigenteException(periodoAcademicoId, $"su estado es {periodo.Estado}.");
        }
    }

    public async Task ValidarProgramaAsync(
        Guid programaBecaId,
        int cuposAsignados,
        CriteriosElegibilidadCiclo criterios,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criterios);

        var fechaReferencia = CalendarioConvocatoria.FechaCaracas(_reloj.Ahora);
        var programa = await _catalogoReader.ObtenerProgramaAsync(
            programaBecaId,
            fechaReferencia,
            cancellationToken);

        if (programa is null)
        {
            throw new ProgramaNoHabilitadoException(programaBecaId, "el programa no existe.");
        }

        if (programa.Estado is not (EstadoProgramaBeca.Configurado or EstadoProgramaBeca.Activo))
        {
            throw new ProgramaNoHabilitadoException(
                programaBecaId,
                $"el estado actual es {programa.Estado} y se requiere CONFIGURADO o ACTIVO.");
        }

        if (programa.PresupuestoTotalAsignado <= 0.00m)
        {
            throw new ProgramaNoHabilitadoException(
                programaBecaId,
                "el programa no tiene presupuesto asignado.");
        }

        if (programa.UniversidadesConConvenioVigente.Count == 0)
        {
            throw new ProgramaNoHabilitadoException(
                programaBecaId,
                "el programa no tiene convenios vigentes con universidades aliadas.");
        }

        if (cuposAsignados > programa.CuposDisponibles)
        {
            throw new CuposInsuficientesException(programaBecaId, cuposAsignados, programa.CuposDisponibles);
        }

        var universidadesValidas = programa.UniversidadesConConvenioVigente.ToHashSet();
        var universidadesInvalidas = criterios.UniversidadesPermitidasIds
            .Where(id => !universidadesValidas.Contains(id))
            .Distinct()
            .ToArray();

        if (universidadesInvalidas.Length > 0)
        {
            throw new CriterioUniversidadInvalidaException(universidadesInvalidas);
        }

        var carrerasValidas = programa.CarrerasElegiblesConConvenioVigente.ToHashSet();
        var carrerasInvalidas = criterios.CarrerasPermitidasIds
            .Where(id => !carrerasValidas.Contains(id))
            .Distinct()
            .ToArray();

        if (carrerasInvalidas.Length > 0)
        {
            throw new CriterioCarreraInvalidaException(carrerasInvalidas);
        }
    }
}
