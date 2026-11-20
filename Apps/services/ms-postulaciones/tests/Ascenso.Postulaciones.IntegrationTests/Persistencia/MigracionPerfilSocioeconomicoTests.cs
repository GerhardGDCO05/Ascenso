using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionPerfilSocioeconomicoTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracion_Cuando_SeMigraDesdeElRegistroBase()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(RegistroPostulantes),
            toMigration: nameof(PerfilSocioeconomicoGrupoFamiliar));

        script.Should().Contain("ALTER TABLE postulaciones.postulantes ADD estado_civil");
        script.Should().Contain("ADD promedio_actual numeric(5,2)");
        script.Should().Contain("ADD trabaja");
        script.Should().Contain("CREATE TABLE postulaciones.miembros_familiares");
        script.Should().Contain("postulante_id uuid NOT NULL");
        script.Should().Contain("fk_miembros_familiares_postulantes");
        script.Should().Contain("ix_miembros_familiares_postulante_id");
        script.Should().Contain("ck_miembro_familiar_parentesco_valido");
        script.Should().Contain("ck_miembro_familiar_edad_rango");
        script.Should().Contain("ck_miembro_familiar_ingreso_no_negativo");
        script.Should().Contain("ck_postulante_perfil_completo_campos_requeridos");
    }
}
