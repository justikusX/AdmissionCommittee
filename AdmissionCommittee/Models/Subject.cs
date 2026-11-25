namespace AdmissionCommittee.Models;

/// <summary>Предмет — вступительное испытание (таблица subjects).</summary>
public class Subject
{
    public int SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Минимальный проходной балл (0–100).</summary>
    public int MinScore { get; set; }

    public override string ToString() => Name;
}
