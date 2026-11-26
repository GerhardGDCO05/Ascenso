using System.Reflection;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Asignaciones.Servicios;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Asignaciones.Servicios;

public sealed class ServicioAsignacionEvaluadoresTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000201");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000202");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000203");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000204");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000205");
    private static readonly DateTimeOffset _fechaCreacionComite =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _ahora =
        new(2026, 11, 24, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<Guid, int> _cargaVacia =
        new Dictionary<Guid, int>();

    [Fact]
    public void Debe_ExponerMinimoEvaluadoresVigentesIgualADos()
    {
        Assert.Equal(2, ServicioAsignacionEvaluadores.MinimoEvaluadoresVigentes);
    }

    [Fact]
    public void Debe_CrearDosAsignacionesPendientes_Cuando_ModoManualTieneDosEvaluadoresActivos()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
            cantidad: null,
            vigentes: [],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, asignacion =>
        {
            Assert.Equal(comite.Id, asignacion.ComiteEvaluadorId);
            Assert.Equal(_convocatoriaId, asignacion.ConvocatoriaId);
            Assert.Equal(_postulacionId, asignacion.PostulacionId);
            Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, asignacion.Estado);
            Assert.Equal(_ahora, asignacion.FechaAsignacion);
            Assert.Equal(_fechaLimite, asignacion.FechaLimiteEvaluacion);
        });
        Assert.Equal(evaluador1.Id, resultado[0].MiembroComiteId);
        Assert.Equal(evaluador1.UsuarioEvaluadorId, resultado[0].UsuarioEvaluadorId);
        Assert.Equal(evaluador2.Id, resultado[1].MiembroComiteId);
        Assert.Equal(evaluador2.UsuarioEvaluadorId, resultado[1].UsuarioEvaluadorId);
    }

    [Fact]
    public void Debe_DeduplicarEvaluadoresRepetidos_Cuando_QuedanAlMenosDosDistintos()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId, evaluador1.UsuarioEvaluadorId],
            cantidad: null,
            vigentes: [],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(
            [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
            resultado.Select(asignacion => asignacion.UsuarioEvaluadorId).ToArray());
    }

    [Fact]
    public void Debe_RechazarConEvaluacionesInsuficientes_Cuando_ModoManualSoloTieneUnEvaluadorORepetido()
    {
        var (comite, evaluador1, _) = CrearComiteConDosEvaluadores();

        var excepcionUno = Assert.Throws<EvaluadoresInsuficientesException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("EVALUACIONES_INSUFICIENTES", excepcionUno.Codigo);
        Assert.Equal(422, excepcionUno.EstadoHttp);

        var excepcionRepetido = Assert.Throws<EvaluadoresInsuficientesException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, evaluador1.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("EVALUACIONES_INSUFICIENTES", excepcionRepetido.Codigo);
    }

    [Fact]
    public void Debe_PermitirEvaluadorExternoYJefaturaActivos_Cuando_SeAsignanManualmente()
    {
        var comite = CrearComiteBase();
        var externo = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [_jefaturaId, externo.UsuarioEvaluadorId],
            cantidad: null,
            vigentes: [],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, a => a.UsuarioEvaluadorId == _jefaturaId);
        Assert.Contains(resultado, a => a.UsuarioEvaluadorId == externo.UsuarioEvaluadorId && a.MiembroComiteId == externo.Id);
    }

    [Fact]
    public void Debe_RechazarAtomicamenteSinDevolverNada_Cuando_UnEvaluadorNoEsMiembroOEstaDesvinculado()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        comite.DesvincularMiembro(evaluador2.Id, _fechaCreacionComite.AddDays(3), _coordinadorId);

        var excepcionNoMiembro = Assert.Throws<EvaluadorNoMiembroActivoException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, Guid.NewGuid()],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("EVALUADOR_NO_MIEMBRO_ACTIVO", excepcionNoMiembro.Codigo);

        var excepcionDesvinculado = Assert.Throws<EvaluadorNoMiembroActivoException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("EVALUADOR_NO_MIEMBRO_ACTIVO", excepcionDesvinculado.Codigo);
    }

    [Fact]
    public void Debe_RechazarConAsignacionDuplicada_Cuando_EvaluadorYaEstaVigenteEnLaPostulacion()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        var existente = AsignacionEvaluacion.Crear(
            comite,
            evaluador1.Id,
            _postulacionId,
            _ahora.AddDays(-1),
            _fechaLimite,
            _coordinadorId);

        var excepcion = Assert.Throws<AsignacionDuplicadaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [existente],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("ASIGNACION_DUPLICADA", excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_PermitirTercerEvaluador_Cuando_LaPostulacionYaTieneDosVigentes()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        var evaluador3 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(3),
            _coordinadorId);

        var vigentes = new[]
        {
            AsignacionEvaluacion.Crear(comite, evaluador1.Id, _postulacionId, _ahora.AddDays(-1), _fechaLimite, _coordinadorId),
            AsignacionEvaluacion.Crear(comite, evaluador2.Id, _postulacionId, _ahora.AddDays(-1), _fechaLimite, _coordinadorId)
        };

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [evaluador3.UsuarioEvaluadorId],
            cantidad: null,
            vigentes,
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        var nueva = Assert.Single(resultado);
        Assert.Equal(evaluador3.Id, nueva.MiembroComiteId);
        Assert.Equal(evaluador3.UsuarioEvaluadorId, nueva.UsuarioEvaluadorId);
        Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, nueva.Estado);
    }

    [Fact]
    public void Debe_RechazarConAsignacionInvalida_Cuando_VigentesPertenecenAOtroComite()
    {
        var (comiteA, evaluadorA1, evaluadorA2) = CrearComiteConDosEvaluadores();
        var otroComite = ComiteEvaluador.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Otro Comité",
            Guid.NewGuid(),
            _fechaCreacionComite,
            _coordinadorId);
        var miembroOtro = otroComite.Miembros.Single();

        var vigenteOtroComite = AsignacionEvaluacion.Crear(
            otroComite,
            miembroOtro.Id,
            _postulacionId,
            _ahora.AddDays(-1),
            _fechaLimite,
            _coordinadorId);

        var excepcion = Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comiteA,
                _postulacionId,
                [evaluadorA1.UsuarioEvaluadorId, evaluadorA2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [vigenteOtroComite],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("ASIGNACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_NoContarComoVigenteUnaAsignacionReasignadaPorConflicto()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        var asignacionReasignada = AsignacionEvaluacion.Crear(
            comite,
            evaluador1.Id,
            _postulacionId,
            _ahora.AddDays(-2),
            _fechaLimite,
            _coordinadorId);
        MarcarEstado(asignacionReasignada, EstadoAsignacionEvaluacion.ReasignadaPorConflicto);

        // No cuenta para alcanzar el mínimo de 2 si solo se agrega un evaluador:
        Assert.Throws<EvaluadoresInsuficientesException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [asignacionReasignada],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        // Y tampoco bloquea volver a asignar al mismo evaluador si se cumplen los 2 vigentes:
        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
            cantidad: null,
            vigentes: [asignacionReasignada],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, a => a.UsuarioEvaluadorId == evaluador1.UsuarioEvaluadorId);
    }

    [Fact]
    public void Debe_SeleccionarPorMenorCargaConEmpateDeterminista_Cuando_ModoEsAutomatico()
    {
        var comite = CrearComiteBase();
        var jefatura = comite.Miembros.Single();
        var miembroA = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);
        var miembroB = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);
        var miembroC = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(2),
            _coordinadorId);

        var carga = new Dictionary<Guid, int>
        {
            [jefatura.Id] = 5,
            [miembroA.Id] = 1,
            [miembroB.Id] = 1,
            [miembroC.Id] = 0
        };

        var esperadoSegundo = miembroA.Id.CompareTo(miembroB.Id) < 0 ? miembroA : miembroB;

        var primeraEjecucion = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            usuariosSolicitados: null,
            cantidad: 2,
            vigentes: [],
            carga,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        var segundaEjecucion = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            usuariosSolicitados: null,
            cantidad: 2,
            vigentes: [],
            carga,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, primeraEjecucion.Count);
        Assert.Equal(miembroC.Id, primeraEjecucion[0].MiembroComiteId);
        Assert.Equal(esperadoSegundo.Id, primeraEjecucion[1].MiembroComiteId);
        Assert.Equal(
            primeraEjecucion.Select(a => a.MiembroComiteId).ToArray(),
            segundaEjecucion.Select(a => a.MiembroComiteId).ToArray());
    }

    [Fact]
    public void Debe_DesempatarPorFechaAsignacionDeMembresiaAntesQuePorId_Cuando_TienenIgualCarga()
    {
        var comite = CrearComiteBase();
        var jefatura = comite.Miembros.Single(); // FechaAsignacion = día 15
        var miembroDia16 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);
        var miembroDia17 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(2),
            _coordinadorId);

        // Todos con carga 0 (ausentes del diccionario)
        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            usuariosSolicitados: null,
            cantidad: 2,
            vigentes: [],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(jefatura.Id, resultado[0].MiembroComiteId);
        Assert.Equal(miembroDia16.Id, resultado[1].MiembroComiteId);
        Assert.DoesNotContain(resultado, a => a.MiembroComiteId == miembroDia17.Id);
    }

    [Fact]
    public void Debe_ExcluirYaVigentesYDesvinculados_Cuando_ModoEsAutomatico()
    {
        var comite = CrearComiteBase();
        var jefatura = comite.Miembros.Single();
        var evaluadorDesvinculado = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);
        var evaluadorElegible1 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(2),
            _coordinadorId);
        var evaluadorElegible2 = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(3),
            _coordinadorId);

        comite.DesvincularMiembro(evaluadorDesvinculado.Id, _fechaCreacionComite.AddDays(4), _coordinadorId);

        var vigenteJefatura = AsignacionEvaluacion.Crear(
            comite,
            jefatura.Id,
            _postulacionId,
            _ahora.AddDays(-1),
            _fechaLimite,
            _coordinadorId);

        var resultado = ServicioAsignacionEvaluadores.Asignar(
            comite,
            _postulacionId,
            usuariosSolicitados: null,
            cantidad: 2,
            vigentes: [vigenteJefatura],
            _cargaVacia,
            _ahora,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(
            [evaluadorElegible1.Id, evaluadorElegible2.Id],
            resultado.Select(a => a.MiembroComiteId).ToArray());
    }

    [Fact]
    public void Debe_RechazarConEvaluacionesInsuficientes_Cuando_NoHaySuficientesMiembrosElegiblesEnModoAutomatico()
    {
        var comite = CrearComiteBase(); // Solo 1 miembro activo (jefatura)

        var excepcion = Assert.Throws<EvaluadoresInsuficientesException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                usuariosSolicitados: null,
                cantidad: 2,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("EVALUACIONES_INSUFICIENTES", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCuandoModoEsAmbiguoOAusente()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();

        Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                usuariosSolicitados: null,
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Throws<AsignacionEvaluacionInvalidaException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
                cantidad: 2,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));
    }

    [Fact]
    public void Debe_RechazarComiteInactivo_Cuando_SeInvocaElServicio()
    {
        var (comite, evaluador1, evaluador2) = CrearComiteConDosEvaluadores();
        typeof(ComiteEvaluador)
            .GetProperty(nameof(ComiteEvaluador.Activo), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(comite, false);

        var excepcion = Assert.Throws<ComiteInactivoException>(() =>
            ServicioAsignacionEvaluadores.Asignar(
                comite,
                _postulacionId,
                [evaluador1.UsuarioEvaluadorId, evaluador2.UsuarioEvaluadorId],
                cantidad: null,
                vigentes: [],
                _cargaVacia,
                _ahora,
                _fechaLimite,
                _coordinadorId));

        Assert.Equal("COMITE_INACTIVO", excepcion.Codigo);
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

    private static void MarcarEstado(AsignacionEvaluacion asignacion, EstadoAsignacionEvaluacion estado)
    {
        var propiedad = typeof(AsignacionEvaluacion)
            .GetProperty(nameof(AsignacionEvaluacion.Estado), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(asignacion, estado);
    }
}
