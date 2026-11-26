using System.Reflection;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Asignaciones.Agregados;

public sealed class AsignacionEvaluacionTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000101");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000102");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000103");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000104");
    private static readonly Guid _evaluadorId = Guid.Parse("01967000-0000-7000-8000-000000000105");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000106");
    private static readonly DateTimeOffset _fechaCreacionComite =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaAsignacion =
        new(2026, 11, 24, 15, 30, 0, TimeSpan.FromHours(-4));
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 5, 23, 59, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_CrearAsignacionPendienteConDatosDelComiteYMiembro_Cuando_LosDatosSonValidos()
    {
        var (comite, miembro) = CrearComiteConEvaluador();

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId);

        Assert.IsAssignableFrom<AggregateRoot>(asignacion);
        Assert.NotEqual(Guid.Empty, asignacion.Id);
        Assert.Equal(comite.Id, asignacion.ComiteEvaluadorId);
        Assert.Equal(_convocatoriaId, asignacion.ConvocatoriaId);
        Assert.Equal(_postulacionId, asignacion.PostulacionId);
        Assert.Equal(miembro.Id, asignacion.MiembroComiteId);
        Assert.Equal(_evaluadorId, asignacion.UsuarioEvaluadorId);
        Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, asignacion.Estado);
        Assert.Equal(_fechaAsignacion.ToUniversalTime(), asignacion.FechaAsignacion);
        Assert.Equal(_fechaLimite.ToUniversalTime(), asignacion.FechaLimiteEvaluacion);
        Assert.Equal(_fechaAsignacion.ToUniversalTime(), asignacion.FechaCreacion);
        Assert.Equal(_coordinadorId, asignacion.UsuarioCreacionId);
        Assert.Equal(_fechaAsignacion.ToUniversalTime(), asignacion.FechaUltimaModificacion);
        Assert.Equal(_coordinadorId, asignacion.UsuarioModificacionId);
        Assert.Equal(1, asignacion.Version);
    }

    [Fact]
    public void Debe_PermitirAsignarALaJefaturaDelComite_Cuando_EsMiembroActivo()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacionComite,
            _coordinadorId);
        var miembroJefatura = Assert.Single(comite.Miembros);

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembroJefatura.Id,
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId);

        Assert.Equal(miembroJefatura.Id, asignacion.MiembroComiteId);
        Assert.Equal(_jefaturaId, asignacion.UsuarioEvaluadorId);
        Assert.Equal(EstadoAsignacionEvaluacion.Pendiente, asignacion.Estado);
    }

    [Fact]
    public void Debe_RechazarComiteNulo_Cuando_SeCreaAsignacion()
    {
        Assert.Throws<ArgumentNullException>(() => AsignacionEvaluacion.Crear(
            null!,
            Guid.NewGuid(),
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId));
    }

    [Fact]
    public void Debe_RechazarPostulacionVacia_Cuando_SeCreaAsignacion()
    {
        var (comite, miembro) = CrearComiteConEvaluador();

        var excepcion = Assert.Throws<AsignacionEvaluacionInvalidaException>(() => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            Guid.Empty,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId));

        Assert.Equal("ASIGNACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarComiteInactivo_Cuando_SeCreaAsignacion()
    {
        var (comite, miembro) = CrearComiteConEvaluador();
        DesactivarComite(comite);

        var excepcion = Assert.Throws<ComiteInactivoException>(() => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId));

        Assert.Equal("COMITE_INACTIVO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarMiembroInexistente_Cuando_NoPerteneceAlComite()
    {
        var (comite, _) = CrearComiteConEvaluador();

        var excepcion = Assert.Throws<EvaluadorNoMiembroActivoException>(() => AsignacionEvaluacion.Crear(
            comite,
            Guid.NewGuid(),
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId));

        Assert.Equal("EVALUADOR_NO_MIEMBRO_ACTIVO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarMiembroDesvinculado_Cuando_SuMembresiaEstaInactiva()
    {
        var (comite, miembro) = CrearComiteConEvaluador();
        comite.DesvincularMiembro(miembro.Id, _fechaCreacionComite.AddDays(2), _coordinadorId);

        var excepcion = Assert.Throws<EvaluadorNoMiembroActivoException>(() => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId));

        Assert.Equal("EVALUADOR_NO_MIEMBRO_ACTIVO", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void Debe_RechazarFechaLimiteNoPosterior_Cuando_EsIgualOAnteriorALaAsignacion(int segundosDelta)
    {
        var (comite, miembro) = CrearComiteConEvaluador();
        var fechaLimiteInvalida = _fechaAsignacion.AddSeconds(segundosDelta);

        var excepcion = Assert.Throws<AsignacionEvaluacionInvalidaException>(() => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaAsignacion,
            fechaLimiteInvalida,
            _coordinadorId));

        Assert.Equal("ASIGNACION_INVALIDA", excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarFechaAsignacionPorDefecto_Cuando_SeCreaAsignacion()
    {
        var (comite, miembro) = CrearComiteConEvaluador();

        Assert.Throws<ArgumentException>(() => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            default,
            _fechaLimite,
            _coordinadorId));
    }

    [Fact]
    public void Debe_RechazarUsuarioAsignaVacio_Cuando_SeCreaAsignacion()
    {
        var (comite, miembro) = CrearComiteConEvaluador();

        Assert.Throws<ArgumentException>(() => AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            Guid.Empty));
    }

    private static (ComiteEvaluador Comite, MiembroComite Miembro) CrearComiteConEvaluador()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacionComite,
            _coordinadorId);

        var miembro = comite.AgregarMiembro(
            _evaluadorId,
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);

        return (comite, miembro);
    }

    private static void DesactivarComite(ComiteEvaluador comite)
    {
        var propiedad = typeof(ComiteEvaluador)
            .GetProperty(nameof(ComiteEvaluador.Activo), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(propiedad);
        propiedad.SetValue(comite, false);
    }
}
