using LogisticaUltimaMilla.Infrastructure.Postgres;
using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Seeding;

/// <summary>
/// Carga datos ficticios para demostrar el flujo. Es repetible: si los datos ya existen, no los duplica.
/// Solo se usa en desarrollo; nunca debe ejecutarse sobre datos reales.
/// </summary>
public sealed class DemoDataSeeder
{
    private readonly NpgsqlDataSource _dataSource;

    public DemoDataSeeder(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await InsertCourierAsync(connection, transaction, DemoData.CourierAndres, "Andrés", true, DemoData.RouteAndres, today, 10, 6, 1, cancellationToken);
        await InsertCourierAsync(connection, transaction, DemoData.CourierLuisa, "Luisa", true, DemoData.RouteLuisa, today, 10, 2, 2, cancellationToken);
        await InsertCourierAsync(connection, transaction, DemoData.CourierPaola, "Paola", false, DemoData.RoutePaola, today, 10, 0, 3, cancellationToken);
        await InsertCourierAsync(connection, transaction, DemoData.CourierCamilo, "Camilo", true, DemoData.RouteCamilo, today, 10, 10, 4, cancellationToken);

        var windowStart = DateTimeOffset.UtcNow.AddHours(2);

        var pending = new[] { DemoData.Delivery1, DemoData.Delivery2, DemoData.Delivery3, DemoData.Delivery4 };
        for (var index = 0; index < pending.Length; index++)
        {
            await InsertDeliveryAsync(connection, transaction, pending[index], index + 1, windowStart, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task InsertCourierAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid courierId,
        string name,
        bool isActive,
        Guid routeId,
        DateOnly routeDate,
        int capacity,
        int occupiedStops,
        int courierNumber,
        CancellationToken cancellationToken)
    {
        await using (var courier = new NpgsqlCommand(
            "INSERT INTO ruteo.repartidor (id, name, is_active) VALUES (@id, @name, @is_active) ON CONFLICT (id) DO NOTHING",
            connection,
            transaction))
        {
            courier.AddUuid("id", courierId);
            courier.AddText("name", name);
            courier.AddBoolean("is_active", isActive);
            await courier.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var route = new NpgsqlCommand(
            "INSERT INTO ruteo.ruta (id, courier_id, route_date, max_stops) VALUES (@id, @courier_id, @route_date, @max_stops) ON CONFLICT (id) DO NOTHING",
            connection,
            transaction))
        {
            route.AddUuid("id", routeId);
            route.AddUuid("courier_id", courierId);
            route.Parameters.AddWithValue("route_date", routeDate);
            route.AddInteger("max_stops", capacity);
            await route.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var position = 1; position <= occupiedStops; position++)
        {
            // Paradas ocupadas por entregas ficticias, con identificadores deterministas.
            var stopDeliveryId = Guid.Parse($"c{courierNumber:D7}-0000-0000-0000-{position:D12}");

            await using var stop = new NpgsqlCommand(
                "INSERT INTO ruteo.parada (route_id, delivery_id, stop_position) VALUES (@route_id, @delivery_id, @stop_position) ON CONFLICT (route_id, delivery_id) DO NOTHING",
                connection,
                transaction);
            stop.AddUuid("route_id", routeId);
            stop.AddUuid("delivery_id", stopDeliveryId);
            stop.AddInteger("stop_position", position);
            await stop.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task InsertDeliveryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid deliveryId,
        int number,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO entregas.entrega
                (id, order_id, address, window_start, window_end, priority, status, version)
            VALUES
                (@id, @order_id, @address, @window_start, @window_end, 'Normal', 'PendingAssignment', 1)
            ON CONFLICT (id) DO NOTHING
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.AddUuid("id", deliveryId);
        command.AddUuid("order_id", Guid.Parse($"e{number:D7}-0000-0000-0000-000000000001"));
        command.AddText("address", $"Dirección ficticia {number}");
        command.AddTimestampTz("window_start", windowStart);
        command.AddTimestampTz("window_end", windowStart.AddHours(2));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
