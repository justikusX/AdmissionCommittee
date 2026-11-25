using System.Collections.ObjectModel;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Справочник предметов (вступительных испытаний) и минимальных баллов.</summary>
public class SubjectsTabViewModel : TabViewModelBase
{
    private Subject? _selected;
    private int _editingId;
    private string _editName = string.Empty;
    private string _editMinText = string.Empty;

    public SubjectsTabViewModel(AppServices services) : base(services, "Предметы")
    {
        NewCommand = new RelayCommand(StartNew);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected != null);
    }

    public ObservableCollection<Subject> Subjects { get; } = new();

    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public Subject? Selected
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value) || value == null) return;
            _editingId = value.SubjectId;
            EditName = value.Name;
            EditMinText = value.MinScore.ToString();
            OnPropertyChanged(nameof(EditorTitle));
        }
    }

    public string EditorTitle => _editingId == 0 ? "Новый предмет" : "Изменение предмета";
    public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
    public string EditMinText { get => _editMinText; set => SetProperty(ref _editMinText, value); }

    protected override async Task LoadAsync()
    {
        var keepId = _editingId;
        var list = await Services.Subjects.GetAllAsync();
        Subjects.Clear();
        foreach (var s in list)
            Subjects.Add(s);

        var again = Subjects.FirstOrDefault(s => s.SubjectId == keepId);
        if (again != null)
            Selected = again;
        else
            StartNew();
    }

    private void StartNew()
    {
        _editingId = 0;
        _selected = null;
        OnPropertyChanged(nameof(Selected));
        EditName = string.Empty;
        EditMinText = "0";
        OnPropertyChanged(nameof(EditorTitle));
    }

    private async Task SaveAsync()
    {
        var name = EditName.Trim();
        if (name.Length == 0)
        {
            Services.Dialogs.ShowError("Введите название предмета.");
            return;
        }
        if (!int.TryParse(EditMinText.Trim(), out var min) || min < 0 || min > 100)
        {
            Services.Dialogs.ShowError("Минимальный балл — целое число от 0 до 100.");
            return;
        }

        _editingId = await Services.Subjects.SaveAsync(new Subject { SubjectId = _editingId, Name = name, MinScore = min });
        _selected = null; // чтобы Selected заново заполнил редактор после перезагрузки
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (Selected == null) return;
        if (!Services.Dialogs.Confirm($"Удалить предмет «{Selected.Name}»?"))
            return;

        await Services.Subjects.DeleteAsync(Selected.SubjectId);
        _editingId = 0;
        await LoadAsync();
    }
}
