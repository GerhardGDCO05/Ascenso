using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Comites.Entidades;

public sealed class MiembroComiteTests
{
    private static readonly Guid _comiteId = Guid.Parse("01966000-0000-7000-8000-000000000301");
    private static readonly Guid _evaluadorId = Guid.Parse("01966000-0000-7000-8000-000000000302");
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000303");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TipoEvaluador.Interno, RolMiembroComite.Jefatura)]
    [InlineData(TipoEvaluador.Interno, RolMiembroComite.Evaluador)]
    [InlineData(TipoEvaluador.Externo, RolMiembroComite.Evaluador)]
    public void Debe_CrearMiembroActivoConFechaAsignacionUtc_Cuando_LosDatosSonValidos(
        TipoEvaluador tipo,
        RolMiembroComite rol)
    {
        var fechaConOffset = new DateTimeOffset(2026, 11, 22, 22, 30, 0, TimeSpan.FromHours(-4));

        var miembro = MiembroComite.Crear(
            _comiteId,
            _evaluadorId,
            tipo,
            rol,
            fechaConOffset,
            _usuarioId);

        Assert.NotEqual(Guid.Empty, miembro.Id);
        Assert.True(UuidV7.EsVersion7(miembro.Id));
        Assert.Equal(_comiteId, miembro.ComiteEvaluadorId);
        Assert.Equal(_evaluadorId, miembro.UsuarioEvaluadorId);
        Assert.Equal(tipo, miembro.Tipo);
        Assert.Equal(rol, miembro.Rol);
        Assert.Equal(new DateOnly(2026, 11, 23), miembro.FechaAsignacion);
        Assert.Null(miembro.FechaDesvinculacion);
        Assert.True(miembro.Activo);
        Assert.Equal(fechaConOffset.ToUniversalTime(), miembro.FechaCreacion);
        Assert.Equal(_usuarioId, miembro.UsuarioCreacionId);
        Assert.Equal(1, miembro.Version);
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_ComiteEvaluadorIdEstaVacio()
    {
        var excepcion = Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => MiembroComite.Crear(
                Guid.Empty,
                _evaluadorId,
                TipoEvaluador.Interno,
                RolMiembroComite.Evaluador,
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(ComiteEvaluadorInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_UsuarioEvaluadorIdEstaVacio()
    {
        var excepcion = Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => MiembroComite.Crear(
                _comiteId,
                Guid.Empty,
                TipoEvaluador.Externo,
                RolMiembroComite.Evaluador,
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(ComiteEvaluadorInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_TipoNoEstaDefinido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MiembroComite.Crear(
                _comiteId,
                _evaluadorId,
                (TipoEvaluador)999,
                RolMiembroComite.Evaluador,
                _fechaCreacion,
                _usuarioId));
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_RolNoEstaDefinido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MiembroComite.Crear(
                _comiteId,
                _evaluadorId,
                TipoEvaluador.Interno,
                (RolMiembroComite)999,
                _fechaCreacion,
                _usuarioId));
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_JefaturaEsExterna()
    {
        var excepcion = Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => MiembroComite.Crear(
                _comiteId,
                _evaluadorId,
                TipoEvaluador.Externo,
                RolMiembroComite.Jefatura,
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(ComiteEvaluadorInvalidoException.CodigoError, excepcion.Codigo);
    }

    [Fact]
    public void Debe_DesvincularConFechaYMarcarInactivo_Cuando_EsEvaluadorActivo()
    {
        var miembro = MiembroComite.Crear(
            _comiteId,
            _evaluadorId,
            TipoEvaluador.Externo,
            RolMiembroComite.Evaluador,
            _fechaCreacion,
            _usuarioId);

        var fechaDesvinculacion = _fechaCreacion.AddDays(3);
        var usuarioEditorId = Guid.NewGuid();

        var desvinculado = miembro.Desvincular(fechaDesvinculacion, usuarioEditorId);

        Assert.True(desvinculado);
        Assert.False(miembro.Activo);
        Assert.Equal(new DateOnly(2026, 11, 25), miembro.FechaDesvinculacion);
        Assert.Equal(fechaDesvinculacion, miembro.FechaUltimaModificacion);
        Assert.Equal(usuarioEditorId, miembro.UsuarioModificacionId);
        Assert.Equal(2, miembro.Version);
    }

    [Fact]
    public void Debe_PermitirDesvinculacionEnLaMismaFechaDeAsignacion()
    {
        var miembro = MiembroComite.Crear(
            _comiteId,
            _evaluadorId,
            TipoEvaluador.Interno,
            RolMiembroComite.Evaluador,
            _fechaCreacion,
            _usuarioId);

        var desvinculado = miembro.Desvincular(_fechaCreacion.AddHours(2), _usuarioId);

        Assert.True(desvinculado);
        Assert.False(miembro.Activo);
        Assert.Equal(miembro.FechaAsignacion, miembro.FechaDesvinculacion);
    }

    [Fact]
    public void Debe_SerIdempotente_Cuando_SeDesvinculaUnMiembroYaInactivo()
    {
        var miembro = MiembroComite.Crear(
            _comiteId,
            _evaluadorId,
            TipoEvaluador.Interno,
            RolMiembroComite.Evaluador,
            _fechaCreacion,
            _usuarioId);

        var primeraFecha = _fechaCreacion.AddDays(1);
        Assert.True(miembro.Desvincular(primeraFecha, _usuarioId));

        var resultadoSegundo = miembro.Desvincular(_fechaCreacion.AddDays(5), Guid.NewGuid());

        Assert.False(resultadoSegundo);
        Assert.False(miembro.Activo);
        Assert.Equal(new DateOnly(2026, 11, 23), miembro.FechaDesvinculacion);
        Assert.Equal(2, miembro.Version);
    }

    [Fact]
    public void Debe_RechazarDesvinculacion_Cuando_FechaEsAnteriorAFechaAsignacion()
    {
        var miembro = MiembroComite.Crear(
            _comiteId,
            _evaluadorId,
            TipoEvaluador.Interno,
            RolMiembroComite.Evaluador,
            _fechaCreacion,
            _usuarioId);

        var fechaAnterior = _fechaCreacion.AddDays(-1);

        var excepcion = Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => miembro.Desvincular(fechaAnterior, _usuarioId));

        Assert.Equal(ComiteEvaluadorInvalidoException.CodigoError, excepcion.Codigo);
        Assert.True(miembro.Activo);
        Assert.Null(miembro.FechaDesvinculacion);
        Assert.Equal(1, miembro.Version);
    }

    [Fact]
    public void Debe_RechazarDesvinculacion_Cuando_MiembroEsJefatura()
    {
        var jefatura = MiembroComite.Crear(
            _comiteId,
            _evaluadorId,
            TipoEvaluador.Interno,
            RolMiembroComite.Jefatura,
            _fechaCreacion,
            _usuarioId);

        var excepcion = Assert.Throws<JefaturaNoDesvinculableException>(
            () => jefatura.Desvincular(_fechaCreacion.AddDays(1), _usuarioId));

        Assert.Equal(JefaturaNoDesvinculableException.CodigoError, excepcion.Codigo);
        Assert.True(jefatura.Activo);
        Assert.Null(jefatura.FechaDesvinculacion);
    }
}
