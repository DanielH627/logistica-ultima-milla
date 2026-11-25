using LogisticaUltimaMilla.Api.Security;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using Npgsql;

namespace LogisticaUltimaMilla.Api.Endpoints;

/// <summary>Consultas operativas protegidas con la clave de API: estado de la bandeja de salida.</summary>
public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/operations").AddEndpointFilter<ApiKeyFilter>();

        group.MapGet("/outbox", async (NpgsqlDataSource dataSource, IClock clock, CancellationToken cancellationToken) =>
        {
            var stats = await OutboxStatisticsReader.ReadAsync(dataSource, clock.UtcNow, cancellationToken);

            return Results.Ok(new
            {
                pending = stats.Pending,
                sent = stats.Sent,
                dead = stats.Dead,
                oldestPendingSeconds = stats.OldestPendingSeconds
            });
        }).WithName("OutboxStatus");

        return app;
    }
}
