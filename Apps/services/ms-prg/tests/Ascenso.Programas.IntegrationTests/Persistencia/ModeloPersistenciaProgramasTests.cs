using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Donantes.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.Entidades;
using Ascenso.Programas.Domain.Universidades.ObjetosValor;
using Ascenso.Programas.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ascenso.Programas.IntegrationTests.Persistencia;

public sealed class ModeloPersistenciaProgramasTests
{
    private static ProgramasDbContext CrearContextoModelo()
        => new ProgramasDbContextFactory().CreateDbContext([]);

    [Theory]
    [InlineData(typeof(Donante), "donantes")]
    [InlineData(typeof(AporteDonante), "aportes_donante")]
    [InlineData(typeof(UniversidadAliada), "universidades_aliadas")]
    [InlineData(typeof(Convenio), "convenios_universidad")]
    [InlineData(typeof(CarreraElegible), "carreras_elegibles")]
    [InlineData(typeof(CostoMatriculaReferencia), "costos_matricula_referencia")]
    [InlineData(typeof(InboxState), "inbox_state")]
    [InlineData(typeof(OutboxState), "outbox_state")]
    [InlineData(typeof(OutboxMessage), "outbox_message")]
    public void Debe_MapearTablasEnSnakeCase_DentroDelEsquemaProgramas(
        Type tipo,
        string tablaEsperada)
    {
        using var contexto = CrearContextoModelo();
        var entidad = contexto.Model.FindEntityType(tipo);

        entidad.Should().NotBeNull();
        entidad!.GetSchema().Should().Be(ProgramasDbContext.Esquema);
        entidad.GetTableName().Should().Be(tablaEsperada);
    }

    [Theory]
    [InlineData(typeof(Donante), nameof(Donante.TipoDonante), "tipo_donante")]
    [InlineData(typeof(Donante), nameof(Donante.IdentificacionFiscal), "identificacion_fiscal")]
    [InlineData(typeof(Donante), nameof(Donante.SaldoTotalHistorico), "saldo_total_historico")]
    [InlineData(typeof(Donante), nameof(Donante.SaldoDisponibleAsignacion), "saldo_disponible_asignacion")]
    [InlineData(typeof(Donante), "ContactoPrincipal.NombreRepresentante", "contacto_nombre_representante")]
    [InlineData(typeof(Donante), "ContactoPrincipal.Cargo", "contacto_cargo")]
    [InlineData(typeof(Donante), "ContactoPrincipal.EmailContacto", "contacto_email")]
    [InlineData(typeof(Donante), "ContactoPrincipal.TelefonoContacto", "contacto_telefono")]
    [InlineData(typeof(AporteDonante), nameof(AporteDonante.NumeroReferenciaBancaria), "numero_referencia_bancaria")]
    [InlineData(typeof(AporteDonante), nameof(AporteDonante.UrlComprobanteFirebase), "url_comprobante_firebase")]
    [InlineData(typeof(AporteDonante), nameof(AporteDonante.PeriodoAcademicoId), "periodo_academico_id")]
    public void Debe_MapearColumnasEnSnakeCase(
        Type tipo,
        string rutaPropiedad,
        string columnaEsperada)
    {
        using var contexto = CrearContextoModelo();
        var propiedad = ObtenerPropiedad(contexto.Model, tipo, rutaPropiedad);

        propiedad.GetColumnName().Should().Be(columnaEsperada);
    }

    [Theory]
    [InlineData(typeof(Donante), nameof(Donante.Id), "uuid")]
    [InlineData(typeof(Donante), nameof(Donante.FechaCreacion), "timestamptz")]
    [InlineData(typeof(Donante), nameof(Donante.SaldoTotalHistorico), "numeric(18,2)")]
    [InlineData(typeof(Donante), nameof(Donante.SaldoDisponibleAsignacion), "numeric(18,2)")]
    [InlineData(typeof(AporteDonante), nameof(AporteDonante.MontoAporte), "numeric(18,2)")]
    [InlineData(typeof(AporteDonante), nameof(AporteDonante.FechaAporte), "timestamptz")]
    public void Debe_ConfigurarTiposDeAlmacenamientoDelContrato(
        Type tipo,
        string rutaPropiedad,
        string tipoEsperado)
    {
        using var contexto = CrearContextoModelo();
        var propiedad = ObtenerPropiedad(contexto.Model, tipo, rutaPropiedad);

        propiedad.GetColumnType().Should().Be(tipoEsperado);
    }

    [Theory]
    [InlineData(typeof(Donante), nameof(Donante.TipoDonante))]
    [InlineData(typeof(Donante), nameof(Donante.EstadoDonante))]
    [InlineData(typeof(AporteDonante), nameof(AporteDonante.EstadoAporte))]
    public void Debe_PersistirEnumeracionesComoVarchar(
        Type tipo,
        string rutaPropiedad)
    {
        using var contexto = CrearContextoModelo();
        var propiedad = ObtenerPropiedad(contexto.Model, tipo, rutaPropiedad);

        var converter = propiedad.GetValueConverter();

        converter.Should().NotBeNull();
        converter!.ProviderClrType.Should().Be<string>();
        propiedad.GetColumnType().Should().Be("varchar");
    }

    [Fact]
    public void Debe_ConfigurarContactoPrincipalComoTipoPropioEIndicesUnicosYConcurrencia()
    {
        using var contexto = CrearContextoModelo();

        var entidadDonante = contexto.Model.FindEntityType(typeof(Donante))!;
        var contacto = entidadDonante.FindNavigation(nameof(Donante.ContactoPrincipal));
        contacto.Should().NotBeNull();
        contacto!.TargetEntityType.IsOwned().Should().BeTrue();
        contacto.TargetEntityType.GetTableName().Should().Be("donantes");

        var version = entidadDonante.FindProperty(nameof(Donante.Version));
        version.Should().NotBeNull();
        version!.IsConcurrencyToken.Should().BeTrue();

        entidadDonante.GetIndexes()
            .Should().Contain(idx => idx.IsUnique && idx.GetDatabaseName() == "ux_donante_identificacion_fiscal");

        var entidadAporte = contexto.Model.FindEntityType(typeof(AporteDonante))!;
        entidadAporte.GetIndexes()
            .Should().Contain(idx => idx.IsUnique && idx.GetDatabaseName() == "ux_aporte_donante_referencia_bancaria");
    }

    [Fact]
    public void Debe_ConfigurarValueGeneratedNeverEnIdsYVersion_YDetectarAportesConsecutivosComoAdded()
    {
        using var contexto = CrearContextoModelo();

        var entidadDonante = contexto.Model.FindEntityType(typeof(Donante))!;
        var idDonante = entidadDonante.FindProperty(nameof(Donante.Id))!;
        var versionDonante = entidadDonante.FindProperty(nameof(Donante.Version))!;

        idDonante.ValueGenerated.Should().Be(ValueGenerated.Never);
        versionDonante.IsConcurrencyToken.Should().BeTrue();
        versionDonante.ValueGenerated.Should().Be(ValueGenerated.Never);

        var entidadAporte = contexto.Model.FindEntityType(typeof(AporteDonante))!;
        var idAporte = entidadAporte.FindProperty(nameof(AporteDonante.Id))!;
        idAporte.ValueGenerated.Should().Be(ValueGenerated.Never);

        var usuarioId = UuidV7.Nuevo();
        var donante = Donante.Crear(
            TipoDonante.Empresa,
            "Fundación Prueba Concurrencia",
            "J-50001234-9",
            "finanzas@fundacion.org",
            "+584141234567",
            "Caracas, Venezuela",
            new ContactoDonante("Ana Pérez", "Directora", "ana@fundacion.org", "+584141234567"),
            usuarioId);

        // Simula la carga con tracking del agregado ya persistido (estado Unchanged, Version = 1).
        contexto.Attach(donante);
        contexto.Entry(donante).State.Should().Be(EntityState.Unchanged);

        var primerAporte = donante.RegistrarAporte(
            1500.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Banesco",
            "REF-TRACK-001",
            $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf",
            usuarioId);

        var segundoAporte = donante.RegistrarAporte(
            2500.00m,
            "USD",
            "2026-1",
            DateTimeOffset.UtcNow,
            "Mercantil",
            "REF-TRACK-002",
            $"/donantes/{donante.Id:D}/aportes/{UuidV7.Nuevo():D}.pdf",
            usuarioId);

        contexto.ChangeTracker.DetectChanges();

        contexto.Entry(primerAporte).State.Should().Be(EntityState.Added);
        contexto.Entry(segundoAporte).State.Should().Be(EntityState.Added);
        contexto.Entry(donante).State.Should().Be(EntityState.Modified);

        var propiedadVersion = contexto.Entry(donante).Property(d => d.Version);
        propiedadVersion.OriginalValue.Should().Be(1);
        propiedadVersion.CurrentValue.Should().Be(3);
        propiedadVersion.IsModified.Should().BeTrue();

        donante.SaldoTotalHistorico.Should().Be(4000.00m);
        donante.SaldoDisponibleAsignacion.Should().Be(4000.00m);
    }

    [Fact]
    public void Debe_ConfigurarValueGeneratedNeverEnUniversidades_YDetectarVersionadoCostosCorrectamente()
    {
        using var contexto = CrearContextoModelo();

        foreach (var tipo in new[]
        {
            typeof(UniversidadAliada),
            typeof(Convenio),
            typeof(CarreraElegible),
            typeof(CostoMatriculaReferencia)
        })
        {
            var entidad = contexto.Model.FindEntityType(tipo)!;
            entidad.FindProperty("Id")!.ValueGenerated.Should().Be(ValueGenerated.Never);
            var version = entidad.FindProperty("Version")!;
            version.IsConcurrencyToken.Should().BeTrue();
            version.ValueGenerated.Should().Be(ValueGenerated.Never);
        }

        var usuarioId = UuidV7.Nuevo();
        var universidad = UniversidadAliada.CrearConConvenioInicial(
            "UCAB-CCS",
            "Universidad Católica Andrés Bello",
            "Montalbán, Caracas",
            new ContactoEnlace("Laura Ríos", "lrios@ucab.edu.ve", "+58 212 4074111"),
            null,
            null,
            "CONV-2026-001",
            "Convenio vigente",
            new DateOnly(2026, 1, 1),
            new DateOnly(2028, 12, 31),
            "/universidades/ucab/convenios/conv-2026.pdf",
            true,
            usuarioId);

        var carrera = universidad.AgregarCarreraElegible("ING-INF", "Ingeniería Informática", 10, usuarioId);
        var costoV1 = universidad.ActualizarCostoMatricula(carrera.Id, "2026-1", 1800.00m, "USD", usuarioId);

        // Simula carga con tracking de universidad + carrera + costo v1 ya persistidos.
        contexto.Attach(universidad);
        contexto.Entry(universidad).State.Should().Be(EntityState.Unchanged);
        contexto.Entry(costoV1).State.Should().Be(EntityState.Unchanged);

        var nuevoConvenio = universidad.RegistrarNuevoConvenio(
            "CONV-2026-002",
            "Adenda de ampliación",
            new DateOnly(2026, 6, 1),
            new DateOnly(2029, 6, 1),
            "/universidades/ucab/convenios/conv-2026-002.pdf",
            true,
            usuarioId);

        var costoV2 = universidad.ActualizarCostoMatricula(carrera.Id, "2026-1", 2100.00m, "USD", usuarioId);

        contexto.ChangeTracker.DetectChanges();

        contexto.Entry(nuevoConvenio).State.Should().Be(EntityState.Added);
        contexto.Entry(costoV2).State.Should().Be(EntityState.Added);
        contexto.Entry(costoV1).State.Should().Be(EntityState.Modified);
        costoV1.Estado.Should().Be(EstadoVersionCosto.Historico);
        costoV2.NumeroVersion.Should().Be(2);
    }

    private static IProperty ObtenerPropiedad(
        IModel modelo,
        Type tipo,
        string rutaPropiedad)
    {
        var entidad = modelo.FindEntityType(tipo)
            ?? throw new InvalidOperationException($"No se encontró la entidad {tipo.Name}.");

        var partes = rutaPropiedad.Split('.');
        IProperty? propiedad = null;

        foreach (var parte in partes)
        {
            propiedad = entidad.FindProperty(parte);
            if (propiedad is not null)
            {
                return propiedad;
            }

            entidad = entidad.FindNavigation(parte)?.TargetEntityType
                ?? throw new InvalidOperationException(
                    $"No se encontró la propiedad o navegación {parte} en {tipo.Name}.");
        }

        return propiedad
            ?? throw new InvalidOperationException($"No se encontró la propiedad {rutaPropiedad}.");
    }
}

