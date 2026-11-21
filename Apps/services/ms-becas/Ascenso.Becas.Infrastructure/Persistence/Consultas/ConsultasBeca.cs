using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Consultas;

/// <summary>
/// Modelo de lectura del otorgamiento. Las consultas internas solo proyectan
/// identificadores, estado y cobertura para no exponer datos personales ni
/// bancarios.
/// </summary>
public sealed class ConsultasBeca : IConsultasBeca
{
    private readonly BecasDbContext _contexto;

    public ConsultasBeca(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public async Task<OfertaBecaDto?> ObtenerOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        var oferta = await _contexto.OfertasBeca
            .AsNoTracking()
            .SingleOrDefaultAsync(
                registro => registro.Id == ofertaId,
                cancellationToken);

        if (oferta is null)
        {
            return null;
        }

        var beca = await _contexto.Becas
            .AsNoTracking()
            .Where(registro => registro.Id == oferta.BecaId)
            .Select(registro => new
            {
                registro.PeriodoVigenteActualId,
                registro.ProgramaBecaId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (beca is null)
        {
            return null;
        }

        var nombrePrograma = await ObtenerNombreProgramaAsync(
            beca.ProgramaBecaId,
            cancellationToken);

        return MapearOferta(
            oferta,
            beca.ProgramaBecaId,
            beca.PeriodoVigenteActualId,
            nombrePrograma);
    }

    public async Task<RecursoOfertaDto?> ObtenerRecursoOfertaAsync(
        Guid ofertaId,
        CancellationToken cancellationToken = default)
        => await (
                from oferta in _contexto.OfertasBeca.AsNoTracking()
                join beca in _contexto.Becas.AsNoTracking()
                    on oferta.BecaId equals beca.Id
                where oferta.Id == ofertaId
                select new RecursoOfertaDto(
                    oferta.Id,
                    beca.Id,
                    beca.ProgramaBecaId,
                    beca.PostulanteEstudianteId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RecursoBecaDto?> ObtenerRecursoBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
        => await _contexto.Becas
            .AsNoTracking()
            .Where(beca => beca.Id == becaId)
            .Select(beca => new RecursoBecaDto(
                beca.Id,
                beca.ProgramaBecaId,
                beca.PostulanteEstudianteId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RecursoCompromisoDto?> ObtenerRecursoCompromisoAsync(
        Guid compromisoId,
        CancellationToken cancellationToken = default)
        => await (
                from compromiso in _contexto.CompromisosBecarios.AsNoTracking()
                join beca in _contexto.Becas.AsNoTracking()
                    on compromiso.BecaId equals beca.Id
                where compromiso.Id == compromisoId
                select new RecursoCompromisoDto(
                    compromiso.Id,
                    beca.Id,
                    beca.ProgramaBecaId,
                    beca.PostulanteEstudianteId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<BecaDto?> ObtenerBecaAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
    {
        var beca = await _contexto.Becas
            .AsNoTracking()
            .Include(registro => registro.Oferta)
            .Include(registro => registro.Compromiso)
            .Include(registro => registro.Historial)
            .AsSplitQuery()
            .SingleOrDefaultAsync(registro => registro.Id == becaId, cancellationToken);

        if (beca is null)
        {
            return null;
        }

        var nombrePrograma = await ObtenerNombreProgramaAsync(
            beca.ProgramaBecaId,
            cancellationToken);

        return MapearBeca(beca, nombrePrograma);
    }

    public async Task<IReadOnlyList<BecaActivaDto>> ConsultarActivasAsync(
        Guid postulanteEstudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        var filas = await _contexto.Becas
            .AsNoTracking()
            .Where(beca => beca.PostulanteEstudianteId == postulanteEstudianteId
                && beca.PeriodoVigenteActualId == periodoAcademicoId
                && beca.Estado == EstadoBeca.Activa)
            .OrderBy(beca => beca.FechaActivacion)
            .Select(beca => new
            {
                beca.Id,
                beca.ProgramaBecaId,
                beca.PeriodoVigenteActualId,
                Modalidad = beca.CoberturaAplicada.Modalidad,
                beca.CoberturaAplicada.PorcentajeCobertura,
                beca.CoberturaAplicada.MontoFijoPeriodo,
                beca.CoberturaAplicada.Moneda,
                beca.PermiteAcumulacion
            })
            .ToListAsync(cancellationToken);

        return filas
            .Select(fila => new BecaActivaDto(
                fila.Id,
                fila.ProgramaBecaId,
                fila.PeriodoVigenteActualId,
                EstadoBeca.Activa.ACodigo(),
                fila.Modalidad.ACodigo(),
                fila.PorcentajeCobertura,
                fila.MontoFijoPeriodo,
                fila.Moneda,
                fila.PermiteAcumulacion))
            .ToArray();
    }

    private Task<string?> ObtenerNombreProgramaAsync(
        Guid programaBecaId,
        CancellationToken cancellationToken)
        => _contexto.ProgramasBecaRef
            .AsNoTracking()
            .Where(programa => programa.ProgramaBecaId == programaBecaId)
            .Select(programa => programa.Nombre)
            .SingleOrDefaultAsync(cancellationToken);

    private static BecaDto MapearBeca(Beca beca, string? nombrePrograma)
        => new(
            beca.Id,
            beca.CodigoBeca,
            beca.PostulanteEstudianteId,
            beca.PostulacionOrigenId,
            beca.ProgramaBecaId,
            nombrePrograma,
            beca.ConvocatoriaOrigenId,
            beca.PeriodoVigenteActualId,
            beca.PeriodosCursados,
            beca.DuracionTotalPeriodos,
            beca.Estado.ACodigo(),
            beca.TipoCierre?.ACodigo(),
            PoliticaCoberturaDto.Desde(beca.CoberturaAplicada),
            beca.MontoComprometidoPeriodoActual,
            beca.MontoDesembolsadoPeriodoActual,
            beca.PermiteAcumulacion,
            beca.RequiereCompromisoFirmado,
            beca.CompromisoFirmadoAprobado,
            beca.HabilitadaParaDesembolso,
            beca.FechaOtorgamiento,
            beca.FechaActivacion,
            beca.FechaConclusionORevocacion,
            beca.Oferta is null
                ? null
                : MapearOferta(
                    beca.Oferta,
                    beca.ProgramaBecaId,
                    beca.PeriodoVigenteActualId,
                    nombrePrograma),
            beca.Compromiso is null ? null : MapearCompromiso(beca.Compromiso),
            beca.Historial
                .OrderBy(historial => historial.FechaCambio)
                .Select(MapearHistorial)
                .ToArray());

    private static OfertaBecaDto MapearOferta(
        OfertaBeca oferta,
        Guid programaBecaId,
        Guid periodoAcademicoId,
        string? nombrePrograma)
        => new(
            oferta.Id,
            oferta.BecaId,
            oferta.PostulacionId,
            programaBecaId,
            nombrePrograma,
            periodoAcademicoId,
            oferta.Estado.ACodigo(),
            oferta.MontoOfertaPorPeriodo,
            oferta.MontoTotalEstimadoDuracion,
            oferta.DuracionPeriodos,
            CondicionesPermanenciaDto.Desde(oferta.CondicionesSnapshot),
            oferta.UrlCartaPdfFirebase,
            oferta.FechaEmision,
            oferta.FechaVencimientoAceptacion,
            oferta.FechaRespuesta,
            oferta.MotivoRechazoOAnulacion);

    private static CompromisoBecarioDto MapearCompromiso(CompromisoBecario compromiso)
        => new(
            compromiso.Id,
            compromiso.BecaId,
            compromiso.Estado.ACodigo(),
            compromiso.NumeroIntentoCarga,
            compromiso.UrlPlantillaGeneradaFirebase,
            compromiso.UrlPdfFirmadoFirebase,
            compromiso.FechaCargaFirma,
            compromiso.ObservacionesRevision,
            compromiso.RevisadoPorCoordinadorId,
            compromiso.FechaRevision);

    private static HistorialEstadoBecaDto MapearHistorial(HistorialEstadoBeca historial)
        => HistorialEstadoBecaDto.Desde(
            historial.Id,
            historial.TipoEntidad,
            historial.EstadoBecaAnterior,
            historial.EstadoBecaNuevo,
            historial.EstadoOfertaAnterior,
            historial.EstadoOfertaNuevo,
            historial.EstadoCompromisoAnterior,
            historial.EstadoCompromisoNuevo,
            historial.FechaCambio,
            historial.UsuarioAutorId,
            historial.Motivo,
            historial.JustificacionDetallada,
            historial.EstadoTerminalIrreversible);
}
