using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Comites.Agregados;

public sealed class ComiteEvaluadorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000401");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000402");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000403");
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000404");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearComiteActivoConJefaturaComoPrimerMiembro_Cuando_LosDatosSonValidos()
    {
        var fechaConOffset = new DateTimeOffset(2026, 11, 22, 21, 30, 0, TimeSpan.FromHours(-4));

        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "  Comité Beca Excelencia 2026-II  ",
            _jefaturaId,
            fechaConOffset,
            _coordinadorId);

        Assert.NotEqual(Guid.Empty, comite.Id);
        Assert.True(UuidV7.EsVersion7(comite.Id));
        Assert.Equal(_programaBecaId, comite.ProgramaBecaId);
        Assert.Equal(_convocatoriaId, comite.ConvocatoriaId);
        Assert.Equal("Comité Beca Excelencia 2026-II", comite.Nombre);
        Assert.Equal(_jefaturaId, comite.JefaturaUsuarioId);
        Assert.Equal(new DateOnly(2026, 11, 23), comite.FechaConformacion);
        Assert.Equal(_coordinadorId, comite.ConformadoPorUsuarioId);
        Assert.True(comite.Activo);
        Assert.Equal(fechaConOffset.ToUniversalTime(), comite.FechaCreacion);
        Assert.Equal(_coordinadorId, comite.UsuarioCreacionId);
        Assert.Equal(1, comite.Version);

        var miembroJefatura = Assert.Single(comite.Miembros);
        Assert.Equal(comite.Id, miembroJefatura.ComiteEvaluadorId);
        Assert.Equal(_jefaturaId, miembroJefatura.UsuarioEvaluadorId);
        Assert.Equal(TipoEvaluador.Interno, miembroJefatura.Tipo);
        Assert.Equal(RolMiembroComite.Jefatura, miembroJefatura.Rol);
        Assert.Equal(new DateOnly(2026, 11, 23), miembroJefatura.FechaAsignacion);
        Assert.Null(miembroJefatura.FechaDesvinculacion);
        Assert.True(miembroJefatura.Activo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarCreacion_Cuando_ElNombreEstaVacio(string nombreInvalido)
    {
        var excepcion = Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => ComiteEvaluador.Crear(
                _programaBecaId,
                _convocatoriaId,
                nombreInvalido,
                _jefaturaId,
                _fechaCreacion,
                _coordinadorId));

        Assert.Equal(ComiteEvaluadorInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_ElNombreSupera200Caracteres()
    {
        var nombreLargo = new string('C', 201);

        var excepcion = Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => ComiteEvaluador.Crear(
                _programaBecaId,
                _convocatoriaId,
                nombreLargo,
                _jefaturaId,
                _fechaCreacion,
                _coordinadorId));

        Assert.Equal(ComiteEvaluadorInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarCreacion_Cuando_LosIdentificadoresObligatoriosEstanVacios()
    {
        Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => ComiteEvaluador.Crear(
                Guid.Empty,
                _convocatoriaId,
                "Comité",
                _jefaturaId,
                _fechaCreacion,
                _coordinadorId));

        Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => ComiteEvaluador.Crear(
                _programaBecaId,
                Guid.Empty,
                "Comité",
                _jefaturaId,
                _fechaCreacion,
                _coordinadorId));

        Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => ComiteEvaluador.Crear(
                _programaBecaId,
                _convocatoriaId,
                "Comité",
                Guid.Empty,
                _fechaCreacion,
                _coordinadorId));
    }

    [Fact]
    public void Debe_AgregarEvaluadoresInternoYExternoConRolEvaluador_Cuando_SonValidos()
    {
        var comite = CrearComiteValido();
        var evaluadorInternoId = Guid.NewGuid();
        var evaluadorExternoId = Guid.NewGuid();

        var interno = comite.AgregarMiembro(
            evaluadorInternoId,
            TipoEvaluador.Interno,
            _fechaCreacion.AddHours(1),
            _coordinadorId);

        var externo = comite.AgregarMiembro(
            evaluadorExternoId,
            TipoEvaluador.Externo,
            _fechaCreacion.AddDays(1),
            _coordinadorId);

        Assert.Equal(3, comite.Miembros.Count);
        Assert.Equal(3, comite.Version);

        Assert.Equal(evaluadorInternoId, interno.UsuarioEvaluadorId);
        Assert.Equal(TipoEvaluador.Interno, interno.Tipo);
        Assert.Equal(RolMiembroComite.Evaluador, interno.Rol);
        Assert.True(interno.Activo);
        Assert.Equal(new DateOnly(2026, 11, 22), interno.FechaAsignacion);

        Assert.Equal(evaluadorExternoId, externo.UsuarioEvaluadorId);
        Assert.Equal(TipoEvaluador.Externo, externo.Tipo);
        Assert.Equal(RolMiembroComite.Evaluador, externo.Rol);
        Assert.True(externo.Activo);
        Assert.Equal(new DateOnly(2026, 11, 23), externo.FechaAsignacion);
    }

    [Fact]
    public void Debe_RechazarMiembroDuplicadoActivo_Cuando_ElEvaluadorYaEstaActivo()
    {
        var comite = CrearComiteValido();
        var evaluadorId = Guid.NewGuid();

        comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Interno,
            _fechaCreacion.AddHours(1),
            _coordinadorId);

        var excepcion = Assert.Throws<MiembroComiteDuplicadoException>(
            () => comite.AgregarMiembro(
                evaluadorId,
                TipoEvaluador.Externo,
                _fechaCreacion.AddHours(2),
                _coordinadorId));

        Assert.Equal(MiembroComiteDuplicadoException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(2, comite.Miembros.Count);
        Assert.Equal(2, comite.Version);
    }

    [Fact]
    public void Debe_RechazarMiembroDuplicadoActivo_Cuando_SeIntentaAgregarALaJefatura()
    {
        var comite = CrearComiteValido();

        var excepcion = Assert.Throws<MiembroComiteDuplicadoException>(
            () => comite.AgregarMiembro(
                _jefaturaId,
                TipoEvaluador.Interno,
                _fechaCreacion.AddHours(1),
                _coordinadorId));

        Assert.Equal(MiembroComiteDuplicadoException.CodigoError, excepcion.Codigo);
        Assert.Single(comite.Miembros);
        Assert.Equal(1, comite.Version);
    }

    [Fact]
    public void Debe_RechazarAgregarMiembro_Cuando_UsuarioEvaluadorVacioOTipoNoDefinido()
    {
        var comite = CrearComiteValido();

        Assert.Throws<ComiteEvaluadorInvalidoException>(
            () => comite.AgregarMiembro(
                Guid.Empty,
                TipoEvaluador.Interno,
                _fechaCreacion.AddHours(1),
                _coordinadorId));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => comite.AgregarMiembro(
                Guid.NewGuid(),
                (TipoEvaluador)999,
                _fechaCreacion.AddHours(1),
                _coordinadorId));
    }

    [Fact]
    public void Debe_PermitirReincorporacionTrasDesvinculacion_ConservandoHistorial()
    {
        var comite = CrearComiteValido();
        var evaluadorId = Guid.NewGuid();

        var primeraMembresia = comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Interno,
            _fechaCreacion.AddHours(1),
            _coordinadorId);

        var desvinculado = comite.DesvincularMiembro(
            primeraMembresia.Id,
            _fechaCreacion.AddDays(2),
            _coordinadorId);

        Assert.True(desvinculado);

        var segundaMembresia = comite.AgregarMiembro(
            evaluadorId,
            TipoEvaluador.Externo,
            _fechaCreacion.AddDays(5),
            _coordinadorId);

        Assert.NotEqual(primeraMembresia.Id, segundaMembresia.Id);
        Assert.Equal(3, comite.Miembros.Count);
        Assert.False(primeraMembresia.Activo);
        Assert.Equal(new DateOnly(2026, 11, 24), primeraMembresia.FechaDesvinculacion);
        Assert.True(segundaMembresia.Activo);
        Assert.Null(segundaMembresia.FechaDesvinculacion);
        Assert.Equal(TipoEvaluador.Externo, segundaMembresia.Tipo);
        Assert.Equal(RolMiembroComite.Evaluador, segundaMembresia.Rol);
        Assert.Equal(new DateOnly(2026, 11, 27), segundaMembresia.FechaAsignacion);
    }

    [Fact]
    public void Debe_DesvincularMiembroEIncrementarVersion_Cuando_EsEvaluadorActivo()
    {
        var comite = CrearComiteValido();
        var evaluador = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            _fechaCreacion.AddHours(1),
            _coordinadorId);
        var versionAntes = comite.Version;
        var fechaBaja = _fechaCreacion.AddDays(3);

        var resultado = comite.DesvincularMiembro(evaluador.Id, fechaBaja, _coordinadorId);

        Assert.True(resultado);
        Assert.Equal(versionAntes + 1, comite.Version);
        Assert.Equal(fechaBaja, comite.FechaUltimaModificacion);
        Assert.False(evaluador.Activo);
        Assert.Equal(new DateOnly(2026, 11, 25), evaluador.FechaDesvinculacion);
    }

    [Fact]
    public void Debe_SerIdempotenteSinCambiarVersionNiFecha_Cuando_SeDesvinculaMiembroYaInactivo()
    {
        var comite = CrearComiteValido();
        var evaluador = comite.AgregarMiembro(
            Guid.NewGuid(),
            TipoEvaluador.Interno,
            _fechaCreacion.AddHours(1),
            _coordinadorId);

        var primeraFechaBaja = _fechaCreacion.AddDays(1);
        Assert.True(comite.DesvincularMiembro(evaluador.Id, primeraFechaBaja, _coordinadorId));
        var versionTrasPrimeraBaja = comite.Version;
        var fechaModificacionTrasPrimeraBaja = comite.FechaUltimaModificacion;

        var resultadoRepetido = comite.DesvincularMiembro(
            evaluador.Id,
            _fechaCreacion.AddDays(4),
            Guid.NewGuid());

        Assert.False(resultadoRepetido);
        Assert.Equal(versionTrasPrimeraBaja, comite.Version);
        Assert.Equal(fechaModificacionTrasPrimeraBaja, comite.FechaUltimaModificacion);
        Assert.Equal(new DateOnly(2026, 11, 23), evaluador.FechaDesvinculacion);
    }

    [Fact]
    public void Debe_RechazarDesvinculacion_Cuando_ElMiembroEsLaJefatura()
    {
        var comite = CrearComiteValido();
        var jefatura = Assert.Single(comite.Miembros);

        var excepcion = Assert.Throws<JefaturaNoDesvinculableException>(
            () => comite.DesvincularMiembro(jefatura.Id, _fechaCreacion.AddDays(1), _coordinadorId));

        Assert.Equal(JefaturaNoDesvinculableException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.True(jefatura.Activo);
        Assert.Null(jefatura.FechaDesvinculacion);
        Assert.Equal(1, comite.Version);
    }

    [Fact]
    public void Debe_RechazarDesvinculacion_Cuando_ElMiembroNoPerteneceAlComite()
    {
        var comite = CrearComiteValido();

        var excepcion = Assert.Throws<MiembroComiteNoEncontradoException>(
            () => comite.DesvincularMiembro(Guid.NewGuid(), _fechaCreacion.AddDays(1), _coordinadorId));

        Assert.Equal(MiembroComiteNoEncontradoException.CodigoError, excepcion.Codigo);
        Assert.Equal(404, excepcion.EstadoHttp);
        Assert.Equal(1, comite.Version);
    }

    [Fact]
    public void Debe_ExponerColeccionNoMutableDesdeFuera()
    {
        var comite = CrearComiteValido();
        var miembroExterno = MiembroComite.Crear(
            comite.Id,
            Guid.NewGuid(),
            TipoEvaluador.Externo,
            RolMiembroComite.Evaluador,
            _fechaCreacion,
            _coordinadorId);

        Assert.IsNotType<List<MiembroComite>>(comite.Miembros);

        var coleccion = Assert.IsAssignableFrom<ICollection<MiembroComite>>(comite.Miembros);
        Assert.True(coleccion.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => coleccion.Add(miembroExterno));
    }

    private static ComiteEvaluador CrearComiteValido()
        => ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Beca Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacion,
            _coordinadorId);
}
