using Ascenso.Postulaciones.Application.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using FluentAssertions;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Evaluaciones.Servicios;

public sealed class BloqueadorRubricaEvaluacionAdapterTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968100-0000-7000-8000-000000000201");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968100-0000-7000-8000-000000000202");
    private static readonly Guid _creadorId = Guid.Parse("01968100-0000-7000-8000-000000000203");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaBloqueo =
        new(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Debe_DelegarEnBloquearRubricaCommandHandlerConPrimeraCalificacionYPropagarToken_Cuando_SeInvoca()
    {
        using var cts = new CancellationTokenSource();
        var rubrica = CrearRubricaPublicada();

        var rubricaRepository = Substitute.For<IRubricaRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        rubricaRepository
            .ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token)
            .Returns(Task.FromResult<RubricaEvaluacion?>(rubrica));
        unitOfWork
            .GuardarCambiosAsync(cts.Token)
            .Returns(Task.FromResult(1));

        var handler = new BloquearRubricaCommandHandler(
            rubricaRepository,
            unitOfWork,
            new RelojFijo(_fechaBloqueo));
        var adapter = new BloqueadorRubricaEvaluacionAdapter(handler);

        await adapter.BloquearPorPrimeraCalificacionAsync(rubrica.Id, cts.Token);

        rubrica.Estado.Should().Be(EstadoRubrica.Bloqueada);
        rubrica.MotivoBloqueo.Should().Be(MotivoBloqueoRubrica.PrimeraCalificacion);
        rubrica.FechaBloqueo.Should().Be(_fechaBloqueo);
        await rubricaRepository.Received(1).ObtenerParaActualizarPorIdAsync(rubrica.Id, cts.Token);
        await unitOfWork.Received(1).GuardarCambiosAsync(cts.Token);
    }

    [Fact]
    public async Task Debe_PropagarCancelacion_Cuando_ElTokenEstaCancelado()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var rubricaRepository = Substitute.For<IRubricaRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        rubricaRepository
            .ObtenerParaActualizarPorIdAsync(Arg.Any<Guid>(), Arg.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .Returns<Task<RubricaEvaluacion?>>(_ => throw new OperationCanceledException(cts.Token));

        var handler = new BloquearRubricaCommandHandler(
            rubricaRepository,
            unitOfWork,
            new RelojFijo(_fechaBloqueo));
        var adapter = new BloqueadorRubricaEvaluacionAdapter(handler);

        var accion = () => adapter.BloquearPorPrimeraCalificacionAsync(Guid.NewGuid(), cts.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Debe_RechazarHandlerNulo_Cuando_SeConstruyeElAdaptador()
    {
        var accion = () => new BloqueadorRubricaEvaluacionAdapter(null!);

        accion.Should().Throw<ArgumentNullException>();
    }

    private static RubricaEvaluacion CrearRubricaPublicada()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Rúbrica Beca",
            _fechaBase,
            _creadorId);
        rubrica.AgregarCriterio("Criterio", "Desc", 100.00m, 0.00m, 20.00m, _fechaBase.AddMinutes(1), _creadorId);
        rubrica.Publicar(_fechaBase.AddMinutes(2), _creadorId);
        return rubrica;
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
