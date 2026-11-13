using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class RepositoriosTests
{
    private readonly PostgreSqlFixture _fixture;

    public RepositoriosTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ReconstruirElAgregadoCompleto_Cuando_SeConsultaLaBeca()
    {
        await using var contextoEscritura = _fixture.CrearContexto();
        var datosBeca = DatosPrueba.CrearDatosBeca(
            modalidad: TipoCobertura.Total,
            permiteAcumulacion: false,
            requiereCompromisoFirmado: true,
            duracionPeriodos: 4);
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datosBeca, contextoOperacion);
        var oferta = beca.EmitirOferta(DatosPrueba.CrearDatosOferta(datosBeca), contextoOperacion);
        beca.AceptarOferta(oferta.Id, contextoOperacion);

        contextoEscritura.Becas.Add(beca);
        await contextoEscritura.SaveChangesAsync();

        await using var contextoLectura = _fixture.CrearContexto();
        var repositorio = new BecaRepository(contextoLectura);

        var reconstruida = await repositorio.ObtenerPorIdAsync(beca.Id);

        Assert.NotNull(reconstruida);
        Assert.Equal(beca.Id, reconstruida.Id);
        Assert.Equal(datosBeca.CodigoBeca, reconstruida.CodigoBeca);
        Assert.Equal(EstadoBeca.Activa, reconstruida.Estado);
        Assert.Equal(datosBeca.CoberturaAplicada.Modalidad, reconstruida.CoberturaAplicada.Modalidad);
        Assert.Equal(
            datosBeca.CoberturaAplicada.PorcentajeCobertura,
            reconstruida.CoberturaAplicada.PorcentajeCobertura);
        Assert.Equal(datosBeca.CoberturaAplicada.MontoFijoPeriodo, reconstruida.CoberturaAplicada.MontoFijoPeriodo);
        Assert.Equal(datosBeca.CoberturaAplicada.Moneda, reconstruida.CoberturaAplicada.Moneda);

        Assert.NotNull(reconstruida.Oferta);
        Assert.Equal(oferta.Id, reconstruida.Oferta.Id);
        Assert.Equal(EstadoOferta.Aceptada, reconstruida.Oferta.Estado);
        Assert.Equal(
            oferta.CondicionesSnapshot.PromedioMinimoRenovacion,
            reconstruida.Oferta.CondicionesSnapshot.PromedioMinimoRenovacion);
        Assert.Equal(
            oferta.CondicionesSnapshot.MateriasMinimasAprobadas,
            reconstruida.Oferta.CondicionesSnapshot.MateriasMinimasAprobadas);
        Assert.Equal(
            oferta.CondicionesSnapshot.MaximoMateriasReprobadas,
            reconstruida.Oferta.CondicionesSnapshot.MaximoMateriasReprobadas);
        Assert.Equal(
            oferta.CondicionesSnapshot.HorasServicioPorPeriodo,
            reconstruida.Oferta.CondicionesSnapshot.HorasServicioPorPeriodo);
        Assert.Equal(
            oferta.CondicionesSnapshot.ConsecuenciaIncumplimiento,
            reconstruida.Oferta.CondicionesSnapshot.ConsecuenciaIncumplimiento);

        Assert.NotNull(reconstruida.Compromiso);
        Assert.Equal(EstadoCompromiso.PendienteCarga, reconstruida.Compromiso.Estado);
        Assert.Equal(0, reconstruida.Compromiso.NumeroIntentoCarga);

        Assert.Equal(4, reconstruida.Historial.Count);
        Assert.Contains(
            reconstruida.Historial,
            registro => registro.TipoEntidad == TipoEntidadHistorial.Beca
                && registro.EstadoBecaNuevo == EstadoBeca.Activa);
        Assert.Contains(
            reconstruida.Historial,
            registro => registro.TipoEntidad == TipoEntidadHistorial.OfertaBeca
                && registro.EstadoOfertaNuevo == EstadoOferta.Aceptada);

        Assert.True(await repositorio.ExisteAsync(beca.Id));
        Assert.True(await repositorio.ExisteActivaConCoberturaTotalAsync(datosBeca.PostulanteEstudianteId));
        Assert.True(await repositorio.ExisteActivaEnPeriodoAsync(
            datosBeca.PostulanteEstudianteId,
            datosBeca.PeriodoOtorgamientoId));
    }

    [Fact]
    public async Task Debe_ReconstruirLaOfertaConSuSnapshot_Cuando_SeConsultaPorId()
    {
        await using var contextoEscritura = _fixture.CrearContexto();
        var datosBeca = DatosPrueba.CrearDatosBeca();
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datosBeca, contextoOperacion);
        var oferta = beca.EmitirOferta(DatosPrueba.CrearDatosOferta(datosBeca), contextoOperacion);

        contextoEscritura.Becas.Add(beca);
        await contextoEscritura.SaveChangesAsync();

        await using var contextoLectura = _fixture.CrearContexto();
        var repositorio = new OfertaBecaRepository(contextoLectura);

        var reconstruida = await repositorio.ObtenerPorIdAsync(oferta.Id);

        Assert.NotNull(reconstruida);
        Assert.Equal(oferta.Id, reconstruida.Id);
        Assert.Equal(oferta.BecaId, reconstruida.BecaId);
        Assert.Equal(EstadoOferta.PendienteRespuesta, reconstruida.Estado);
        Assert.Equal(
            oferta.CondicionesSnapshot.ConsecuenciaIncumplimiento,
            reconstruida.CondicionesSnapshot.ConsecuenciaIncumplimiento);
        Assert.Equal(
            oferta.CondicionesSnapshot.HorasServicioPorPeriodo,
            reconstruida.CondicionesSnapshot.HorasServicioPorPeriodo);
        Assert.True(await repositorio.ExisteAsync(oferta.Id));
        Assert.True(await repositorio.EstaPendienteAsync(oferta.Id));
    }

    [Fact]
    public async Task Debe_ReconstruirElCompromiso_Cuando_SeConsultaPorBeca()
    {
        await using var contextoEscritura = _fixture.CrearContexto();
        var datosBeca = DatosPrueba.CrearDatosBeca(requiereCompromisoFirmado: true);
        var contextoOperacion = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datosBeca, contextoOperacion);

        contextoEscritura.Becas.Add(beca);
        await contextoEscritura.SaveChangesAsync();

        await using var contextoLectura = _fixture.CrearContexto();
        var repositorio = new CompromisoBecarioRepository(contextoLectura);

        var reconstruido = await repositorio.ObtenerPorBecaAsync(beca.Id);

        Assert.NotNull(reconstruido);
        Assert.Equal(beca.Compromiso!.Id, reconstruido.Id);
        Assert.Equal(beca.Id, reconstruido.BecaId);
        Assert.Equal(EstadoCompromiso.PendienteCarga, reconstruido.Estado);
        Assert.True(await repositorio.ExisteAsync(reconstruido.Id));
        Assert.False(await repositorio.ExisteAprobadoParaBecaAsync(beca.Id));
    }

    [Fact]
    public async Task Debe_RecuperarLaSolicitudIdempotente_Cuando_CoincidenEndpointYClave()
    {
        await using var contextoEscritura = _fixture.CrearContexto();
        var creadoEn = DateTimeOffset.UtcNow;
        var solicitud = new IdempotencyRequest
        {
            Id = Guid.CreateVersion7(),
            Endpoint = $"/api/v1/becas/{Guid.NewGuid():N}/oferta",
            Clave = Guid.NewGuid().ToString("N"),
            HashPayload = new string('d', 64),
            Estado = "COMPLETADA",
            StatusCode = 201,
            ResponseBody = """{"ok":true}""",
            RecursoId = Guid.CreateVersion7(),
            CreadoEn = creadoEn,
            ExpiraEn = creadoEn.AddDays(7)
        };

        var repositorioEscritura = new IdempotencyRepository(contextoEscritura);
        await repositorioEscritura.AgregarAsync(solicitud);
        await contextoEscritura.SaveChangesAsync();

        await using var contextoLectura = _fixture.CrearContexto();
        var repositorioLectura = new IdempotencyRepository(contextoLectura);

        var recuperada = await repositorioLectura.ObtenerAsync(solicitud.Endpoint, solicitud.Clave);

        Assert.NotNull(recuperada);
        Assert.Equal(solicitud.Id, recuperada.Id);
        Assert.Equal(solicitud.HashPayload, recuperada.HashPayload);
        Assert.Equal(solicitud.StatusCode, recuperada.StatusCode);
        Assert.Equal(solicitud.ResponseBody, recuperada.ResponseBody);
        Assert.True(await repositorioLectura.ExisteAsync(solicitud.Endpoint, solicitud.Clave));
    }

    [Fact]
    public async Task Debe_EliminarSoloLasSolicitudesExpiradas_Cuando_SeLimpiaLaIdempotencia()
    {
        await using var contexto = _fixture.CrearContexto();
        var repositorio = new IdempotencyRepository(contexto);
        var ahora = DateTimeOffset.UtcNow;
        var expirada = CrearSolicitud(ahora.AddDays(-8), ahora.AddDays(-1));
        var vigente = CrearSolicitud(ahora, ahora.AddDays(7));

        await repositorio.AgregarAsync(expirada);
        await repositorio.AgregarAsync(vigente);
        await contexto.SaveChangesAsync();

        var eliminadas = await repositorio.EliminarExpiradasAsync(ahora);

        Assert.Equal(1, eliminadas);
        Assert.False(await repositorio.ExisteAsync(expirada.Endpoint, expirada.Clave));
        Assert.True(await repositorio.ExisteAsync(vigente.Endpoint, vigente.Clave));
    }

    private static IdempotencyRequest CrearSolicitud(
        DateTimeOffset creadoEn,
        DateTimeOffset expiraEn)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Endpoint = $"/api/v1/becas/{Guid.NewGuid():N}/oferta",
            Clave = Guid.NewGuid().ToString("N"),
            HashPayload = new string('e', 64),
            Estado = "COMPLETADA",
            StatusCode = 200,
            ResponseBody = """{"ok":true}""",
            RecursoId = Guid.CreateVersion7(),
            CreadoEn = creadoEn,
            ExpiraEn = expiraEn
        };
}
