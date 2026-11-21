using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class ConcurrenciaOptimistaTests
{
    [Fact]
    public async Task Debe_TraducirA409_Cuando_EFDetectaUnConflictoDeConcurrencia()
    {
        var opciones = new DbContextOptionsBuilder<PostulacionesDbContext>()
            .UseNpgsql(PostulacionesDbContextFactory.CadenaConexionPorDefecto)
            .AddInterceptors(new InterceptorConflictoConcurrencia())
            .Options;

        await using var contexto = new PostulacionesDbContext(opciones);
        contexto.Postulantes.Add(CrearPostulante());

        var accion = () => contexto.GuardarCambiosAsync();

        await accion.Should()
            .ThrowAsync<ExcepcionConflictoConcurrencia>()
            .Where(excepcion =>
                excepcion.Codigo == ExcepcionConflictoConcurrencia.CodigoError
                && excepcion.EstadoHttp == 409);
    }

    private static Postulante CrearPostulante()
        => Postulante.Crear(
            personaId: null,
            usuarioId: Guid.NewGuid(),
            numeroDocumentoIdentidad: "V-40404040",
            nombres: "Ana",
            apellidos: "Pérez",
            datosContacto: new DatosContacto("ana@correo.com", "+584141234567"),
            universidadAliadaId: Guid.NewGuid(),
            carreraId: Guid.NewGuid(),
            periodoActualId: Guid.NewGuid(),
            condicionAcademica: CondicionAcademicaIngreso.Admitido,
            aceptaTratamientoDatos: true,
            fechaRegistro: DateTimeOffset.UtcNow);

    private sealed class InterceptorConflictoConcurrencia : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new DbUpdateConcurrencyException("Conflicto de concurrencia simulado.");
    }
}
