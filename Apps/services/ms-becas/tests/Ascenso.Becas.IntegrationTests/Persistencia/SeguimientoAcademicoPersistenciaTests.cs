using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class SeguimientoAcademicoPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public SeguimientoAcademicoPersistenciaTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task ReconstruyeReporteConEvaluacionYCuatroCondiciones()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var datos = new DatosReporteAcademico(beca.Id,
            beca.PeriodoVigenteActualId, 17m, 5, 0, 0, 20,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1), 40,
            "gs://becas/servicio/constancia.pdf", new string('b', 64));
        var reporte = ReporteAcademico.Registrar(datos, contextoOperacion);
        reporte.IniciarRevision(contextoOperacion);
        reporte.Evaluar(beca.Oferta!.CondicionesSnapshot, contextoOperacion);
        reporte.DecidirConsecuencia(true, "Cumple las condiciones",
            "Continuar en el siguiente periodo", contextoOperacion);

        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.Add(beca);
            escritura.ReportesAcademicos.Add(reporte);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var reconstruido = await lectura.ReportesAcademicos
            .Include(valor => valor.Evaluacion!)
            .ThenInclude(valor => valor.DetalleCondiciones)
            .AsSplitQuery()
            .SingleAsync(valor => valor.Id == reporte.Id);
        Assert.Equal(40, reconstruido.HorasServicioCumplidas);
        Assert.Equal("gs://becas/servicio/constancia.pdf", reconstruido.UrlComprobanteServicio);
        Assert.NotNull(reconstruido.Evaluacion);
        Assert.True(reconstruido.Evaluacion.CumpleCondiciones);
        Assert.Equal(4, reconstruido.Evaluacion.DetalleCondiciones.Count);
        Assert.All(reconstruido.Evaluacion.DetalleCondiciones,
            detalle => Assert.True(detalle.Cumple));
        Assert.Equal(EstadoConsecuencia.Confirmada,
            reconstruido.Evaluacion.EstadoConfirmacion);
    }

    [Fact]
    public async Task RechazaPromedioFueraDeRangoEnPostgreSql()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var datos = new DatosReporteAcademico(beca.Id,
            beca.PeriodoVigenteActualId, 16m, 4, 0, 0, 16,
            EstadoMatricula.Matriculado, "gs://becas/reportes/notas.pdf",
            new string('a', 64), 1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 3, 1));
        var reporte = ReporteAcademico.Registrar(datos, DatosPrueba.CrearContextoOperacion());

        await using var contexto = _fixture.CrearContexto();
        contexto.Becas.Add(beca);
        contexto.ReportesAcademicos.Add(reporte);
        await contexto.SaveChangesAsync();

        var excepcion = await Assert.ThrowsAsync<PostgresException>(() =>
            contexto.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE becas.reporte_academico SET promedio_periodo = 21 WHERE id = {reporte.Id}"));
        Assert.Equal("ck_reporte_academico_promedio", excepcion.ConstraintName);
    }
}
