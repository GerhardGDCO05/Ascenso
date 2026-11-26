using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Application.Renovacion.Servicios;
using Ascenso.Becas.Application.Tests.Otorgamiento;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Agregados;
using Ascenso.Becas.Domain.SeguimientoAcademico.Datos;
using NSubstitute;

namespace Ascenso.Becas.Application.Tests.Renovacion;

public sealed class EvaluadorElegibilidadRenovacionTests
{
    private static readonly DateTimeOffset _fecha =
        new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _inicio = new(2026, 11, 20);
    private static readonly DateOnly _fin = new(2026, 11, 30);

    [Fact]
    public async Task AceptaSoloConVentanaEvaluacionConfirmadaYPresupuesto()
    {
        var escenario = new Escenario();
        var datos = await escenario.Evaluador.EvaluarAsync(
            escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m);

        Assert.Equal(escenario.Beca.Id, datos.BecaId);
        Assert.Equal(escenario.Beca.PeriodoVigenteActualId, datos.PeriodoOrigenId);
        Assert.Equal(escenario.PeriodoSolicitadoId, datos.PeriodoSolicitadoId);
        Assert.Equal(_fin, datos.FechaLimiteVentanaRenovacion);
        Assert.True(datos.CumpleRendimientoAcademico);
        Assert.True(datos.TienePresupuestoDisponible);
        Assert.Equal(EstadoBeca.Activa, escenario.Beca.Estado);
        Assert.False(escenario.Beca.TieneRenovacionEnCurso);
        Assert.Equal(EstadoRenovacion.SolicitadaEnCurso,
            SolicitudRenovacion.Solicitar(datos, DatosAplicacion.Contexto(_fecha)).Estado);
        await escenario.Becas.DidNotReceive().AgregarAsync(
            Arg.Any<Beca>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-6)]
    [InlineData(6)]
    public async Task RechazaFueraDeVentanaSinConsultarPresupuesto(int dias)
    {
        var escenario = new Escenario(_fecha.AddDays(dias));
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            escenario.Evaluador.EvaluarAsync(
                escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m));
        await escenario.Presupuestos.DidNotReceive()
            .TieneDisponibilidadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<decimal>(), Arg.Any<CancellationToken>());
        Assert.Equal(EstadoBeca.Activa, escenario.Beca.Estado);
    }

    [Fact]
    public async Task RechazaSinEvaluacionAcademicaCumplida()
    {
        var escenario = new Escenario(promedio: 10m);
        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            escenario.Evaluador.EvaluarAsync(
                escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m));
        await escenario.Presupuestos.DidNotReceive()
            .TieneDisponibilidadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RechazaSinReporteDelPeriodoDeOrigen()
    {
        var escenario = new Escenario();
        escenario.Reportes.ObtenerPorBecaYPeriodoParaConsultaAsync(
            escenario.Beca.Id, escenario.Beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>()).Returns((ReporteAcademico?)null);

        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            escenario.Evaluador.EvaluarAsync(
                escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m));
    }

    [Fact]
    public async Task RechazaSinPresupuestoConfirmadoPorMsPrg()
    {
        var escenario = new Escenario();
        escenario.Presupuestos.TieneDisponibilidadAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<ConflictoOperacionException>(() =>
            escenario.Evaluador.EvaluarAsync(
                escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m));
        Assert.Equal(EstadoBeca.Activa, escenario.Beca.Estado);
    }

    [Fact]
    public async Task FallaCerradoSiMsPrgNoPublicaVentanaOPresupuesto()
    {
        var escenario = new Escenario();
        escenario.Ventanas.ObtenerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>()).Returns((VentanaRenovacionPrograma?)null);
        await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(() =>
            escenario.Evaluador.EvaluarAsync(
                escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m));

        escenario.Ventanas.ObtenerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new VentanaRenovacionPrograma(_inicio, _fin, true));
        escenario.Presupuestos.TieneDisponibilidadAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<CancellationToken>()).Returns((bool?)null);
        await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(() =>
            escenario.Evaluador.EvaluarAsync(
                escenario.Beca.Id, escenario.PeriodoSolicitadoId, 500m));
        Assert.Equal(EstadoBeca.Activa, escenario.Beca.Estado);
    }

    private sealed class Escenario
    {
        public Escenario(DateTimeOffset? fecha = null, decimal promedio = 16m)
        {
            Beca = DatosAplicacion.CrearBecaActiva(
                Guid.CreateVersion7(), Guid.CreateVersion7(),
                Guid.CreateVersion7(), Guid.CreateVersion7());
            PeriodoSolicitadoId = Guid.CreateVersion7();
            Becas = Substitute.For<IBecaRepository>();
            Reportes = Substitute.For<IReporteAcademicoRepository>();
            Ventanas = Substitute.For<IVentanaRenovacionPrograma>();
            Presupuestos = Substitute.For<IPresupuestoRenovacionPrograma>();
            var reloj = Substitute.For<IClock>();
            reloj.UtcNow.Returns(fecha ?? _fecha);

            Becas.ObtenerPorIdAsync(Beca.Id, Arg.Any<CancellationToken>())
                .Returns(Beca);
            Reportes.ObtenerPorBecaYPeriodoParaConsultaAsync(Beca.Id,
                Beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
                .Returns(CrearReporte(Beca, promedio));
            Ventanas.ObtenerAsync(Beca.ProgramaBecaId, PeriodoSolicitadoId,
                Arg.Any<CancellationToken>())
                .Returns(new VentanaRenovacionPrograma(_inicio, _fin, true));
            Presupuestos.TieneDisponibilidadAsync(Beca.ProgramaBecaId,
                PeriodoSolicitadoId, 500m, Arg.Any<CancellationToken>())
                .Returns(true);

            Evaluador = new EvaluadorElegibilidadRenovacion(Becas, Reportes,
                Ventanas, Presupuestos, reloj);
        }

        public Beca Beca { get; }
        public Guid PeriodoSolicitadoId { get; }
        public IBecaRepository Becas { get; }
        public IReporteAcademicoRepository Reportes { get; }
        public IVentanaRenovacionPrograma Ventanas { get; }
        public IPresupuestoRenovacionPrograma Presupuestos { get; }
        public EvaluadorElegibilidadRenovacion Evaluador { get; }

        private static ReporteAcademico CrearReporte(Beca beca, decimal promedio)
        {
            var contexto = DatosAplicacion.Contexto(_fecha);
            var reporte = ReporteAcademico.Registrar(new DatosReporteAcademico(
                beca.Id, beca.PeriodoVigenteActualId, promedio,
                4, 0, 0, 20, EstadoMatricula.Matriculado,
                "gs://becas/reportes/notas.pdf", DatosAplicacion.HashValido,
                1024, "application/pdf", FuenteReporte.Becario,
                Guid.CreateVersion7(), _fin, 80,
                "gs://becas/servicio/constancia.pdf", DatosAplicacion.HashValido),
                contexto);
            reporte.IniciarRevision(contexto);
            reporte.Evaluar(beca.Oferta!.CondicionesSnapshot, contexto);
            reporte.DecidirConsecuencia(true, "Evaluación académica confirmada",
                "Consultar decisión de renovación", contexto);
            reporte.ResolverRevision(true, contexto);
            return reporte;
        }
    }
}
