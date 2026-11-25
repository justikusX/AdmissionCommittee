using Npgsql;

namespace AdmissionCommittee.Data;

/// <summary>
/// Подключение к PostgreSQL и общие методы выполнения запросов.
/// ВСЕ запросы параметризованные — пользовательский ввод никогда
/// не подставляется в текст SQL (защита от SQL-инъекций).
/// </summary>
public sealed class Database
{
    private readonly NpgsqlDataSource _dataSource;

    public Database(string connectionString)
    {
        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    /// <summary>Логин текущего пользователя приложения — попадает в журнал audit_log.</summary>
    public string? CurrentLogin { get; set; }

    /// <summary>Открывает соединение и сообщает базе, кто работает (для триггеров журнала).</summary>
    public async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT set_config('app.user_login', @login, false)", connection);
        cmd.Parameters.AddWithValue("login", CurrentLogin ?? string.Empty);
        await cmd.ExecuteNonQueryAsync();
        return connection;
    }

    /// <summary>SELECT, возвращающий список объектов.</summary>
    public async Task<List<T>> QueryAsync<T>(string sql, Func<NpgsqlDataReader, T> map,
                                             params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, connection);
        AddParameters(cmd, parameters);

        var result = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(map(reader));
        return result;
    }

    /// <summary>INSERT / UPDATE / DELETE. Возвращает число затронутых строк.</summary>
    public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, connection);
        AddParameters(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Запрос, возвращающий одно значение.</summary>
    public async Task<object?> ScalarAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, connection);
        AddParameters(cmd, parameters);
        var value = await cmd.ExecuteScalarAsync();
        return value is DBNull ? null : value;
    }

    public static void AddParameters(NpgsqlCommand cmd, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
