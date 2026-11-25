using System.Collections.ObjectModel;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>
/// Личный кабинет абитуриента: ввод баллов по предметам факультета,
/// сумма баллов, место в рейтинге, статус зачисления.
/// </summary>
public class ApplicantCabinetViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private Applicant? _applicant;
    private RatingRow? _rating;
    private Faculty? _newFaculty;

    public ApplicantCabinetViewModel(AppServices services)
    {
        _services = services;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveScoresCommand = new AsyncRelayCommand(SaveScoresAsync, () => CanEdit && Scores.Any(s => s.CanEdit));
        ChangeFacultyCommand = new AsyncRelayCommand(ChangeFacultyAsync,
            () => CanEdit && NewFaculty != null && NewFaculty.FacultyId != _applicant?.FacultyId);
    }

    public ObservableCollection<ScoreItemViewModel> Scores { get; } = new();
    public ObservableCollection<Faculty> Faculties { get; } = new();

    public ICommand LoadCommand { get; }
    public ICommand SaveScoresCommand { get; }
    public ICommand ChangeFacultyCommand { get; }

    public Faculty? NewFaculty
    {
        get => _newFaculty;
        set => SetProperty(ref _newFaculty, value);
    }

    /// <summary>Пока итоги не подведены, абитуриент может вносить баллы и менять факультет.</summary>
    public bool CanEdit => _applicant?.Status == ApplicantStatuses.Submitted;

    public string FullName => _applicant?.FullName ?? string.Empty;
    public string FacultyName => _applicant?.FacultyName ?? string.Empty;
    public string Status => _applicant?.Status ?? string.Empty;
    public string CertificateText => _applicant == null ? string.Empty : _applicant.CertificateAvg.ToString("0.00");
    public string ContactsText => _applicant == null ? string.Empty
        : $"Дата рождения: {_applicant.BirthDate:dd.MM.yyyy}   Телефон: {_applicant.Phone ?? "—"}   E-mail: {_applicant.Email ?? "—"}";

    public string TotalText => _rating == null ? "—"
        : $"{_rating.TotalScore:0.#}";

    public string TotalDetailsText => _rating == null ? string.Empty
        : $"экзамены {_rating.ExamSum} + аттестат {_rating.CertificatePoints:0.#} ({_rating.CertificateAvg:0.00} × 10)";

    public string RatingText
    {
        get
        {
            if (_rating == null) return string.Empty;
            if (!_rating.IsEligible)
                return $"Вы пока не участвуете в конкурсе: зарегистрировано {_rating.PassedCount} из {_rating.RequiredCount} " +
                       "предметов с баллом не ниже минимального.";
            return $"Место в рейтинге: {_rating.RankPosition} при плане набора {_rating.PlanCount}. " +
                   (_rating.Passes ? "Вы проходите в план набора." : "Пока вы вне плана набора.");
        }
    }

    public bool IsEnrolled => _applicant?.Status == ApplicantStatuses.Enrolled;
    public bool IsRejected => _applicant?.Status == ApplicantStatuses.Rejected;

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var userId = _services.CurrentUser!.UserId;
            _applicant = await _services.Applicants.GetByUserIdAsync(userId);
            if (_applicant == null)
            {
                _services.Dialogs.ShowError("Заявление не найдено. Возможно, оно было удалено администратором.");
                return;
            }

            _rating = await _services.Rating.GetForApplicantAsync(_applicant.ApplicantId);

            var scores = await _services.Statement.GetApplicantScoresAsync(_applicant.ApplicantId);
            Scores.Clear();
            foreach (var s in scores)
                Scores.Add(new ScoreItemViewModel(s, CanEdit));

            var faculties = await _services.Faculties.GetAllAsync();
            Faculties.Clear();
            foreach (var f in faculties)
                Faculties.Add(f);
            NewFaculty = Faculties.FirstOrDefault(f => f.FacultyId == _applicant.FacultyId);

            // обновить на экране все вычисляемые свойства
            OnPropertyChanged(string.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveScoresAsync()
    {
        if (_applicant == null) return;

        var toSave = new List<(int SubjectId, int Score)>();
        foreach (var item in Scores.Where(s => s.CanEdit))
        {
            var text = item.ScoreText.Trim();
            if (text.Length == 0)
                continue;

            if (!int.TryParse(text, out var score) || score < 0 || score > 100)
            {
                _services.Dialogs.ShowError($"«{item.SubjectName}»: балл должен быть целым числом от 0 до 100.");
                return;
            }
            toSave.Add((item.SubjectId, score));
        }

        if (toSave.Count == 0)
        {
            _services.Dialogs.ShowError("Введите хотя бы один балл.");
            return;
        }

        foreach (var (subjectId, score) in toSave)
            await _services.Statement.SaveApplicantScoreAsync(_applicant.ApplicantId, subjectId, score);

        await LoadAsync();
        _services.Dialogs.ShowInfo("Баллы сохранены. Они будут учтены в сумме после регистрации администратором в ведомости.");
    }

    private async Task ChangeFacultyAsync()
    {
        if (_applicant == null || NewFaculty == null) return;

        if (!_services.Dialogs.Confirm($"Перевести заявление на факультет «{NewFaculty.Name}»?\n" +
                                       "Баллы по предметам, которых нет на новом факультете, учитываться не будут."))
            return;

        var ok = await _services.Applicants.ChangeFacultyAsync(_applicant.ApplicantId, NewFaculty.FacultyId);
        if (!ok)
            _services.Dialogs.ShowError("Сменить факультет нельзя: итоги приёма уже подведены.");

        await LoadAsync();
    }
}
