using System.Data.Common;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Infrastructure.Consultas;
using Ascenso.Programas.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Ascenso.Programas.Infrastructure.Persistence.Repositorios;
using Testcontainers.PostgreSql;

namespace Ascenso.Programas.IntegrationTests.Persistencia;

public sealed class ConvocatoriaLockingIntegrationTests : IAsyncLifetime
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000701");
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
        => await _postgres.DisposeAsync();

    [Fact]
    public async Task Debe_MantenerElBloqueoForUpdateHastaConfirmarLaTransaccion()
    {
        var programa = CrearPrograma();
        await using (var contextoInicial = CrearContexto())
        {
            await contextoInicial.ProgramasBeca.AddAsync(programa);
            await contextoInicial.SaveChangesAsync();
        }

        var inicioConsultaSecundaria = new ConsultaForUpdateInterceptor();
        await using var contextoPrimario = CrearContexto();
        await using var contextoSecundario = CrearContexto(inicioConsultaSecundaria);

        var lecturaPrimariaCompleta = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var permitirCommitPrimario = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var ejecutorPrimario = new ConvocatoriaTransactionExecutor(contextoPrimario);
        var ejecutorSecundario = new ConvocatoriaTransactionExecutor(contextoSecundario);

        var transaccionPrimaria = ejecutorPrimario.ExecuteAsync(async cancellationToken =>
        {
            var instantanea = await new CatalogoConvocatoriaReader(contextoPrimario)
                .ObtenerProgramaAsync(programa.Id, new DateOnly(2026, 11, 25), cancellationToken);
            lecturaPrimariaCompleta.TrySetResult(true);
            await permitirCommitPrimario.Task.WaitAsync(cancellationToken);
            return instantanea;
        });

        await lecturaPrimariaCompleta.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var transaccionSecundaria = ejecutorSecundario.ExecuteAsync(cancellationToken =>
            new CatalogoConvocatoriaReader(contextoSecundario)
                .ObtenerProgramaAsync(programa.Id, new DateOnly(2026, 11, 25), cancellationToken));

        try
        {
            await inicioConsultaSecundaria.ForUpdateEnviado.Task.WaitAsync(TimeSpan.FromSeconds(10));
            transaccionSecundaria.IsCompleted.Should().BeFalse();
        }
        finally
        {
            permitirCommitPrimario.TrySetResult(true);
        }

        (await transaccionPrimaria).Should().NotBeNull();
        (await transaccionSecundaria.WaitAsync(TimeSpan.FromSeconds(10))).Should().NotBeNull();
    }

    [Fact]
    public async Task Debe_LeerConveniosVigentesYPaginarConvocatoriasConDocumentos()
    {
        var universidadVigente = CrearUniversidad("UCAB-LOCK-OK", new DateOnly(2027, 12, 31));
        var carreraVigente = universidadVigente.AgregarCarreraElegible(
            "ING-INF", "Ingeniería Informática", 10, _usuarioId);
        var universidadVencida = CrearUniversidad("UCAB-LOCK-EX", new DateOnly(2026, 10, 31));
        var carreraVencida = universidadVencida.AgregarCarreraElegible(
            "ING-IND", "Ingeniería Industrial", 10, _usuarioId);
        var programa = CrearPrograma();
        var donante = CrearDonante();
        donante.RegistrarAporte(
            20000.00m,
            "USD",
            "2026-1",
            new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero),
            "Mercantil",
            "REF-CONVOCATORIA-LOCK",
            $"/donantes/{donante.Id:D}/aportes/lock-test.pdf",
            _usuarioId);
        programa.AsignarFondoDonante(donante.Id, "2026-1", 10000.00m, _usuarioId);
        programa.ConfigurarElegibilidadYPermanencia(
            new CriteriosElegibilidadPrograma(
                15.00m,
                17,
                35,
                1200.00m,
                [universidadVigente.Id, universidadVencida.Id],
                [carreraVigente.Id, carreraVencida.Id]),
            new CondicionesPermanencia(
                14.00m,
                4,
                1,
                15,
                FrecuenciaReporteAcademico.Semestral,
                ConsecuenciaIncumplimiento.Suspension,
                0.50m,
                2,
                true),
            todasLasUniversidadesTienenConvenioVigente: true,
            todasLasCarrerasSonElegibles: true,
            _usuarioId);

        await using var contexto = CrearContexto();
        await contexto.Donantes.AddAsync(donante);
        await contexto.UniversidadesAliadas.AddRangeAsync(universidadVigente, universidadVencida);
        await contexto.ProgramasBeca.AddAsync(programa);
        await contexto.SaveChangesAsync();

        var catalogo = await new ConvocatoriaTransactionExecutor(contexto).ExecuteAsync(
            cancellationToken => new CatalogoConvocatoriaReader(contexto).ObtenerProgramaAsync(
                programa.Id,
                new DateOnly(2026, 11, 25),
                cancellationToken));

        catalogo.Should().NotBeNull();
        catalogo!.UniversidadesConConvenioVigente.Should().Equal(universidadVigente.Id);
        catalogo.CarrerasElegiblesConConvenioVigente.Should().Equal(carreraVigente.Id);

        var borradorUno = CrearConvocatoria(programa.Id, "RF21-TEST-01");
        var borradorDos = CrearConvocatoria(programa.Id, "RF21-TEST-02");
        var publicada = CrearConvocatoria(programa.Id, "RF21-TEST-03", publicada: true);
        var repositorio = new ConvocatoriaRepository(contexto);
        await repositorio.AgregarAsync(borradorUno);
        await repositorio.AgregarAsync(borradorDos);
        await repositorio.AgregarAsync(publicada);
        await contexto.GuardarCambiosAsync();

        var pagina = await repositorio.ConsultarPaginadoAsync(
            1, 1, EstadoConvocatoria.Borrador, programa.Id, "test");
        pagina.TotalCount.Should().Be(2);
        pagina.Items.Should().ContainSingle();
        pagina.Items.Single().Estado.Should().Be(EstadoConvocatoria.Borrador);

        var abiertas = await repositorio.ConsultarAbiertasAsync(
            new DateTimeOffset(2026, 12, 10, 12, 0, 0, TimeSpan.Zero));
        abiertas.Should().ContainSingle();
        abiertas.Single().DocumentosRequeridos.Should().ContainSingle();
    }

    [Fact]
    public async Task Debe_AplicarMigracionYRechazarViolacionesDeIndiceUnicoYCheck()
    {
        var programa = CrearPrograma();
        var convocatoria = CrearConvocatoria(programa.Id, "RF21-MIGRACION-01");

        await using var contexto = CrearContexto();
        await contexto.ProgramasBeca.AddAsync(programa);
        await contexto.Convocatorias.AddAsync(convocatoria);
        await contexto.GuardarCambiosAsync();

        var migracionesAplicadas = await contexto.Database.GetAppliedMigrationsAsync();
        migracionesAplicadas.Should().Contain("20261125204533_Convocatorias");

        await contexto.Database.OpenConnectionAsync();
        var conexion = contexto.Database.GetDbConnection();
        await using (var comando = conexion.CreateCommand())
        {
            comando.CommandText =
                "SELECT conname FROM pg_constraint " +
                "WHERE conrelid = 'programas.convocatorias'::regclass AND contype = 'c'";
            await using var lector = await comando.ExecuteReaderAsync();
            var nombresRestricciones = new List<string>();
            while (await lector.ReadAsync())
            {
                nombresRestricciones.Add(lector.GetString(0));
            }

            var restriccionesEsperadas = new[]
            {
                "ck_convocatoria_cupos_positivos",
                "ck_convocatoria_edad_minima_valida",
                "ck_convocatoria_edad_maxima_valida",
                "ck_convocatoria_edades_ordenadas",
                "ck_convocatoria_estado_valido",
                "ck_convocatoria_evaluacion_despues_de_subsanacion",
                "ck_convocatoria_evaluacion_ordenada",
                "ck_convocatoria_fechas_ordenadas",
                "ck_convocatoria_promedio_valido",
                "ck_convocatoria_subsanacion_despues_del_cierre",
                "ck_convocatoria_tope_ingreso_no_negativo"
            };
            foreach (var restriccion in restriccionesEsperadas)
            {
                nombresRestricciones.Should().Contain(restriccion);
            }
        }

        var actualizacionInvalida = () => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE programas.convocatorias SET cupos_asignados = 0 WHERE id = {convocatoria.Id}");
        var violacionCheck = await actualizacionInvalida.Should().ThrowAsync<PostgresException>();
        violacionCheck.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        violacionCheck.Which.ConstraintName.Should().Be("ck_convocatoria_cupos_positivos");

        var convocatoriaDuplicada = CrearConvocatoria(programa.Id, convocatoria.Codigo);
        await contexto.Convocatorias.AddAsync(convocatoriaDuplicada);
        var violacionIndice = await Assert.ThrowsAsync<Ascenso.Programas.Domain.Excepciones.CodigoConvocatoriaDuplicadoException>(
            () => contexto.GuardarCambiosAsync());
        violacionIndice.EstadoHttp.Should().Be(409);
    }

    private ProgramasDbContext CrearContexto(DbCommandInterceptor? interceptor = null)
    {
        var opciones = new DbContextOptionsBuilder<ProgramasDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ProgramasDbContext.Esquema));

        if (interceptor is not null)
        {
            opciones.AddInterceptors(interceptor);
        }

        return new ProgramasDbContext(opciones.Options);
    }

    private static ProgramaBeca CrearPrograma()
        => ProgramaBeca.Crear(
            "BECA-LOCK-TEST",
            "Programa de prueba de bloqueo",
            "Programa utilizado para validar el bloqueo de concurrencia.",
            new PoliticaCobertura(ModalidadCobertura.Total, 100.00m, 0.00m, "USD"),
            10,
            5,
            "2026-1",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            false,
            true,
            null,
            _usuarioId);

    private static Donante CrearDonante()
        => Donante.Crear(
            TipoDonante.Empresa,
            "Fundación de prueba de concurrencia",
            "J-50999999-1",
            "finanzas-lock@example.org",
            "+58 212 5550001",
            "Caracas",
            new ContactoDonante("Ana Pérez", "Directora", "ana-lock@example.org", "+58 212 5550002"),
            _usuarioId);

    private static UniversidadAliada CrearUniversidad(string codigo, DateOnly finConvenio)
        => UniversidadAliada.CrearConConvenioInicial(
            codigo,
            $"Universidad {codigo}",
            "Caracas",
            new ContactoEnlace("Ana Pérez", "ana@example.org", "+58 212 5550000"),
            null,
            null,
            $"CONV-{codigo}",
            "Convenio de prueba",
            new DateOnly(2026, 1, 1),
            finConvenio,
            $"/universidades/{codigo}/convenio.pdf",
            true,
            _usuarioId);

    private static Convocatoria CrearConvocatoria(Guid programaId, string codigo, bool publicada = false)
    {
        var convocatoria = Convocatoria.CrearBorrador(
            programaId,
            Guid.Parse("01966000-0000-7000-8000-000000000601").ToString("D"),
            codigo,
            "Convocatoria de integración",
            new DateTimeOffset(2026, 12, 1, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 21, 3, 59, 59, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 23, 3, 59, 59, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 23, 4, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 3, 59, 59, TimeSpan.Zero),
            true,
            3,
            new CriteriosElegibilidadCiclo(15.00m, 17, 25, 1200.00m, [Guid.NewGuid()], [Guid.NewGuid()]),
            _usuarioId);

        if (publicada)
        {
            convocatoria.AgregarDocumento(
                "IDENTIDAD", "Documento de identidad", null, true, ["application/pdf"], 5, _usuarioId);
            convocatoria.Publicar(new DateTimeOffset(2026, 11, 25, 16, 0, 0, TimeSpan.Zero), _usuarioId);
        }

        return convocatoria;
    }

    private sealed class ConsultaForUpdateInterceptor : DbCommandInterceptor
    {
        public TaskCompletionSource<bool> ForUpdateEnviado { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                ForUpdateEnviado.TrySetResult(true);
            }

            return ValueTask.FromResult(result);
        }
    }
}
