using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LogisticaUltimaMilla.Infrastructure.Seeding;
using LogisticaUltimaMilla.Integration.Tests.Postgres;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>Salud y operación con PostgreSQL real.</summary>
public class OperationsAndHealthPostgresTests : PostgresTestBase
{
    [RequiresPostgresFact]
    public async Task HealthReady_WithHealthyDatabaseAndEmptyOutbox_ReturnsHealthy()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Healthy", body.RootElement.GetProperty("checks").GetProperty("database").GetString());
        Assert.Equal("Healthy", body.RootElement.GetProperty("checks").GetProperty("outbox").GetString());
    }

    [RequiresPostgresFact]
    public async Task HealthReady_WithADeadMessage_IsDegradedButStillServing()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        await using (var insert = DataSource.CreateCommand(
            """
            INSERT INTO transversal.bandeja_salida (id, aggregate_id, message_type, payload, occurred_at, status, attempts)
            VALUES (@id, @aggregate, 'DeliveryAssigned.v1', '{}'::jsonb, now(), 'Dead', 3)
            """))
        {
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("aggregate", Guid.NewGuid());
            await insert.ExecuteNonQueryAsync();
        }

        var response = await client.GetAsync("/health/ready");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Degraded", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Degraded", body.RootElement.GetProperty("checks").GetProperty("outbox").GetString());
    }

    [RequiresPostgresFact]
    public async Task OperationsOutbox_AfterAnAssignment_ReportsOnePendingMessage()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        using var assign = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/deliveries/{DemoData.Delivery1}/assignment")
        {
            Content = JsonContent.Create(new { courierId = DemoData.CourierLuisa })
        };
        assign.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        assign.Headers.Add("X-Requested-By", "sandra");
        Assert.True((await client.SendAsync(assign)).IsSuccessStatusCode);

        using var status = new HttpRequestMessage(HttpMethod.Get, "/api/v1/operations/outbox");
        status.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        var response = await client.SendAsync(status);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.RootElement.GetProperty("pending").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("dead").GetInt32());
    }
}
