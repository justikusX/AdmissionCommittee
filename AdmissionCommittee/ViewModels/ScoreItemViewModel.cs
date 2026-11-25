using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;

namespace AdmissionCommittee.ViewModels;

/// <summary>Строка «предмет — балл» в личном кабинете абитуриента.</summary>
public class ScoreItemViewModel : ViewModelBase
{
    private string _scoreText;

    public ScoreItemViewModel(ApplicantSubjectScore model, bool applicantCanEdit)
    {
        SubjectId = model.SubjectId;
        SubjectName = model.SubjectName;
        MinScore = model.MinScore;
        IsConfirmed = model.IsConfirmed;
        SavedScore = model.Score;
        _scoreText = model.Score?.ToString() ?? string.Empty;
        CanEdit = applicantCanEdit && !model.IsConfirmed;
    }

    public int SubjectId { get; }
    public string SubjectName { get; }
    public int MinScore { get; }
    public bool IsConfirmed { get; }
    public int? SavedScore { get; }
    public bool CanEdit { get; }

    public string MinScoreText => $"мин. {MinScore}";

    public string ScoreText
    {
        get => _scoreText;
        set => SetProperty(ref _scoreText, value);
    }

    public string StateText =>
        IsConfirmed ? "✔ Зарегистрирован в ведомости"
        : SavedScore == null ? "Не введён"
        : "Ожидает проверки администратором";

    public bool IsBelowMin => SavedScore.HasValue && SavedScore.Value < MinScore;
}
