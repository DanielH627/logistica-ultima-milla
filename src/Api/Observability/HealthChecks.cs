using System.Text.Json;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace LogisticaUltimaMilla.Api.Observability;

/// <summary>Verifica que la base de datos responda. No expone la excepción: solo el estado.</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource _dataSource;

    public DatabaseHealthCheck(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            await using var command = _dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(timeout.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            // Sin pasar la excepción: así ni la cadena de conexión ni el servidor llegan a la respuesta.
            return HealthCheckResult.Unhealthy("La base de datos no responde.");
        }
    }
}

/// <summary>
/// Degradada si hay mensajes "Dead" (requieren revisión) o si el pendiente más antiguo lleva demasiado esperando
/// (el procesador está detenido o atrasado). La aplicación sigue funcionando: por eso es Degraded y no Unhealthy.
/// </summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    public static readonly TimeSpan MaxPendingAge = TimeSpan.FromMinutes(5);

    private readonly NpgsqlDataSource _dataSource;
    private readonly IClock _clock;

    public OutboxHealthCheck(NpgsqlDataSource dataSource, IClock clock)
    {
        _dataSource = dataSource;
        _clock = clock;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            var stats = await OutboxStatisticsReader.ReadAsync(_dataSource, _clock.UtcNow, timeout.Token);

            if (stats.Dead > 0)
            {
                return HealthCheckResult.Degraded($"Hay {stats.Dead} mensajes muertos por revisar.");
            }

            if (stats.OldestPendingSeconds > MaxPendingAge.TotalSeconds)
            {
                return HealthCheckResult.Degraded("Hay mensajes pendientes con demasiado tiempo de espera.");
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("No se pudo consultar la bandeja de salida.");
        }
    }
}

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        // Viva: el proceso responde. No consulta nada externo.
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteMinimalAsync
        });

        // Lista: puede atender solicitudes (base de datos y bandeja de salida).
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteMinimalAsync
        });

        return app;
    }

    // La respuesta solo trae estados: nunca descripciones, excepciones ni datos internos.
    private static Task WriteMinimalAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
