using System.Reflection;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Asignaciones.Agregados;

public sealed class AsignacionEvaluacionTransicionesTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01968000-0000-7000-8000-000000000101");
    private static readonly Guid _convocatoriaId = Guid.Parse("01968000-0000-7000-8000-000000000102");
    private static readonly Guid _jefaturaId = Guid.Parse("01968000-0000-7000-8000-000000000103");
    private static readonly Guid _coordinadorId = Guid.Parse("01968000-0000-7000-8000-000000000104");
    private static readonly Guid _evaluadorId = Guid.Parse("01968000-0000-7000-8000-000000000105");
    private static readonly Guid _otroEvaluadorId = Guid.Parse("01968000-0000-7000-8000-000000000106");
    private static readonly Guid _postulacionId = Guid.Parse("01968000-0000-7000-8000-000000000107");
    private static readonly DateTimeOffset _fechaBase =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_TransicionarDePendienteAEnProgreso_Cuando_EvaluadorIniciaEvaluacion()
    {
        var asignacion = CrearAsignacionPendiente();
        var fechaInicio = new DateTimeOffset(2026, 11, 25, 14, 0, 0, TimeSpan.FromHours(-4));

        var cambioAplicado = asignacion.IniciarEvaluacion(fechaInicio, _evaluadorId);

        Assert.True(cambioAplicado);
        Assert.Equal(EstadoAsignacionEvaluacion.EnProgreso, asignacion.Estado);
        Assert.Equal(2, asignacion.Version);
        Assert.Equal(fechaInicio.ToUniversalTime(), asignacion.FechaUltimaModificacion);
        Assert.Equal(_evaluadorId, asignacion.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_SerIdempotenteSinIncrementarVersion_Cuando_SeIniciaEvaluacionYaEnProgreso()
    {
        var asignacion = CrearAsignacionPendiente();
        var primerInicio = _fechaBase.AddDays(3);
        asignacion.IniciarEvaluacion(primerInicio, _evaluadorId);

        var cambioAplicado = asignacion.IniciarEvaluacion(primerInicio.AddHours(2), _evaluadorId);

        Assert.False(cambioAplicado);
        Assert.Equal(EstadoAsignacionEvaluacion.EnProgreso, asignacion.Estado);
        Assert.Equal(2, asignacion.Version);
        Assert.Equal(primerInicio.ToUniversalTime(), asignacion.FechaUltimaModificacion);
        Assert.Equal(_evaluadorId, asignacion.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_TransicionarDeEnProgresoACompletada_Cuando_EvaluadorCompletaEvaluacion()
    {
        var asignacion = CrearAsignacionPendiente();
        var fechaInicio = _fechaBase.AddDays(3);
        var fechaCierre = new DateTimeOffset(2026, 11, 26, 18, 30, 0, TimeSpan.FromHours(-4));
        asignacion.IniciarEvaluacion(fechaInicio, _evaluadorId);

        asignacion.CompletarEvaluacion(fechaCierre, _evaluadorId);

        Assert.Equal(EstadoAsignacionEvaluacion.Completada, asignacion.Estado);
        Assert.Equal(3, asignacion.Version);
        Assert.Equal(fechaCierre.ToUniversalTime(), asignacion.FechaUltimaModificacion);
        Assert.Equal(_evaluadorId, asignacion.UsuarioModificacionId);
    }

    [Fact]
    public void Debe_RechazarCon403ConfidencialAntesDeRevelarEstado_Cuando_OtroUsuarioIniciaOCompleta()
    {
        var asignacionReasignada = CrearAsignacionPendiente();
        FijarEstado(asignacionReasignada, EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        var exIniciar = Assert.Throws<EvaluacionConfidencialException>(
            () => asignacionReasignada.IniciarEvaluacion(_fechaBase.AddDays(3), _otroEvaluadorId));
        var exCompletar = Assert.Throws<EvaluacionConfidencialException>(
            () => asignacionReasignada.CompletarEvaluacion(_fechaBase.AddDays(3), _otroEvaluadorId));

        Assert.Equal("EVALUACION_CONFIDENCIAL", exIniciar.Codigo);
        Assert.Equal(403, exIniciar.EstadoHttp);
        Assert.Equal("EVALUACION_CONFIDENCIAL", exCompletar.Codigo);
        Assert.Equal(403, exCompletar.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon409YaEnviada_Cuando_SeIniciaOCompletaAsignacionCompletada()
    {
        var asignacion = CrearAsignacionPendiente();
        asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), _evaluadorId);
        asignacion.CompletarEvaluacion(_fechaBase.AddDays(4), _evaluadorId);

        var exIniciar = Assert.Throws<EvaluacionYaEnviadaException>(
            () => asignacion.IniciarEvaluacion(_fechaBase.AddDays(5), _evaluadorId));
        var exCompletar = Assert.Throws<EvaluacionYaEnviadaException>(
            () => asignacion.CompletarEvaluacion(_fechaBase.AddDays(5), _evaluadorId));

        Assert.Equal("EVALUACION_YA_ENVIADA", exIniciar.Codigo);
        Assert.Equal(409, exIniciar.EstadoHttp);
        Assert.Equal("EVALUACION_YA_ENVIADA", exCompletar.Codigo);
        Assert.Equal(409, exCompletar.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon409NoEvaluable_Cuando_SeIniciaOCompletaAsignacionReasignadaPorConflicto()
    {
        var asignacion = CrearAsignacionPendiente();
        FijarEstado(asignacion, EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        var exIniciar = Assert.Throws<AsignacionNoEvaluableException>(
            () => asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), _evaluadorId));
        var exCompletar = Assert.Throws<AsignacionNoEvaluableException>(
            () => asignacion.CompletarEvaluacion(_fechaBase.AddDays(3), _evaluadorId));

        Assert.Equal("ASIGNACION_NO_EVALUABLE", exIniciar.Codigo);
        Assert.Equal(409, exIniciar.EstadoHttp);
        Assert.Equal("ASIGNACION_NO_EVALUABLE", exCompletar.Codigo);
        Assert.Equal(409, exCompletar.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCon409NoEvaluable_Cuando_SeCompletaDesdePendiente()
    {
        var asignacion = CrearAsignacionPendiente();

        var excepcion = Assert.Throws<AsignacionNoEvaluableException>(
            () => asignacion.CompletarEvaluacion(_fechaBase.AddDays(3), _evaluadorId));

        Assert.Equal("ASIGNACION_NO_EVALUABLE", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, asignacion.Estado);
    }

    [Fact]
    public void Debe_RechazarArgumentosVacios_Cuando_FechaOUsuarioSonInvalidos()
    {
        var asignacion = CrearAsignacionPendiente();

        Assert.Throws<ArgumentException>(() => asignacion.IniciarEvaluacion(default, _evaluadorId));
        Assert.Throws<ArgumentException>(() => asignacion.IniciarEvaluacion(_fechaBase.AddDays(3), Guid.Empty));
        Assert.Throws<ArgumentException>(() => asignacion.CompletarEvaluacion(default, _evaluadorId));
        Assert.Throws<ArgumentException>(() => asignacion.CompletarEvaluacion(_fechaBase.AddDays(3), Guid.Empty));
    }

    private static AsignacionEvaluacion CrearAsignacionPendiente()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaBase,
            _coordinadorId);

        var miembro = comite.AgregarMiembro(
            _evaluadorId,
            TipoEvaluador.Externo,
            _fechaBase.AddDays(1),
            _coordinadorId);

        return AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaBase.AddDays(2),
            _fechaBase.AddDays(12),
            _coordinadorId);
    }

    private static void FijarEstado(AsignacionEvaluacion asignacion, EstadoAsignacionEvaluacion estado)
    {
        var propiedad = typeof(AsignacionEvaluacion)
            .GetProperty(nameof(AsignacionEvaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(asignacion, estado);
    }
}
