using System.Data;
using Api.Helpers;
using Dapper;

namespace Api.SQLService;

/// <summary>
/// Dapper type handler that stores DateTime in SQLite TEXT columns as canonical
/// RFC 3339 UTC ("yyyy-MM-ddTHH:mm:ssZ") and reads back both canonical and legacy
/// ("yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss[.fffffff]") values as Kind=Utc.
/// </summary>
public sealed class SqliteDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = DateTimeHelper.ToRfc3339(value);
    }

    public override DateTime Parse(object value) => value switch
    {
        string s => DateTimeHelper.ParseFlexible(s)
                    ?? throw new DataException($"Cannot parse '{s}' as an RFC 3339 date"),
        DateTime dt => DateTimeHelper.NormalizeToUtc(dt),
        _ => throw new DataException($"Cannot convert {value.GetType()} to DateTime")
    };
}

public sealed class SqliteNullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
{
    private static readonly SqliteDateTimeHandler Inner = new();

    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.HasValue ? DateTimeHelper.ToRfc3339(value.Value) : DBNull.Value;
    }

    public override DateTime? Parse(object value) =>
        value is null or DBNull ? null : Inner.Parse(value);
}
