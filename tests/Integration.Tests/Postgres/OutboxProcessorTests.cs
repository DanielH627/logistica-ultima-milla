using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

/// <summary>
/// Comprueba el procesador de la bandeja de salida contra PostgreSQL real: entrega, reintentos,
/// mensajes "Dead", idempotencia, concurrencia y ausencia de datos personales en registros y errores.
/// El tiempo lo controla un reloj de prueba: no hay esperas reales.
/// </summary>
public class OutboxProcessorTests : PostgresTestBase
{
    private const string Address = "Carrera 70 # 44-18";
    private const string Requester = "Sandra";

    private static readonly DateTimeOffset AssignedAt = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

    private PostgresDeliveryRepository Repository => new(DataSource);

    /// <summary>Crea una entrega ya asignada, con su mensaje pendiente en la bandeja.</summary>
    private async Task<DeliveryId> CreateAssignedDeliveryAsync()
    {
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(Repository);
        var delivery = await Repository.GetAsync(deliveryId);
        delivery!.AssignCourier(CourierId.New(), AssignedAt, Requester);
        await Repository.SaveAsync(delivery);
        return deliveryId;
    }

    private (OutboxProcessor Processor, FlakyNotifier Notifier, TestClock Clock, ListLogger<OutboxProcessor> Log) Build(
        int notifierFailures, OutboxOptions? options = null)
    {
        var notifier = new FlakyNotifier(notifierFailures);
        var clock = new TestClock();
        var log = new ListLogger<OutboxProcessor>();
        IOutboxMessageHandler[] handlers =
        [
            new EventHistoryHandler(DataSource),
            new DeliveryAssignedNotificationHandler(notifier)
        ];

        return (OutboxTestSupport.CreateProcessor(DataSource, handlers, clock, options, log), notifier, clock, log);
    }

    [RequiresPostgresFact]
    public async Task Process_PendingMessage_RunsHandlersAndMarksItSent()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var (processor, notifier, _, _) = Build(notifierFailures: 0);

        var handled = await processor.ProcessPendingAsync();

        Assert.Equal(1, handled);
        var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        Assert.Equal("Sent", row.Status);
        Assert.Equal(1, row.Attempts);
        Assert.Equal(1, notifier.Delivered);
        Assert.Equal(1, await OutboxTestSupport.CountHistoryAsync(DataSource, deliveryId.Value));
    }

    [RequiresPostgresFact]
    public async Task Process_WithNoMessages_HandlesNothing()
    {
        var (processor, _, _, _) = Build(notifierFailures: 0);

        Assert.Equal(0, await processor.ProcessPendingAsync());
    }

    [RequiresPostgresFact]
    public async Task Process_AlreadySentMessage_IsNotProcessedAgain()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var (processor, notifier, _, _) = Build(notifierFailures: 0);

        await processor.ProcessPendingAsync();
        var secondRun = await processor.ProcessPendingAsync();

        Assert.Equal(0, secondRun);
        Assert.Equal(1, notifier.Calls);
        Assert.Equal("Sent", (await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value)).Status);
    }

    [RequiresPostgresFact]
    public async Task Process_FailingNotification_RetriesWithExponentialBackoffAndEventuallySucceeds()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var (processor, notifier, clock, _) = Build(notifierFailures: 2);

        // Intento 1: falla; queda pendiente y se reintenta 5 s después.
        var firstRunTime = clock.UtcNow;
        Assert.Equal(1, await processor.ProcessPendingAsync());
        var afterFirst = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        Assert.Equal("Pending", afterFirst.Status);
        Assert.Equal(1, afterFirst.Attempts);
        Assert.Equal(firstRunTime.AddSeconds(5), afterFirst.NextAttemptAt, TimeSpan.FromMilliseconds(5));

        // Antes de que venza la espera no se vuelve a intentar.
        Assert.Equal(0, await processor.ProcessPendingAsync());
        Assert.Equal(1, notifier.Calls);

        // Intento 2: falla de nuevo; la espera se duplica (10 s).
        clock.Advance(TimeSpan.FromSeconds(5));
        var secondRunTime = clock.UtcNow;
        Assert.Equal(1, await processor.ProcessPendingAsync());
        var afterSecond = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        Assert.Equal("Pending", afterSecond.Status);
        Assert.Equal(2, afterSecond.Attempts);
        Assert.Equal(secondRunTime.AddSeconds(10), afterSecond.NextAttemptAt, TimeSpan.FromMilliseconds(5));

        // Intento 3: el servicio se recuperó.
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(1, await processor.ProcessPendingAsync());
        var final = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        Assert.Equal("Sent", final.Status);
        Assert.Equal(3, final.Attempts);
        Assert.Null(final.LastError);
        Assert.Equal(1, notifier.Delivered);

        // El historial no se duplicó a pesar de los reintentos (idempotencia).
        Assert.Equal(1, await OutboxTestSupport.CountHistoryAsync(DataSource, deliveryId.Value));
    }

    [RequiresPostgresFact]
    public async Task Process_AlwaysFailing_BecomesDeadAfterMaxAttempts()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var (processor, notifier, clock, _) = Build(notifierFailures: int.MaxValue);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Assert.Equal(1, await processor.ProcessPendingAsync());
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        Assert.Equal("Dead", row.Status);
        Assert.Equal(3, row.Attempts);
        Assert.Contains(FlakyNotifier.FailureMessage, row.LastError);

        // Un mensaje Dead no se vuelve a procesar solo.
        Assert.Equal(0, await processor.ProcessPendingAsync());
        Assert.Equal(3, notifier.Calls);
    }

    [RequiresPostgresFact]
    public async Task Process_FailedNotification_DoesNotUndoTheAssignment()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var (processor, _, clock, _) = Build(notifierFailures: int.MaxValue);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await processor.ProcessPendingAsync();
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        var delivery = await Repository.GetAsync(deliveryId);
        Assert.Equal(DeliveryStatus.Assigned, delivery!.Status);
        Assert.NotNull(delivery.CourierId);
    }

    [RequiresPostgresFact]
    public async Task Process_MessageWithoutHandler_IsMarkedDeadInsteadOfBeingDiscarded()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var processor = OutboxTestSupport.CreateProcessor(DataSource, [], new TestClock());

        Assert.Equal(1, await processor.ProcessPendingAsync());

        var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        Assert.Equal("Dead", row.Status);
        Assert.Contains("manejador", row.LastError);
    }

    [RequiresPostgresFact]
    public async Task HistoryHandler_ProcessingTheSameMessageTwice_LeavesASingleRecord()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var handler = new EventHistoryHandler(DataSource);
        var message = new OutboxMessage(
            Guid.NewGuid(), deliveryId.Value, OutboxEventSerializer.DeliveryAssignedV1,
            """{"deliveryId":"00000000-0000-0000-0000-000000000000"}""", AssignedAt, 1);

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(1, await OutboxTestSupport.CountHistoryAsync(DataSource, deliveryId.Value));
    }

    [RequiresPostgresFact]
    public async Task Process_TwoProcessorsAtTheSameTime_NeverHandleTheSameMessageTwice()
    {
        const int messageCount = 10;
        var ids = new List<DeliveryId>();
        for (var i = 0; i < messageCount; i++)
        {
            ids.Add(await CreateAssignedDeliveryAsync());
        }

        // Los dos procesadores comparten el notificador para contar cuántos avisos se solicitaron en total.
        var notifier = new FlakyNotifier(0);
        var clock = new TestClock();
        IOutboxMessageHandler[] handlers =
        [
            new EventHistoryHandler(DataSource),
            new DeliveryAssignedNotificationHandler(notifier)
        ];
        var first = OutboxTestSupport.CreateProcessor(DataSource, handlers, clock);
        var second = OutboxTestSupport.CreateProcessor(DataSource, handlers, clock);

        var results = await Task.WhenAll(first.ProcessPendingAsync(), second.ProcessPendingAsync());

        Assert.Equal(messageCount, results.Sum());
        Assert.Equal(messageCount, notifier.Calls);
        foreach (var id in ids)
        {
            var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, id.Value);
            Assert.Equal("Sent", row.Status);
            Assert.Equal(1, row.Attempts);
        }
    }

    [RequiresPostgresFact]
    public async Task Process_WhenItFails_LogsAndStoredErrorsContainNoPersonalData()
    {
        var deliveryId = await CreateAssignedDeliveryAsync();
        var (processor, _, clock, log) = Build(notifierFailures: int.MaxValue);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await processor.ProcessPendingAsync();
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, deliveryId.Value);
        var everything = string.Join('\n', log.Lines) + "\n" + row.LastError;

        Assert.NotEmpty(log.Lines);
        Assert.DoesNotContain(Address, everything);
        Assert.DoesNotContain("Carrera", everything);
        Assert.DoesNotContain(Requester, everything);
        Assert.DoesNotContain("payload", everything, StringComparison.OrdinalIgnoreCase);
    }
}
