using AdmissionCommittee.Models;

namespace AdmissionCommittee.Data;

public class AuditRepository
{
    private readonly Database _db;

    public AuditRepository(Database db) => _db = db;

    public Task<List<AuditEntry>> GetLastAsync(int count)
        => _db.QueryAsync(@"
            SELECT log_id, event_time, app_user, db_user, table_name, operation, row_data::text AS row_data
            FROM audit_log
            ORDER BY log_id DESC
            LIMIT @count",
            r => new AuditEntry
            {
                LogId = r.GetLong("log_id"),
                EventTime = r.GetDate("event_time"),
                AppUser = r.GetNullableStr("app_user"),
                DbUser = r.GetStr("db_user"),
                TableName = r.GetStr("table_name"),
                Operation = r.GetStr("operation"),
                RowData = r.GetNullableStr("row_data")
            }, ("count", count));
}
