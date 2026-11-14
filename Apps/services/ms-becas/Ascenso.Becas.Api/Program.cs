using System.Globalization;
using Ascenso.Becas.Api.Correlacion;
using Ascenso.Becas.Api.Errores;
using Ascenso.Becas.Api.Idempotencia;
using Ascenso.Becas.Api.Logging;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Infrastructure.Clientes;
using Ascenso.Becas.Infrastructure.Dependencias;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuracion) => configuracion
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.With<RedaccionLogEventEnricher>()
    .Destructure.With<RedaccionDestructuringPolicy>()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesApi>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelacionContexto, CorrelacionContexto>();
builder.Services.AddTransient<CorrelacionDelegatingHandler>();
builder.Services.AgregarAutenticacionJwt(builder.Configuration);
builder.Services.AgregarAutorizacion(builder.Configuration);

var cadenaConexionBecas = builder.Configuration.GetConnectionString("Becas")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Becas'.");

builder.Services.AgregarPersistencia(cadenaConexionBecas);
builder.Services.AgregarHangfire(cadenaConexionBecas);
builder.Services.AddHangfireServer(opciones => opciones.Queues = ["critical", "default"]);

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
}

app.UseMiddleware<CorrelacionMiddleware>();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotenciaMiddleware>();

app.MapControllers();

app.Run();
