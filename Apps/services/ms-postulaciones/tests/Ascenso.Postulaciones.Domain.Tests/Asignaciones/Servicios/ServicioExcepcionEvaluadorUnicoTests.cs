using System.Reflection;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Asignaciones.Servicios;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Asignaciones.Servicios;

public sealed class ServicioExcepcionEvaluadorUnicoTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000401");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000402");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000403");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000404");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000405");
    private static readonly Guid _correlacionId = Guid.Parse("01967000-0000-7000-8000-000000000406");
    private static readonly DateTimeOffset _fechaCreacionComite =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 24, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);
    private const string JustificacionValida = "Comité con un único evaluador especialista disponible para el área.";

    [Fact]
    public void Debe_AutorizarExcepcion_Cuando_LaPostulacionTieneCeroAsignacionesVigentes()
    {
        var autorizacion = ServicioExcepcionEvaluadorUnico.Autorizar(
            _postulacionId,
            JustificacionValida,
            vigentes: [],
            yaAutorizada: false,
            _ahora,
            _coordinadorId,
            _correlacionId);

        Assert.NotNull(autorizacion);
        Assert.Equal(_postulacionId, autorizacion.PostulacionId);
        Assert.Equal(JustificacionValida, autorizacion.Justificacion);
        Assert.Equal(_coordinadorId, autorizacion.UsuarioAutorId);
        Assert.Equal(_ahora, autorizacion.FechaCreacion);
        Assert.Equal(_correlacionId, autorizacion.CorrelacionId);
    }

    [Fact]
    public void Debe_AutorizarExcepcion_Cuando_LaPostulacionTieneUnaAsignacionVigente()
    {
        var (comite, evaluador1, _) = CrearComiteConDosEvaluadores();
        var vigente = CrearAsignacion(comite, evaluador1);

        var autorizacion = ServicioExcepcionEvaluadorUnico.Autorizar(
            _postulacionId,
            JustificacionValida,
            vigentes: [vigente],
            yaAutorizada: false,
            _ahora,
            _coordinadorId,
            _correlacionId);

        Assert.Equal(_postulacionId, autorizacion.PostulacionId);
        Assert.Equal(JustificacionValida, autorizacion.Justificacion);
    }

    [Fact]
    public void Debe_RechazarConExcepcionYaAutorizada_Cuando_YaExisteUnaAutorizacion()
    {
        AutorizacionExcepcionEvaluadorUnico? creada = null;

        var error = Assert.Throws<ExcepcionEvaluadorUnicoYaAutorizadaException>(() =>
            creada = ServicioExcepcionEvaluadorUnico.Autorizar(
                _postulacionId,
                JustificacionValida,
                vigentes: [],
                yaAutorizada: true,
                _ahora,
                _coordinadorId,
                _correlacionId));

        Assert.Null(creada);
        Assert.Equal(ExcepcionEvaluadorUnicoYaAutorizadaException.CodigoError, error.Codigo);
        Assert.Equal(409, error.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarConExcepcionNoAplicable_Cuando_TieneDosOMasAsignacionesVigentes()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        var evaluador3 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(3),
            _coordinadorId);

        var dosVigentes = new[]
        {
            CrearAsignacion(comite, evaluador1),
            CrearAsignacion(comite, evaluador2)
        };
        var tresVigentes = new[]
        {
            CrearAsignacion(comite, evaluador1),
            CrearAsignacion(comite, evaluador2),
            CrearAsignacion(comite, evaluador3)
        };

        AutorizacionExcepcionEvaluadorUnico? creadaConDos = null;
        var errorDos = Assert.Throws<ExcepcionEvaluadorUnicoNoAplicableException>(() =>
            creadaConDos = ServicioExcepcionEvaluadorUnico.Autorizar(
                _postulacionId,
                JustificacionValida,
                dosVigentes,
                yaAutorizada: false,
                _ahora,
                _coordinadorId,
                _correlacionId));

        Assert.Null(creadaConDos);
        Assert.Equal(ExcepcionEvaluadorUnicoNoAplicableException.CodigoError, errorDos.Codigo);
        Assert.Equal(422, errorDos.EstadoHttp);

        AutorizacionExcepcionEvaluadorUnico? creadaConTres = null;
        var errorTres = Assert.Throws<ExcepcionEvaluadorUnicoNoAplicableException>(() =>
            creadaConTres = ServicioExcepcionEvaluadorUnico.Autorizar(
                _postulacionId,
                JustificacionValida,
                tresVigentes,
                yaAutorizada: false,
                _ahora,
                _coordinadorId,
                _correlacionId));

        Assert.Null(creadaConTres);
        Assert.Equal(ExcepcionEvaluadorUnicoNoAplicableException.CodigoError, errorTres.Codigo);
    }

    [Fact]
    public void Debe_NoContarComoVigenteUnaAsignacionReasignadaPorConflicto()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        var vigente = CrearAsignacion(comite, evaluador1);
        var reasignada = CrearAsignacion(comite, evaluador2);
        MarcarEstado(reasignada, EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        var autorizacion = ServicioExcepcionEvaluadorUnico.Autorizar(
            _postulacionId,
            JustificacionValida,
            vigentes: [vigente, reasignada],
            yaAutorizada: false,
            _ahora,
            _coordinadorId,
            _correlacionId);

        Assert.NotNull(autorizacion);
        Assert.Equal(_postulacionId, autorizacion.PostulacionId);
    }

    [Fact]
    public void Debe_RechazarSinCrearEntidad_Cuando_LaJustificacionEsInvalida()
    {
        AutorizacionExcepcionEvaluadorUnico? creada = null;

        var error = Assert.Throws<JustificacionExcepcionInvalidaException>(() =>
            creada = ServicioExcepcionEvaluadorUnico.Autorizar(
                _postulacionId,
                "corta",
                vigentes: [],
                yaAutorizada: false,
                _ahora,
                _coordinadorId,
                _correlacionId));

        Assert.Null(creada);
        Assert.Equal(JustificacionExcepcionInvalidaException.CodigoError, error.Codigo);
        Assert.Equal(400, error.EstadoHttp);
    }

    private static (ComiteEvaluador Comite, MiembroComite Evaluador1, MiembroComite Evaluador2)
        CrearComiteConDosEvaluadores()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacionComite,
            _coordinadorId);
        var evaluador1 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);
        var evaluador2 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(2),
            _coordinadorId);

        return (comite, evaluador1, evaluador2);
    }

    private static AsignacionEvaluacion CrearAsignacion(ComiteEvaluador comite, MiembroComite miembro)
        => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _ahora.AddDays(-1),
            _fechaLimite,
            _coordinadorId);

    private static void MarcarEstado(AsignacionEvaluacion asignacion, EstadoAsignacionEvaluacion estado)
    {
        var propiedad = typeof(AsignacionEvaluacion)
            .GetProperty(nameof(AsignacionEvaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(asignacion, estado);
    }
}
