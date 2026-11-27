using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Desembolsos.Entidades;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;

namespace Ascenso.Becas.Domain.Desembolsos.Agregados;

/// <summary>Plan financiero de una beca durante un periodo académico.</summary>
public sealed class CalendarioDesembolso : AggregateRoot
{
    private readonly List<Desembolso> _desembolsos = [];

    private CalendarioDesembolso()
    {
    }

    private CalendarioDesembolso(Guid id, ContextoOperacion contexto)
        : base(id, contexto.Fecha, contexto.UsuarioAutorId)
    {
    }

    public Guid BecaId { get; private set; }

    public Guid PeriodoAcademicoId { get; private set; }

    public FrecuenciaDesembolso Frecuencia { get; private set; }

    public DestinatarioPago DestinatarioPredeterminado { get; private set; }

    public decimal MontoMaximoAprobadoPeriodo { get; private set; }

    public decimal SumaCuotasProgramadas { get; private set; }

    public int NumeroCuotas { get; private set; }

    public DateOnly FechaInicio { get; private set; }

    public DateOnly FechaFin { get; private set; }

    public EstadoPlanDesembolso EstadoPlan { get; private set; }

    public Guid CreadoPorUsuarioId { get; private set; }

    public IReadOnlyCollection<Desembolso> Desembolsos => _desembolsos.AsReadOnly();

    public Desembolso ObtenerCuotaVigente(int numeroCuota)
        => _desembolsos.SingleOrDefault(cuota => cuota.NumeroCuota == numeroCuota && cuota.EsVigente)
            ?? throw new InvalidOperationException("La cuota vigente no existe.");

    public void RegistrarEjecucion(
        int numeroCuota,
        decimal monto,
        DateOnly fecha,
        DestinatarioPago destinatario,
        Guid? datosPagoBecarioId,
        string cuentaDestinoCifrada,
        MetodoPago metodo,
        string referencia,
        ContextoOperacion contexto)
    {
        var cuota = ObtenerCuotaVigente(numeroCuota);
        var totalEnProcesoOPagado = _desembolsos
            .Where(item => item.EsVigente && item.Id != cuota.Id
                && item.Estado is EstadoDesembolso.EnProceso or EstadoDesembolso.PagadoConfirmado)
            .Sum(item => item.MontoEjecutado ?? 0m);
        if (totalEnProcesoOPagado + monto > MontoMaximoAprobadoPeriodo)
        {
            throw new ArgumentException("EXCEDE_OFERTA: los pagos en proceso o confirmados superan el monto aprobado.", nameof(monto));
        }

        cuota.RegistrarEjecucion(monto, fecha, destinatario, datosPagoBecarioId,
            cuentaDestinoCifrada, metodo, referencia, contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public void Confirmar(int numeroCuota, Guid autorizacionOtpId, ContextoOperacion contexto)
    {
        ObtenerCuotaVigente(numeroCuota).Confirmar(autorizacionOtpId, contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public void SuspenderCuota(int numeroCuota, EstadoBeca estadoBeca, Guid suspensionId, Guid? pausaId, ContextoOperacion contexto)
    {
        if (estadoBeca != EstadoBeca.Suspendida)
        {
            throw new InvalidOperationException("Solo una beca suspendida puede suspender cuotas.");
        }

        ObtenerCuotaVigente(numeroCuota).Suspender(suspensionId, pausaId, contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public int SuspenderCuotasProgramadas(
        Guid becaId, EstadoBeca estadoBeca, Guid suspensionId,
        Guid? pausaId, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (suspensionId == Guid.Empty)
            throw new ArgumentException("La suspensión es obligatoria.", nameof(suspensionId));
        if (BecaId != becaId || estadoBeca != EstadoBeca.Suspendida)
            throw new InvalidOperationException("El calendario debe pertenecer a la beca suspendida.");

        var cuotas = _desembolsos.Where(item => item.EsVigente
            && item.Estado == EstadoDesembolso.Programado).ToArray();
        foreach (var cuota in cuotas)
            SuspenderCuota(cuota.NumeroCuota, estadoBeca, suspensionId, pausaId, contexto);

        return cuotas.Length;
    }

    public void ReanudarCuota(int numeroCuota, EstadoBeca estadoBeca, ContextoOperacion contexto)
    {
        if (estadoBeca != EstadoBeca.Activa)
        {
            throw new InvalidOperationException("Solo una beca activa puede reanudar cuotas.");
        }

        ObtenerCuotaVigente(numeroCuota).Reanudar(contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public int ReanudarCuotasSuspendidas(
        Guid becaId, EstadoBeca estadoBeca, Guid suspensionId,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (suspensionId == Guid.Empty)
            throw new ArgumentException("La suspensión es obligatoria.", nameof(suspensionId));
        if (BecaId != becaId || estadoBeca != EstadoBeca.Activa)
            throw new InvalidOperationException("El calendario debe pertenecer a la beca reactivada.");

        var cuotas = _desembolsos.Where(item => item.EsVigente
            && item.Estado == EstadoDesembolso.Suspendido
            && item.SuspensionDesembolsoId == suspensionId).ToArray();
        if (cuotas.Any(item => item.PausaAplazamientoId.HasValue))
            throw new InvalidOperationException(
                "La reanudación de cuotas de una pausa exige validación académica.");

        foreach (var cuota in cuotas)
            ReanudarCuota(cuota.NumeroCuota, estadoBeca, contexto);

        return cuotas.Length;
    }

    public void CancelarCuota(int numeroCuota, EstadoBeca estadoBeca, ContextoOperacion contexto)
    {
        if (estadoBeca is not (EstadoBeca.Revocada or EstadoBeca.Concluida))
        {
            throw new InvalidOperationException("Solo una beca revocada o concluida puede cancelar cuotas futuras.");
        }

        ObtenerCuotaVigente(numeroCuota).Cancelar(contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public int CancelarCuotasFuturasPorRevocacion(
        Guid becaId, EstadoBeca estadoBeca, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (BecaId != becaId || estadoBeca != EstadoBeca.Revocada)
            throw new InvalidOperationException("El calendario debe pertenecer a la beca revocada.");

        var cuotas = _desembolsos.Where(item => item.EsVigente
            && item.Estado is EstadoDesembolso.Programado or EstadoDesembolso.Suspendido).ToArray();
        foreach (var cuota in cuotas)
        {
            CancelarCuota(cuota.NumeroCuota, estadoBeca, contexto);
            RegistrarEventoDominio(new DesembolsoCanceladoEvento(
                BecaId, Id, cuota.Id, contexto.CorrelacionId, contexto.Fecha));
        }

        return cuotas.Length;
    }

    public IncidenciaDesembolso ReportarIncidencia(
        int numeroCuota,
        EstadoDesembolso estado,
        string motivo,
        string observacionBanco,
        ContextoOperacion contexto)
    {
        var incidencia = ObtenerCuotaVigente(numeroCuota)
            .ReportarIncidencia(estado, motivo, observacionBanco, contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
        return incidencia;
    }

    public void ResolverIncidencia(int numeroCuota, Guid incidenciaId, ContextoOperacion contexto)
    {
        ObtenerCuotaVigente(numeroCuota).ResolverIncidencia(incidenciaId, contexto);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public Desembolso Reintentar(int numeroCuota, ContextoOperacion contexto)
    {
        var nuevo = ObtenerCuotaVigente(numeroCuota).Reintentar(contexto);
        _desembolsos.Add(nuevo);
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
        return nuevo;
    }

    public static CalendarioDesembolso Programar(
        Guid becaId,
        Guid periodoAcademicoId,
        FrecuenciaDesembolso frecuencia,
        DestinatarioPago destinatarioPredeterminado,
        decimal montoMaximoAprobadoPeriodo,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        IEnumerable<CuotaProgramada> cuotas,
        ContextoOperacion contexto,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(cuotas);
        ArgumentNullException.ThrowIfNull(contexto);

        if (becaId == Guid.Empty || periodoAcademicoId == Guid.Empty)
        {
            throw new ArgumentException("La beca y el periodo académico son obligatorios.");
        }

        if (!Enum.IsDefined(frecuencia) || !Enum.IsDefined(destinatarioPredeterminado))
        {
            throw new ArgumentOutOfRangeException(nameof(frecuencia), "Frecuencia o destinatario inválido.");
        }

        ValidarMonto(montoMaximoAprobadoPeriodo, nameof(montoMaximoAprobadoPeriodo));

        if (fechaInicio == default || fechaFin < fechaInicio)
        {
            throw new ArgumentException("El rango de fechas del calendario es inválido.");
        }

        var cuotasMaterializadas = cuotas.ToArray();
        if (cuotasMaterializadas.Length == 0)
        {
            throw new ArgumentException("El calendario requiere al menos una cuota.", nameof(cuotas));
        }

        if (cuotasMaterializadas.Select(cuota => cuota.NumeroCuota).Distinct().Count() != cuotasMaterializadas.Length
            || cuotasMaterializadas.Any(cuota => cuota.NumeroCuota <= 0))
        {
            throw new ArgumentException("Los números de cuota deben ser positivos y únicos.", nameof(cuotas));
        }

        foreach (var cuota in cuotasMaterializadas)
        {
            ValidarMonto(cuota.Monto, nameof(cuotas));
            if (cuota.Monto == 0 || cuota.FechaProgramada < fechaInicio || cuota.FechaProgramada > fechaFin)
            {
                throw new ArgumentException("Cada cuota debe tener monto positivo y fecha dentro del calendario.", nameof(cuotas));
            }
        }

        if (!cuotasMaterializadas.Select(cuota => cuota.NumeroCuota).Order().SequenceEqual(Enumerable.Range(1, cuotasMaterializadas.Length))
            || !cuotasMaterializadas.OrderBy(cuota => cuota.NumeroCuota)
                .Select(cuota => cuota.FechaProgramada)
                .SequenceEqual(cuotasMaterializadas.OrderBy(cuota => cuota.FechaProgramada)
                    .Select(cuota => cuota.FechaProgramada)))
        {
            throw new ArgumentException("Las cuotas deben numerarse desde uno y mantener orden cronológico.", nameof(cuotas));
        }

        var total = cuotasMaterializadas.Sum(cuota => cuota.Monto);
        if (total > montoMaximoAprobadoPeriodo)
        {
            throw new ArgumentException("EXCEDE_OFERTA: las cuotas superan el monto aprobado.", nameof(cuotas));
        }

        var calendarioId = id ?? UuidV7.Nuevo();
        if (!UuidV7.EsVersion7(calendarioId))
        {
            throw new ArgumentException("El calendario debe tener un UUIDv7.", nameof(id));
        }

        var calendario = new CalendarioDesembolso(calendarioId, contexto)
        {
            BecaId = becaId,
            PeriodoAcademicoId = periodoAcademicoId,
            Frecuencia = frecuencia,
            DestinatarioPredeterminado = destinatarioPredeterminado,
            MontoMaximoAprobadoPeriodo = montoMaximoAprobadoPeriodo,
            SumaCuotasProgramadas = total,
            NumeroCuotas = cuotasMaterializadas.Length,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            EstadoPlan = EstadoPlanDesembolso.Programado,
            CreadoPorUsuarioId = contexto.UsuarioAutorId
        };

        foreach (var cuota in cuotasMaterializadas)
        {
            calendario._desembolsos.Add(Desembolso.Programar(
                calendarioId,
                becaId,
                cuota.NumeroCuota,
                cuota.Monto,
                cuota.FechaProgramada,
                destinatarioPredeterminado,
                contexto));
        }

        return calendario;
    }

    private static void ValidarMonto(decimal monto, string parametro)
    {
        if (monto < 0 || decimal.Round(monto, 2) != monto)
        {
            throw new ArgumentOutOfRangeException(parametro, "El monto debe ser no negativo y tener como máximo dos decimales.");
        }
    }
}
