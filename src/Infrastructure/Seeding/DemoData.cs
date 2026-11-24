namespace LogisticaUltimaMilla.Infrastructure.Seeding;

/// <summary>
/// Identificadores fijos de los datos de demostración, para poder usarlos en la demostración y en las pruebas.
/// Son datos ficticios: no corresponden a personas ni direcciones reales.
/// </summary>
public static class DemoData
{
    // Repartidores
    public static readonly Guid CourierAndres = Guid.Parse("a0000000-0000-0000-0000-000000000001");  // activo, 6 de 10 paradas
    public static readonly Guid CourierLuisa = Guid.Parse("a0000000-0000-0000-0000-000000000002");   // activo, 2 de 10 paradas
    public static readonly Guid CourierPaola = Guid.Parse("a0000000-0000-0000-0000-000000000003");   // inactiva
    public static readonly Guid CourierCamilo = Guid.Parse("a0000000-0000-0000-0000-000000000004");  // activo, ruta saturada 10 de 10

    // Rutas
    public static readonly Guid RouteAndres = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid RouteLuisa = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid RoutePaola = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid RouteCamilo = Guid.Parse("b0000000-0000-0000-0000-000000000004");

    // Entregas pendientes de asignación
    public static readonly Guid Delivery1 = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Delivery2 = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid Delivery3 = Guid.Parse("d0000000-0000-0000-0000-000000000003");
    public static readonly Guid Delivery4 = Guid.Parse("d0000000-0000-0000-0000-000000000004");
}
