using System.Collections.ObjectModel;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>
/// Конкурсный рейтинг: система считает сумму баллов и определяет,
/// кто проходит в план набора. Администратор подводит итоги (зачисление).
/// </summary>
public class RatingTabViewModel : TabViewModelBase
{
    private string _summary = string.Empty;

    public RatingTabViewModel(AppServices services) : base(services, "Рейтинг и зачисление")
    {
        RunEnrollmentCommand = new AsyncRelayCommand(RunEnrollmentAsync);
        ResetEnrollmentCommand = new AsyncRelayCommand(ResetEnrollmentAsync);
    }

    public ObservableCollection<RatingRow> Rows { get; } = new();

    public ICommand RunEnrollmentCommand { get; }
    public ICommand ResetEnrollmentCommand { get; }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    protected override async Task LoadAsync()
    {
        await ReloadFacultyFilterAsync();

        var rows = await Services.Rating.GetRatingAsync(SelectedFacultyId);
        Rows.Clear();
        foreach (var r in rows)
            Rows.Add(r);

        Summary = string.Join(Environment.NewLine, rows
            .GroupBy(r => new { r.FacultyName, r.PlanCount })
            .Select(g => $"{g.Key.FacultyName}: план {g.Key.PlanCount}, заявлений {g.Count()}, " +
                         $"допущено к конкурсу {g.Count(r => r.IsEligible)}, проходят {g.Count(r => r.Passes)}, " +
                         $"зачислено {g.Count(r => r.Status == ApplicantStatuses.Enrolled)}"));
    }

    private async Task RunEnrollmentAsync()
    {
        if (!Services.Dialogs.Confirm(
                "Подвести итоги приёма?\n\nАбитуриенты, проходящие в план набора, получат статус «Зачислен», " +
                "остальные — «Не зачислен». После этого абитуриенты не смогут менять баллы и факультет."))
            return;

        var enrolled = await Services.Rating.RunEnrollmentAsync();
        await LoadAsync();
        Services.Dialogs.ShowInfo($"Итоги подведены. Зачислено абитуриентов: {enrolled}.");
    }

    private async Task ResetEnrollmentAsync()
    {
        if (!Services.Dialogs.Confirm("Отменить итоги приёма? Все заявления вернутся в статус «Подано»."))
            return;

        await Services.Rating.ResetEnrollmentAsync();
        await LoadAsync();
    }
}
