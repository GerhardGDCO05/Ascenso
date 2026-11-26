using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;

namespace Ascenso.Becas.Domain.Tests.SeguimientoAcademico;

public sealed class ReporteAcademicoTests
{
    private static readonly DateTimeOffset _fecha =
        new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegistraRendimientoYComprobanteSinMarcarEntregaATiempoComoTardia()
    {
        var datos = CrearDatos();
        var reporte = ReporteAcademico.Registrar(datos, CrearContexto());

        Assert.Equal(datos.BecaId, reporte.BecaId);
        Assert.Equal(datos.PeriodoAcademicoId, reporte.PeriodoAcademicoId);
        Assert.Equal(17.5m, reporte.PromedioPeriodo);
        Assert.Equal(EstadoReporteAcademico.Entregado, reporte.Estado);
        Assert.False(reporte.EntregaTardia);
        Assert.Equal(new string('A', 64), reporte.HashComprobanteSha256);
        Assert.Null(reporte.FechaRevision);
    }

    [Fact]
    public void MarcaEntregaTardiaSoloDespuesDeLaFechaLimite()
    {
        var datos = CrearDatos() with { FechaLimitePeriodo = new DateOnly(2026, 11, 24) };

        var reporte = ReporteAcademico.Registrar(datos, CrearContexto());

        Assert.True(reporte.EntregaTardia);
        Assert.Equal(_fecha, reporte.FechaEntrega);
    }

    [Theory]
    [InlineData("https://archivo.example/notas.pdf", "application/pdf", 1024)]
    [InlineData("gs://becas/notas.pdf", "image/png", 1024)]
    [InlineData("gs://becas/notas.pdf", "application/pdf", 5242881)]
    [InlineData("gs://becas", "application/pdf", 1024)]
    public void RechazaComprobanteInvalido(string uri, string tipoMime, long tamano)
    {
        var datos = CrearDatos() with
        {
            UrlComprobanteNotasFirebase = uri,
            TipoMimeComprobante = tipoMime,
            TamanioComprobanteBytes = tamano
        };

        Assert.Throws<ArgumentException>(() =>
            ReporteAcademico.Registrar(datos, CrearContexto()));
    }

    [Fact]
    public void RechazaMateriasIncoherentes()
    {
        var datos = CrearDatos() with
        {
            EstadoMatricula = EstadoMatricula.Retirado,
            MateriasCursando = 2
        };

        Assert.Throws<ArgumentException>(() =>
            ReporteAcademico.Registrar(datos, CrearContexto()));
    }

    [Fact]
    public void SoloPermiteResolverUnaRevisionIniciada()
    {
        var reporte = ReporteAcademico.Registrar(CrearDatos(), CrearContexto());
        var revisor = CrearContexto();

        Assert.Throws<InvalidOperationException>(() => reporte.ResolverRevision(true, revisor));
        reporte.IniciarRevision(revisor);
        reporte.ResolverRevision(false, revisor);

        Assert.Equal(EstadoReporteAcademico.Rechazado, reporte.Estado);
        Assert.Equal(revisor.UsuarioAutorId, reporte.RevisadoPorUsuarioId);
        Assert.Equal(revisor.Fecha, reporte.FechaRevision);
    }

    private static DatosReporteAcademico CrearDatos()
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(), 17.5m,
            5, 1, 2, 20, EstadoMatricula.Matriculado,
            "gs://becas/reportes/notas.pdf", new string('a', 64),
            1024, "application/pdf", FuenteReporte.Becario,
            Guid.CreateVersion7(), new DateOnly(2026, 11, 25));

    private static ContextoOperacion CrearContexto()
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(), _fecha);
}
