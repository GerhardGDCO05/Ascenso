using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

/// <summary>
/// Datos válidos compartidos por las pruebas de los casos de uso.
/// </summary>
internal static class DatosAplicacion
{
    public static readonly DateTimeOffset FechaBase =
        new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    public const string HashValido = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public static ContextoOperacion Contexto(
        DateTimeOffset? fecha = null,
        string? justificacion = null,
        Guid? autorizacionOtpId = null)
        => new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            fecha ?? FechaBase,
            justificacion,
            autorizacionOtpId);

    public static ResultadoAdjudicabilidad Adjudicacion(
        Guid? postulacionId = null,
        Guid? programaBecaId = null,
        Guid? periodoAcademicoId = null,
        Guid? universidadAliadaId = null,
        Guid? carreraId = null,
        Guid? estudianteId = null,
        Guid? convocatoriaId = null,
        bool adjudicable = true,
        string? motivo = null)
        => new(
            postulacionId ?? Guid.CreateVersion7(),
            adjudicable,
            programaBecaId ?? Guid.CreateVersion7(),
            periodoAcademicoId ?? Guid.CreateVersion7(),
            "2026-2",
            universidadAliadaId ?? Guid.CreateVersion7(),
            carreraId ?? Guid.CreateVersion7(),
            estudianteId ?? Guid.CreateVersion7(),
            "Ana Pérez",
            "12345678",
            convocatoriaId ?? Guid.CreateVersion7(),
            motivo);

    public static ProyeccionOtorgamiento Proyeccion(
        Guid? programaBecaId = null,
        Guid? costoMatriculaId = null,
        Guid? convenioId = null,
        DateOnly? fecha = null,
        string estadoPrograma = "ACTIVO",
        string estadoCosto = "VIGENTE",
        string estadoConvenio = "VIGENTE",
        decimal montoMatricula = 1000m,
        TipoCobertura modalidad = TipoCobertura.Parcial,
        decimal porcentaje = 50m,
        decimal montoFijo = 0m,
        int duracionPeriodos = 4,
        bool permiteAcumulacion = false,
        bool requiereCompromisoFirmado = false)
    {
        var fechaReferencia = fecha ?? DateOnly.FromDateTime(FechaBase.UtcDateTime);

        return new ProyeccionOtorgamiento(
            new ProgramaOtorgamientoProyeccion(
                programaBecaId ?? Guid.CreateVersion7(),
                "PROG-BECAS",
                "Programa de becas de prueba",
                new PoliticaCobertura(modalidad, porcentaje, montoFijo, "USD"),
                new CondicionesPermanencia(
                    14m,
                    4,
                    1,
                    80,
                    ConsecuenciaPermanencia.Suspension),
                permiteAcumulacion,
                requiereCompromisoFirmado,
                duracionPeriodos,
                estadoPrograma),
            new CostoMatriculaProyeccion(
                costoMatriculaId ?? Guid.CreateVersion7(),
                montoMatricula,
                "USD",
                estadoCosto,
                fechaReferencia.AddDays(-30),
                null,
                1),
            new ConvenioProyeccion(
                convenioId ?? Guid.CreateVersion7(),
                estadoConvenio,
                fechaReferencia.AddYears(1)));
    }

    public static ResultadoReservaPresupuesto Reserva(
        Guid? reservaId = null,
        decimal monto = 2000m,
        int cupos = 1,
        string estado = "PENDIENTE",
        DateTimeOffset? expiraEn = null)
        => new(
            reservaId ?? Guid.CreateVersion7(),
            estado,
            monto,
            cupos,
            expiraEn ?? FechaBase.AddMinutes(5));

    public static Beca CrearBecaConOferta(
        Guid postulacionId,
        Guid programaBecaId,
        Guid periodoAcademicoId,
        Guid estudianteId,
        Guid? convocatoriaId = null,
        bool requiereCompromisoFirmado = false,
        bool permiteAcumulacion = false,
        decimal montoPorPeriodo = 500m,
        int duracionPeriodos = 4,
        DateTimeOffset? fecha = null)
    {
        var contexto = Contexto(fecha);
        var datosBeca = new DatosCreacionBeca(
            estudianteId,
            "12345678",
            postulacionId,
            programaBecaId,
            convocatoriaId ?? Guid.CreateVersion7(),
            periodoAcademicoId,
            duracionPeriodos,
            new PoliticaCobertura(TipoCobertura.Parcial, 50m, 0m, "USD"),
            Guid.CreateVersion7(),
            montoPorPeriodo,
            permiteAcumulacion,
            requiereCompromisoFirmado,
            $"BECA-{Guid.NewGuid():N}"[..20],
            requiereCompromisoFirmado
                ? "https://storage.example/becas/plantilla-compromiso.pdf"
                : null);

        var beca = Beca.CrearOtorgada(datosBeca, contexto);
        beca.EmitirOferta(
            new DatosOfertaBeca(
                postulacionId,
                Guid.CreateVersion7(),
                montoPorPeriodo,
                CalculadoraMontoOferta.CalcularMontoTotalEstimado(
                    montoPorPeriodo,
                    duracionPeriodos),
                duracionPeriodos,
                new CondicionesPermanencia(
                    14m,
                    4,
                    1,
                    80,
                    ConsecuenciaPermanencia.Suspension),
                "https://storage.example/carta.pdf",
                HashValido,
                "application/pdf",
                1024,
                "job-expiracion-oferta"),
            contexto);

        return beca;
    }

    public static Beca CrearBecaActiva(
        Guid postulacionId,
        Guid programaBecaId,
        Guid periodoAcademicoId,
        Guid estudianteId,
        bool requiereCompromisoFirmado = false,
        bool permiteAcumulacion = false,
        decimal montoPorPeriodo = 500m,
        int duracionPeriodos = 4,
        DateTimeOffset? fecha = null)
    {
        var beca = CrearBecaConOferta(
            postulacionId,
            programaBecaId,
            periodoAcademicoId,
            estudianteId,
            requiereCompromisoFirmado: requiereCompromisoFirmado,
            permiteAcumulacion: permiteAcumulacion,
            montoPorPeriodo: montoPorPeriodo,
            duracionPeriodos: duracionPeriodos,
            fecha: fecha);

        var aceptacion = Contexto((fecha ?? FechaBase).AddDays(1));
        beca.AceptarOferta(beca.Oferta!.Id, aceptacion);

        return beca;
    }

    public static Beca CrearBecaActivaConCompromisoEnRevision(
        Guid postulacionId,
        Guid programaBecaId,
        Guid periodoAcademicoId,
        Guid estudianteId,
        DateTimeOffset? fecha = null)
    {
        var beca = CrearBecaActiva(
            postulacionId,
            programaBecaId,
            periodoAcademicoId,
            estudianteId,
            requiereCompromisoFirmado: true,
            fecha: fecha);

        beca.CargarCompromiso(
            "https://storage.example/compromiso.pdf",
            new string('b', 64),
            1024,
            "application/pdf",
            "10.0.0.5",
            Contexto((fecha ?? FechaBase).AddDays(2)));

        return beca;
    }
}
