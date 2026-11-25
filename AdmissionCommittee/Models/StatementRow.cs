namespace AdmissionCommittee.Models;

/// <summary>Строка ведомости (представление v_statement).</summary>
public class StatementRow
{
    public int EntryId { get; set; }
    public int ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int Score { get; set; }
    public DateTime EnteredAt { get; set; }
    public bool IsConfirmed { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public bool IsBelowMin => Score < MinScore;
    public string StateText => IsConfirmed ? "Зарегистрирован" : "Ожидает проверки";
}
