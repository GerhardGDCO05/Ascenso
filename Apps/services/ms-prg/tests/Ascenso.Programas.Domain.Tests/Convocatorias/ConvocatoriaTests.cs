using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Convocatorias.Entidades;
using Ascenso.Programas.Domain.Convocatorias.Eventos;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using FluentAssertions;

namespace Ascenso.Programas.Domain.Tests.Convocatorias;

public sealed class ConvocatoriaTests
{
    private static readonly Guid _programaId = Guid.Parse("01966000-0000-7000-8000-000000000101");
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000201");
    private static readonly Guid _universidadId = Guid.Parse("01966000-0000-7000-8000-000000000301");
    private static readonly Guid _carreraId = Guid.Parse("01966000-0000-7000-8000-000000000401");
    private static readonly DateTimeOffset _fechaApertura = new(2026, 12, 1, 4, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaCierre = new(2026, 12, 21, 3, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaSubsanacion = new(2026, 12, 23, 3, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaInicioEvaluacion = new(2026, 12, 23, 4, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaCierreEvaluacion = new(2026, 12, 31, 3, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaCreacion = new(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_SerializarLosEstadosComoCodigosMayusculos()
    {
        EstadoConvocatoria.Borrador.ACodigo().Should().Be("BORRADOR");
        EstadoConvocatoria.Publicada.ACodigo().Should().Be("PUBLICADA");
        EstadoConvocatoria.Cerrada.ACodigo().Should().Be("CERRADA");
        EstadoConvocatoria.Cancelada.ACodigo().Should().Be("CANCELADA");
        CodigoEnum.DesdeCodigo<EstadoConvocatoria>("PUBLICADA").Should().Be(EstadoConvocatoria.Publicada);
    }

    [Fact]
    public void Debe_CrearConvocatoriaBorrador_ConCodigoNormalizadoYUuidV7()
    {
        var aperturaCaracas = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(-4));
        var cierreCaracas = new DateTimeOffset(2026, 12, 20, 23, 59, 59, TimeSpan.FromHours(-4));
        var convocatoria = CrearConvocatoria(
            fechaApertura: aperturaCaracas,
            fechaCierre: cierreCaracas);

        convocatoria.Id.Should().NotBe(Guid.Empty);
        UuidV7.EsVersion7(convocatoria.Id).Should().BeTrue();
        convocatoria.Codigo.Should().Be("RF21-2026");
        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
        convocatoria.FechaApertura.Offset.Should().Be(TimeSpan.Zero);
        convocatoria.FechaCierre.Offset.Should().Be(TimeSpan.Zero);
        convocatoria.FechaApertura.Should().Be(aperturaCaracas.ToUniversalTime());
        convocatoria.FechaCierre.Should().Be(cierreCaracas.ToUniversalTime());
    }

    [Fact]
    public void Debe_ExponerSoloPropiedadesSinSetterPublico()
    {
        typeof(Convocatoria)
            .GetProperties()
            .Where(propiedad => propiedad.SetMethod?.IsPublic == true)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void Debe_AgregarYQuitarDocumentoRequerido_MientrasPermaneceEnBorrador()
    {
        var convocatoria = CrearConvocatoria();
        var documento = convocatoria.AgregarDocumento(
            " identidad ",
            " Cédula ",
            " Documento vigente ",
            obligatorio: true,
            ["APPLICATION/PDF", "application/pdf"],
            tamanioMaximoMb: 5,
            _usuarioId,
            _fechaCreacion);

        documento.ConvocatoriaId.Should().Be(convocatoria.Id);
        documento.TipoDocumento.Should().Be("IDENTIDAD");
        documento.Nombre.Should().Be("Cédula");
        documento.Descripcion.Should().Be("Documento vigente");
        documento.Obligatorio.Should().BeTrue();
        documento.TiposMimePermitidos.Should().ContainSingle("application/pdf");
        convocatoria.DocumentosRequeridos.Should().ContainSingle().Which.Should().Be(documento);

        convocatoria.QuitarDocumento(documento.Id, _usuarioId, _fechaCreacion.AddMinutes(1));

        convocatoria.DocumentosRequeridos.Should().BeEmpty();
    }

    [Fact]
    public void Debe_RechazarDocumentoInvalido_Cuando_FaltaUnDatoRequerido()
    {
        var convocatoria = CrearConvocatoria();

        var sinTipo = () => convocatoria.AgregarDocumento(
            " ", "Identidad", null, true, ["application/pdf"], 5, _usuarioId, _fechaCreacion);
        var sinNombre = () => convocatoria.AgregarDocumento(
            "IDENTIDAD", " ", null, true, ["application/pdf"], 5, _usuarioId, _fechaCreacion);
        var sinMime = () => convocatoria.AgregarDocumento(
            "IDENTIDAD", "Identidad", null, true, [], 5, _usuarioId, _fechaCreacion);
        var sinTamanio = () => convocatoria.AgregarDocumento(
            "IDENTIDAD", "Identidad", null, true, ["application/pdf"], 0, _usuarioId, _fechaCreacion);

        sinTipo.Should().Throw<DocumentoRequeridoInvalidoException>();
        sinNombre.Should().Throw<DocumentoRequeridoInvalidoException>();
        sinMime.Should().Throw<DocumentoRequeridoInvalidoException>();
        sinTamanio.Should().Throw<DocumentoRequeridoInvalidoException>();
        convocatoria.DocumentosRequeridos.Should().BeEmpty();
    }

    [Fact]
    public void Debe_RechazarLaCronologiaInvalida_Cuando_SeIncumpleCualquieraDeSusReglas()
    {
        var escenarios = new Action[]
        {
            () => CrearConvocatoria(fechaCierre: _fechaApertura),
            () => CrearConvocatoria(fechaLimiteSubsanacion: _fechaCierre.AddSeconds(-1)),
            () => CrearConvocatoria(fechaInicioEvaluacion: _fechaCierreEvaluacion),
            () => CrearConvocatoria(fechaCierreEvaluacion: _fechaSubsanacion),
            () => CrearConvocatoria(fechaInicioEvaluacion: _fechaApertura, requiereRevisionPrevia: true),
            () => CrearConvocatoria(
                fechaInicioEvaluacion: _fechaApertura.AddSeconds(-1),
                requiereRevisionPrevia: false)
        };

        foreach (var escenario in escenarios)
        {
            escenario.Should().Throw<FechasConvocatoriaInvalidasException>();
        }
    }

    [Fact]
    public void Debe_PermitirEvaluacionParalelaDesdeLaApertura_Cuando_NoRequiereRevisionPrevia()
    {
        var convocatoria = CrearConvocatoria(
            fechaInicioEvaluacion: _fechaApertura,
            requiereRevisionPrevia: false);

        convocatoria.RequiereRevisionDocumentalPrevia.Should().BeFalse();
        convocatoria.FechaInicioEvaluacion.Should().Be(_fechaApertura);
    }

    [Fact]
    public void Debe_PermitirInicioEvaluacionIgualAlCierre_Cuando_RequiereRevisionPrevia()
    {
        var convocatoria = CrearConvocatoria(fechaInicioEvaluacion: _fechaCierre);

        convocatoria.FechaInicioEvaluacion.Should().Be(_fechaCierre);
        convocatoria.RequiereRevisionDocumentalPrevia.Should().BeTrue();
    }

    [Fact]
    public void Debe_PermitirLimiteSubsanacionIgualAlCierre()
    {
        var convocatoria = CrearConvocatoria(fechaLimiteSubsanacion: _fechaCierre);

        convocatoria.FechaLimiteSubsanacionDocumentos.Should().Be(_fechaCierre);
    }

    [Fact]
    public void Debe_RechazarPromocionSinDocumentosYConCierreVencido_SinCambiarElEstado()
    {
        var convocatoria = CrearConvocatoria();

        var sinDocumentos = () => convocatoria.Publicar(_fechaCreacion, _usuarioId);
        sinDocumentos.Should().Throw<ConvocatoriaSinDocumentosException>();
        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);

        AgregarDocumento(convocatoria);
        var cierreVencido = () => convocatoria.Publicar(_fechaCierre, _usuarioId);
        cierreVencido.Should().Throw<ConvocatoriaFechaCierreVencidaException>();
        convocatoria.Estado.Should().Be(EstadoConvocatoria.Borrador);
    }

    [Fact]
    public void Debe_PublicarBorradorConDocumentoYEmitirEvento()
    {
        var convocatoria = CrearConvocatoria();
        AgregarDocumento(convocatoria);

        convocatoria.Publicar(_fechaCreacion, _usuarioId);

        convocatoria.Estado.Should().Be(EstadoConvocatoria.Publicada);
        convocatoria.FechaPublicacion.Should().Be(_fechaCreacion);
        convocatoria.EventosDominio.OfType<ConvocatoriaPublicadaDominioEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Debe_RechazarEdicionYDocumentos_Cuando_LaConvocatoriaYaFuePublicada()
    {
        var convocatoria = CrearConvocatoria();
        AgregarDocumento(convocatoria);
        convocatoria.Publicar(_fechaCreacion, _usuarioId);

        var editar = () => convocatoria.Actualizar(
            "2026-I",
            "Nombre modificado",
            _fechaApertura,
            _fechaCierre,
            _fechaSubsanacion,
            _fechaInicioEvaluacion,
            _fechaCierreEvaluacion,
            true,
            4,
            CrearCriterios(),
            _usuarioId);
        var agregarDocumento = () => AgregarDocumento(convocatoria);

        editar.Should().Throw<ConvocatoriaNoEditableException>();
        agregarDocumento.Should().Throw<ConvocatoriaNoEditableException>();
        convocatoria.Nombre.Should().Be("Convocatoria de prueba");
    }

    [Fact]
    public void Debe_ActualizarBorradorYIncrementarAuditoria_Cuando_LosDatosSonValidos()
    {
        var convocatoria = CrearConvocatoria();
        var versionAnterior = convocatoria.Version;

        convocatoria.Actualizar(
            " 2027-I ",
            " Nueva convocatoria ",
            _fechaApertura,
            _fechaCierre,
            _fechaSubsanacion,
            _fechaInicioEvaluacion,
            _fechaCierreEvaluacion,
            true,
            4,
            CrearCriterios(),
            _usuarioId,
            _fechaCreacion.AddMinutes(5));

        convocatoria.PeriodoAcademicoId.Should().Be("2027-I");
        convocatoria.Nombre.Should().Be("Nueva convocatoria");
        convocatoria.CuposAsignados.Should().Be(4);
        convocatoria.Version.Should().Be(versionAnterior + 1);
    }

    [Fact]
    public void Debe_CerrarSoloCuandoLaFechaDeCierreFueAlcanzada_YEmitirUnEvento()
    {
        var convocatoria = CrearConvocatoria();
        AgregarDocumento(convocatoria);
        convocatoria.Publicar(_fechaCreacion, _usuarioId);

        convocatoria.Cerrar(_fechaCierre.AddSeconds(-1), _usuarioId).Should().BeFalse();
        convocatoria.Estado.Should().Be(EstadoConvocatoria.Publicada);

        convocatoria.Cerrar(_fechaCierre, _usuarioId).Should().BeTrue();
        convocatoria.Estado.Should().Be(EstadoConvocatoria.Cerrada);
        convocatoria.EventosDominio.OfType<ConvocatoriaCerradaDominioEvent>().Should().ContainSingle();
        convocatoria.Cerrar(_fechaCierre.AddMinutes(1), _usuarioId).Should().BeFalse();
    }

    [Fact]
    public void Debe_NoMutarElAgregado_Cuando_LaAuditoriaDeActualizacionEsInvalida()
    {
        var convocatoria = CrearConvocatoria();
        var nombreOriginal = convocatoria.Nombre;
        var versionOriginal = convocatoria.Version;

        var accion = () => convocatoria.Actualizar(
            "2027-I",
            "Nombre nuevo",
            _fechaApertura,
            _fechaCierre,
            _fechaSubsanacion,
            _fechaInicioEvaluacion,
            _fechaCierreEvaluacion,
            true,
            4,
            CrearCriterios(),
            Guid.Empty);

        accion.Should().Throw<ArgumentException>();
        convocatoria.Nombre.Should().Be(nombreOriginal);
        convocatoria.Version.Should().Be(versionOriginal);
    }

    private static Convocatoria CrearConvocatoria(
        DateTimeOffset? fechaApertura = null,
        DateTimeOffset? fechaCierre = null,
        DateTimeOffset? fechaLimiteSubsanacion = null,
        DateTimeOffset? fechaInicioEvaluacion = null,
        DateTimeOffset? fechaCierreEvaluacion = null,
        bool requiereRevisionPrevia = true)
        => Convocatoria.CrearBorrador(
            _programaId,
            "2026-I",
            " rf21-2026 ",
            "Convocatoria de prueba",
            fechaApertura ?? _fechaApertura,
            fechaCierre ?? _fechaCierre,
            fechaLimiteSubsanacion ?? _fechaSubsanacion,
            fechaInicioEvaluacion ?? _fechaInicioEvaluacion,
            fechaCierreEvaluacion ?? _fechaCierreEvaluacion,
            requiereRevisionPrevia,
            5,
            CrearCriterios(),
            _usuarioId,
            _fechaCreacion);

    private static CriteriosElegibilidadCiclo CrearCriterios()
        => new(15.00m, 18, 35, 1200.00m, [_universidadId], [_carreraId]);

    private static DocumentoRequerido AgregarDocumento(Convocatoria convocatoria)
        => convocatoria.AgregarDocumento(
            "IDENTIDAD",
            "Documento de identidad",
            null,
            obligatorio: true,
            ["application/pdf"],
            tamanioMaximoMb: 5,
            _usuarioId,
            _fechaCreacion);
}
