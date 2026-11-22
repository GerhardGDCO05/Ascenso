using System.Reflection;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Domain.Tests.Rubricas.Agregados;

public sealed class RubricaEvaluacionTests
{
    private static readonly Guid _programaBecaId = Guid.NewGuid();
    private static readonly Guid _convocatoriaId = Guid.NewGuid();
    private static readonly Guid _usuarioId = Guid.NewGuid();
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaBloqueo =
        new(2026, 11, 22, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearLaRubricaEnBorrador_Cuando_LosDatosSonValidos()
    {
        var rubrica = RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            "  Rúbrica General 2026-II  ",
            _fechaCreacion,
            _usuarioId);

        Assert.NotEqual(Guid.Empty, rubrica.Id);
        Assert.True(UuidV7.EsVersion7(rubrica.Id));
        Assert.Equal(_programaBecaId, rubrica.ProgramaBecaId);
        Assert.Equal(_convocatoriaId, rubrica.ConvocatoriaId);
        Assert.Equal("Rúbrica General 2026-II", rubrica.Nombre);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
        Assert.Null(rubrica.FechaPublicacion);
        Assert.Null(rubrica.MotivoBloqueo);
        Assert.Null(rubrica.FechaBloqueo);
        Assert.Empty(rubrica.Criterios);
        Assert.Equal(_fechaCreacion, rubrica.FechaCreacion);
        Assert.Equal(_usuarioId, rubrica.UsuarioCreacionId);
        Assert.Equal(1, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarLaCreacion_Cuando_LosIdentificadoresEstanVacios()
    {
        var excepcionPrograma = Assert.Throws<RubricaEvaluacionInvalidaException>(
            () => RubricaEvaluacion.Crear(
                Guid.Empty,
                _convocatoriaId,
                "Rúbrica 2026",
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(RubricaEvaluacionInvalidaException.CodigoError, excepcionPrograma.Codigo);
        Assert.Equal(400, excepcionPrograma.EstadoHttp);

        var excepcionConvocatoria = Assert.Throws<RubricaEvaluacionInvalidaException>(
            () => RubricaEvaluacion.Crear(
                _programaBecaId,
                Guid.Empty,
                "Rúbrica 2026",
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(RubricaEvaluacionInvalidaException.CodigoError, excepcionConvocatoria.Codigo);
        Assert.Equal(400, excepcionConvocatoria.EstadoHttp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Debe_RechazarLaCreacion_Cuando_ElNombreEstaVacio(string nombreInvalido)
    {
        var excepcion = Assert.Throws<RubricaEvaluacionInvalidaException>(
            () => RubricaEvaluacion.Crear(
                _programaBecaId,
                _convocatoriaId,
                nombreInvalido,
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(RubricaEvaluacionInvalidaException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarLaCreacion_Cuando_ElNombreSupera200Caracteres()
    {
        var nombreLargo = new string('R', 201);

        var excepcion = Assert.Throws<RubricaEvaluacionInvalidaException>(
            () => RubricaEvaluacion.Crear(
                _programaBecaId,
                _convocatoriaId,
                nombreLargo,
                _fechaCreacion,
                _usuarioId));

        Assert.Equal(RubricaEvaluacionInvalidaException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_AgregarCriteriosConOrdenCorrelativo_Cuando_LaRubricaEstaEnBorrador()
    {
        var rubrica = CrearRubricaValida();
        var fechaModificacion = _fechaCreacion.AddMinutes(10);

        var primerCriterio = rubrica.AgregarCriterio(
            "Rendimiento académico",
            "Promedio ponderado.",
            60.00m,
            0.00m,
            20.00m,
            fechaModificacion,
            _usuarioId);

        var segundoCriterio = rubrica.AgregarCriterio(
            "Situación socioeconómica",
            "Ingreso familiar.",
            40.00m,
            0.00m,
            10.00m,
            fechaModificacion.AddMinutes(5),
            _usuarioId);

        Assert.Equal(2, rubrica.Criterios.Count);
        Assert.Equal(1, primerCriterio.Orden);
        Assert.Equal(2, segundoCriterio.Orden);
        Assert.Equal(rubrica.Id, primerCriterio.RubricaEvaluacionId);
        Assert.Equal(rubrica.Id, segundoCriterio.RubricaEvaluacionId);
        Assert.Equal(3, rubrica.Version);
    }

    [Fact]
    public void Debe_EditarElCriterioExistente_Cuando_LosDatosSonValidos()
    {
        var rubrica = CrearRubricaValida();
        var criterio = rubrica.AgregarCriterio(
            "Rendimiento académico",
            "Promedio ponderado.",
            60.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        var fechaEdicion = _fechaCreacion.AddMinutes(15);
        var usuarioEditorId = Guid.NewGuid();

        var criterioEditado = rubrica.EditarCriterio(
            criterio.Id,
            "  Rendimiento académico actualizado  ",
            "  Promedio ponderado general.  ",
            55.00m,
            1.00m,
            25.00m,
            fechaEdicion,
            usuarioEditorId);

        Assert.Same(criterio, criterioEditado);
        Assert.Equal("Rendimiento académico actualizado", criterio.Nombre);
        Assert.Equal("Promedio ponderado general.", criterio.Descripcion);
        Assert.Equal(55.00m, criterio.PesoPorcentual);
        Assert.Equal(1.00m, criterio.PuntajeMinimo);
        Assert.Equal(25.00m, criterio.PuntajeMaximo);
        Assert.Equal(1, criterio.Orden);
        Assert.Equal(fechaEdicion, rubrica.FechaUltimaModificacion);
        Assert.Equal(usuarioEditorId, rubrica.UsuarioModificacionId);
        Assert.Equal(3, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarLaEdicionDeCriterio_Cuando_ElCriterioNoExiste()
    {
        var rubrica = CrearRubricaValida();

        var excepcion = Assert.Throws<CriterioNoEncontradoException>(
            () => rubrica.EditarCriterio(
                Guid.NewGuid(),
                "Criterio",
                "Descripción",
                50.00m,
                0.00m,
                10.00m,
                _fechaCreacion.AddMinutes(5),
                _usuarioId));

        Assert.Equal(CriterioNoEncontradoException.CodigoError, excepcion.Codigo);
        Assert.Equal(404, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarLaEdicionDeCriterio_Cuando_LosDatosSonInvalidos()
    {
        var rubrica = CrearRubricaValida();
        var criterio = rubrica.AgregarCriterio(
            "Rendimiento académico",
            "Promedio ponderado.",
            60.00m,
            0.00m,
            20.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        Assert.Throws<CriterioEvaluacionInvalidoException>(
            () => rubrica.EditarCriterio(
                criterio.Id,
                "",
                "Descripción",
                50.00m,
                0.00m,
                10.00m,
                _fechaCreacion.AddMinutes(10),
                _usuarioId));

        Assert.Equal("Rendimiento académico", criterio.Nombre);
        Assert.Equal(60.00m, criterio.PesoPorcentual);
        Assert.Equal(2, rubrica.Version);
    }

    [Fact]
    public void Debe_EliminarElCriterioYAsignarSiguienteOrdenSobreElMaximo_Cuando_ElCriterioExiste()
    {
        var rubrica = CrearRubricaValida();
        var primero = rubrica.AgregarCriterio(
            "Criterio 1",
            "Descripción 1",
            50.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);
        var segundo = rubrica.AgregarCriterio(
            "Criterio 2",
            "Descripción 2",
            50.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(10),
            _usuarioId);

        rubrica.EliminarCriterio(primero.Id, _fechaCreacion.AddMinutes(15), _usuarioId);

        Assert.Single(rubrica.Criterios);
        Assert.Contains(segundo, rubrica.Criterios);
        Assert.Equal(2, segundo.Orden);

        var tercero = rubrica.AgregarCriterio(
            "Criterio 3",
            "Descripción 3",
            50.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(20),
            _usuarioId);

        Assert.Equal(3, tercero.Orden);
    }

    [Fact]
    public void Debe_RechazarLaEliminacionDeCriterio_Cuando_ElCriterioNoExiste()
    {
        var rubrica = CrearRubricaValida();

        var excepcion = Assert.Throws<CriterioNoEncontradoException>(
            () => rubrica.EliminarCriterio(
                Guid.NewGuid(),
                _fechaCreacion.AddMinutes(5),
                _usuarioId));

        Assert.Equal(CriterioNoEncontradoException.CodigoError, excepcion.Codigo);
        Assert.Equal(404, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_CalcularSumaConDecimalesExactos_Cuando_LosPesosSuman100()
    {
        var rubrica = CrearRubricaValida();

        rubrica.AgregarCriterio("Criterio 1", "Descripción 1", 33.33m, 0m, 10m, _fechaCreacion.AddMinutes(1), _usuarioId);
        rubrica.AgregarCriterio("Criterio 2", "Descripción 2", 33.33m, 0m, 10m, _fechaCreacion.AddMinutes(2), _usuarioId);
        rubrica.AgregarCriterio("Criterio 3", "Descripción 3", 33.34m, 0m, 10m, _fechaCreacion.AddMinutes(3), _usuarioId);

        Assert.Equal(100.00m, rubrica.SumaPesos);
        Assert.Equal(0.00m, rubrica.DiferenciaPara100);
    }

    [Fact]
    public void Debe_CalcularSumaCeroYDiferencia100_Cuando_LaRubricaNoTieneCriterios()
    {
        var rubrica = CrearRubricaValida();

        Assert.Equal(0.00m, rubrica.SumaPesos);
        Assert.Equal(100.00m, rubrica.DiferenciaPara100);
    }

    [Fact]
    public void Debe_CalcularDiferenciaPositiva_Cuando_LaSumaDePesosEsMenorQue100()
    {
        var rubrica = CrearRubricaValida();

        rubrica.AgregarCriterio("Criterio 1", "Descripción 1", 60.00m, 0m, 10m, _fechaCreacion.AddMinutes(1), _usuarioId);
        rubrica.AgregarCriterio("Criterio 2", "Descripción 2", 30.50m, 0m, 10m, _fechaCreacion.AddMinutes(2), _usuarioId);

        Assert.Equal(90.50m, rubrica.SumaPesos);
        Assert.Equal(9.50m, rubrica.DiferenciaPara100);
    }

    [Fact]
    public void Debe_CalcularDiferenciaNegativa_Cuando_LaSumaDePesosSupera100()
    {
        var rubrica = CrearRubricaValida();

        rubrica.AgregarCriterio("Criterio 1", "Descripción 1", 60.00m, 0m, 10m, _fechaCreacion.AddMinutes(1), _usuarioId);
        rubrica.AgregarCriterio("Criterio 2", "Descripción 2", 40.01m, 0m, 10m, _fechaCreacion.AddMinutes(2), _usuarioId);

        Assert.Equal(100.01m, rubrica.SumaPesos);
        Assert.Equal(-0.01m, rubrica.DiferenciaPara100);
    }

    [Fact]
    public void Debe_PublicarLaRubrica_Cuando_LaSumaDePesosEsExactamente100()
    {
        var rubrica = CrearRubricaValida();
        rubrica.AgregarCriterio("Criterio 1", "Descripción 1", 60.00m, 0m, 10m, _fechaCreacion.AddMinutes(1), _usuarioId);
        rubrica.AgregarCriterio("Criterio 2", "Descripción 2", 40.00m, 0m, 10m, _fechaCreacion.AddMinutes(2), _usuarioId);

        var fechaPublicacion = _fechaCreacion.AddHours(1);
        var usuarioPublicadorId = Guid.NewGuid();

        rubrica.Publicar(fechaPublicacion, usuarioPublicadorId);

        Assert.Equal(EstadoRubrica.Publicada, rubrica.Estado);
        Assert.Equal(fechaPublicacion, rubrica.FechaPublicacion);
        Assert.Null(rubrica.MotivoBloqueo);
        Assert.Null(rubrica.FechaBloqueo);
        Assert.Equal(fechaPublicacion, rubrica.FechaUltimaModificacion);
        Assert.Equal(usuarioPublicadorId, rubrica.UsuarioModificacionId);
        Assert.Equal(4, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarLaPublicacion_Cuando_FaltanPuntosParaLlegarA100()
    {
        var rubrica = CrearRubricaValida();
        rubrica.AgregarCriterio("Criterio 1", "Descripción 1", 50.00m, 0m, 10m, _fechaCreacion.AddMinutes(1), _usuarioId);
        rubrica.AgregarCriterio("Criterio 2", "Descripción 2", 49.99m, 0m, 10m, _fechaCreacion.AddMinutes(2), _usuarioId);

        var excepcion = Assert.Throws<PesosInvalidosException>(
            () => rubrica.Publicar(_fechaCreacion.AddHours(1), _usuarioId));

        Assert.Equal(PesosInvalidosException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(99.99m, rubrica.SumaPesos);
        Assert.Equal(0.01m, rubrica.DiferenciaPara100);
        Assert.Equal(0.01m, excepcion.DiferenciaPara100);
        Assert.Contains("faltan 0.01", excepcion.Message, StringComparison.Ordinal);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
        Assert.Null(rubrica.FechaPublicacion);
    }

    [Fact]
    public void Debe_RechazarLaPublicacion_Cuando_LaSumaDePesosExcede100()
    {
        var rubrica = CrearRubricaValida();
        rubrica.AgregarCriterio("Criterio 1", "Descripción 1", 60.00m, 0m, 10m, _fechaCreacion.AddMinutes(1), _usuarioId);
        rubrica.AgregarCriterio("Criterio 2", "Descripción 2", 40.01m, 0m, 10m, _fechaCreacion.AddMinutes(2), _usuarioId);

        var excepcion = Assert.Throws<PesosInvalidosException>(
            () => rubrica.Publicar(_fechaCreacion.AddHours(1), _usuarioId));

        Assert.Equal(PesosInvalidosException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(100.01m, rubrica.SumaPesos);
        Assert.Equal(-0.01m, rubrica.DiferenciaPara100);
        Assert.Equal(-0.01m, excepcion.DiferenciaPara100);
        Assert.Contains("excede por 0.01", excepcion.Message, StringComparison.Ordinal);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
        Assert.Null(rubrica.FechaPublicacion);
    }

    [Fact]
    public void Debe_RechazarLaPublicacion_Cuando_LaRubricaNoTieneCriterios()
    {
        var rubrica = CrearRubricaValida();

        var excepcion = Assert.Throws<PesosInvalidosException>(
            () => rubrica.Publicar(_fechaCreacion.AddHours(1), _usuarioId));

        Assert.Equal(PesosInvalidosException.CodigoError, excepcion.Codigo);
        Assert.Equal(422, excepcion.EstadoHttp);
        Assert.Equal(0.00m, rubrica.SumaPesos);
        Assert.Equal(100.00m, rubrica.DiferenciaPara100);
        Assert.Equal(100.00m, excepcion.DiferenciaPara100);
        Assert.Contains("faltan 100.00", excepcion.Message, StringComparison.Ordinal);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
        Assert.Null(rubrica.FechaPublicacion);
    }

    [Theory]
    [InlineData(MotivoBloqueoRubrica.InicioEvaluacion)]
    [InlineData(MotivoBloqueoRubrica.PrimeraCalificacion)]
    public void Debe_BloquearLaRubricaConMotivoFechaUtcYAuditoria_Cuando_EstaPublicada(
        MotivoBloqueoRubrica motivo)
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(EstadoRubrica.Publicada);
        var versionPrevia = rubrica.Version;
        var fechaBloqueoConOffset = new DateTimeOffset(2026, 11, 22, 10, 15, 0, TimeSpan.FromHours(-4));

        var cambioAplicado = rubrica.Bloquear(motivo, fechaBloqueoConOffset, UsuarioSistema.Id);

        Assert.True(cambioAplicado);
        Assert.Equal(EstadoRubrica.Bloqueada, rubrica.Estado);
        Assert.Equal(motivo, rubrica.MotivoBloqueo);
        Assert.Equal(fechaBloqueoConOffset.ToUniversalTime(), rubrica.FechaBloqueo);
        Assert.Equal(TimeSpan.Zero, rubrica.FechaBloqueo!.Value.Offset);
        Assert.Equal(fechaBloqueoConOffset.ToUniversalTime(), rubrica.FechaUltimaModificacion);
        Assert.Equal(UsuarioSistema.Id, rubrica.UsuarioModificacionId);
        Assert.Equal(versionPrevia + 1, rubrica.Version);
    }

    [Fact]
    public void Debe_SerIdempotenteSinCambios_Cuando_SeBloqueaUnaRubricaYaBloqueada()
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(EstadoRubrica.Publicada);
        var fechaPrimerBloqueo = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

        var primerResultado = rubrica.Bloquear(
            MotivoBloqueoRubrica.InicioEvaluacion,
            fechaPrimerBloqueo,
            UsuarioSistema.Id);
        var versionTrasPrimerBloqueo = rubrica.Version;

        var segundoResultado = rubrica.Bloquear(
            MotivoBloqueoRubrica.PrimeraCalificacion,
            fechaPrimerBloqueo.AddHours(2),
            Guid.NewGuid());

        Assert.True(primerResultado);
        Assert.False(segundoResultado);
        Assert.Equal(EstadoRubrica.Bloqueada, rubrica.Estado);
        Assert.Equal(MotivoBloqueoRubrica.InicioEvaluacion, rubrica.MotivoBloqueo);
        Assert.Equal(fechaPrimerBloqueo, rubrica.FechaBloqueo);
        Assert.Equal(fechaPrimerBloqueo, rubrica.FechaUltimaModificacion);
        Assert.Equal(UsuarioSistema.Id, rubrica.UsuarioModificacionId);
        Assert.Equal(versionTrasPrimerBloqueo, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarElBloqueoConRubricaNoPublicada_Cuando_LaRubricaEstaEnBorrador()
    {
        var rubrica = CrearRubricaValida();
        var versionInicial = rubrica.Version;

        var excepcion = Assert.Throws<RubricaNoPublicadaException>(
            () => rubrica.Bloquear(
                MotivoBloqueoRubrica.InicioEvaluacion,
                _fechaCreacion.AddHours(1),
                UsuarioSistema.Id));

        Assert.Equal(RubricaNoPublicadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
        Assert.Null(rubrica.MotivoBloqueo);
        Assert.Null(rubrica.FechaBloqueo);
        Assert.Equal(versionInicial, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarElBloqueo_Cuando_ElMotivoNoEstaDefinido()
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(EstadoRubrica.Publicada);
        var versionPrevia = rubrica.Version;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => rubrica.Bloquear(
                (MotivoBloqueoRubrica)999,
                _fechaCreacion.AddHours(1),
                UsuarioSistema.Id));

        Assert.Equal(EstadoRubrica.Publicada, rubrica.Estado);
        Assert.Null(rubrica.MotivoBloqueo);
        Assert.Null(rubrica.FechaBloqueo);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarElBloqueo_Cuando_LaFechaEsPorDefecto()
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(EstadoRubrica.Publicada);
        var versionPrevia = rubrica.Version;

        Assert.Throws<ArgumentException>(
            () => rubrica.Bloquear(
                MotivoBloqueoRubrica.InicioEvaluacion,
                default,
                UsuarioSistema.Id));

        Assert.Equal(EstadoRubrica.Publicada, rubrica.Estado);
        Assert.Null(rubrica.MotivoBloqueo);
        Assert.Null(rubrica.FechaBloqueo);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Fact]
    public void Debe_RechazarElBloqueo_Cuando_ElUsuarioEstaVacio()
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(EstadoRubrica.Publicada);
        var versionPrevia = rubrica.Version;

        Assert.Throws<ArgumentException>(
            () => rubrica.Bloquear(
                MotivoBloqueoRubrica.InicioEvaluacion,
                _fechaCreacion.AddHours(1),
                Guid.Empty));

        Assert.Equal(EstadoRubrica.Publicada, rubrica.Estado);
        Assert.Null(rubrica.MotivoBloqueo);
        Assert.Null(rubrica.FechaBloqueo);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Theory]
    [InlineData(EstadoRubrica.Publicada)]
    [InlineData(EstadoRubrica.Bloqueada)]
    public void Debe_RechazarAgregarCriterioConRubricaBloqueada_Cuando_LaRubricaNoEstaEnBorrador(
        EstadoRubrica estadoInmutable)
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(estadoInmutable);
        var versionPrevia = rubrica.Version;

        var excepcion = Assert.Throws<RubricaBloqueadaException>(
            () => rubrica.AgregarCriterio(
                "Nuevo criterio",
                "Descripción",
                10.00m,
                0.00m,
                10.00m,
                _fechaCreacion.AddHours(2),
                _usuarioId));

        Assert.Equal(RubricaBloqueadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        AssertMensajeInmutabilidadEsperado(estadoInmutable, excepcion);
        Assert.Single(rubrica.Criterios);
        Assert.Equal(estadoInmutable, rubrica.Estado);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Theory]
    [InlineData(EstadoRubrica.Publicada)]
    [InlineData(EstadoRubrica.Bloqueada)]
    public void Debe_RechazarEditarCriterioConRubricaBloqueada_Cuando_LaRubricaNoEstaEnBorrador(
        EstadoRubrica estadoInmutable)
    {
        var (rubrica, criterioId) = CrearRubricaEnEstadoInmutable(estadoInmutable);
        var versionPrevia = rubrica.Version;

        var excepcion = Assert.Throws<RubricaBloqueadaException>(
            () => rubrica.EditarCriterio(
                criterioId,
                "Criterio editado",
                "Descripción editada",
                100.00m,
                0.00m,
                20.00m,
                _fechaCreacion.AddHours(2),
                _usuarioId));

        Assert.Equal(RubricaBloqueadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        AssertMensajeInmutabilidadEsperado(estadoInmutable, excepcion);
        Assert.Equal("Criterio único", rubrica.Criterios.Single().Nombre);
        Assert.Equal(estadoInmutable, rubrica.Estado);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Theory]
    [InlineData(EstadoRubrica.Publicada)]
    [InlineData(EstadoRubrica.Bloqueada)]
    public void Debe_RechazarEliminarCriterioConRubricaBloqueada_Cuando_LaRubricaNoEstaEnBorrador(
        EstadoRubrica estadoInmutable)
    {
        var (rubrica, criterioId) = CrearRubricaEnEstadoInmutable(estadoInmutable);
        var versionPrevia = rubrica.Version;

        var excepcion = Assert.Throws<RubricaBloqueadaException>(
            () => rubrica.EliminarCriterio(
                criterioId,
                _fechaCreacion.AddHours(2),
                _usuarioId));

        Assert.Equal(RubricaBloqueadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        AssertMensajeInmutabilidadEsperado(estadoInmutable, excepcion);
        Assert.Single(rubrica.Criterios);
        Assert.Equal(estadoInmutable, rubrica.Estado);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Theory]
    [InlineData(EstadoRubrica.Publicada)]
    [InlineData(EstadoRubrica.Bloqueada)]
    public void Debe_RechazarPublicarConRubricaBloqueada_Cuando_LaRubricaNoEstaEnBorrador(
        EstadoRubrica estadoInmutable)
    {
        var (rubrica, _) = CrearRubricaEnEstadoInmutable(estadoInmutable);
        var versionPrevia = rubrica.Version;

        var excepcion = Assert.Throws<RubricaBloqueadaException>(
            () => rubrica.Publicar(_fechaCreacion.AddHours(2), _usuarioId));

        Assert.Equal(RubricaBloqueadaException.CodigoError, excepcion.Codigo);
        Assert.Equal(409, excepcion.EstadoHttp);
        AssertMensajeInmutabilidadEsperado(estadoInmutable, excepcion);
        Assert.Equal(estadoInmutable, rubrica.Estado);
        Assert.Equal(versionPrevia, rubrica.Version);
    }

    [Fact]
    public void Debe_OcultarActualizarEnCriterioEvaluacion_Cuando_SeInspeccionaLaSuperficiePublica()
    {
        var metodoPublico = typeof(CriterioEvaluacion).GetMethod(
            nameof(CriterioEvaluacion.Actualizar),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        var metodoInterno = typeof(CriterioEvaluacion).GetMethod(
            nameof(CriterioEvaluacion.Actualizar),
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Null(metodoPublico);
        Assert.NotNull(metodoInterno);
        Assert.False(metodoInterno.IsPublic);
        Assert.True(metodoInterno.IsAssembly);
    }

    private static void AssertMensajeInmutabilidadEsperado(
        EstadoRubrica estadoInmutable,
        RubricaBloqueadaException excepcion)
    {
        if (estadoInmutable == EstadoRubrica.Publicada)
        {
            Assert.Equal(
                "La rúbrica está publicada y no admite modificaciones; los cambios requieren una rúbrica nueva.",
                excepcion.Message);
        }
        else
        {
            Assert.Equal(
                "La rúbrica está bloqueada desde 2026-11-22 14:30:00 UTC por inicio de la etapa de evaluación; los cambios requieren una rúbrica nueva.",
                excepcion.Message);
        }
    }

    private static (RubricaEvaluacion Rubrica, Guid CriterioId) CrearRubricaEnEstadoInmutable(
        EstadoRubrica estadoInmutable)
    {
        var rubrica = CrearRubricaValida();
        var criterio = rubrica.AgregarCriterio(
            "Criterio único",
            "Cubre el 100%.",
            100.00m,
            0.00m,
            10.00m,
            _fechaCreacion.AddMinutes(5),
            _usuarioId);

        rubrica.Publicar(_fechaCreacion.AddMinutes(10), _usuarioId);

        if (estadoInmutable == EstadoRubrica.Bloqueada)
        {
            rubrica.Bloquear(
                MotivoBloqueoRubrica.InicioEvaluacion,
                _fechaBloqueo,
                UsuarioSistema.Id);
        }

        return (rubrica, criterio.Id);
    }

    private static RubricaEvaluacion CrearRubricaValida(string nombre = "Rúbrica General 2026-II")
        => RubricaEvaluacion.Crear(
            _programaBecaId,
            _convocatoriaId,
            nombre,
            _fechaCreacion,
            _usuarioId);
}
