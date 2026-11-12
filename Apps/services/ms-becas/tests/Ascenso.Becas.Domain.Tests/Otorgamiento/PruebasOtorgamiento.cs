using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

internal static class PruebasOtorgamiento
{
    public static readonly DateTimeOffset FechaBase =
        new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid UsuarioCoordinador = UuidV7.Nuevo();

    public static Guid NuevoId() => UuidV7.Nuevo();

    public static ContextoOperacion Contexto(
        DateTimeOffset? fecha = null,
        string? justificacion = null,
        Guid? autorizacionOtpId = null)
        => new(
            UsuarioCoordinador,
            UuidV7.Nuevo(),
            fecha ?? FechaBase,
            justificacion,
            autorizacionOtpId);

    public static PoliticaCobertura Cobertura(
        TipoCobertura modalidad = TipoCobertura.Parcial,
        decimal porcentaje = 50m,
        decimal montoFijo = 0m)
        => new(modalidad, porcentaje, montoFijo, "USD");

    public static CondicionesPermanencia Condiciones()
        => new(14m, 4, 1, 80, ConsecuenciaPermanencia.Suspension);

    public static DatosCreacionBeca DatosBeca(
        Guid? estudianteId = null,
        Guid? postulacionId = null,
        Guid? programaId = null,
        Guid? periodoId = null,
        PoliticaCobertura? cobertura = null,
        int duracion = 4,
        decimal montoComprometido = 1500m,
        bool permiteAcumulacion = false,
        bool requiereCompromiso = false)
        => new(
            estudianteId ?? UuidV7.Nuevo(),
            "DOC-123456",
            postulacionId ?? UuidV7.Nuevo(),
            programaId ?? UuidV7.Nuevo(),
            UuidV7.Nuevo(),
            periodoId ?? UuidV7.Nuevo(),
            duracion,
            cobertura ?? Cobertura(TipoCobertura.Parcial, 50m),
            UuidV7.Nuevo(),
            montoComprometido,
            permiteAcumulacion,
            requiereCompromiso,
            "BEC-2026-0001");

    public static DatosOfertaBeca DatosOferta(
        Guid postulacionId,
        decimal montoPorPeriodo = 500m,
        int duracion = 4,
        Guid? reservaId = null,
        string? hash = null,
        Guid? autorizacionOtpEmisionId = null)
        => new(
            postulacionId,
            reservaId ?? UuidV7.Nuevo(),
            montoPorPeriodo,
            CalculadoraMontoOferta.CalcularMontoTotalEstimado(montoPorPeriodo, duracion),
            duracion,
            Condiciones(),
            "https://storage.example/becas/carta.pdf",
            hash ?? new string('a', 64),
            "hangfire-job-oferta-1",
            autorizacionOtpEmisionId);

    public static Beca CrearBeca(DatosCreacionBeca? datos = null, ContextoOperacion? contexto = null)
        => Beca.CrearOtorgada(datos ?? DatosBeca(), contexto ?? Contexto());

    public static Beca CrearBecaConOferta(
        DatosCreacionBeca? datos = null,
        ContextoOperacion? contexto = null,
        decimal montoPorPeriodo = 500m)
    {
        var beca = CrearBeca(datos, contexto);
        beca.EmitirOferta(
            DatosOferta(beca.PostulacionOrigenId, montoPorPeriodo, beca.DuracionTotalPeriodos),
            contexto ?? Contexto());
        return beca;
    }

    public static Beca CrearBecaActiva(
        DatosCreacionBeca? datos = null,
        ContextoOperacion? contexto = null,
        decimal montoPorPeriodo = 500m)
    {
        var beca = CrearBecaConOferta(datos, contexto, montoPorPeriodo);
        var aceptacion = Contexto(
            (contexto?.Fecha ?? FechaBase).AddDays(1),
            autorizacionOtpId: null);
        beca.AceptarOferta(beca.Oferta!.Id, aceptacion);
        return beca;
    }

    public static string Hash(char caracter)
    {
        var hex = "0123456789abcdef"[caracter % 16];
        return new string(hex, 64);
    }
}
