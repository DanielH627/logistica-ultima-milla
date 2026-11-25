using LogisticaUltimaMilla.Api.Endpoints;
using LogisticaUltimaMilla.Api.Observability;
using LogisticaUltimaMilla.Api.Security;
using LogisticaUltimaMilla.Api.Startup;
using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Infrastructure.Notifications;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using LogisticaUltimaMilla.Infrastructure.Routing;
using LogisticaUltimaMilla.Infrastructure.Time;
using Npgsql;

// Raíz de composición: el único lugar donde se decide qué adaptador concreto implementa cada puerto.
var builder = WebApplication.CreateBuilder(args);

// Registros: en JSON (una línea por evento, con sus campos y el ámbito de la solicitud) cuando está activado;
// en formato legible con ámbitos en desarrollo. Nunca se registran claves, cuerpos ni datos personales.
builder.Logging.ClearProviders();
if (builder.Configuration.GetValue<bool>("Observability:JsonLogs"))
{
    builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
}
else
{
    builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);
}

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

// Bandeja de salida (AJ-01): los eventos se guardan con la entrega y este procesador los entrega después.
builder.Services.AddSingleton(provider =>
    provider.GetRequiredService<IConfiguration>().GetSection(OutboxOptions.SectionName).Get<OutboxOptions>()
        ?? new OutboxOptions());

builder.Services.AddSingleton<INotificationRequester>(provider => new SimulatedNotificationGateway(
    provider.GetRequiredService<IConfiguration>().GetValue<bool>("Notifications:SimulateFailure"),
    provider.GetRequiredService<ILogger<SimulatedNotificationGateway>>()));

builder.Services.AddSingleton<IOutboxMessageHandler, EventHistoryHandler>();
builder.Services.AddSingleton<IOutboxMessageHandler, DeliveryAssignedNotificationHandler>();
builder.Services.AddSingleton<OutboxProcessor>();

// El orden importa: primero se prepara la base de datos y después arranca el procesador.
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddHostedService<OutboxBackgroundService>();

// Salud: "viva" no consulta nada externo; "lista" verifica la base de datos y la bandeja de salida.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<OutboxHealthCheck>("outbox", tags: new[] { "ready" });

var app = builder.Build();

// El orden importa: la correlación envuelve todo, de modo que hasta los errores quedan correlacionados.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

app.MapHealth();
app.MapOperationsEndpoints();
app.MapAssignmentEndpoints();

app.Run();

// Permite que las pruebas de integración arranquen la aplicación completa.
public partial class Program;
