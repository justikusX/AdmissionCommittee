using System.Collections.ObjectModel;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>
/// Ведомость: администратор проверяет баллы, введённые абитуриентами,
/// и регистрирует их (при необходимости исправив).
/// </summary>
public class StatementTabViewModel : TabViewModelBase
{
    private StatementRow? _selectedRow;
    private string _editScoreText = string.Empty;
    private bool _onlyUnconfirmed = true;
    private string _summary = string.Empty;

    public StatementTabViewModel(AppServices services) : base(services, "Ведомость")
    {
        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => SelectedRow != null);
        UnconfirmCommand = new AsyncRelayCommand(UnconfirmAsync, () => SelectedRow is { IsConfirmed: true });
        ConfirmAllCommand = new AsyncRelayCommand(ConfirmAllAsync, () => Rows.Any(r => !r.IsConfirmed));
    }

    public ObservableCollection<StatementRow> Rows { get; } = new();

    public ICommand ConfirmCommand { get; }
    public ICommand UnconfirmCommand { get; }
    public ICommand ConfirmAllCommand { get; }

    public bool OnlyUnconfirmed
    {
        get => _onlyUnconfirmed;
        set
        {
            if (SetProperty(ref _onlyUnconfirmed, value))
                RefreshCommand.Execute(null);
        }
    }

    public StatementRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
                EditScoreText = value?.Score.ToString() ?? string.Empty;
        }
    }

    public string EditScoreText
    {
        get => _editScoreText;
        set => SetProperty(ref _editScoreText, value);
    }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    protected override async Task LoadAsync()
    {
        await ReloadFacultyFilterAsync();
        var keepId = SelectedRow?.EntryId;

        var rows = await Services.Statement.GetStatementAsync(SelectedFacultyId, OnlyUnconfirmed);
        Rows.Clear();
        foreach (var r in rows)
            Rows.Add(r);

        SelectedRow = Rows.FirstOrDefault(r => r.EntryId == keepId);
        Summary = $"Записей: {Rows.Count}, ожидают проверки: {Rows.Count(r => !r.IsConfirmed)}";
    }

    private async Task ConfirmAsync()
    {
        if (SelectedRow == null) return;

        if (!int.TryParse(EditScoreText.Trim(), out var score) || score < 0 || score > 100)
        {
            Services.Dialogs.ShowError("Балл должен быть целым числом от 0 до 100.");
            return;
        }

        if (SelectedRow.IsConfirmed && score != SelectedRow.Score &&
            !Services.Dialogs.Confirm("Балл уже зарегистрирован. Изменить зарегистрированный балл?\n" +
                                      "Изменение будет записано в журнал."))
            return;

        await Services.Statement.ConfirmAsync(SelectedRow.EntryId, score, Services.CurrentUser!.UserId);
        await LoadAsync();
    }

    private async Task UnconfirmAsync()
    {
        if (SelectedRow == null) return;
        if (!Services.Dialogs.Confirm("Снять регистрацию балла? Абитуриент снова сможет его изменить."))
            return;

        await Services.Statement.UnconfirmAsync(SelectedRow.EntryId);
        await LoadAsync();
    }

    private async Task ConfirmAllAsync()
    {
        var ids = Rows.Where(r => !r.IsConfirmed).Select(r => r.EntryId).ToList();
        if (!Services.Dialogs.Confirm($"Зарегистрировать в ведомости все показанные непроверенные баллы ({ids.Count})?"))
            return;

        await Services.Statement.ConfirmManyAsync(ids, Services.CurrentUser!.UserId);
        await LoadAsync();
    }
}
