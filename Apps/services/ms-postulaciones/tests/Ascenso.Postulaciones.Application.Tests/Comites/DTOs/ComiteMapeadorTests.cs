using Ascenso.Postulaciones.Application.Comites.DTOs;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Enums;
using FluentAssertions;

namespace Ascenso.Postulaciones.Application.Tests.Comites.DTOs;

public sealed class ComiteMapeadorTests
{
    private static readonly Guid _programaBecaId = Guid.Parse("01966000-0000-7000-8000-000000000501");
    private static readonly Guid _convocatoriaId = Guid.Parse("01966000-0000-7000-8000-000000000502");
    private static readonly Guid _jefaturaId = Guid.Parse("01966000-0000-7000-8000-000000000503");
    private static readonly Guid _coordinadorId = Guid.Parse("01966000-0000-7000-8000-000000000504");
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_MapearComiteRecienCreadoConJefatura_Cuando_SeConvierteADto()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Beca Excelencia 2026-II",
            _jefaturaId,
            _fechaCreacion,
            _coordinadorId);

        var dto = comite.ADto();

        dto.Id.Should().Be(comite.Id);
        dto.ProgramaBecaId.Should().Be(_programaBecaId);
        dto.ConvocatoriaId.Should().Be(_convocatoriaId);
        dto.Nombre.Should().Be("Comité Beca Excelencia 2026-II");
        dto.JefaturaUsuarioId.Should().Be(_jefaturaId);
        dto.FechaConformacion.Should().Be(new DateOnly(2026, 11, 22));
        dto.ConformadoPorUsuarioId.Should().Be(_coordinadorId);
        dto.Activo.Should().BeTrue();
        dto.Version.Should().Be(1);

        dto.Miembros.Should().ContainSingle();
        var jefatura = dto.Miembros[0];
        jefatura.UsuarioEvaluadorId.Should().Be(_jefaturaId);
        jefatura.Tipo.Should().Be("INTERNO");
        jefatura.Rol.Should().Be("JEFATURA");
        jefatura.FechaAsignacion.Should().Be(new DateOnly(2026, 11, 22));
        jefatura.FechaDesvinculacion.Should().BeNull();
        jefatura.Activo.Should().BeTrue();
    }

    [Fact]
    public void Debe_MapearMiembrosActivosYDesvinculadosConCodigosYVersion_Cuando_TieneHistorial()
    {
        var comite = ComiteEvaluador.Crear(
            _programaBecaId,
            _convocatoriaId,
            "Comité Socioeconómico 2026-II",
            _jefaturaId,
            _fechaCreacion,
            _coordinadorId);

        var evaluadorInternoId = Guid.NewGuid();
        var evaluadorExternoId = Guid.NewGuid();

        var interno = comite.AgregarMiembro(
            evaluadorInternoId,
            TipoEvaluador.Interno,
            _fechaCreacion.AddDays(1),
            _coordinadorId);

        var externo = comite.AgregarMiembro(
            evaluadorExternoId,
            TipoEvaluador.Externo,
            _fechaCreacion.AddDays(2),
            _coordinadorId);

        comite.DesvincularMiembro(interno.Id, _fechaCreacion.AddDays(3), _coordinadorId);

        var dto = comite.ADto();

        dto.Version.Should().Be(4);
        dto.Activo.Should().BeTrue();
        dto.Miembros.Should().HaveCount(3);

        dto.Miembros[0].UsuarioEvaluadorId.Should().Be(_jefaturaId);
        dto.Miembros[0].Tipo.Should().Be("INTERNO");
        dto.Miembros[0].Rol.Should().Be("JEFATURA");
        dto.Miembros[0].Activo.Should().BeTrue();
        dto.Miembros[0].FechaDesvinculacion.Should().BeNull();

        dto.Miembros[1].Id.Should().Be(interno.Id);
        dto.Miembros[1].UsuarioEvaluadorId.Should().Be(evaluadorInternoId);
        dto.Miembros[1].Tipo.Should().Be("INTERNO");
        dto.Miembros[1].Rol.Should().Be("EVALUADOR");
        dto.Miembros[1].FechaAsignacion.Should().Be(new DateOnly(2026, 11, 23));
        dto.Miembros[1].FechaDesvinculacion.Should().Be(new DateOnly(2026, 11, 25));
        dto.Miembros[1].Activo.Should().BeFalse();

        dto.Miembros[2].Id.Should().Be(externo.Id);
        dto.Miembros[2].UsuarioEvaluadorId.Should().Be(evaluadorExternoId);
        dto.Miembros[2].Tipo.Should().Be("EXTERNO");
        dto.Miembros[2].Rol.Should().Be("EVALUADOR");
        dto.Miembros[2].FechaAsignacion.Should().Be(new DateOnly(2026, 11, 24));
        dto.Miembros[2].FechaDesvinculacion.Should().BeNull();
        dto.Miembros[2].Activo.Should().BeTrue();
    }
}
