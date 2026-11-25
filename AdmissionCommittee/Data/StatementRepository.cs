using AdmissionCommittee.Models;
using Npgsql;

namespace AdmissionCommittee.Data;

/// <summary>Ведомость: баллы абитуриентов по предметам.</summary>
public class StatementRepository
{
    private readonly Database _db;

    public StatementRepository(Database db) => _db = db;

    /// <summary>Предметы факультета абитуриента и его баллы по ним.</summary>
    public Task<List<ApplicantSubjectScore>> GetApplicantScoresAsync(int applicantId)
        => _db.QueryAsync(@"
            SELECT s.subject_id, s.subject_name, s.min_score, se.score,
                   COALESCE(se.is_confirmed, FALSE) AS is_confirmed
            FROM applicants a
            JOIN faculty_subjects fs ON fs.faculty_id = a.faculty_id
            JOIN subjects s          ON s.subject_id  = fs.subject_id
            LEFT JOIN statement_entries se
                   ON se.applicant_id = a.applicant_id AND se.subject_id = s.subject_id
            WHERE a.applicant_id = @id
            ORDER BY s.subject_name",
            r => new ApplicantSubjectScore
            {
                SubjectId = r.GetInt("subject_id"),
                SubjectName = r.GetStr("subject_name"),
                MinScore = r.GetInt("min_score"),
                Score = r.GetNullableInt("score"),
                IsConfirmed = r.GetBool("is_confirmed")
            }, ("id", applicantId));

    /// <summary>
    /// Абитуриент вводит (или исправляет) балл. Уже зарегистрированный администратором
    /// балл изменить нельзя — условие WHERE в ON CONFLICT это гарантирует.
    /// </summary>
    public Task<int> SaveApplicantScoreAsync(int applicantId, int subjectId, int score)
        => _db.ExecuteAsync(@"
            INSERT INTO statement_entries(applicant_id, subject_id, score)
            VALUES (@a, @s, @score)
            ON CONFLICT (applicant_id, subject_id) DO UPDATE
               SET score = EXCLUDED.score, entered_at = now()
             WHERE statement_entries.is_confirmed = FALSE",
            ("a", applicantId), ("s", subjectId), ("score", score));

    /// <summary>Ведомость для администратора с фильтрами.</summary>
    public Task<List<StatementRow>> GetStatementAsync(int? facultyId, bool onlyUnconfirmed)
        => _db.QueryAsync(@"
            SELECT v.*
            FROM v_statement v
            JOIN applicants a ON a.applicant_id = v.applicant_id
            WHERE (@faculty::int IS NULL OR a.faculty_id = @faculty::int)
              AND (NOT @only OR NOT v.is_confirmed)
            ORDER BY v.faculty_name, v.applicant_name, v.subject_name",
            r => new StatementRow
            {
                EntryId = r.GetInt("entry_id"),
                ApplicantId = r.GetInt("applicant_id"),
                ApplicantName = r.GetStr("applicant_name"),
                FacultyName = r.GetStr("faculty_name"),
                SubjectId = r.GetInt("subject_id"),
                SubjectName = r.GetStr("subject_name"),
                MinScore = r.GetInt("min_score"),
                Score = r.GetInt("score"),
                EnteredAt = r.GetDate("entered_at"),
                IsConfirmed = r.GetBool("is_confirmed"),
                ConfirmedBy = r.GetNullableStr("confirmed_by_login"),
                ConfirmedAt = r.GetNullableDate("confirmed_at")
            }, ("faculty", facultyId), ("only", onlyUnconfirmed));

    /// <summary>Администратор регистрирует балл в ведомости (при необходимости исправив его).</summary>
    public Task<int> ConfirmAsync(int entryId, int score, int adminUserId)
        => _db.ExecuteAsync(@"
            UPDATE statement_entries
               SET score = @score, is_confirmed = TRUE, confirmed_by = @admin, confirmed_at = now()
             WHERE entry_id = @id",
            ("score", score), ("admin", adminUserId), ("id", entryId));

    /// <summary>Зарегистрировать сразу несколько записей (одна транзакция).</summary>
    public async Task ConfirmManyAsync(IEnumerable<int> entryIds, int adminUserId)
    {
        await using var connection = await _db.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        foreach (var id in entryIds)
        {
            await using var cmd = new NpgsqlCommand(@"
                UPDATE statement_entries
                   SET is_confirmed = TRUE, confirmed_by = @admin, confirmed_at = now()
                 WHERE entry_id = @id AND is_confirmed = FALSE", connection, tx);
            cmd.Parameters.AddWithValue("admin", adminUserId);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
    }

    /// <summary>Снять регистрацию (вернуть балл абитуриенту на исправление).</summary>
    public Task<int> UnconfirmAsync(int entryId)
        => _db.ExecuteAsync(@"
            UPDATE statement_entries
               SET is_confirmed = FALSE, confirmed_by = NULL, confirmed_at = NULL
             WHERE entry_id = @id", ("id", entryId));
}
