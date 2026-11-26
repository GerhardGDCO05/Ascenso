using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Ascenso.Programas.IntegrationTests.Persistencia;

public sealed class ProgramasDbContextExceptionTests
{
    [Fact]
    public async Task Debe_TraducirIndiceUnicoDeConvocatoriaAConflictoDeDominio()
    {
        var excepcionPostgres = new PostgresException(
            "duplicate convocatoria code",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            constraintName: "ux_convocatoria_codigo");
        var interceptor = new InterceptorDeGuardado(
            new DbUpdateException("No se pudo guardar.", excepcionPostgres));
        var opciones = new DbContextOptionsBuilder<ProgramasDbContext>()
            .UseNpgsql("Host=localhost;Database=sin_conexion;Username=test;Password=test")
            .AddInterceptors(interceptor)
            .Options;

        await using var contexto = new ProgramasDbContext(opciones);
        var accion = () => contexto.GuardarCambiosAsync();

        var excepcion = await accion.Should().ThrowAsync<CodigoConvocatoriaDuplicadoException>();
        excepcion.Which.EstadoHttp.Should().Be(409);
    }

    private sealed class InterceptorDeGuardado(DbUpdateException excepcion) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<InterceptionResult<int>>(excepcion);
    }
}
