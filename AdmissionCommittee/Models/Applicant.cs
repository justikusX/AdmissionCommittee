namespace AdmissionCommittee.Models;

/// <summary>Статусы заявления — как в CHECK таблицы applicants.</summary>
public static class ApplicantStatuses
{
    public const string Submitted = "Подано";
    public const string Enrolled = "Зачислен";
    public const string Rejected = "Не зачислен";
}

/// <summary>Абитуриент (таблица applicants).</summary>
public class Applicant
{
    public int ApplicantId { get; set; }
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;

    /// <summary>Средний балл аттестата (3.00–5.00).</summary>
    public decimal CertificateAvg { get; set; }

    public string Status { get; set; } = ApplicantStatuses.Submitted;
    public DateTime RegisteredAt { get; set; }
}
