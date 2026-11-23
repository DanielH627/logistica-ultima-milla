using LogisticaUltimaMilla.Application.Entregas;
using LogisticaUltimaMilla.Application.Ports;
using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Shared;
using Xunit;

namespace LogisticaUltimaMilla.Application.Tests;

/// <summary>
/// Pruebas del caso de uso con dobles de los puertos: no usan base de datos ni servicios reales.
/// </summary>
public class AssignCourierUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeDeliveryRepository _deliveries = new();
    private readonly FakeCourierRoutes _routes = new();
    private readonly AssignCourierUseCase _useCase;

    public AssignCourierUseCaseTests()
    {
        _useCase = new AssignCourierUseCase(_deliveries, _routes, new FixedClock(Now));
    }

    private static DeliveryWindow Window() =>
        new(new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 13, 16, 0, 0, TimeSpan.Zero));

    private Delivery AddPendingDelivery()
    {
        var delivery = Delivery.CreatePending(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Carrera 70 # 44-18"),
            Window(),
            DeliveryPriority.Normal);

        _deliveries.Add(delivery);
        return delivery;
    }

    private Delivery AddRestoredDelivery(DeliveryStatus status, CourierId? courierId)
    {
        var delivery = Delivery.Restore(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Carrera 70 # 44-18"),
            Window(),
            DeliveryPriority.Normal,
            status,
            courierId,
            courierId is null ? null : Now,
            1);

        _deliveries.Add(delivery);
        return delivery;
    }

    private static AssignCourierCommand Command(Delivery delivery, CourierId courierId) =>
        new(delivery.Id.Value, courierId.Value, "Sandra");

    // EA01
    [Fact]
    public async Task Execute_WithAvailableCourier_AssignsAndSavesTheEventWithTheDelivery()
    {
        var delivery = AddPendingDelivery();
        var courierId = CourierId.New();

        var result = await _useCase.ExecuteAsync(Command(delivery, courierId));

        Assert.True(result.IsSuccess);
        Assert.True(result.NoticeQueued);
        Assert.Equal(Now, result.AssignedAt);
        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal(courierId, delivery.CourierId);
        Assert.Equal(1, _deliveries.SaveCount);
        Assert.Equal(1, _routes.ReserveCalls);

        var assigned = Assert.IsType<DeliveryAssigned>(Assert.Single(_deliveries.SavedEvents));
        Assert.Equal("Sandra", assigned.RequestedBy);
    }

    [Fact]
    public async Task Execute_OnSuccess_ClearsTheDomainEventsOnceTheyAreSaved()
    {
        var delivery = AddPendingDelivery();

        await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.Empty(delivery.DomainEvents);
    }

    // EA06
    [Fact]
    public async Task Execute_WhenDeliveryDoesNotExist_Rejects()
    {
        var command = new AssignCourierCommand(Guid.NewGuid(), Guid.NewGuid(), "Sandra");

        var result = await _useCase.ExecuteAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.DeliveryNotFound, result.ErrorCode);
        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Empty(_deliveries.SavedEvents);
    }

    // EA07
    [Fact]
    public async Task Execute_WhenAvailabilityCannotBeVerified_DoesNotAssign()
    {
        var delivery = AddPendingDelivery();
        _routes.ThrowOnAvailability = true;

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.AvailabilityUnverifiable, result.ErrorCode);
        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Empty(_deliveries.SavedEvents);
    }

    // EA08
    [Fact]
    public async Task Execute_WhenCourierIsNotActive_Rejects()
    {
        var delivery = AddPendingDelivery();
        _routes.Availability = CourierAvailability.Unavailable(UnavailabilityReason.CourierNotActive);

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.CourierNotActive, result.ErrorCode);
        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Equal(0, _routes.ReserveCalls);
    }

    // EA09
    [Fact]
    public async Task Execute_WhenRouteIsSaturated_Rejects()
    {
        var delivery = AddPendingDelivery();
        _routes.Availability = CourierAvailability.Unavailable(UnavailabilityReason.RouteSaturated);

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.RouteSaturated, result.ErrorCode);
        Assert.Equal(0, _deliveries.SaveCount);
    }

    // EA02
    [Fact]
    public async Task Execute_WhenDeliveryAlreadyAssigned_Rejects()
    {
        var current = CourierId.New();
        var delivery = AddRestoredDelivery(DeliveryStatus.Assigned, current);

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.DeliveryAlreadyAssigned, result.ErrorCode);
        Assert.Equal(current, delivery.CourierId);
        Assert.Equal(0, _routes.ReserveCalls);
        Assert.Empty(_deliveries.SavedEvents);
    }

    // EA04
    [Theory]
    [InlineData(DeliveryStatus.InTransit)]
    [InlineData(DeliveryStatus.Delivered)]
    [InlineData(DeliveryStatus.Failed)]
    [InlineData(DeliveryStatus.Cancelled)]
    public async Task Execute_WhenDeliveryIsNotPending_Rejects(DeliveryStatus status)
    {
        var courierId = status == DeliveryStatus.Cancelled ? null : CourierId.New();
        var delivery = AddRestoredDelivery(status, courierId);

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.DeliveryNotPending, result.ErrorCode);
        Assert.Equal(status, delivery.Status);
        Assert.Equal(0, _routes.ReserveCalls);
    }

    [Fact]
    public async Task Execute_WhenReservationFails_DoesNotSave()
    {
        var delivery = AddPendingDelivery();
        _routes.Reservation = StopReservationStatus.RouteSaturated;

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.RouteSaturated, result.ErrorCode);
        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Equal(0, _routes.ReleaseCalls);
        Assert.Empty(_deliveries.SavedEvents);
    }

    [Fact]
    public async Task Execute_WhenSaveConflicts_ReleasesStopAndSavesNoEvent()
    {
        var delivery = AddPendingDelivery();
        _deliveries.ThrowConcurrencyOnSave = true;

        var result = await _useCase.ExecuteAsync(Command(delivery, CourierId.New()));

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignCourierErrorCodes.DeliveryAlreadyAssigned, result.ErrorCode);
        Assert.Equal(1, _routes.ReserveCalls);
        Assert.Equal(1, _routes.ReleaseCalls);
        Assert.Empty(_deliveries.SavedEvents);
    }
}
