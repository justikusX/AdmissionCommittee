using Npgsql;

namespace AdmissionCommittee.Data;

/// <summary>Удобное чтение столбцов по имени с учётом NULL.</summary>
internal static class DbReaderExtensions
{
    public static int GetInt(this NpgsqlDataReader r, string column)
        => Convert.ToInt32(r[column]);

    public static long GetLong(this NpgsqlDataReader r, string column)
        => Convert.ToInt64(r[column]);

    public static int? GetNullableInt(this NpgsqlDataReader r, string column)
        => r[column] is DBNull ? null : Convert.ToInt32(r[column]);

    public static decimal GetDec(this NpgsqlDataReader r, string column)
        => Convert.ToDecimal(r[column]);

    public static bool GetBool(this NpgsqlDataReader r, string column)
        => Convert.ToBoolean(r[column]);

    public static string GetStr(this NpgsqlDataReader r, string column)
        => r[column] as string ?? string.Empty;

    public static string? GetNullableStr(this NpgsqlDataReader r, string column)
        => r[column] as string;

    public static DateTime GetDate(this NpgsqlDataReader r, string column)
        => Convert.ToDateTime(r[column]);

    public static DateTime? GetNullableDate(this NpgsqlDataReader r, string column)
        => r[column] is DBNull ? null : Convert.ToDateTime(r[column]);
}
