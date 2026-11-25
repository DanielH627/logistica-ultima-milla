using System.Net.Http.Json;
using LogisticaUltimaMilla.Infrastructure.Seeding;
using LogisticaUltimaMilla.Integration.Tests.Postgres;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>
/// De extremo a extremo con el servicio en segundo plano activo: la solicitud HTTP asigna la entrega y,
/// sin intervención adicional, el procesador registra el historial y marca el mensaje como enviado.
/// </summary>
public class OutboxEndToEndTests : PostgresTestBase
{
    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var limit = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < limit)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(200);
        }

        return await condition();
    }

    [RequiresPostgresFact]
    public async Task Post_WithBackgroundProcessorEnabled_EventuallyRecordsHistoryAndMarksMessageSent()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Outbox:Enabled"] = "true",
            ["Outbox:PollIntervalSeconds"] = "1"
        };
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true, settings);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/deliveries/{DemoData.Delivery1}/assignment")
        {
            Content = JsonContent.Create(new { courierId = DemoData.CourierLuisa })
        };
        request.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        request.Headers.Add("X-Requested-By", "sandra");

        var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode);

        var processed = await WaitUntilAsync(async () =>
        {
            var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, DemoData.Delivery1);
            return row.Status == "Sent";
        }, TimeSpan.FromSeconds(20));

        Assert.True(processed, "El mensaje no pasó a Sent a tiempo.");
        Assert.Equal(1, await OutboxTestSupport.CountHistoryAsync(DataSource, DemoData.Delivery1));
    }

    [RequiresPostgresFact]
    public async Task Post_WhenNotificationsAlwaysFail_StillReturnsSuccessAndLeavesTheAssignmentInPlace()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Outbox:Enabled"] = "true",
            ["Outbox:PollIntervalSeconds"] = "1",
            ["Outbox:BaseDelaySeconds"] = "1",
            ["Outbox:MaxAttempts"] = "2",
            ["Notifications:SimulateFailure"] = "true"
        };
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true, settings);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/deliveries/{DemoData.Delivery2}/assignment")
        {
            Content = JsonContent.Create(new { courierId = DemoData.CourierLuisa })
        };
        request.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        request.Headers.Add("X-Requested-By", "sandra");

        // La asignación responde bien aunque el aviso vaya a fallar: el aviso es asíncrono (ADR-005).
        var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode);

        var dead = await WaitUntilAsync(async () =>
        {
            var row = await OutboxTestSupport.GetOnlyRowAsync(DataSource, DemoData.Delivery2);
            return row.Status == "Dead";
        }, TimeSpan.FromSeconds(20));

        Assert.True(dead, "El mensaje no pasó a Dead a tiempo.");
    }
}
