using Npgsql;
using NpgsqlTypes;

namespace LogisticaUltimaMilla.Infrastructure.Postgres;

/// <summary>
/// Ayudas para agregar parámetros con su tipo explícito. Todo el acceso a datos usa
/// parámetros: nunca se concatenan valores dentro del SQL.
/// </summary>
internal static class NpgsqlCommandExtensions
{
    public static void AddUuid(this NpgsqlCommand command, string name, Guid? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Uuid)
        {
            Value = value.HasValue ? (object)value.Value : DBNull.Value
        });

    public static void AddText(this NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text)
        {
            Value = value is null ? DBNull.Value : (object)value
        });

    public static void AddInteger(this NpgsqlCommand command, string name, int? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Integer)
        {
            Value = value.HasValue ? (object)value.Value : DBNull.Value
        });

    public static void AddBoolean(this NpgsqlCommand command, string name, bool value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Boolean) { Value = value });

    public static void AddTimestampTz(this NpgsqlCommand command, string name, DateTimeOffset? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.TimestampTz)
        {
            Value = value.HasValue ? (object)value.Value.ToUniversalTime() : DBNull.Value
        });
}
