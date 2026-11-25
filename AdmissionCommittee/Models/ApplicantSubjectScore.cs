namespace AdmissionCommittee.Models;

/// <summary>Предмет факультета и балл абитуриента по нему (если уже введён).</summary>
public class ApplicantSubjectScore
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int MinScore { get; set; }
    public int? Score { get; set; }
    public bool IsConfirmed { get; set; }
}
