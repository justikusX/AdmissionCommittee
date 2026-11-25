namespace AdmissionCommittee.Models;

/// <summary>Факультет с фиксированным планом набора (таблица faculties).</summary>
public class Faculty
{
    public int FacultyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlanCount { get; set; }
    public string? Description { get; set; }

    /// <summary>Список предметов строкой (для таблицы на экране).</summary>
    public string SubjectsText { get; set; } = string.Empty;

    /// <summary>Сколько абитуриентов подали заявление.</summary>
    public int ApplicantsCount { get; set; }

    public override string ToString() => Name;
}
