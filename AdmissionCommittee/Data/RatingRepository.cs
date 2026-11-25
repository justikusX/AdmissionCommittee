using AdmissionCommittee.Models;

namespace AdmissionCommittee.Data;

/// <summary>Конкурсный рейтинг и зачисление.</summary>
public class RatingRepository
{
    private readonly Database _db;

    public RatingRepository(Database db) => _db = db;

    private const string SelectSql = @"
        SELECT applicant_id, full_name, faculty_id, faculty_name, plan_count, certificate_avg, status,
               exam_sum, certificate_points, total_score, required_count, passed_count,
               is_eligible, rank_position, passes
        FROM v_rating";

    private static RatingRow Map(Npgsql.NpgsqlDataReader r) => new()
    {
        ApplicantId = r.GetInt("applicant_id"),
        FullName = r.GetStr("full_name"),
        FacultyId = r.GetInt("faculty_id"),
        FacultyName = r.GetStr("faculty_name"),
        PlanCount = r.GetInt("plan_count"),
        CertificateAvg = r.GetDec("certificate_avg"),
        Status = r.GetStr("status"),
        ExamSum = r.GetInt("exam_sum"),
        CertificatePoints = r.GetDec("certificate_points"),
        TotalScore = r.GetDec("total_score"),
        RequiredCount = r.GetInt("required_count"),
        PassedCount = r.GetInt("passed_count"),
        IsEligible = r.GetBool("is_eligible"),
        RankPosition = r.GetNullableInt("rank_position"),
        Passes = r.GetBool("passes")
    };

    public Task<List<RatingRow>> GetRatingAsync(int? facultyId)
        => _db.QueryAsync(SelectSql + @"
            WHERE (@faculty::int IS NULL OR faculty_id = @faculty::int)
            ORDER BY faculty_name, is_eligible DESC, rank_position, total_score DESC",
            Map, ("faculty", facultyId));

    public async Task<RatingRow?> GetForApplicantAsync(int applicantId)
        => (await _db.QueryAsync(SelectSql + " WHERE applicant_id = @id", Map, ("id", applicantId)))
            .FirstOrDefault();

    /// <summary>Подвести итоги: вызывает функцию fn_run_enrollment(). Возвращает число зачисленных.</summary>
    public async Task<int> RunEnrollmentAsync()
        => Convert.ToInt32(await _db.ScalarAsync("SELECT fn_run_enrollment()"));

    public Task<int> ResetEnrollmentAsync()
        => _db.ExecuteAsync("SELECT fn_reset_enrollment()");
}
