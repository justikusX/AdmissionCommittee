using AdmissionCommittee.Models;
using Npgsql;

namespace AdmissionCommittee.Data;

public class ApplicantRepository
{
    private readonly Database _db;

    public ApplicantRepository(Database db) => _db = db;

    private const string SelectSql = @"
        SELECT a.applicant_id, a.user_id, u.login, a.full_name, a.birth_date, a.phone, a.email,
               a.faculty_id, f.faculty_name, a.certificate_avg, a.status, a.registered_at
        FROM applicants a
        JOIN faculties f ON f.faculty_id = a.faculty_id
        JOIN users     u ON u.user_id    = a.user_id";

    private static Applicant Map(NpgsqlDataReader r) => new()
    {
        ApplicantId = r.GetInt("applicant_id"),
        UserId = r.GetInt("user_id"),
        Login = r.GetStr("login"),
        FullName = r.GetStr("full_name"),
        BirthDate = r.GetDate("birth_date"),
        Phone = r.GetNullableStr("phone"),
        Email = r.GetNullableStr("email"),
        FacultyId = r.GetInt("faculty_id"),
        FacultyName = r.GetStr("faculty_name"),
        CertificateAvg = r.GetDec("certificate_avg"),
        Status = r.GetStr("status"),
        RegisteredAt = r.GetDate("registered_at")
    };

    public async Task<Applicant?> GetByUserIdAsync(int userId)
        => (await _db.QueryAsync(SelectSql + " WHERE a.user_id = @id", Map, ("id", userId))).FirstOrDefault();

    public Task<List<Applicant>> GetAllAsync()
        => _db.QueryAsync(SelectSql + " ORDER BY f.faculty_name, a.full_name", Map);

    /// <summary>
    /// Регистрация абитуриента: в одной транзакции создаётся учётная запись (users)
    /// и заявление на факультет (applicants). Если что-то не получилось — откат обеих вставок.
    /// </summary>
    public async Task<int> RegisterAsync(string login, string passwordHash, Applicant a)
    {
        await using var connection = await _db.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        await using var userCmd = new NpgsqlCommand(@"
            INSERT INTO users(login, password_hash, full_name, role_id)
            VALUES (@login, @hash, @name, (SELECT role_id FROM roles WHERE role_name = @role))
            RETURNING user_id", connection, tx);
        Database.AddParameters(userCmd, new (string, object?)[]
        {
            ("login", login), ("hash", passwordHash), ("name", a.FullName), ("role", RoleNames.Applicant)
        });
        var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

        await using var appCmd = new NpgsqlCommand(@"
            INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg)
            VALUES (@user, @name, @birth, @phone, @email, @faculty, @cert)
            RETURNING applicant_id", connection, tx);
        Database.AddParameters(appCmd, new (string, object?)[]
        {
            ("user", userId), ("name", a.FullName), ("birth", DateOnly.FromDateTime(a.BirthDate)),
            ("phone", a.Phone), ("email", a.Email), ("faculty", a.FacultyId), ("cert", a.CertificateAvg)
        });
        var applicantId = Convert.ToInt32(await appCmd.ExecuteScalarAsync());

        await tx.CommitAsync();
        return applicantId;
    }

    /// <summary>Абитуриент меняет факультет — только пока итоги не подведены.</summary>
    public async Task<bool> ChangeFacultyAsync(int applicantId, int facultyId)
    {
        var rows = await _db.ExecuteAsync(@"
            UPDATE applicants SET faculty_id = @faculty
            WHERE applicant_id = @id AND status = @status",
            ("faculty", facultyId), ("id", applicantId), ("status", ApplicantStatuses.Submitted));
        return rows == 1;
    }

    /// <summary>Администратор исправляет данные абитуриента.</summary>
    public Task<int> UpdateAsync(Applicant a)
        => _db.ExecuteAsync(@"
            UPDATE applicants
               SET full_name = @name, birth_date = @birth, phone = @phone, email = @email,
                   faculty_id = @faculty, certificate_avg = @cert
             WHERE applicant_id = @id",
            ("name", a.FullName), ("birth", DateOnly.FromDateTime(a.BirthDate)), ("phone", a.Phone),
            ("email", a.Email), ("faculty", a.FacultyId), ("cert", a.CertificateAvg), ("id", a.ApplicantId));

    /// <summary>
    /// Удаление заявления. Баллы удаляются каскадно, а учётная запись не удаляется,
    /// а блокируется (у роли приложения нет права DELETE на users).
    /// </summary>
    public async Task DeleteAsync(Applicant a)
    {
        await using var connection = await _db.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        await using (var del = new NpgsqlCommand("DELETE FROM applicants WHERE applicant_id = @id", connection, tx))
        {
            del.Parameters.AddWithValue("id", a.ApplicantId);
            await del.ExecuteNonQueryAsync();
        }

        await using (var block = new NpgsqlCommand("UPDATE users SET is_active = FALSE WHERE user_id = @id", connection, tx))
        {
            block.Parameters.AddWithValue("id", a.UserId);
            await block.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }
}
