namespace AdmissionCommittee.Models;

/// <summary>Запись журнала действий (таблица audit_log).</summary>
public class AuditEntry
{
    public long LogId { get; set; }
    public DateTime EventTime { get; set; }
    public string? AppUser { get; set; }
    public string DbUser { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? RowData { get; set; }
}
