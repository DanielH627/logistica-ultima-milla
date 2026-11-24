using LogisticaUltimaMilla.Api.Endpoints;
using LogisticaUltimaMilla.Api.Security;
using LogisticaUltimaMilla.Api.Startup;
using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using LogisticaUltimaMilla.Infrastructure.Routing;
using LogisticaUltimaMilla.Infrastructure.Time;
using Npgsql;

// Raíz de composición: el único lugar donde se decide qué adaptador concreto implementa cada puerto.
var builder = WebApplication.CreateBuilder(args);

// Las respuestas de error siguen un formato estándar y no exponen detalles internos.
builder.Services.AddProblemDetails();

// La clave de API es obligatoria: si falta, la aplicación no arranca.
builder.Services
    .AddOptions<SecurityOptions>()
    .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Falta la clave de API (Security:ApiKey).")
    .ValidateOnStart();

// La cadena de conexión se lee al usar el servicio, no antes: así toma la configuración definitiva.
builder.Services.AddSingleton(provider =>
{
    var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("Logistica")
        ?? throw new InvalidOperationException("Falta la cadena de conexión (ConnectionStrings:Logistica).");

    return NpgsqlDataSource.Create(connectionString);
});

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IDeliveryRepository, PostgresDeliveryRepository>();
builder.Services.AddSingleton<ICourierRoutes, PostgresCourierRoutes>();
builder.Services.AddSingleton<AssignCourierUseCase>();
builder.Services.AddHostedService<DatabaseInitializer>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapAssignmentEndpoints();

app.Run();

// Permite que las pruebas de integración arranquen la aplicación completa.
public partial class Program;
