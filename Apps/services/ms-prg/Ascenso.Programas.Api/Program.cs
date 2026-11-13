using Ascenso.Programas.Api.Middlewares;
using Ascenso.Programas.Application;
using Ascenso.Programas.Infrastructure;
using Ascenso.Programas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesDominio>();

builder.Services.AgregarAplicacionProgramas();

var cadenaConexionProgramas = builder.Configuration.GetConnectionString("Programas");
if (!string.IsNullOrWhiteSpace(cadenaConexionProgramas))
{
    builder.Services.AgregarInfraestructuraProgramas(cadenaConexionProgramas);
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
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetService<ProgramasDbContext>();
    if (dbContext is not null)
    {
        await dbContext.Database.MigrateAsync();
    }

    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
