using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using Npgsql;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>Un mensaje de la bandeja de salida, tal como quedó guardado.</summary>
internal sealed record OutboxRow(string MessageType, string Status, int Attempts, string Payload);

/// <summary>Datos de prueba: crean repartidores, rutas y entregas directamente en la base.</summary>
internal static class PostgresTestData
{
    public static async Task<CourierId> InsertCourierWithRouteAsync(
        NpgsqlDataSource dataSource,
        string name,
        int capacity,
        int occupiedStops,
        bool isActive = true)
    {
        var courierId = Guid.NewGuid();
        var routeId = Guid.NewGuid();

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var courier = new NpgsqlCommand(
            "INSERT INTO ruteo.repartidor (id, name, is_active) VALUES (@id, @name, @active)",
            connection,
            transaction))
        {
            courier.Parameters.AddWithValue("id", courierId);
            courier.Parameters.AddWithValue("name", name);
            courier.Parameters.AddWithValue("active", isActive);
            await courier.ExecuteNonQueryAsync();
        }

        await using (var route = new NpgsqlCommand(
            "INSERT INTO ruteo.ruta (id, courier_id, route_date, max_stops) VALUES (@id, @courier_id, @route_date, @max_stops)",
            connection,
            transaction))
        {
            route.Parameters.AddWithValue("id", routeId);
            route.Parameters.AddWithValue("courier_id", courierId);
            route.Parameters.AddWithValue("route_date", DateOnly.FromDateTime(DateTime.UtcNow));
            route.Parameters.AddWithValue("max_stops", capacity);
            await route.ExecuteNonQueryAsync();
        }

        for (var position = 1; position <= occupiedStops; position++)
        {
            await using var stop = new NpgsqlCommand(
                "INSERT INTO ruteo.parada (route_id, delivery_id, stop_position) VALUES (@route_id, @delivery_id, @position)",
                connection,
                transaction);
            stop.Parameters.AddWithValue("route_id", routeId);
            stop.Parameters.AddWithValue("delivery_id", Guid.NewGuid());
            stop.Parameters.AddWithValue("position", position);
            await stop.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return new CourierId(courierId);
    }

    public static async Task<CourierId> InsertCourierWithoutRouteAsync(NpgsqlDataSource dataSource, string name)
    {
        var courierId = Guid.NewGuid();

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO ruteo.repartidor (id, name, is_active) VALUES (@id, @name, true)",
            connection);
        command.Parameters.AddWithValue("id", courierId);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync();

        return new CourierId(courierId);
    }

    public static async Task<int> CountStopsAsync(NpgsqlDataSource dataSource, CourierId courierId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*)
            FROM ruteo.parada p
            JOIN ruteo.ruta r ON r.id = p.route_id
            WHERE r.courier_id = @courier_id
            """,
            connection);
        command.Parameters.AddWithValue("courier_id", courierId.Value);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>Mensajes de la bandeja de salida que pertenecen a una entrega.</summary>
    public static async Task<IReadOnlyList<OutboxRow>> GetOutboxRowsAsync(NpgsqlDataSource dataSource, DeliveryId deliveryId)
    {
        var rows = new List<OutboxRow>();

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT message_type, status, attempts, payload::text
            FROM transversal.bandeja_salida
            WHERE aggregate_id = @aggregate_id
            ORDER BY occurred_at
            """,
            connection);
        command.Parameters.AddWithValue("aggregate_id", deliveryId.Value);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new OutboxRow(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3)));
        }

        return rows;
    }

    public static Delivery NewPendingDelivery(DeliveryPriority priority = DeliveryPriority.Normal) =>
        Delivery.CreatePending(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Carrera 70 # 44-18"),
            new DeliveryWindow(
                new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 13, 16, 0, 0, TimeSpan.Zero)),
            priority);

    public static async Task<DeliveryId> SeedPendingDeliveryAsync(IDeliveryRepository repository)
    {
        var delivery = NewPendingDelivery();
        await repository.SaveAsync(delivery);
        return delivery.Id;
    }
}
