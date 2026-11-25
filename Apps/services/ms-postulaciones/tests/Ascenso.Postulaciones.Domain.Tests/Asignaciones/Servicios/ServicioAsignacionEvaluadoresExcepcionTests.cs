using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Servicios;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Asignaciones.Servicios;

public sealed class ServicioAsignacionEvaluadoresExcepcionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000501");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000502");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000503");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000504");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000505");
    private static readonly DateTimeOffset _fechaCreacionComite =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 24, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<Guid, int> _cargaVacia =
        new Dictionary<Guid, int>();

    [Fact]
    public void Debe_ExponerMinimoEvaluadoresConExcepcionIgualAUno()
    {
        Assert.Equal(1, ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion);
    }

    [Fact]
    public void Debe_PermitirUnEvaluadorEnModoManual_Cuando_MinimoEsUno()
    {
        var (comite, evaluador1, _) = CrearComiteConDosEvaluadores();

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [evaluador1.UsuarioEvaluadorId],
            cantidad: null,
            vigentes: [],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId,
            ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion);

        var asignacion = Assert.Single(resultado);
        Assert.Equal(comite.Id, asignacion.ComiteEvaluadorId);
        Assert.Equal(_postulacionId, asignacion.PostulacionId);
        Assert.Equal(evaluador1.Id, asignacion.MiembroComiteId);
        Assert.Equal(evaluador1.UsuarioEvaluadorId, asignacion.UsuarioEvaluadorId);
        Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, asignacion.Estado);
    }

    [Fact]
    public void Debe_SeleccionarUnEvaluadorDeMenorCargaEnModoAutomatico_Cuando_MinimoEsUno()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        var jefatura = comite.Miembros.Single(m => m.UsuarioEvaluadorId == _jefaturaId);
        var carga = new Dictionary<Guid, int>
        {
            [jefatura.Id] = 4,
            [evaluador1.Id] = 3,
            [evaluador2.Id] = 1
        };

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            usuariosSolicitados: null,
            cantidad: 1,
            vigentes: [],
            carga,
            _ahora,
            _fechaLimite,
            _coordinadorId,
            ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion);

        var asignacion = Assert.Single(resultado);
        Assert.Equal(evaluador2.Id, asignacion.MiembroComiteId);
        Assert.Equal(evaluador2.UsuarioEvaluadorId, asignacion.UsuarioEvaluadorId);
        Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, asignacion.Estado);
    }

    [Fact]
    public void Debe_RechazarCuandoNoHayNuevosEvaluadoresO_NoHayElegiblesConMinimoUno()
    {
        var comiteSoloJefatura = CrearComiteBase();
        var jefatura = comiteSoloJefatura.Miembros.Single();
        var vigente = AsignacionEvaluacion.Crear(
            comiteSoloJefatura,
            jefatura.Id,
            _postulacionId,
            _ahora.AddDays(-1),
            _fechaLimite,
            _coordinadorId);

        // Con 1 vigente y ningún evaluador nuevo indicado en la solicitud:
        Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comiteSoloJefatura,
                _postulacionId,
                usuariosSolicitados: [],
                cantidad: null,
                vigentes: [vigente],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion));

        // En modo automático solicitando 1 nuevo cuando ya no queda ningún miembro elegible:
        var errorInsuficientes = Assert.Throws<EvaluadoresInsuficientesException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comiteSoloJefatura,
                _postulacionId,
                usuariosSolicitados: null,
                cantidad: 1,
                vigentes: [vigente],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion));

        Assert.Equal(EvaluadoresInsuficientesException.CodigoError, errorInsuficientes.Codigo);
        Assert.Equal(422, errorInsuficientes.EstadoHttp);
    }

    [Fact]
    public void Debe_ComportarseIgualAlPorDefecto_Cuando_MinimoEsDosExplicito()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();

        var error = Assert.Throws<EvaluadoresInsuficientesException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresVigentes));

        Assert.Equal(EvaluadoresInsuficientesException.CodigoError, error.Codigo);

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
            cantidad: null,
            vigentes: [],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId,
            ServicioAsignacionEvaluadores.MinimoEvaluadoresVigentes);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public void Debe_MantenerReglasDeMiembroActivoDuplicadoComiteYFechaLimite_Cuando_MinimoEsUno()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        comite.DesvincularMiembro(evaluador2.Id, _fechaCreacionComite.AddDays(3), _coordinadorId);

        // Miembro no activo:
        Assert.Throws<EvaluadorNoMiembroActivoException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion));

        // Evaluador ya vigente (duplicado):
        var vigente = AsignacionEvaluacion.Crear(
            comite,
            evaluador1.Id,
            _postulacionId,
            _ahora.AddDays(-1),
            _fechaLimite,
            _coordinadorId);

        Assert.Throws<AsignacionDuplicadaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [vigente],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion));

        // Comité distinto al de la asignación vigente:
        var otroComite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Otro Comité",
            Guid.NewGuid(),
            _fechaCreacionComite,
            _coordinadorId);
        var miembroOtro = otroComite.Miembros.Single();

        Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                otroComite,
                _postulacionId,
                [miembroOtro.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [vigente],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion));

        // Fecha límite no futura:
        Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                fechaLimite: _ahora,
                _coordinadorId,
                ServicioAsignacionEvaluadores.MinimoEvaluadoresConExcepcion));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    public void Debe_RechazarConArgumentOutOfRange_Cuando_MinimoEvaluadoresEsInvalido(int minimoInvalido)
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId,
                minimoInvalido));

        Assert.Equal("minimoEvaluadores", error.ParamName);
    }

    private static ComiteEvaluador CrearComiteBase()
        => ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacionComite,
            _coordinadorId);

    private static (ComiteEvaluador Comite, MiembroComite Evaluador1, MiembroComite Evaluador2)
        CrearComiteConDosEvaluadores()
    {
        var comite = CrearComiteBase();
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
}
