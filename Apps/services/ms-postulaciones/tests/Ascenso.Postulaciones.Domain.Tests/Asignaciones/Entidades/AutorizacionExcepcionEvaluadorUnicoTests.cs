using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Asignaciones.Entidades;

public sealed class AutorizacionExcepcionEvaluadorUnicoTests
{
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000301");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000302");
    private static readonly Guid _correlacionId = Guid.Parse("01967000-0000-7000-8000-000000000303");
    private static readonly DateTimeOffset _fechaConOffset =
        new(2026, 11, 24, 15, 30, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_CrearEntidadInmutableConJustificacionRecortadaYFechaUtc_Cuando_LosDatosSonValidos()
    {
        var excepcion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "   Único evaluador disponible por licencia médica del comité.   ",
            _fechaConOffset,
            _coordinadorId,
            _correlacionId);

        Assert.IsAssignableFrom<EntidadInmutable>(excepcion);
        Assert.NotEqual(Guid.Empty, excepcion.Id);
        Assert.Equal(_postulacionId, excepcion.PostulacionId);
        Assert.Equal("Único evaluador disponible por licencia médica del comité.", excepcion.Justificacion);
        Assert.Equal(_coordinadorId, excepcion.UsuarioAutorId);
        Assert.Equal(_correlacionId, excepcion.CorrelacionId);
        Assert.Equal(TimeSpan.Zero, excepcion.FechaCreacion.Offset);
        Assert.Equal(new DateTimeOffset(2026, 11, 24, 19, 30, 0, TimeSpan.Zero), excepcion.FechaCreacion);
    }

    [Fact]
    public void Debe_AceptarLimitesDeLongitudValidosDeJustificacion_Cuando_TieneDiezOMilCaracteres()
    {
        var justificacionMinima = new string('a', 10);
        var justificacionMaxima = new string('b', 1000);

        var minima = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            $"  {justificacionMinima}  ",
            _fechaConOffset,
            _coordinadorId,
            _correlacionId);

        var maxima = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            $"  {justificacionMaxima}  ",
            _fechaConOffset,
            _coordinadorId,
            _correlacionId);

        Assert.Equal(justificacionMinima, minima.Justificacion);
        Assert.Equal(10, minima.Justificacion.Length);
        Assert.Equal(justificacionMaxima, maxima.Justificacion);
        Assert.Equal(1000, maxima.Justificacion.Length);
    }

    [Fact]
    public void Debe_RechazarConAsignacionInvalida_Cuando_PostulacionIdEsVacio()
    {
        var error = Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            AutorizacionExcepcionEvaluadorUnico.Crear(
                Guid.Empty,
                "Justificación suficiente para la excepción.",
                _fechaConOffset,
                _coordinadorId,
                _correlacionId));

        Assert.Equal(AsignacionEvaluacionInvalidaException.CodigoError, error.Codigo);
        Assert.Equal(422, error.EstadoHttp);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("          ")]
    [InlineData("123456789")]
    [InlineData("   123456789   ")]
    public void Debe_RechazarConJustificacionRequerida_Cuando_JustificacionEsNulaVaciaEspaciosOMenorDeDiez(string? justificacion)
    {
        var error = Assert.Throws<JustificacionExcepcionInvalidaException>(() =>
            AutorizacionExcepcionEvaluadorUnico.Crear(
                _postulacionId,
                justificacion!,
                _fechaConOffset,
                _coordinadorId,
                _correlacionId));

        Assert.Equal(JustificacionExcepcionInvalidaException.CodigoError, error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarConJustificacionRequerida_Cuando_JustificacionSuperaMilCaracteres()
    {
        var justificacionLarga = new string('x', 1001);

        var error = Assert.Throws<JustificacionExcepcionInvalidaException>(() =>
            AutorizacionExcepcionEvaluadorUnico.Crear(
                _postulacionId,
                justificacionLarga,
                _fechaConOffset,
                _coordinadorId,
                _correlacionId));

        Assert.Equal(JustificacionExcepcionInvalidaException.CodigoError, error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarConArgumentException_Cuando_ActorCorrelacionOFechaSonInvalidos()
    {
        Assert.Throws<ArgumentException>(() =>
            AutorizacionExcepcionEvaluadorUnico.Crear(
                _postulacionId,
                "Justificación válida de prueba.",
                _fechaConOffset,
                Guid.Empty,
                _correlacionId));

        Assert.Throws<ArgumentException>(() =>
            AutorizacionExcepcionEvaluadorUnico.Crear(
                _postulacionId,
                "Justificación válida de prueba.",
                _fechaConOffset,
                _coordinadorId,
                Guid.Empty));

        Assert.Throws<ArgumentException>(() =>
            AutorizacionExcepcionEvaluadorUnico.Crear(
                _postulacionId,
                "Justificación válida de prueba.",
                default,
                _coordinadorId,
                _correlacionId));
    }
}
