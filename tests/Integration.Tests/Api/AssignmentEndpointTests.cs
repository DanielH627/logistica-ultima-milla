using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using LogisticaUltimaMilla.Infrastructure.Persistence;
using LogisticaUltimaMilla.Infrastructure.Seeding;
using LogisticaUltimaMilla.Integration.Tests.Postgres;
using Xunit;

namespace LogisticaUltimaMilla.Integration.Tests.Api;

/// <summary>
/// Pruebas de la entrada HTTP de extremo a extremo: la solicitud atraviesa la API, el caso de uso,
/// el dominio y PostgreSQL. Cada prueba usa su propia base de datos con los datos de demostración.
/// </summary>
public class AssignmentEndpointTests : PostgresTestBase
{
    private static string Url(Guid deliveryId) => $"/api/v1/deliveries/{deliveryId}/assignment";

    private static async Task<HttpResponseMessage> AssignAsync(
        HttpClient client,
        Guid deliveryId,
        Guid courierId,
        string? apiKey = ApiFactory.ApiKey,
        string? requestedBy = "sandra")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url(deliveryId))
        {
            Content = JsonContent.Create(new { courierId })
        };

        if (apiKey is not null)
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }

        if (requestedBy is not null)
        {
            request.Headers.Add("X-Requested-By", requestedBy);
        }

        return await client.SendAsync(request);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private async Task<DeliveryStatus> StatusOfAsync(Guid deliveryId)
    {
        var delivery = await new PostgresDeliveryRepository(DataSource).GetAsync(new DeliveryId(deliveryId));
        return delivery!.Status;
    }

    [RequiresPostgresFact]
    public async Task Post_ValidRequest_AssignsTheDeliveryAndQueuesTheEvent()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierAndres);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = await ReadJsonAsync(response);
        Assert.Equal(DemoData.Delivery1, body.RootElement.GetProperty("deliveryId").GetGuid());
        Assert.Equal(DemoData.CourierAndres, body.RootElement.GetProperty("courierId").GetGuid());
        Assert.True(body.RootElement.GetProperty("noticeQueued").GetBoolean());

        Assert.Equal(DeliveryStatus.Assigned, await StatusOfAsync(DemoData.Delivery1));

        var message = Assert.Single(await PostgresTestData.GetOutboxRowsAsync(DataSource, new DeliveryId(DemoData.Delivery1)));
        Assert.Equal("Pending", message.Status);
    }

    [RequiresPostgresFact]
    public async Task Post_WithoutApiKey_Returns401AndChangesNothing()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierAndres, apiKey: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(DeliveryStatus.PendingAssignment, await StatusOfAsync(DemoData.Delivery1));
    }

    [RequiresPostgresFact]
    public async Task Post_WithWrongApiKey_Returns401()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierAndres, apiKey: "clave-incorrecta");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var body = await ReadJsonAsync(response);
        Assert.Equal("UNAUTHORIZED", body.RootElement.GetProperty("code").GetString());
    }

    [RequiresPostgresFact]
    public async Task Post_UnknownDelivery_Returns404()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, Guid.NewGuid(), DemoData.CourierAndres);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var body = await ReadJsonAsync(response);
        Assert.Equal(AssignCourierErrorCodes.DeliveryNotFound, body.RootElement.GetProperty("code").GetString());
    }

    // EA08
    [RequiresPostgresFact]
    public async Task Post_InactiveCourier_Returns422()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierPaola);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var body = await ReadJsonAsync(response);
        Assert.Equal(AssignCourierErrorCodes.CourierNotActive, body.RootElement.GetProperty("code").GetString());
    }

    // EA09
    [RequiresPostgresFact]
    public async Task Post_SaturatedRoute_Returns409AndLeavesTheDeliveryPending()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierCamilo);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var body = await ReadJsonAsync(response);
        Assert.Equal(AssignCourierErrorCodes.RouteSaturated, body.RootElement.GetProperty("code").GetString());
        Assert.Equal(DeliveryStatus.PendingAssignment, await StatusOfAsync(DemoData.Delivery1));
    }

    // EA02
    [RequiresPostgresFact]
    public async Task Post_AlreadyAssignedDelivery_Returns409()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var first = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierLuisa);
        var second = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierAndres);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var body = await ReadJsonAsync(second);
        Assert.Equal(AssignCourierErrorCodes.DeliveryAlreadyAssigned, body.RootElement.GetProperty("code").GetString());
    }

    [RequiresPostgresFact]
    public async Task Post_EmptyCourierId_Returns400WithTheInvalidFields()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, Guid.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = await ReadJsonAsync(response);
        Assert.Equal("INVALID_REQUEST", body.RootElement.GetProperty("code").GetString());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("courierId", out _));
    }

    [RequiresPostgresFact]
    public async Task Post_MissingRequestedBy_Returns400()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var response = await AssignAsync(client, DemoData.Delivery1, DemoData.CourierAndres, requestedBy: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(DeliveryStatus.PendingAssignment, await StatusOfAsync(DemoData.Delivery1));
    }

    // EA11 a través de HTTP
    [RequiresPostgresFact]
    public async Task Post_SimultaneousRequests_ForTheSameDelivery_OnlyOneSucceeds()
    {
        using var factory = new ApiFactory(ConnectionString, prepareDatabase: true);
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            Task.Run(() => AssignAsync(client, DemoData.Delivery1, DemoData.CourierAndres)),
            Task.Run(() => AssignAsync(client, DemoData.Delivery1, DemoData.CourierLuisa)));

        var statuses = responses.Select(response => response.StatusCode).OrderBy(status => (int)status).ToList();

        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses);
        Assert.Single(await PostgresTestData.GetOutboxRowsAsync(DataSource, new DeliveryId(DemoData.Delivery1)));
    }
}
