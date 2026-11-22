using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Ruteo;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Postgres;
using Npgsql;

namespace LogisticaUltimaMilla.Infrastructure.Routing;

/// <summary>
/// Adaptador del puerto ICourierRoutes sobre PostgreSQL. Representa el contrato público del contexto Ruteo
/// y es la capa anticorrupción del lado de Entregas: traduce rutas y paradas a "disponible o no".
///
/// La regla de capacidad (INV04) la protege el agregado Route. Para que dos reservas simultáneas no la superen,
/// la reserva bloquea la fila de la ruta, reconstruye el agregado y le pide agregar la parada.
/// Un repartidor tiene una sola ruta activa a la vez (supuesto A3-S03): se usa la más reciente.
/// </summary>
public sealed class PostgresCourierRoutes : ICourierRoutes
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresCourierRoutes(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<CourierAvailability> GetAvailabilityAsync(
        CourierId courierId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = """
                SELECT c.is_active,
                       r.max_stops,
                       (SELECT count(*) FROM ruteo.parada p WHERE p.route_id = r.id) AS stops
                FROM ruteo.repartidor c
                LEFT JOIN LATERAL (
                    SELECT id, max_stops
                    FROM ruteo.ruta
                    WHERE courier_id = c.id
                    ORDER BY route_date DESC
                    LIMIT 1) r ON true
                WHERE c.id = @courier_id
                """;

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.AddUuid("courier_id", courierId.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken) || !reader.GetBoolean(0))
            {
                return CourierAvailability.Unavailable(UnavailabilityReason.CourierNotActive);
            }

            if (reader.IsDBNull(1))
            {
                // El repartidor existe y está activo, pero no tiene una ruta para operar.
                return CourierAvailability.Unavailable(UnavailabilityReason.OutOfShift);
            }

            var maxStops = reader.GetInt32(1);
            var stops = reader.GetInt64(2);

            return stops >= maxStops
                ? CourierAvailability.Unavailable(UnavailabilityReason.RouteSaturated)
                : CourierAvailability.Available();
        }
        catch (NpgsqlException exception) when (exception.IsTransient)
        {
            throw new CourierRoutesUnavailableException("No se pudo consultar la disponibilidad en Ruteo.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new CourierRoutesUnavailableException("Ruteo no respondió a tiempo.", exception);
        }
    }

    public async Task<StopReservationStatus> ReserveStopAsync(
        CourierId courierId,
        DeliveryId deliveryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            // Se bloquea la fila de la ruta: dos reservas simultáneas sobre la misma ruta se ejecutan una tras otra.
            const string routeSql = """
                SELECT r.id, r.route_date, r.max_stops, c.is_active
                FROM ruteo.repartidor c
                JOIN ruteo.ruta r ON r.courier_id = c.id
                WHERE c.id = @courier_id
                ORDER BY r.route_date DESC
                LIMIT 1
                FOR UPDATE OF r
                """;

            Guid routeId;
            DateOnly routeDate;
            int maxStops;
            bool isActive;

            await using (var command = new NpgsqlCommand(routeSql, connection, transaction))
            {
                command.AddUuid("courier_id", courierId.Value);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return StopReservationStatus.CourierNotActive;
                }

                routeId = reader.GetGuid(0);
                routeDate = reader.GetFieldValue<DateOnly>(1);
                maxStops = reader.GetInt32(2);
                isActive = reader.GetBoolean(3);
            }

            if (!isActive)
            {
                return StopReservationStatus.CourierNotActive;
            }

            var route = new Route(new RouteId(routeId), courierId, routeDate, new RouteCapacity(maxStops));

            const string stopsSql = """
                SELECT delivery_id
                FROM ruteo.parada
                WHERE route_id = @route_id
                ORDER BY stop_position
                """;

            await using (var command = new NpgsqlCommand(stopsSql, connection, transaction))
            {
                command.AddUuid("route_id", routeId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    route.AddStop(new DeliveryId(reader.GetGuid(0)));
                }
            }

            try
            {
                // El agregado protege INV04: si no hay cupo, lanza la excepción de ruta saturada.
                route.AddStop(deliveryId);
            }
            catch (StopAlreadyRegisteredException)
            {
                // La reserva es repetible: si la entrega ya tiene su parada, no se agrega otra.
                return StopReservationStatus.Reserved;
            }
            catch (RouteSaturatedException)
            {
                return StopReservationStatus.RouteSaturated;
            }

            const string insertSql = """
                INSERT INTO ruteo.parada (route_id, delivery_id, stop_position)
                VALUES (@route_id, @delivery_id, @stop_position)
                """;

            await using (var command = new NpgsqlCommand(insertSql, connection, transaction))
            {
                command.AddUuid("route_id", routeId);
                command.AddUuid("delivery_id", deliveryId.Value);
                command.AddInteger("stop_position", route.Stops[route.Stops.Count - 1].Position);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return StopReservationStatus.Reserved;
        }
        catch (NpgsqlException exception) when (exception.IsTransient)
        {
            throw new CourierRoutesUnavailableException("No se pudo reservar el cupo en Ruteo.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new CourierRoutesUnavailableException("Ruteo no respondió a tiempo.", exception);
        }
    }

    public async Task ReleaseStopAsync(
        CourierId courierId,
        DeliveryId deliveryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = """
                DELETE FROM ruteo.parada
                WHERE delivery_id = @delivery_id
                  AND route_id IN (SELECT id FROM ruteo.ruta WHERE courier_id = @courier_id)
                """;

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.AddUuid("delivery_id", deliveryId.Value);
            command.AddUuid("courier_id", courierId.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (NpgsqlException exception) when (exception.IsTransient)
        {
            throw new CourierRoutesUnavailableException("No se pudo liberar el cupo en Ruteo.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new CourierRoutesUnavailableException("Ruteo no respondió a tiempo.", exception);
        }
    }
}
