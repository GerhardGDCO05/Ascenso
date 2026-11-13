using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class BloqueosPesimistasTests
{
    private static readonly TimeSpan _esperaMaxima = TimeSpan.FromSeconds(15);

    private readonly PostgreSqlFixture _fixture;

    public BloqueosPesimistasTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_SerializarDosTransacciones_Cuando_SeBloqueaLaFilaDeLaBeca()
    {
        var becaId = await CrearBecaAsync();

        await using var contextoPrimero = _fixture.CrearContexto();
        await using var contextoSegundo = _fixture.CrearContexto();
        await using var transaccionPrimera = await contextoPrimero.Database.BeginTransactionAsync();
        await using var transaccionSegunda = await contextoSegundo.Database.BeginTransactionAsync();

        var repositorioPrimero = new BecaRepository(contextoPrimero);
        var repositorioSegundo = new BecaRepository(contextoSegundo);

        var bloqueada = await repositorioPrimero.ObtenerBecaParaActualizarAsync(becaId);
        Assert.NotNull(bloqueada);

        var segundaLectura = repositorioSegundo.ObtenerBecaParaActualizarAsync(becaId);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        Assert.False(segundaLectura.IsCompleted);

        await transaccionPrimera.CommitAsync();

        var reconstruida = await segundaLectura.WaitAsync(_esperaMaxima);
        Assert.NotNull(reconstruida);
        await transaccionSegunda.CommitAsync();
    }

    [Fact]
    public async Task Debe_SerializarDosTransacciones_Cuando_SeBloqueaElEstudiante()
    {
        var estudianteId = Guid.CreateVersion7();

        await using var contextoPrimero = _fixture.CrearContexto();
        await using var contextoSegundo = _fixture.CrearContexto();
        await using var transaccionPrimera = await contextoPrimero.Database.BeginTransactionAsync();
        await using var transaccionSegunda = await contextoSegundo.Database.BeginTransactionAsync();

        var repositorioPrimero = new BecaRepository(contextoPrimero);
        var repositorioSegundo = new BecaRepository(contextoSegundo);

        await repositorioPrimero.AdquirirBloqueoEstudianteAsync(estudianteId);

        var segundoBloqueo = repositorioSegundo.AdquirirBloqueoEstudianteAsync(estudianteId);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        Assert.False(segundoBloqueo.IsCompleted);

        await transaccionPrimera.CommitAsync();

        await segundoBloqueo.WaitAsync(_esperaMaxima);
        await transaccionSegunda.CommitAsync();
    }

    [Fact]
    public async Task Debe_RechazarLosBloqueos_Cuando_NoHayTransaccionActiva()
    {
        await using var contexto = _fixture.CrearContexto();
        var repositorio = new BecaRepository(contexto);

        var excepcionBeca = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repositorio.ObtenerBecaParaActualizarAsync(Guid.CreateVersion7()));
        Assert.Contains(
            "transacción activa",
            excepcionBeca.Message,
            StringComparison.OrdinalIgnoreCase);

        var excepcionEstudiante = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repositorio.AdquirirBloqueoEstudianteAsync(Guid.CreateVersion7()));
        Assert.Contains(
            "transacción activa",
            excepcionEstudiante.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Debe_RetornarNull_Cuando_LaBecaNoExisteDentroDeLaTransaccion()
    {
        await using var contexto = _fixture.CrearContexto();
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var repositorio = new BecaRepository(contexto);

        var resultado = await repositorio.ObtenerBecaParaActualizarAsync(Guid.CreateVersion7());

        Assert.Null(resultado);
        await transaccion.CommitAsync();
    }

    private async Task<Guid> CrearBecaAsync()
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = Beca.CrearOtorgada(
            DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());

        contexto.Becas.Add(beca);
        await contexto.SaveChangesAsync();

        return beca.Id;
    }
}
