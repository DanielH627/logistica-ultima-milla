using LogisticaUltimaMilla.Domain.Entregas;
using LogisticaUltimaMilla.Domain.Ruteo;
using LogisticaUltimaMilla.Domain.Shared;
using Xunit;

namespace LogisticaUltimaMilla.Domain.Tests;

public class ValueObjectTests
{
    // EA13
    [Fact]
    public void DeliveryWindow_WithStartAfterEnd_Throws()
    {
        var start = new DateTimeOffset(2026, 11, 13, 16, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 11, 13, 14, 0, 0, TimeSpan.Zero);

        Assert.Throws<InvalidValueException>(() => new DeliveryWindow(start, end));
    }

    // EA14
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RouteCapacity_BelowOne_Throws(int maxStops)
    {
        Assert.Throws<InvalidValueException>(() => new RouteCapacity(maxStops));
    }

    // EA15
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeliveryAddress_Empty_Throws(string text)
    {
        Assert.Throws<InvalidValueException>(() => new DeliveryAddress(text));
    }

    [Fact]
    public void Identifier_Empty_Throws()
    {
        Assert.Throws<InvalidValueException>(() => new DeliveryId(Guid.Empty));
        Assert.Throws<InvalidValueException>(() => new CourierId(Guid.Empty));
    }

    [Fact]
    public void DeliveryAddress_WithSameText_AreEqual()
    {
        Assert.Equal(new DeliveryAddress("Carrera 70 # 44-18"), new DeliveryAddress("  Carrera 70 # 44-18  "));
    }
}
