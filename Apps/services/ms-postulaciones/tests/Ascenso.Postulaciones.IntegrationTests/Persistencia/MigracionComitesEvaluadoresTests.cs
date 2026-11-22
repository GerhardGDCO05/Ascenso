using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class MigracionComitesEvaluadoresTests
{
    [Fact]
    public void Debe_GenerarElScriptDeLaMigracionComitesEvaluadores_Cuando_SeMigraDesdeBloqueoRubrica()
    {
        using var contexto = new PostulacionesDbContextFactory().CreateDbContext([]);
        var migrador = contexto.GetService<IMigrator>();

        var script = migrador.GenerateScript(
            fromMigration: nameof(BloqueoRubrica),
            toMigration: nameof(ComitesEvaluadores));

        script.Should().Contain("CREATE TABLE postulaciones.comites_evaluadores");
        script.Should().Contain("pk_comites_evaluadores");
        script.Should().Contain("fecha_conformacion date");
        script.Should().Contain("fecha_creacion timestamptz");
        script.Should().Contain("fecha_ultima_modificacion timestamptz");
        script.Should().Contain("ux_comites_programa_convocatoria");

        script.Should().Contain("CREATE TABLE postulaciones.miembros_comite");
        script.Should().Contain("pk_miembros_comite");
        script.Should().Contain("fk_miembros_comite_comites");
        script.Should().Contain("ON DELETE CASCADE");
        script.Should().Contain("fecha_asignacion date");
        script.Should().Contain("fecha_desvinculacion date");
        script.Should().Contain("ck_miembros_comite_tipo_valido");
        script.Should().Contain("ck_miembros_comite_rol_valido");
        script.Should().Contain("ck_miembros_comite_desvinculacion_coherente");
        script.Should().Contain("ck_miembros_comite_jefatura_interna");
        script.Should().Contain("ux_miembros_comite_usuario_activo");
        script.Should().Contain("WHERE activo");
        script.Should().Contain("ux_miembros_comite_jefatura_activa");
        script.Should().Contain("WHERE activo AND rol = 'JEFATURA'");
    }
}
