using Npgsql;

namespace AdmissionCommittee.Data;

/// <summary>Переводит ошибки PostgreSQL в понятные пользователю сообщения.</summary>
public static class DbErrors
{
    public static string ToUserMessage(Exception ex) => ex switch
    {
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } =>
            "Такая запись уже существует (нарушено ограничение уникальности).",

        PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } =>
            "Операция невозможна: на эту запись ссылаются другие данные " +
            "(например, на факультет уже подали заявления или предмет используется в ведомости).",

        PostgresException { SqlState: PostgresErrorCodes.CheckViolation } pg =>
            $"Значение не прошло проверку в базе данных (ограничение {pg.ConstraintName}).",

        PostgresException { SqlState: PostgresErrorCodes.NotNullViolation } pg =>
            $"Не заполнено обязательное поле: {pg.ColumnName}.",

        PostgresException { SqlState: PostgresErrorCodes.InsufficientPrivilege } =>
            "Недостаточно прав в базе данных. Выполните скрипт Database/03_security.sql.",

        PostgresException { SqlState: PostgresErrorCodes.InvalidPassword } =>
            "Не удалось войти в PostgreSQL: неверный пароль в appsettings.json.",

        PostgresException { SqlState: PostgresErrorCodes.InvalidCatalogName } =>
            "База данных admission_committee не найдена. Создайте её и выполните скрипты из папки Database.",

        PostgresException { SqlState: PostgresErrorCodes.UndefinedTable } =>
            "В базе нет нужных таблиц. Выполните скрипты Database/01_schema.sql, 02_seed.sql, 03_security.sql.",

        PostgresException pg => "Ошибка базы данных: " + pg.MessageText,

        NpgsqlException =>
            "Нет связи с сервером PostgreSQL. Проверьте, что сервер запущен и строка " +
            "подключения в appsettings.json верна.\n\n" + ex.Message,

        _ => ex.Message
    };
}
