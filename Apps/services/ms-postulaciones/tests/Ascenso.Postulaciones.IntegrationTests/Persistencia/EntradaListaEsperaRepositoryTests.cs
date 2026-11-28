using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class EntradaListaEsperaRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    private static EntradaListaEspera CrearEntrada(
        Guid convocatoriaId,
        int posicionOrden = 1)
        => EntradaListaEspera.Crear(
            convocatoriaId: convocatoriaId,
            programaBecaId: Guid.CreateVersion7(),
            postulacionId: Guid.CreateVersion7(),
            posicionOrden: posicionOrden,
            montoEstimadoBecaSnapshot: 1500.00m,
            fechaIngreso: new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero),
            usuarioActorId: Guid.CreateVersion7());

    [Fact]
    public async Task Debe_ValidarArgumentosDeConstructorYMetodos_Cuando_SonNulosOVacios()
    {
        var ctorRepoNulo = () => new EntradaListaEsperaRepository(null!);
        var ctorGestorNulo = () => new GestorConcurrenciaListaEsperaPostgreSql(null!);

        ctorRepoNulo.Should().Throw<ArgumentNullException>();
        ctorGestorNulo.Should().Throw<ArgumentNullException>();

        await using var contexto = CrearContexto();
        var repositorio = new EntradaListaEsperaRepository(contexto);
        var gestor = new GestorConcurrenciaListaEsperaPostgreSql(contexto);

        var agregarNulo = () => repositorio.AgregarRangoAsync(null!);
        var obtenerIdVacio = () => repositorio.ObtenerPorIdAsync(Guid.Empty);
        var obtenerPostulacionVacio = () => repositorio.ObtenerPorPostulacionAsync(Guid.Empty);
        var listarVacio = () => repositorio.ListarPorConvocatoriaAsync(Guid.Empty);
        var listarEstadoInvalido = () => repositorio.ListarPorConvocatoriaAsync(Guid.CreateVersion7(), (EstadoListaEspera)999);
        var siguienteVacio = () => repositorio.ObtenerSiguientePendientePorConvocatoriaAsync(Guid.Empty);
        var ejecutarConvocatoriaVacia = () => gestor.EjecutarConBloqueoConvocatoriaAsync(
            Guid.Empty,
            (_, _) => Task.FromResult(1));
        var ejecutarAccionNula = () => gestor.EjecutarConBloqueoConvocatoriaAsync<int>(
            Guid.CreateVersion7(),
            null!);

        await agregarNulo.Should().ThrowAsync<ArgumentNullException>();
        await obtenerIdVacio.Should().ThrowAsync<ArgumentException>();
        await obtenerPostulacionVacio.Should().ThrowAsync<ArgumentException>();
        await listarVacio.Should().ThrowAsync<ArgumentException>();
        await listarEstadoInvalido.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await siguienteVacio.Should().ThrowAsync<ArgumentException>();
        await ejecutarConvocatoriaVacia.Should().ThrowAsync<ArgumentException>();
        await ejecutarAccionNula.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Debe_EncolarEntradasEnChangeTrackerYNoFallarConColeccionVacia_Cuando_SeInvocaAgregarRangoAsync()
    {
        await using var contexto = CrearContexto();
        var repositorio = new EntradaListaEsperaRepository(contexto);

        await repositorio.AgregarRangoAsync([]);
        contexto.ChangeTracker.Entries<EntradaListaEspera>().Should().BeEmpty();

        var convocatoriaId = Guid.CreateVersion7();
        var e1 = CrearEntrada(convocatoriaId, 1);
        var e2 = CrearEntrada(convocatoriaId, 2);

        await repositorio.AgregarRangoAsync([e1, e2]);

        var rastreadas = contexto.ChangeTracker.Entries<EntradaListaEspera>().ToList();
        rastreadas.Should().HaveCount(2);
        rastreadas.Should().OnlyContain(entry => entry.State == EntityState.Added);
    }

    [Fact]
    public void Debe_ConstruirConsultasDeLecturaAsNoTrackingOrdenadasPorPosicionOrden_Cuando_SeInspeccionaEntradaListaEsperaRepository()
    {
        using var contexto = CrearContexto();
        var repositorio = new EntradaListaEsperaRepository(contexto);
        var convocatoriaId = Guid.CreateVersion7();
        var entradaId = Guid.CreateVersion7();
        var postulacionId = Guid.CreateVersion7();

        var consultaPorId = repositorio.ConsultarPorId(entradaId);
        consultaPorId.Expression.ToString().Should().Contain("AsNoTracking");
        var sqlPorId = consultaPorId.ToQueryString();
        sqlPorId.Should().Contain("postulaciones.entradas_lista_espera");
        sqlPorId.Should().Contain("id");

        var consultaPorPostulacion = repositorio.ConsultarPorPostulacion(postulacionId);
        consultaPorPostulacion.Expression.ToString().Should().Contain("AsNoTracking");
        var sqlPorPostulacion = consultaPorPostulacion.ToQueryString();
        sqlPorPostulacion.Should().Contain("postulaciones.entradas_lista_espera");
        sqlPorPostulacion.Should().Contain("postulacion_id");

        var consultaLista = repositorio.ConsultarPorConvocatoria(convocatoriaId, EstadoListaEspera.ElegiblePorVacante);
        consultaLista.Expression.ToString().Should().Contain("AsNoTracking");
        var sqlLista = consultaLista.ToQueryString();
        sqlLista.Should().Contain("postulaciones.entradas_lista_espera");
        sqlLista.Should().Contain("convocatoria_id");
        sqlLista.Should().Contain("ELEGIBLE_POR_VACANTE");
        sqlLista.Should().Contain("ORDER BY");
        sqlLista.Should().Contain("posicion_orden");

        var consultaSiguiente = repositorio.ConsultarSiguientePendientePorConvocatoria(convocatoriaId);
        consultaSiguiente.Expression.ToString().Should().Contain("AsNoTracking");
        var sqlSiguiente = consultaSiguiente.ToQueryString();
        sqlSiguiente.Should().Contain("postulaciones.entradas_lista_espera");
        sqlSiguiente.Should().Contain("convocatoria_id");
        sqlSiguiente.Should().Contain("PROMOVIDA");
        sqlSiguiente.Should().Contain("ORDER BY");
        sqlSiguiente.Should().Contain("posicion_orden");
    }

    [Fact]
    public void Debe_ConstruirConsultaForUpdateDirectaSinSubconsultaYConTracking_Cuando_SeInspeccionaGestorConcurrenciaListaEsperaPostgreSql()
    {
        using var contexto = CrearContexto();
        var gestor = new GestorConcurrenciaListaEsperaPostgreSql(contexto);
        var convocatoriaId = Guid.CreateVersion7();

        var consultaBloqueada = gestor.ConsultarEntradasBloqueadas(convocatoriaId);
        consultaBloqueada.Expression.ToString().Should().NotContain("AsNoTracking");

        var sql = consultaBloqueada.ToQueryString();
        sql.Should().Contain("SELECT * FROM postulaciones.entradas_lista_espera WHERE convocatoria_id =");
        sql.Should().Contain("ORDER BY posicion_orden ASC FOR UPDATE");
        sql.Should().NotContain("FROM (");
    }

    [Fact]
    public void Debe_RegistrarRepositorioYGestorConcurrenciaComoScoped_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarInfraestructuraPostulaciones("Host=localhost;Database=test;Username=postgres;Password=postgres");

        var descriptorRepo = servicios.SingleOrDefault(d => d.ServiceType == typeof(IEntradaListaEsperaRepository));
        descriptorRepo.Should().NotBeNull();
        descriptorRepo!.ImplementationType.Should().Be<EntradaListaEsperaRepository>();
        descriptorRepo.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var descriptorGestor = servicios.SingleOrDefault(d => d.ServiceType == typeof(IGestorConcurrenciaListaEspera));
        descriptorGestor.Should().NotBeNull();
        descriptorGestor!.ImplementationType.Should().Be<GestorConcurrenciaListaEsperaPostgreSql>();
        descriptorGestor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
