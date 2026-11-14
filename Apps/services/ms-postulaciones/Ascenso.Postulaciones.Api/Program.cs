using Ascenso.Postulaciones.Api.Middlewares;
using Ascenso.Postulaciones.Application;
using Ascenso.Postulaciones.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesDominio>();

builder.Services.AgregarAplicacionPostulaciones();

var cadenaConexionPostulaciones = builder.Configuration.GetConnectionString("Postulaciones");
if (!string.IsNullOrWhiteSpace(cadenaConexionPostulaciones))
{
    builder.Services.AgregarInfraestructuraPostulaciones(cadenaConexionPostulaciones);
}

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

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
