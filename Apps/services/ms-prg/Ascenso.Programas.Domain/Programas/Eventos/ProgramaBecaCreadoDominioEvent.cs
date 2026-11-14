using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido al registrar un nuevo programa de becas en estado borrador.
/// </summary>
public sealed record class ProgramaBecaCreadoDominioEvent : EventoDominio
{
    public ProgramaBecaCreadoDominioEvent(
        Guid programaBecaId,
        string codigo,
        string nombre,
        ModalidadCobertura modalidadCobertura,
        int cupoTotal,
        string periodoInicialId,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ProgramaBecaId = programaBecaId;
        Codigo = codigo;
        Nombre = nombre;
        ModalidadCobertura = modalidadCobertura;
        CupoTotal = cupoTotal;
        PeriodoInicialId = periodoInicialId;
    }

    public Guid ProgramaBecaId { get; }

    public string Codigo { get; }

    public string Nombre { get; }

    public ModalidadCobertura ModalidadCobertura { get; }

    public int CupoTotal { get; }

    public string PeriodoInicialId { get; }
}
