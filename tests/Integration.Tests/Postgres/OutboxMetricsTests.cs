using System.Diagnostics.Metrics;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Outbox;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Postgres;

public class OutboxMetricsTests : PostgresTestBase
{
    [RequiresPostgresFact]
    public async Task Process_SuccessfulMessage_IncrementsTheSentOutcome()
    {
        var outcomes = new List<string>();
        var gate = new object();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == OutboxMetrics.MessagesCounterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "outcome")
                {
                    lock (gate) { outcomes.Add(tag.Value?.ToString() ?? ""); }
                }
            }
        });
        listener.Start();

        var repository = new PostgresDeliveryRepository(DataSource);
        var deliveryId = await PostgresTestData.SeedPendingDeliveryAsync(repository);
        var delivery = await repository.GetAsync(deliveryId);
        delivery!.AssignCourier(CourierId.New(), new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero), "Sandra");
        await repository.SaveAsync(delivery);

        var processor = OutboxTestSupport.CreateProcessor(
            DataSource, new IOutboxMessageHandler[] { new EventHistoryHandler(DataSource) }, new TestClock());

        await processor.ProcessPendingAsync();

        lock (gate)
        {
            Assert.Contains("sent", outcomes);
        }
    }
}
