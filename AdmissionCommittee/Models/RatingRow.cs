namespace AdmissionCommittee.Models;

/// <summary>Строка конкурсного рейтинга (представление v_rating).</summary>
public class RatingRow
{
    public int ApplicantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public int PlanCount { get; set; }
    public decimal CertificateAvg { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ExamSum { get; set; }
    public decimal CertificatePoints { get; set; }
    public decimal TotalScore { get; set; }
    public int RequiredCount { get; set; }
    public int PassedCount { get; set; }
    public bool IsEligible { get; set; }
    public int? RankPosition { get; set; }
    public bool Passes { get; set; }

    public string RankText => RankPosition?.ToString() ?? "—";

    public string Note =>
        !IsEligible ? $"Не допущен: подтверждено {PassedCount} из {RequiredCount} предметов с баллом не ниже минимума"
        : Passes ? "Проходит в план набора"
        : "Вне плана набора";
}
