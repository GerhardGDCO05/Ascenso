using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Tests.Rubricas.Entidades;

public sealed class CriterioEvaluacionTests
{
    private static readonly Guid _rubricaId = Guid.NewGuid();
    private static readonly Guid _usuarioId = Guid.NewGuid();
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearElCriterio_Cuando_LosDatosSonValidos()
    {
        var criterio = CriterioEvaluacion.Crear(
            _rubricaId,
            "  Rendimiento académico  ",
            "  Evalúa el promedio ponderado del postulante.  ",
            30.50m,
            0.00m,
            20.00m,
            1,
            _fechaCreacion,
            _usuarioId);

        Assert.NotEqual(Guid.Empty, criterio.Id);
        Assert.True(UuidV7.EsVersion7(criterio.Id));
        Assert.Equal(_rubricaId, criterio.RubricaEvaluacionId);
        Assert.Equal("Rendimiento académico", criterio.Nombre);
        Assert.Equal("Evalúa el promedio ponderado del postulante.", criterio.Descripcion);
        Assert.Equal(30.50m, criterio.PesoPorcentual);
        Assert.Equal(0.00m, criterio.PuntajeMinimo);
        Assert.Equal(20.00m, criterio.PuntajeMaximo);
        Assert.Equal(1, criterio.Orden);
        Assert.Equal(_fechaCreacion, criterio.FechaCreacion);
        Assert.Equal(_usuarioId, criterio.UsuarioCreacionId);
        Assert.Equal(1, criterio.Version);
    }

    [Fact]
    public void Debe_ActualizarElCriterio_Cuando_LosNuevosDatosSonValidos()
    {
        var criterio = CrearValido();
        var usuarioEditorId = Guid.NewGuid();
        var fechaModificacion = _fechaCreacion.AddHours(2);

        criterio.Actualizar(
            "  Situación socioeconómica  ",
            "  Evalúa la vulnerabilidad económica familiar.  ",
            45.25m,
            1.00m,
            100.00m,
            fechaModificacion,
            usuarioEditorId);

        Assert.Equal("Situación socioeconómica", criterio.Nombre);
        Assert.Equal("Evalúa la vulnerabilidad económica familiar.", criterio.Descripcion);
        Assert.Equal(45.25m, criterio.PesoPorcentual);
        Assert.Equal(1.00m, criterio.PuntajeMinimo);
        Assert.Equal(100.00m, criterio.PuntajeMaximo);
        Assert.Equal(1, criterio.Orden);
        Assert.Equal(fechaModificacion, criterio.FechaUltimaModificacion);
        Assert.Equal(usuarioEditorId, criterio.UsuarioModificacionId);
        Assert.Equal(2, criterio.Version);
    }

    [Fact]
    public void Debe_RechazarElCriterio_Cuando_NoTieneRubricaAsociada()
    {
        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(rubricaId: Guid.Empty));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Debe_RechazarElCriterio_Cuando_ElOrdenNoEsPositivo(int ordenInvalido)
    {
        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(orden: ordenInvalido));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarElCriterio_Cuando_ElNombreEstaVacio(string nombreInvalido)
    {
        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(nombre: nombreInvalido));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElCriterio_Cuando_ElNombreSupera200Caracteres()
    {
        var nombreLargo = new string('A', 201);

        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(nombre: nombreLargo));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarElCriterio_Cuando_LaDescripcionEstaVacia(string descripcionInvalida)
    {
        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(descripcion: descripcionInvalida));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElCriterio_Cuando_LaDescripcionSupera1000Caracteres()
    {
        var descripcionLarga = new string('D', 1001);

        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(descripcion: descripcionLarga));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-10)]
    [InlineData(100.01)]
    [InlineData(25.123)]
    public void Debe_RechazarElCriterio_Cuando_ElPesoPorcentualEsInvalido(decimal pesoInvalido)
    {
        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(pesoPorcentual: pesoInvalido));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Theory]
    [InlineData(-0.01, 10.00)]
    [InlineData(0.00, -1.00)]
    [InlineData(1000.01, 1005.00)]
    [InlineData(0.00, 1000.01)]
    [InlineData(0.123, 10.00)]
    [InlineData(0.00, 10.123)]
    [InlineData(10.00, 10.00)]
    [InlineData(15.00, 10.00)]
    public void Debe_RechazarElCriterio_Cuando_ElRangoDePuntajeEsInvalido(
        decimal puntajeMinimo,
        decimal puntajeMaximo)
    {
        var excepcion = Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => CrearValido(puntajeMinimo: puntajeMinimo, puntajeMaximo: puntajeMaximo));

        Assert.Equal(CriterioEvaluacionInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarLaActualizacion_Cuando_LosDatosSonInvalidos()
    {
        var criterio = CrearValido();
        var fechaModificacion = _fechaCreacion.AddHours(1);

        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => criterio.Actualizar("", "Descripción", 20m, 0m, 10m, fechaModificacion, _usuarioId));
        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => criterio.Actualizar(new string('A', 201), "Descripción", 20m, 0m, 10m, fechaModificacion, _usuarioId));
        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => criterio.Actualizar("Nombre", "   ", 20m, 0m, 10m, fechaModificacion, _usuarioId));
        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => criterio.Actualizar("Nombre", new string('D', 1001), 20m, 0m, 10m, fechaModificacion, _usuarioId));
        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => criterio.Actualizar("Nombre", "Descripción", 0m, 0m, 10m, fechaModificacion, _usuarioId));
        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => criterio.Actualizar("Nombre", "Descripción", 20m, 10m, 10m, fechaModificacion, _usuarioId));
    }

    private static CriterioEvaluacion CrearValido(
        Guid? rubricaId = null,
        string nombre = "Rendimiento académico",
        string descripcion = "Evalúa el promedio ponderado.",
        decimal pesoPorcentual = 30.00m,
        decimal puntajeMinimo = 0.00m,
        decimal puntajeMaximo = 10.00m,
        int orden = 1)
        => CriterioEvaluacion.Crear(
            rubricaId ?? _rubricaId,
            nombre,
            descripcion,
            pesoPorcentual,
            puntajeMinimo,
            puntajeMaximo,
            orden,
            _fechaCreacion,
            _usuarioId);
}
