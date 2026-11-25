using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Asignaciones.DTOs;

public sealed class ExcepcionEvaluadorUnicoMapeadorTests
{
    private static readonly Guid _postulacionId = Guid.Parse("01967000-0000-7000-8000-000000000601");
    private static readonly Guid _coordinadorId = Guid.Parse("01967000-0000-7000-8000-000000000602");
    private static readonly Guid _correlacionId = Guid.Parse("01967000-0000-7000-8000-000000000603");
    private static readonly DateTimeOffset _fechaLocal =
        new(2026, 11, 24, 15, 45, 0, TimeSpan.FromHours(-4));

    [Fact]
    public void Debe_MapearUsuarioAutorizadorIdYFechaAutorizacionUtc_Cuando_SeConvierteADto()
    {
        var autorizacion = AutorizacionExcepcionEvaluadorUnico.Crear(
            _postulacionId,
            "  Solo existe un evaluador sin conflicto en el comité.  ",
            _fechaLocal,
            _coordinadorId,
            _correlacionId);

        var dto = autorizacion.ADto();

        dto.Id.Should().Be(autorizacion.Id);
        dto.PostulacionId.Should().Be(_postulacionId);
        dto.Justificacion.Should().Be("Solo existe un evaluador sin conflicto en el comité.");
        dto.UsuarioAutorizadorId.Should().Be(_coordinadorId);
        dto.FechaAutorizacion.Should().Be(new DateTimeOffset(2026, 11, 24, 19, 45, 0, TimeSpan.Zero));
        dto.FechaAutorizacion.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Debe_LanzarArgumentNullException_Cuando_LaEntidadEsNula()
    {
        AutorizacionExcepcionEvaluadorUnico autorizacionNula = null!;

        var accion = () => autorizacionNula.ADto();

        accion.Should().Throw<ArgumentNullException>();
    }
}
