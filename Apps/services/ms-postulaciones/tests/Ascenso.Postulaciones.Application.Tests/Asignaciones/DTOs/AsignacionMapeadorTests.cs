using System.Reflection;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.DTOs;

public sealed class AsignacionMapeadorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01967000-0000-7000-8000-000000000301");
    private static readonly Guid _convocatoriaId = Guid.Parse("01967000-0000-7000-8000-000000000302");
    private static readonly Guid _jefaturaId = Guid.Parse("01967000-0000-7000-8000-000000000303");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000304");
    private static readonly Guid _evaluadorId = Guid.Parse("01967000-0000-7000-8000-000000000305");
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000306");
    private static readonly DateTimeOffset _fechaCreacionComite =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaAsignacion =
        new(2026, 11, 24, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _fechaLimite =
        new(2026, 12, 10, 23, 59, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearAsignacionEvaluacionDtoConCodigoFechasYVersion_Cuando_SeConvierteADto()
    {
        var (comite, asignacion) = CrearAsignacionDePrueba();

        var dto = asignacion.ADto();

        dto.Id.Should().Be(asignacion.Id);
        dto.ComiteEvaluadorId.Should().Be(comite.Id);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.PostulacionId.Should().Be(_postulacionId);
        dto.MiembroComiteId.Should().Be(asignacion.MiembroComiteId);
        dto.UsuarioEvaluadorId.Should().Be(_evaluadorId);
        dto.Estado.Should().Be("PENDIENTE");
        dto.FechaAsignacion.Should().Be(_fechaAsignacion);
        dto.FechaLimiteEvaluacion.Should().Be(_fechaLimite);
        dto.Version.Should().Be(1);
    }

    [Fact]
    public void Debe_MapearMiAsignacionDtoConCamposCiegos_Cuando_SeConvierteAMiAsignacionDto()
    {
        var (_, asignacion) = CrearAsignacionDePrueba();

        var dto = asignacion.AMiAsignacionDto();

        dto.AsignacionEvaluacionId.Should().Be(asignacion.Id);
        dto.PostulacionId.Should().Be(_postulacionId);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.Estado.Should().Be("PENDIENTE");
        dto.FechaAsignacion.Should().Be(_fechaAsignacion);
        dto.FechaLimiteEvaluacion.Should().Be(_fechaLimite);
    }

    [Fact]
    public void Debe_MapearCargaEvaluadorDtoConCodigosDeTipoYRol_Cuando_SeConvierteACargaDto()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Excelencia",
            _jefaturaId,
            _fechaCreacionComite,
            _coordinadorId);

        var miembroExterno = comite.AgregarMiembro(
            _evaluadorId,
            TipoEvaluador.Externo,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);

        var dto = miembroExterno.ACargaDto(3);

        dto.MiembroComiteId.Should().Be(miembroExterno.Id);
        dto.UsuarioEvaluadorId.Should().Be(_evaluadorId);
        dto.Tipo.Should().Be("EXTERNO");
        dto.Rol.Should().Be("EVALUADOR");
        dto.AsignacionesAbiertas.Should().Be(3);
    }

    [Fact]
    public void Debe_ExponerSoloLosCamposCiegosPermitidos_Cuando_SeInspeccionaMiAsignacionDtoPorReflexion()
    {
        var propiedades = typeof(MiAsignacionDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(propiedad => propiedad.Name)
            .ToArray();

        propiedades.Should().BeEquivalentTo(
            [
                nameof(MiAsignacionDto.AsignacionEvaluacionId),
                nameof(MiAsignacionDto.PostulacionId),
                nameof(MiAsignacionDto.ConvocatoriaId),
                nameof(MiAsignacionDto.Estado),
                nameof(MiAsignacionDto.FechaAsignacion),
                nameof(MiAsignacionDto.FechaLimiteEvaluacion)
            ]);

        var fragmentosProhibidos = new[]
        {
            "Coevaluador",
            "Evaluador",
            "Miembro",
            "Comite",
            "Postulante",
            "Nombre",
            "Documento",
            "Correo"
        };

        foreach (var propiedad in propiedades)
        {
            foreach (var prohibido in fragmentosProhibidos)
            {
                propiedad.Contains(prohibido, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"MiAsignacionDto no debe exponer '{propiedad}' por evaluación ciega.");
            }
        }
    }

    [Fact]
    public void Debe_ConstruirContenedoresAsignacionesPostulacionYPaginaDto()
    {
        var (_, asignacion) = CrearAsignacionDePrueba();
        var item = asignacion.ADto();

        var contenedor = new AsignacionesPostulacionDto(_postulacionId, [item]);
        contenedor.PostulacionId.Should().Be(_postulacionId);
        contenedor.Asignaciones.Should().ContainSingle().Which.Should().Be(item);

        var pagina = new PaginaDto<MiAsignacionDto>([asignacion.AMiAsignacionDto()], 1, 20, 1);
        pagina.Items.Should().ContainSingle();
        pagina.Pagina.Should().Be(1);
        pagina.Tamano.Should().Be(20);
        pagina.Total.Should().Be(1);
    }

    private static (ComiteEvaluador Comite, AsignacionEvaluacion Asignacion) CrearAsignacionDePrueba()
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
            TipoEvaluador.Interno,
            _fechaCreacionComite.AddDays(1),
            _coordinadorId);

        var asignacion = AsignacionEvaluacion.Crear(
            comite,
            miembro.Id,
            _postulacionId,
            _fechaAsignacion,
            _fechaLimite,
            _coordinadorId);

        return (comite, asignacion);
    }
}
