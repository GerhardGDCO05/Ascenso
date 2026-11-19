using System.Globalization;
using Ascenso.Becas.Api.Contexto;
using Ascenso.Becas.Api.Correlacion;
using Ascenso.Becas.Api.Errores;
using Ascenso.Becas.Api.Idempotencia;
using Ascenso.Becas.Api.Logging;
using Ascenso.Becas.Api.OpenApi;
using Ascenso.Becas.Api.Salud;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Api.Trabajos;
using Ascenso.Becas.Application;
using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Infrastructure.Clientes;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Trabajos;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuracion) => configuracion
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.With<RedaccionLogEventEnricher>()
    .Destructure.With<RedaccionDestructuringPolicy>()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddControllers()
    .AgregarProblemDetailsDeValidacion();
builder.Services.AddOpenApi(opciones =>
{
    opciones.AddDocumentTransformer<DocumentoOpenApiTransformer>();
    opciones.AddOperationTransformer<OperacionOpenApiTransformer>();
    opciones.AddSchemaTransformer<EjemplosEsquemasOpenApiTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesApi>();
builder.Services.AgregarChequeosDeSalud();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelacionContexto, CorrelacionContexto>();
builder.Services.AddScoped<IContextoOperacionActual, ContextoOperacionHttp>();
builder.Services.AgregarAplicacion();
builder.Services.AddTransient<CorrelacionDelegatingHandler>();
builder.Services.AgregarAutenticacionJwt(builder.Configuration);
builder.Services.AgregarAutorizacion(builder.Configuration);

var cadenaConexionBecas = builder.Configuration.GetConnectionString("Becas")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Becas'.");

builder.Services.AgregarPersistencia(cadenaConexionBecas);
builder.Services.AgregarMetricas();
builder.Services.AgregarClientes(builder.Configuration);
builder.Services.AgregarMensajeria(builder.Configuration);
builder.Services.AgregarHangfire(cadenaConexionBecas);
builder.Services.AddHangfireServer(opciones => opciones.Queues =
    [RegistroHangfire.ColaCritica, RegistroHangfire.ColaPorDefecto]);
builder.Services.AgregarTrabajosHangfire(builder.Configuration);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapOpenApi("/swagger/{documentName}/swagger.json");
}

app.UseMiddleware<CorrelacionMiddleware>();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapearDashboardHangfire();
app.UseMiddleware<IdempotenciaMiddleware>();

app.MapControllers();
app.MapearSalud();

app.Run();

/// <summary>
/// Punto de entrada de la API, expuesto para las pruebas de integración con
/// <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program
{
}
