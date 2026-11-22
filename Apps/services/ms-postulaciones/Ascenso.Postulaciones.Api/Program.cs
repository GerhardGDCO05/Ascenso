using Ascenso.Postulaciones.Api.Middlewares;
using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ascenso Postulaciones API",
        Version = "v1",
        Description = "API de postulaciones de ascenso. Endpoints protegidos con JWT de Keycloak."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el access token de Keycloak sin la palabra Bearer."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = []
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesDominio>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualJwt>();

var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
var keycloakAudience = builder.Configuration["Keycloak:Audience"];
var requireHttpsMetadata = builder.Configuration.GetValue<bool?>("Keycloak:RequireHttpsMetadata")
    ?? !builder.Environment.IsDevelopment();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = requireHttpsMetadata;

        if (!string.IsNullOrWhiteSpace(keycloakAuthority))
        {
            options.Authority = keycloakAuthority;
        }

        if (!string.IsNullOrWhiteSpace(keycloakAudience))
        {
            options.Audience = keycloakAudience;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "sub",
            RoleClaimType = "roles",
            ValidateIssuer = !string.IsNullOrWhiteSpace(keycloakAuthority),
            ValidateAudience = !string.IsNullOrWhiteSpace(keycloakAudience)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        PoliticasAutorizacionRubricas.CoordinadorPrograma,
        policy => policy.RequireRole(PoliticasAutorizacionRubricas.RolCoordinadorPrograma));

    options.AddPolicy(
        PoliticasAutorizacionRubricas.LecturaRubrica,
        policy => policy.RequireRole(PoliticasAutorizacionRubricas.RolCoordinadorPrograma));
});
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ManejadorResultadoAutorizacionRubricas>();

builder.Services.AgregarAplicacionPostulaciones();

var cadenaConexionPostulaciones = builder.Configuration.GetConnectionString("Postulaciones");
if (!string.IsNullOrWhiteSpace(cadenaConexionPostulaciones))
{
    builder.Services.AgregarInfraestructuraPostulaciones(
        cadenaConexionPostulaciones,
        builder.Configuration);
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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ascenso Postulaciones API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
