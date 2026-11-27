using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.ListaEspera.Agregados;

namespace Ascenso.Postulaciones.Domain.Tests.ListaEspera.Agregados;

public sealed class EntradaListaEsperaTests
{
    private static readonly DateTimeOffset _fechaIngreso = new(2026, 11, 27, 10, 0, 0, TimeSpan.FromHours(-4));
    private static readonly DateTimeOffset _fechaElegibilidad = new(2026, 11, 28, 12, 30, 0, TimeSpan.FromHours(-4));
    private static readonly DateTimeOffset _fechaPromocion = new(2026, 11, 29, 15, 0, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_CrearEntradaEnEsperaConUuidV7YFechasUtc_Cuando_LosDatosSonValidos()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaBecaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var entrada = EntradaListaEspera.Crear(
            convocatoriaId,
            programaBecaId,
            postulacionId,
            posicionOrden: 1,
            montoEstimadoBecaSnapshot: 1500.50m,
            _fechaIngreso,
            actorId);

        Assert.NotEqual(Guid.Empty, entrada.Id);
        Assert.Equal(7, entrada.Id.Version);
        Assert.Equal(convocatoriaId, entrada.ConvocatoriaId);
        Assert.Equal(programaBecaId, entrada.ProgramaBecaId);
        Assert.Equal(postulacionId, entrada.PostulacionId);
        Assert.Equal(1, entrada.PosicionOrden);
        Assert.Equal(1500.50m, entrada.MontoEstimadoBecaSnapshot);
        Assert.Equal(EstadoListaEspera.EnEspera, entrada.Estado);
        Assert.Equal(_fechaIngreso.ToUniversalTime(), entrada.FechaIngreso);
        Assert.Equal(TimeSpan.Zero, entrada.FechaIngreso.Offset);
        Assert.Null(entrada.FechaElegibilidad);
        Assert.Null(entrada.FechaPromocion);
        Assert.Null(entrada.PromovidoPorUsuarioId);
        Assert.Null(entrada.MotivoPromocion);
        Assert.False(entrada.EstaPromovida);
        Assert.True(entrada.EstaPendiente);
        Assert.Equal(1, entrada.Version);
        Assert.Equal(actorId, entrada.UsuarioCreacionId);
        Assert.Equal(actorId, entrada.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_RechazarCreacionConListaEsperaInvalida_Cuando_IdsPosicionOMontoSonInvalidos()
    {
        var convocatoriaId = Guid.NewGuid();
        var programaBecaId = Guid.NewGuid();
        var postulacionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(Guid.Empty, programaBecaId, postulacionId, 1, 1000.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, Guid.Empty, postulacionId, 1, 1000.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, Guid.Empty, 1, 1000.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, 0, 1000.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, -2, 1000.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, 1, 0.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, 1, -100.00m, _fechaIngreso, actorId));

        Assert.Throws<ListaEsperaInvalidaException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, 1, 1000.123m, _fechaIngreso, actorId));

        Assert.Throws<ArgumentException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, 1, 1000.00m, default, actorId));

        Assert.Throws<ArgumentException>(() =>
            EntradaListaEspera.Crear(convocatoriaId, programaBecaId, postulacionId, 1, 1000.00m, _fechaIngreso, Guid.Empty));
    }

    [Fact]
    public void Debe_TransicionarAEstadoElegiblePorVacanteEIncrementarVersion_Cuando_EstaEnEspera()
    {
        var entrada = CrearEntradaBase();
        var actorVacanteId = Guid.NewGuid();

        entrada.MarcarElegiblePorVacante(_fechaElegibilidad, actorVacanteId);

        Assert.Equal(EstadoListaEspera.ElegiblePorVacante, entrada.Estado);
        Assert.Equal(_fechaElegibilidad.ToUniversalTime(), entrada.FechaElegibilidad);
        Assert.Equal(TimeSpan.Zero, entrada.FechaElegibilidad!.Value.Offset);
        Assert.Null(entrada.FechaPromocion);
        Assert.Null(entrada.PromovidoPorUsuarioId);
        Assert.Null(entrada.MotivoPromocion);
        Assert.False(entrada.EstaPromovida);
        Assert.True(entrada.EstaPendiente);
        Assert.Equal(2, entrada.Version);
        Assert.Equal(actorVacanteId, entrada.UsuarioModificacionId);
        Assert.Equal(_fechaElegibilidad.ToUniversalTime(), entrada.FechaUltimaModificacion);
    }

    [Fact]
    public void Debe_TransicionarDeElegiblePorVacanteAPromovidaPreservandoFechaElegibilidad_Cuando_CoordinadorPromueve()
    {
        var entrada = CrearEntradaBase();
        var coordinadorId = Guid.NewGuid();
        entrada.MarcarElegiblePorVacante(_fechaElegibilidad, Guid.NewGuid());

        entrada.Promover(_fechaPromocion, coordinadorId, "  Vacante confirmada tras rechazo de oferta.  ");

        Assert.Equal(EstadoListaEspera.Promovida, entrada.Estado);
        Assert.Equal(_fechaElegibilidad.ToUniversalTime(), entrada.FechaElegibilidad);
        Assert.Equal(_fechaPromocion.ToUniversalTime(), entrada.FechaPromocion);
        Assert.Equal(TimeSpan.Zero, entrada.FechaPromocion!.Value.Offset);
        Assert.Equal(coordinadorId, entrada.PromovidoPorUsuarioId);
        Assert.Equal("Vacante confirmada tras rechazo de oferta.", entrada.MotivoPromocion);
        Assert.True(entrada.EstaPromovida);
        Assert.False(entrada.EstaPendiente);
        Assert.Equal(3, entrada.Version);
        Assert.Equal(coordinadorId, entrada.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_TransicionarDirectamenteDeEnEsperaAPromovidaConMotivoNulo_Cuando_MotivoEsVacioOEspacios()
    {
        var entrada = CrearEntradaBase();
        var coordinadorId = Guid.NewGuid();

        entrada.Promover(_fechaPromocion, coordinadorId, "   ");

        Assert.Equal(EstadoListaEspera.Promovida, entrada.Estado);
        Assert.Null(entrada.FechaElegibilidad);
        Assert.Equal(_fechaPromocion.ToUniversalTime(), entrada.FechaPromocion);
        Assert.Equal(coordinadorId, entrada.PromovidoPorUsuarioId);
        Assert.Null(entrada.MotivoPromocion);
        Assert.True(entrada.EstaPromovida);
        Assert.False(entrada.EstaPendiente);
        Assert.Equal(2, entrada.Version);
    }

    [Fact]
    public void Debe_RechazarTransicionesInvalidasYMotivoLargo_Cuando_NoCumplenInvariantes()
    {
        var entradaElegible = CrearEntradaBase();
        entradaElegible.MarcarElegiblePorVacante(_fechaElegibilidad, Guid.NewGuid());

        var exYaElegible = Assert.Throws<EntradaListaEsperaYaElegibleException>(() =>
            entradaElegible.MarcarElegiblePorVacante(_fechaPromocion, Guid.NewGuid()));
        Assert.Equal(409, exYaElegible.EstadoHttp);
        Assert.Equal("ENTRADA_LISTA_ESPERA_YA_ELEGIBLE", exYaElegible.Codigo);

        var motivoLargo = new string('A', EntradaListaEspera.LongitudMaximaMotivoPromocion + 1);
        var exMotivo = Assert.Throws<MotivoPromocionInvalidoException>(() =>
            entradaElegible.Promover(_fechaPromocion, Guid.NewGuid(), motivoLargo));
        Assert.Equal(422, exMotivo.EstadoHttp);
        Assert.Equal("MOTIVO_PROMOCION_INVALIDO", exMotivo.Codigo);
        Assert.Equal(EstadoListaEspera.ElegiblePorVacante, entradaElegible.Estado);

        var motivoExacto = new string('B', EntradaListaEspera.LongitudMaximaMotivoPromocion);
        entradaElegible.Promover(_fechaPromocion, Guid.NewGuid(), $"  {motivoExacto}  ");
        Assert.Equal(motivoExacto, entradaElegible.MotivoPromocion);

        var exYaPromovidaEnElegible = Assert.Throws<EntradaListaEsperaYaPromovidaException>(() =>
            entradaElegible.MarcarElegiblePorVacante(_fechaPromocion.AddHours(1), Guid.NewGuid()));
        Assert.Equal(409, exYaPromovidaEnElegible.EstadoHttp);
        Assert.Equal("ENTRADA_LISTA_ESPERA_YA_PROMOVIDA", exYaPromovidaEnElegible.Codigo);

        var exYaPromovidaEnPromover = Assert.Throws<EntradaListaEsperaYaPromovidaException>(() =>
            entradaElegible.Promover(_fechaPromocion.AddHours(1), Guid.NewGuid(), "Intento duplicado"));
        Assert.Equal(409, exYaPromovidaEnPromover.EstadoHttp);
        Assert.Equal("ENTRADA_LISTA_ESPERA_YA_PROMOVIDA", exYaPromovidaEnPromover.Codigo);
    }

    [Fact]
    public void Debe_RechazarArgumentosTemporalesYUsuarioVacios_Cuando_SeEjecutanTransiciones()
    {
        var entrada = CrearEntradaBase();

        Assert.Throws<ArgumentException>(() => entrada.MarcarElegiblePorVacante(default, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => entrada.MarcarElegiblePorVacante(_fechaElegibilidad, Guid.Empty));
        Assert.Throws<ArgumentException>(() => entrada.Promover(default, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => entrada.Promover(_fechaPromocion, Guid.Empty));
    }

    private static EntradaListaEspera CrearEntradaBase()
        => EntradaListaEspera.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            posicionOrden: 1,
            montoEstimadoBecaSnapshot: 1200.00m,
            _fechaIngreso,
            Guid.NewGuid());
}
