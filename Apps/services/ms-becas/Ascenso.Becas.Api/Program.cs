using Ascenso.Becas.Infrastructure.Dependencias;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var cadenaConexionBecas = builder.Configuration.GetConnectionString("Becas")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Becas'.");

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

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();

app.Run();
