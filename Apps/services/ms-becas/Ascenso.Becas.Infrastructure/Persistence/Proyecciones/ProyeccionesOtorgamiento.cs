using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Proyecciones;

/// <summary>
/// Lee las proyecciones locales de configuración que alimentan la emisión de
/// ofertas. La emisión se rechaza si falta cualquiera de las tres piezas.
/// </summary>
public sealed class ProyeccionesOtorgamiento : IProyeccionesOtorgamiento
{
    private readonly BecasDbContext _contexto;

    public ProyeccionesOtorgamiento(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<ProyeccionOtorgamiento?> ObtenerAsync(
        Guid programaBecaId,
        Guid carreraId,
        Guid periodoAcademicoId,
        Guid universidadAliadaId,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        if (programaBecaId == Guid.Empty
            || carreraId == Guid.Empty
            || periodoAcademicoId == Guid.Empty
            || universidadAliadaId == Guid.Empty)
        {
            return null;
        }

        var programa = await _contexto.ProgramasBecaRef
            .AsNoTracking()
            .SingleOrDefaultAsync(
                referencia => referencia.ProgramaBecaId == programaBecaId,
                cancellationToken);

        if (programa is null)
        {
            return null;
        }

        var costo = await _contexto.CostosMatriculaRef
            .AsNoTracking()
            .Where(referencia => referencia.CarreraId == carreraId
                && referencia.PeriodoAcademicoId == periodoAcademicoId
                && referencia.FechaVigenciaDesde <= fecha
                && (referencia.FechaVigenciaHasta == null
                    || referencia.FechaVigenciaHasta >= fecha))
            .OrderByDescending(referencia => referencia.NumeroVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (costo is null)
        {
            return null;
        }

        var convenio = await _contexto.ConveniosRef
            .AsNoTracking()
            .SingleOrDefaultAsync(
                referencia => referencia.UniversidadAliadaId == universidadAliadaId,
                cancellationToken);

        if (convenio is null)
        {
            return null;
        }

        return new ProyeccionOtorgamiento(
            new ProgramaOtorgamientoProyeccion(
                programa.ProgramaBecaId,
                programa.Codigo,
                programa.Nombre,
                programa.PoliticaCobertura,
                programa.CondicionesPermanencia,
                programa.PermiteAcumulacion,
                programa.RequiereCompromisoFirmado,
                programa.DuracionPeriodos,
                programa.Estado),
            new CostoMatriculaProyeccion(
                costo.CostoMatriculaReferenciaId,
                costo.MontoReferenciaMatricula,
                costo.Moneda,
                costo.Estado,
                costo.FechaVigenciaDesde,
                costo.FechaVigenciaHasta,
                costo.NumeroVersion),
            new ConvenioProyeccion(
                convenio.ConvenioId,
                convenio.Estado,
                convenio.FechaFin));
    }
}
