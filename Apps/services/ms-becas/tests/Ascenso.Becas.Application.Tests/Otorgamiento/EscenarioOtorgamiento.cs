using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;
using Ascenso.Becas.Application.Tests.Dobles;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

/// <summary>
/// Escenario con todos los dobles de prueba y las dependencias por defecto de
/// una emisión válida.
/// </summary>
internal sealed class EscenarioOtorgamiento
{
    public Guid PostulacionId { get; } = Guid.CreateVersion7();

    public Guid ProgramaBecaId { get; } = Guid.CreateVersion7();

    public Guid PeriodoAcademicoId { get; } = Guid.CreateVersion7();

    public Guid UniversidadAliadaId { get; } = Guid.CreateVersion7();

    public Guid CarreraId { get; } = Guid.CreateVersion7();

    public Guid EstudianteId { get; } = Guid.CreateVersion7();

    public Guid ConvocatoriaId { get; } = Guid.CreateVersion7();

    public Guid CostoMatriculaId { get; } = Guid.CreateVersion7();

    public Guid ConvenioId { get; } = Guid.CreateVersion7();

    public ContextoOperacionFalso Contexto { get; } = new();

    public RelojFalso Reloj { get; } = new() { UtcNow = DatosAplicacion.FechaBase };

    public PostulacionesClientFalso Postulaciones { get; } = new();

    public ProgramasClientFalso Programas { get; } = new();

    public ProyeccionesOtorgamientoFalso Proyecciones { get; } = new();

    public MetricaConfiguracionFaltanteFalsa MetricaConfiguracion { get; } = new();

    public MetricasOtorgamientoFalsas Metricas { get; } = new();

    public GeneradorCartaFalso Generador { get; } = new();

    public GeneradorPlantillaFalso GeneradorPlantilla { get; } = new();

    public AlmacenamientoArchivosFalso Almacenamiento { get; } = new();

    public ProgramadorExpiracionFalso Programador { get; } = new();

    public BecaRepositoryFalso Repositorio { get; } = new();

    public CompromisoBecarioRepositoryFalso Compromisos { get; } = new();

    public UnidadDeTrabajoFalsa Unidad { get; } = new();

    public OutboxBecaFalso Outbox { get; } = new();

    public ConsultasBecaFalsas Consultas { get; } = new();

    public EscenarioOtorgamiento()
    {
        Postulaciones.Resultado = DatosAplicacion.Adjudicacion(
            PostulacionId,
            ProgramaBecaId,
            PeriodoAcademicoId,
            UniversidadAliadaId,
            CarreraId,
            EstudianteId,
            ConvocatoriaId);

        Proyecciones.Proyeccion = DatosAplicacion.Proyeccion(
            ProgramaBecaId,
            CostoMatriculaId,
            ConvenioId,
            DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime));

        Programas.Resultado = DatosAplicacion.Reserva(monto: 2000m);

        Unidad.AlConfirmar = _ =>
        {
            Outbox.Confirmar();
            return Task.CompletedTask;
        };

        Unidad.AlRevertir = _ =>
        {
            Outbox.Descartar();
            return Task.CompletedTask;
        };
    }

    public EmitirOfertaBecaHandler CrearEmitir()
        => new(
            Contexto,
            Reloj,
            Postulaciones,
            Proyecciones,
            MetricaConfiguracion,
            Metricas,
            Programas,
            Generador,
            GeneradorPlantilla,
            Almacenamiento,
            Programador,
            Repositorio,
            Unidad,
            Outbox);

    public AceptarOfertaBecaHandler CrearAceptar()
        => new(Contexto, Reloj, Metricas, Repositorio, Unidad, Outbox);

    public RechazarOfertaBecaHandler CrearRechazar()
        => new(Contexto, Reloj, Metricas, Repositorio, Unidad, Outbox);

    public AnularOfertaBecaHandler CrearAnular()
        => new(Contexto, Reloj, Repositorio, Unidad, Outbox);

    public ExpirarOfertaBecaHandler CrearExpirar()
        => new(Contexto, Reloj, Metricas, Repositorio, Unidad, Outbox);

    public CargarCompromisoBecarioHandler CrearCargarCompromiso()
        => new(Contexto, Reloj, Repositorio, Almacenamiento, Unidad, Outbox);

    public RevisarCompromisoBecarioHandler CrearRevisarCompromiso()
        => new(Contexto, Reloj, Repositorio, Compromisos, Unidad, Outbox);

    public ObtenerOfertaHandler CrearObtenerOferta() => new(Consultas);

    public ObtenerBecaHandler CrearObtenerBeca() => new(Consultas);

    public ConsultarBecasActivasHandler CrearConsultarActivas() => new(Consultas);
}
