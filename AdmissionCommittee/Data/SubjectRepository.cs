using AdmissionCommittee.Models;
using Npgsql;

namespace AdmissionCommittee.Data;

public class SubjectRepository
{
    private readonly Database _db;

    public SubjectRepository(Database db) => _db = db;

    private static Subject Map(NpgsqlDataReader r) => new()
    {
        SubjectId = r.GetInt("subject_id"),
        Name = r.GetStr("subject_name"),
        MinScore = r.GetInt("min_score")
    };

    public Task<List<Subject>> GetAllAsync()
        => _db.QueryAsync("SELECT subject_id, subject_name, min_score FROM subjects ORDER BY subject_name", Map);

    public async Task<int> SaveAsync(Subject s)
    {
        if (s.SubjectId == 0)
        {
            var id = await _db.ScalarAsync(
                "INSERT INTO subjects(subject_name, min_score) VALUES (@name, @min) RETURNING subject_id",
                ("name", s.Name), ("min", s.MinScore));
            return Convert.ToInt32(id);
        }

        await _db.ExecuteAsync(
            "UPDATE subjects SET subject_name = @name, min_score = @min WHERE subject_id = @id",
            ("name", s.Name), ("min", s.MinScore), ("id", s.SubjectId));
        return s.SubjectId;
    }

    public Task<int> DeleteAsync(int subjectId)
        => _db.ExecuteAsync("DELETE FROM subjects WHERE subject_id = @id", ("id", subjectId));
}
