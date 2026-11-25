using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>
/// Salud, correlación, encabezados de seguridad, métricas y registros.
/// No necesitan PostgreSQL: usan una base inalcanzable a propósito, lo que además prueba la degradación.
/// </summary>
public class ObservabilityTests
{
    private const string Unreachable =
        "Host=localhost;Port=1;Username=postgres;Password=secreto-de-prueba;Database=logistica;Timeout=2";

    private static ApiFactory NewFactory() => new(Unreachable, prepareDatabase: false);

    [Fact]
    public async Task HealthLive_StaysHealthyEvenWhenTheDatabaseIsDown()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task HealthReady_WhenTheDatabaseIsDown_Returns503WithoutInternalDetails()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Unhealthy", body);
        Assert.DoesNotContain("secreto-de-prueba", body);
        Assert.DoesNotContain("localhost", body);
        Assert.DoesNotContain("Npgsql", body);
        Assert.DoesNotContain("Exception", body);
    }

    [Fact]
    public async Task Response_WithoutCorrelationId_GetsAGeneratedOne()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Matches("^[0-9a-f]{32}$", values!.Single());
    }

    [Fact]
    public async Task Response_WithValidCorrelationId_EchoesItAndLogsItInScope()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/operations/outbox");
        request.Headers.Add("X-Correlation-Id", "prueba-123");  
        var response = await client.SendAsync(request);

        Assert.Equal("prueba-123", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Contains(
            factory.Logs.Entries,
            entry => entry.Category.EndsWith("RequestLoggingMiddleware") && entry.Scopes.Contains("CorrelationId=prueba-123"));
    }

    [Fact]
    public async Task Response_WithUnsafeCorrelationId_ReplacesIt()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", "id con espacios <script>");
        var response = await client.SendAsync(request);

        var returned = response.Headers.GetValues("X-Correlation-Id").Single();
        Assert.Matches("^[0-9a-f]{32}$", returned);
    }

    [Fact]
    public async Task Response_IncludesSecurityHeaders()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task OperationsEndpoint_WithoutApiKey_Returns401()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/operations/outbox");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_IncrementsTheHttpRequestsMetricWithTheRouteTemplate()
    {
        var measurements = new List<(long Value, string? Route, string? StatusClass)>();
        var gate = new object();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "logistica.http.requests")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            string? route = null, statusClass = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "route") route = tag.Value?.ToString();
                if (tag.Key == "status_class") statusClass = tag.Value?.ToString();
            }

            lock (gate) { measurements.Add((value, route, statusClass)); }
        });
        listener.Start();

        using var factory = NewFactory();
        using var client = factory.CreateClient();
        await client.GetAsync("/health/live");

        lock (gate)
        {
            Assert.Contains(measurements, m => m.Route == "/health/live" && m.StatusClass == "2xx");
        }
    }

    [Fact]
    public async Task Logs_NeverContainTheApiKeyThePasswordOrTheRequester()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/deliveries/{Guid.NewGuid()}/assignment")
        {
            Content = JsonContent.Create(new { courierId = Guid.NewGuid() })
        };
        request.Headers.Add("X-Api-Key", ApiFactory.ApiKey);
        request.Headers.Add("X-Requested-By", "sandra");

        // La base está caída: la solicitud falla con 500 y genera los registros de error.
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var text = factory.Logs.AllText();
        Assert.Contains("respondió 500", text);
        Assert.DoesNotContain(ApiFactory.ApiKey, text);
        Assert.DoesNotContain("secreto-de-prueba", text);
        Assert.DoesNotContain("sandra", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLog_UsesTheRouteTemplateInsteadOfTheRealUrl()
    {
        using var factory = NewFactory();
        using var client = factory.CreateClient();
        var deliveryId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deliveries/{deliveryId}/assignment");
        request.Headers.Add("X-Api-Key", "clave-equivocada");
        await client.SendAsync(request);
        var line = Assert.Single(
            factory.Logs.Entries,
            entry => entry.Category.EndsWith("RequestLoggingMiddleware"));

        Assert.Contains("/api/v1/deliveries/{deliveryId:guid}/assignment", line.Message);
        Assert.DoesNotContain(deliveryId.ToString(), line.Message);
        Assert.DoesNotContain("clave-equivocada", factory.Logs.AllText());
    }
}
