using AdmissionCommittee.Models;
using Npgsql;

namespace AdmissionCommittee.Data;

public class FacultyRepository
{
    private readonly Database _db;

    public FacultyRepository(Database db) => _db = db;

    public Task<List<Faculty>> GetAllAsync()
        => _db.QueryAsync(@"
            SELECT f.faculty_id, f.faculty_name, f.plan_count, f.description,
                   COALESCE(string_agg(s.subject_name, ', ' ORDER BY s.subject_name), '') AS subjects,
                   (SELECT COUNT(*) FROM applicants a WHERE a.faculty_id = f.faculty_id) AS applicants_count
            FROM faculties f
            LEFT JOIN faculty_subjects fs ON fs.faculty_id = f.faculty_id
            LEFT JOIN subjects s          ON s.subject_id  = fs.subject_id
            GROUP BY f.faculty_id
            ORDER BY f.faculty_name",
            r => new Faculty
            {
                FacultyId = r.GetInt("faculty_id"),
                Name = r.GetStr("faculty_name"),
                PlanCount = r.GetInt("plan_count"),
                Description = r.GetNullableStr("description"),
                SubjectsText = r.GetStr("subjects"),
                ApplicantsCount = r.GetInt("applicants_count")
            });

    /// <summary>Все связи «факультет — предмет».</summary>
    public async Task<List<(int FacultyId, int SubjectId)>> GetLinksAsync()
    {
        var links = await _db.QueryAsync("SELECT faculty_id, subject_id FROM faculty_subjects",
                                         r => (r.GetInt("faculty_id"), r.GetInt("subject_id")));
        return links.Select(x => (FacultyId: x.Item1, SubjectId: x.Item2)).ToList();
    }

    /// <summary>Добавляет или изменяет факультет вместе со списком предметов (одна транзакция).</summary>
    public async Task<int> SaveAsync(Faculty faculty, IEnumerable<int> subjectIds)
    {
        await using var connection = await _db.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        int id;
        if (faculty.FacultyId == 0)
        {
            await using var insert = new NpgsqlCommand(@"
                INSERT INTO faculties(faculty_name, plan_count, description)
                VALUES (@name, @plan, @descr) RETURNING faculty_id", connection, tx);
            Database.AddParameters(insert, new (string, object?)[]
            {
                ("name", faculty.Name), ("plan", faculty.PlanCount), ("descr", faculty.Description)
            });
            id = Convert.ToInt32(await insert.ExecuteScalarAsync());
        }
        else
        {
            id = faculty.FacultyId;
            await using var update = new NpgsqlCommand(@"
                UPDATE faculties SET faculty_name = @name, plan_count = @plan, description = @descr
                WHERE faculty_id = @id", connection, tx);
            Database.AddParameters(update, new (string, object?)[]
            {
                ("name", faculty.Name), ("plan", faculty.PlanCount), ("descr", faculty.Description), ("id", id)
            });
            await update.ExecuteNonQueryAsync();
        }

        await using (var clear = new NpgsqlCommand("DELETE FROM faculty_subjects WHERE faculty_id = @id", connection, tx))
        {
            clear.Parameters.AddWithValue("id", id);
            await clear.ExecuteNonQueryAsync();
        }

        foreach (var subjectId in subjectIds)
        {
            await using var link = new NpgsqlCommand(
                "INSERT INTO faculty_subjects(faculty_id, subject_id) VALUES (@f, @s)", connection, tx);
            link.Parameters.AddWithValue("f", id);
            link.Parameters.AddWithValue("s", subjectId);
            await link.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return id;
    }

    public Task<int> DeleteAsync(int facultyId)
        => _db.ExecuteAsync("DELETE FROM faculties WHERE faculty_id = @id", ("id", facultyId));
}
