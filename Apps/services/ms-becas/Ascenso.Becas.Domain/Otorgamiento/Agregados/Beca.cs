using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Transiciones;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Agregados;

/// <summary>
/// Raíz del agregado de otorgamiento. Concentra las transiciones de la beca,
/// de su oferta y de su compromiso firmado.
/// </summary>
public sealed class Beca : AggregateRoot
{
    private const int DiasVigenciaOferta = 15;
    private const int LongitudMinimaJustificacion = 10;
    private const string JustificacionRechazoOferta = "Oferta rechazada por el postulante.";
    private const string JustificacionExpiracionOferta = "Expiración automática de la oferta de beca.";

    private readonly List<HistorialEstadoBeca> _historial = [];

    private Beca()
    {
    }

    private Beca(Guid id, ContextoOperacion contexto)
        : base(id, contexto.Fecha, contexto.UsuarioAutorId)
    {
    }

    public Guid CadenaBecaId { get; private set; }

    public Guid? BecaAnteriorId { get; private set; }

    public string CodigoBeca { get; private set; } = string.Empty;

    public Guid PostulanteEstudianteId { get; private set; }

    public string NumeroDocumentoEstudiante { get; private set; } = string.Empty;

    public Guid PostulacionOrigenId { get; private set; }

    public Guid ProgramaBecaId { get; private set; }

    public Guid ConvocatoriaOrigenId { get; private set; }

    public Guid PeriodoOtorgamientoId { get; private set; }

    public Guid PeriodoVigenteActualId { get; private set; }

    public int PeriodosCursados { get; private set; }

    public int DuracionTotalPeriodos { get; private set; }

    public PoliticaCobertura CoberturaAplicada { get; private set; } = null!;

    public Guid CostoMatriculaReferenciaSnapshotId { get; private set; }

    public decimal MontoComprometidoPeriodoActual { get; private set; }

    public decimal MontoDesembolsadoPeriodoActual { get; private set; }

    public EstadoBeca Estado { get; private set; }

    public TipoCierreBeca? TipoCierre { get; private set; }

    public bool PermiteAcumulacion { get; private set; }

    public bool RequiereCompromisoFirmado { get; private set; }

    public bool CompromisoFirmadoAprobado { get; private set; }

    public bool HabilitadaParaDesembolso { get; private set; }

    public bool TieneRenovacionEnCurso { get; private set; }

    public DateOnly FechaOtorgamiento { get; private set; }

    public DateOnly? FechaActivacion { get; private set; }

    public DateOnly? FechaConclusionORevocacion { get; private set; }

    public OfertaBeca? Oferta { get; private set; }

    public CompromisoBecario? Compromiso { get; private set; }

    public IReadOnlyCollection<HistorialEstadoBeca> Historial => _historial.AsReadOnly();

    public static Beca CrearOtorgada(
        DatosCreacionBeca datos,
        ContextoOperacion contexto,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(contexto);
        ValidarDatosCreacion(datos);

        var becaId = id ?? UuidV7.Nuevo();
        if (!UuidV7.EsVersion7(becaId))
        {
            throw new ArgumentException("El identificador de la beca debe ser UUIDv7.", nameof(id));
        }

        var beca = new Beca(becaId, contexto)
        {
            CadenaBecaId = becaId,
            BecaAnteriorId = null,
            CodigoBeca = datos.CodigoBeca.Trim(),
            PostulanteEstudianteId = datos.PostulanteEstudianteId,
            NumeroDocumentoEstudiante = datos.NumeroDocumentoEstudiante.Trim(),
            PostulacionOrigenId = datos.PostulacionOrigenId,
            ProgramaBecaId = datos.ProgramaBecaId,
            ConvocatoriaOrigenId = datos.ConvocatoriaOrigenId,
            PeriodoOtorgamientoId = datos.PeriodoOtorgamientoId,
            PeriodoVigenteActualId = datos.PeriodoOtorgamientoId,
            PeriodosCursados = 0,
            DuracionTotalPeriodos = datos.DuracionTotalPeriodos,
            CoberturaAplicada = datos.CoberturaAplicada,
            CostoMatriculaReferenciaSnapshotId = datos.CostoMatriculaReferenciaSnapshotId,
            MontoComprometidoPeriodoActual = datos.MontoComprometidoPeriodoActual,
            MontoDesembolsadoPeriodoActual = 0m,
            Estado = EstadoBeca.Otorgada,
            TipoCierre = null,
            PermiteAcumulacion = datos.PermiteAcumulacion,
            RequiereCompromisoFirmado = datos.RequiereCompromisoFirmado,
            CompromisoFirmadoAprobado = false,
            HabilitadaParaDesembolso = false,
            TieneRenovacionEnCurso = false,
            FechaOtorgamiento = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime),
            FechaActivacion = null,
            FechaConclusionORevocacion = null
        };

        if (datos.RequiereCompromisoFirmado)
        {
            beca.Compromiso = CompromisoBecario.Crear(becaId, contexto);
        }

        beca.RegistrarHistorialBeca(
            estadoAnterior: null,
            estadoNuevo: EstadoBeca.Otorgada,
            motivo: "Creación de la beca en estado OTORGADA",
            contexto,
            estadoTerminalIrreversible: false);

        return beca;
    }

    public OfertaBeca EmitirOferta(
        DatosOfertaBeca datos,
        ContextoOperacion contexto,
        Guid? ofertaId = null)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(contexto);

        if (Estado != EstadoBeca.Otorgada)
        {
            throw new InvalidOperationException("Solo una beca en estado OTORGADA puede emitir una oferta.");
        }

        if (Oferta is not null)
        {
            throw new InvalidOperationException("La beca ya tiene una oferta emitida.");
        }

        if (datos.PostulacionId != PostulacionOrigenId)
        {
            throw new ArgumentException(
                "La postulación de la oferta debe coincidir con la postulación de origen de la beca.",
                nameof(datos));
        }

        if (datos.DuracionPeriodos != DuracionTotalPeriodos)
        {
            throw new ArgumentException(
                "La duración de la oferta debe coincidir con la duración total de la beca.",
                nameof(datos));
        }

        var oferta = OfertaBeca.Crear(
            Id,
            datos,
            contexto,
            contexto.Fecha.AddDays(DiasVigenciaOferta),
            ofertaId);

        Oferta = oferta;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
        RegistrarHistorialOferta(
            estadoAnterior: null,
            estadoNuevo: EstadoOferta.PendienteRespuesta,
            motivo: "Emisión de la oferta de beca",
            contexto,
            estadoTerminalIrreversible: false);

        RegistrarEventoDominio(new OfertaBecaEmitidaEvento(
            Id,
            oferta.Id,
            oferta.PostulacionId,
            ProgramaBecaId,
            PeriodoVigenteActualId,
            oferta.FechaVencimientoAceptacion,
            contexto.CorrelacionId,
            contexto.Fecha));

        return oferta;
    }

    public void AceptarOferta(
        Guid ofertaId,
        ContextoOperacion contexto,
        Guid? autorizacionOtpAceptacionId = null)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var oferta = ObtenerOferta(ofertaId);

        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Activa);
        TransicionesOfertaBeca.Tabla.Validar(oferta.Estado, EstadoOferta.Aceptada);

        var estadoAnteriorBeca = Estado;
        var estadoAnteriorOferta = oferta.Estado;
        var contextoConOtp = autorizacionOtpAceptacionId is null
            ? contexto
            : ContextoConOtp(contexto, autorizacionOtpAceptacionId.Value);

        oferta.Aceptar(contextoConOtp, autorizacionOtpAceptacionId);

        Estado = EstadoBeca.Activa;
        FechaActivacion = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime);
        HabilitadaParaDesembolso = !RequiereCompromisoFirmado || CompromisoFirmadoAprobado;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialOferta(
            estadoAnteriorOferta,
            oferta.Estado,
            "Aceptación de la oferta",
            contextoConOtp,
            estadoTerminalIrreversible: true);
        RegistrarHistorialBeca(
            estadoAnteriorBeca,
            Estado,
            "Activación de la beca",
            contextoConOtp,
            estadoTerminalIrreversible: false);

        RegistrarEventoDominio(new BecaActivadaEvento(
            Id,
            oferta.Id,
            oferta.PostulacionId,
            ProgramaBecaId,
            PeriodoVigenteActualId,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    public void RechazarOferta(Guid ofertaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var oferta = ObtenerOferta(ofertaId);

        ValidarCierrePorOferta(EstadoOferta.RechazadaPorPostulante);

        var estadoAnteriorBeca = Estado;
        var estadoAnteriorOferta = oferta.Estado;

        oferta.Rechazar(contexto.Justificacion, contexto);
        CerrarPorOferta(TipoCierreBeca.OfertaRechazada, contexto);

        RegistrarHistorialOferta(
            estadoAnteriorOferta,
            oferta.Estado,
            "Rechazo de la oferta por el postulante",
            contexto,
            estadoTerminalIrreversible: true);
        RegistrarHistorialBeca(
            estadoAnteriorBeca,
            Estado,
            "Cierre de la beca por rechazo de la oferta",
            ContextoConJustificacion(contexto, JustificacionRechazoOferta),
            estadoTerminalIrreversible: true);

        RegistrarEventoDominio(new OfertaBecaRechazadaEvento(
            Id,
            oferta.Id,
            oferta.PostulacionId,
            ProgramaBecaId,
            PeriodoVigenteActualId,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    public void AnularOferta(Guid ofertaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var oferta = ObtenerOferta(ofertaId);

        ValidarCierrePorOferta(EstadoOferta.AnuladaPorCoordinacion);
        ExigirJustificacion(contexto, "anular la oferta de beca");

        var estadoAnteriorBeca = Estado;
        var estadoAnteriorOferta = oferta.Estado;
        var justificacion = contexto.Justificacion!;

        oferta.Anular(justificacion, contexto);
        CerrarPorOferta(TipoCierreBeca.OfertaAnulada, contexto);

        RegistrarHistorialOferta(
            estadoAnteriorOferta,
            oferta.Estado,
            "Anulación de la oferta por coordinación",
            contexto,
            estadoTerminalIrreversible: true);
        RegistrarHistorialBeca(
            estadoAnteriorBeca,
            Estado,
            "Cierre de la beca por anulación de la oferta",
            contexto,
            estadoTerminalIrreversible: true);

        RegistrarEventoDominio(new OfertaBecaAnuladaEvento(
            Id,
            oferta.Id,
            oferta.PostulacionId,
            ProgramaBecaId,
            PeriodoVigenteActualId,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    public void ExpirarOferta(Guid ofertaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var oferta = ObtenerOferta(ofertaId);

        ValidarCierrePorOferta(EstadoOferta.Expirada);

        var estadoAnteriorBeca = Estado;
        var estadoAnteriorOferta = oferta.Estado;

        oferta.Expirar(contexto);
        CerrarPorOferta(TipoCierreBeca.OfertaExpirada, contexto);

        RegistrarHistorialOferta(
            estadoAnteriorOferta,
            oferta.Estado,
            "Expiración automática de la oferta",
            contexto,
            estadoTerminalIrreversible: true);
        RegistrarHistorialBeca(
            estadoAnteriorBeca,
            Estado,
            "Cierre de la beca por expiración de la oferta",
            ContextoConJustificacion(contexto, JustificacionExpiracionOferta),
            estadoTerminalIrreversible: true);

        RegistrarEventoDominio(new OfertaBecaExpiradaEvento(
            Id,
            oferta.Id,
            oferta.PostulacionId,
            ProgramaBecaId,
            PeriodoVigenteActualId,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    public void Suspender(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Suspendida);
        ExigirJustificacion(contexto, "suspender la beca");

        var estadoAnterior = Estado;
        Estado = EstadoBeca.Suspendida;
        HabilitadaParaDesembolso = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialBeca(
            estadoAnterior,
            Estado,
            "Suspensión de la beca",
            contexto,
            estadoTerminalIrreversible: false);
    }

    public void Reactivar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (Estado != EstadoBeca.Suspendida)
        {
            throw new TransicionEstadoInvalidaException(Estado, EstadoBeca.Activa);
        }

        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Activa);
        ExigirJustificacion(contexto, "reactivar la beca");

        var estadoAnterior = Estado;
        Estado = EstadoBeca.Activa;
        HabilitadaParaDesembolso = !RequiereCompromisoFirmado || CompromisoFirmadoAprobado;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialBeca(
            estadoAnterior,
            Estado,
            "Reactivación de la beca",
            contexto,
            estadoTerminalIrreversible: false);
    }

    public void Renovar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Renovada);
        ExigirJustificacion(contexto, "renovar la beca");

        var estadoAnterior = Estado;
        Estado = EstadoBeca.Renovada;
        FechaConclusionORevocacion = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime);
        HabilitadaParaDesembolso = false;
        TieneRenovacionEnCurso = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialBeca(
            estadoAnterior,
            Estado,
            "Renovación de la beca",
            contexto,
            estadoTerminalIrreversible: true);
    }

    public void Concluir(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Concluida);
        ExigirJustificacion(contexto, "concluir la beca");

        var estadoAnterior = Estado;
        Estado = EstadoBeca.Concluida;
        FechaConclusionORevocacion = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime);
        HabilitadaParaDesembolso = false;
        TieneRenovacionEnCurso = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialBeca(
            estadoAnterior,
            Estado,
            "Conclusión de la beca",
            contexto,
            estadoTerminalIrreversible: true);
    }

    public void Revocar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (Estado is not (EstadoBeca.Activa or EstadoBeca.Suspendida))
        {
            throw new TransicionEstadoInvalidaException(Estado, EstadoBeca.Revocada);
        }

        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Revocada);
        ExigirJustificacion(contexto, "revocar la beca");

        var estadoAnterior = Estado;
        CerrarPorOferta(TipoCierreBeca.RevocacionDefinitiva, contexto);

        RegistrarHistorialBeca(
            estadoAnterior,
            Estado,
            "Revocación definitiva de la beca",
            contexto,
            estadoTerminalIrreversible: true);
    }

    public void CargarCompromiso(
        string urlPdfFirmadoFirebase,
        string hashDocumentoSha256,
        string ipCargaFirma,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var compromiso = ObtenerCompromiso();
        var estadoAnterior = compromiso.Estado;

        compromiso.Cargar(urlPdfFirmadoFirebase, hashDocumentoSha256, ipCargaFirma, contexto);
        CompromisoFirmadoAprobado = false;
        HabilitadaParaDesembolso = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialCompromiso(
            estadoAnterior,
            compromiso.Estado,
            "Carga del compromiso firmado",
            contexto);

        RegistrarEventoDominio(new CompromisoBecarioCargadoEvento(
            Id,
            compromiso.Id,
            compromiso.NumeroIntentoCarga,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    public void AprobarCompromiso(string? observaciones, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var compromiso = ObtenerCompromiso();
        var estadoAnterior = compromiso.Estado;

        compromiso.Aprobar(observaciones, contexto);
        CompromisoFirmadoAprobado = true;
        HabilitadaParaDesembolso = Estado == EstadoBeca.Activa;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialCompromiso(
            estadoAnterior,
            compromiso.Estado,
            "Aprobación del compromiso firmado",
            contexto);

        RegistrarEventoDominio(new CompromisoBecarioAprobadoEvento(
            Id,
            compromiso.Id,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    public void RechazarCompromiso(string observaciones, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var compromiso = ObtenerCompromiso();
        var estadoAnterior = compromiso.Estado;

        compromiso.Rechazar(observaciones, contexto);
        CompromisoFirmadoAprobado = false;
        HabilitadaParaDesembolso = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);

        RegistrarHistorialCompromiso(
            estadoAnterior,
            compromiso.Estado,
            "Rechazo del compromiso firmado",
            contexto);

        RegistrarEventoDominio(new CompromisoBecarioRechazadoEvento(
            Id,
            compromiso.Id,
            contexto.CorrelacionId,
            contexto.Fecha));
    }

    private static void ValidarDatosCreacion(DatosCreacionBeca datos)
    {
        if (datos.PostulanteEstudianteId == Guid.Empty)
        {
            throw new ArgumentException("El estudiante de la beca es obligatorio.", nameof(datos));
        }

        if (string.IsNullOrWhiteSpace(datos.NumeroDocumentoEstudiante))
        {
            throw new ArgumentException("El documento del estudiante es obligatorio.", nameof(datos));
        }

        if (datos.PostulacionOrigenId == Guid.Empty)
        {
            throw new ArgumentException("La postulación de origen es obligatoria.", nameof(datos));
        }

        if (datos.ProgramaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El programa de beca es obligatorio.", nameof(datos));
        }

        if (datos.ConvocatoriaOrigenId == Guid.Empty)
        {
            throw new ArgumentException("La convocatoria de origen es obligatoria.", nameof(datos));
        }

        if (datos.PeriodoOtorgamientoId == Guid.Empty)
        {
            throw new ArgumentException("El periodo de otorgamiento es obligatorio.", nameof(datos));
        }

        if (datos.DuracionTotalPeriodos <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datos),
                "La duración total de la beca debe ser mayor que cero.");
        }

        ArgumentNullException.ThrowIfNull(datos.CoberturaAplicada);

        if (datos.CostoMatriculaReferenciaSnapshotId == Guid.Empty)
        {
            throw new ArgumentException("El costo de matrícula de referencia es obligatorio.", nameof(datos));
        }

        if (datos.MontoComprometidoPeriodoActual < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(datos),
                "El monto comprometido del periodo no puede ser negativo.");
        }

        if (CalculadoraMontoOferta.Redondear(datos.MontoComprometidoPeriodoActual)
            != datos.MontoComprometidoPeriodoActual)
        {
            throw new ArgumentException(
                "El monto comprometido del periodo admite como máximo dos decimales.",
                nameof(datos));
        }

        if (string.IsNullOrWhiteSpace(datos.CodigoBeca))
        {
            throw new ArgumentException("El código de la beca es obligatorio.", nameof(datos));
        }
    }

    private void ValidarCierrePorOferta(EstadoOferta estadoDestino)
    {
        TransicionesBeca.Tabla.Validar(Estado, EstadoBeca.Revocada);
        TransicionesOfertaBeca.Tabla.Validar(Oferta!.Estado, estadoDestino);
    }

    private void CerrarPorOferta(TipoCierreBeca tipoCierre, ContextoOperacion contexto)
    {
        Estado = EstadoBeca.Revocada;
        TipoCierre = tipoCierre;
        FechaConclusionORevocacion = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime);
        HabilitadaParaDesembolso = false;
        TieneRenovacionEnCurso = false;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    private OfertaBeca ObtenerOferta(Guid ofertaId)
    {
        if (Oferta is null)
        {
            throw new InvalidOperationException("La beca no tiene una oferta emitida.");
        }

        if (Oferta.Id != ofertaId)
        {
            throw new InvalidOperationException("La oferta no pertenece a la beca.");
        }

        return Oferta;
    }

    private CompromisoBecario ObtenerCompromiso()
    {
        if (!RequiereCompromisoFirmado)
        {
            throw new InvalidOperationException("El programa de la beca no exige compromiso firmado.");
        }

        return Compromiso
            ?? throw new InvalidOperationException("La beca no tiene un compromiso inicializado.");
    }

    private static void ExigirJustificacion(ContextoOperacion contexto, string operacion)
    {
        if (!contexto.TieneJustificacionSuficiente(LongitudMinimaJustificacion))
        {
            throw new JustificacionRequeridaException(operacion);
        }
    }

    private static ContextoOperacion ContextoConJustificacion(
        ContextoOperacion contexto,
        string justificacion)
        => new(
            contexto.UsuarioAutorId,
            contexto.CorrelacionId,
            contexto.Fecha,
            justificacion,
            contexto.AutorizacionOtpId);

    private static ContextoOperacion ContextoConOtp(ContextoOperacion contexto, Guid autorizacionOtpId)
        => new(
            contexto.UsuarioAutorId,
            contexto.CorrelacionId,
            contexto.Fecha,
            contexto.Justificacion,
            autorizacionOtpId);

    private void RegistrarHistorialBeca(
        EstadoBeca? estadoAnterior,
        EstadoBeca estadoNuevo,
        string motivo,
        ContextoOperacion contexto,
        bool estadoTerminalIrreversible)
        => _historial.Add(HistorialEstadoBeca.ParaBeca(
            Id,
            estadoAnterior,
            estadoNuevo,
            motivo,
            contexto,
            estadoTerminalIrreversible));

    private void RegistrarHistorialOferta(
        EstadoOferta? estadoAnterior,
        EstadoOferta estadoNuevo,
        string motivo,
        ContextoOperacion contexto,
        bool estadoTerminalIrreversible)
        => _historial.Add(HistorialEstadoBeca.ParaOferta(
            Id,
            Oferta!.Id,
            estadoAnterior,
            estadoNuevo,
            motivo,
            contexto,
            estadoTerminalIrreversible));

    private void RegistrarHistorialCompromiso(
        EstadoCompromiso? estadoAnterior,
        EstadoCompromiso estadoNuevo,
        string motivo,
        ContextoOperacion contexto)
        => _historial.Add(HistorialEstadoBeca.ParaCompromiso(
            Id,
            Compromiso!.Id,
            estadoAnterior,
            estadoNuevo,
            motivo,
            contexto));
}
