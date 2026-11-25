using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>Reloj controlable: las pruebas avanzan el tiempo en lugar de esperar los retrasos reales.</summary>
internal sealed class TestClock : IClock
{
    // Arranca un minuto en el futuro: los mensajes se insertan con la hora de la base y deben estar vencidos.
    public DateTimeOffset UtcNow { get; private set; } = DateTimeOffset.UtcNow.AddMinutes(1);

    public void Advance(TimeSpan time) => UtcNow += time;
}

/// <summary>Solicitante de avisos que falla las primeras N veces. Seguro para uso concurrente.</summary>
internal sealed class FlakyNotifier : INotificationRequester
{
    private readonly int _failuresBeforeSuccess;
    private int _calls;
    private int _delivered;

    public FlakyNotifier(int failuresBeforeSuccess) => _failuresBeforeSuccess = failuresBeforeSuccess;

    public const string FailureMessage = "Servicio de avisos no disponible.";

    public int Calls => Volatile.Read(ref _calls);

    public int Delivered => Volatile.Read(ref _delivered);

    public Task RequestCourierAssignedNoticeAsync(CourierId courierId, DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        var call = Interlocked.Increment(ref _calls);

        if (call <= _failuresBeforeSuccess)
        {
            throw new NotificationRequestFailedException(FailureMessage);
        }

        Interlocked.Increment(ref _delivered);
        return Task.CompletedTask;
    }
}

/// <summary>Registro en memoria para comprobar qué se escribe en los logs.</summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    private readonly List<string> _lines = new();
    private readonly object _gate = new();

    public IReadOnlyList<string> Lines
    {
        get { lock (_gate) { return _lines.ToList(); } }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (_gate)
        {
            _lines.Add(formatter(state, exception));
        }
    }
}

internal sealed record BandejaRow(Guid Id, string Status, int Attempts, string? LastError, DateTimeOffset NextAttemptAt);

internal static class OutboxTestSupport
{
    public static OutboxProcessor CreateProcessor(
        NpgsqlDataSource dataSource,
        IEnumerable<IOutboxMessageHandler> handlers,
        TestClock clock,
        OutboxOptions? options = null,
        ILogger<OutboxProcessor>? logger = null) =>
        new(
            dataSource,
            handlers,
            clock,
            options ?? new OutboxOptions { MaxAttempts = 3, BaseDelaySeconds = 5 },
            logger ?? new ListLogger<OutboxProcessor>());

    public static async Task<BandejaRow> GetOnlyRowAsync(NpgsqlDataSource dataSource, Guid aggregateId)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id, status, attempts, last_error, next_attempt_at FROM transversal.bandeja_salida WHERE aggregate_id = @id");
        command.Parameters.AddWithValue("id", aggregateId);
        await using var reader = await command.ExecuteReaderAsync();

        Assert_True(await reader.ReadAsync(), "No hay mensajes en la bandeja de salida.");
        return new BandejaRow(
            reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetFieldValue<DateTimeOffset>(4));
    }

    public static async Task<int> CountHistoryAsync(NpgsqlDataSource dataSource, Guid aggregateId)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT count(*) FROM transversal.historial_eventos WHERE aggregate_id = @id");
        command.Parameters.AddWithValue("id", aggregateId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static void Assert_True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
