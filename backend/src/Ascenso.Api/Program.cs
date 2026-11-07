var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "Ascenso API",
    status = "ok"
}))
.WithName("GetApiInfo");

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
