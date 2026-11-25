using System.Collections.ObjectModel;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Справочник факультетов: название, план набора, вступительные испытания.</summary>
public class FacultiesTabViewModel : TabViewModelBase
{
    private List<(int FacultyId, int SubjectId)> _links = new();
    private List<Subject> _subjects = new();
    private Faculty? _selected;
    private int _editingId;
    private string _editName = string.Empty;
    private string _editPlanText = string.Empty;
    private string _editDescription = string.Empty;

    public FacultiesTabViewModel(AppServices services) : base(services, "Факультеты")
    {
        NewCommand = new RelayCommand(StartNew);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected != null);
    }

    public ObservableCollection<Faculty> Faculties { get; } = new();
    public ObservableCollection<SubjectCheckItem> SubjectChecks { get; } = new();

    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public Faculty? Selected
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;
            if (value != null)
                FillEditor(value);
        }
    }

    public string EditorTitle => _editingId == 0 ? "Новый факультет" : "Изменение факультета";

    public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
    public string EditPlanText { get => _editPlanText; set => SetProperty(ref _editPlanText, value); }
    public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }

    protected override async Task LoadAsync()
    {
        var keepId = _editingId;

        _subjects = await Services.Subjects.GetAllAsync();
        _links = await Services.Faculties.GetLinksAsync();
        var faculties = await Services.Faculties.GetAllAsync();

        Faculties.Clear();
        foreach (var f in faculties)
            Faculties.Add(f);

        var again = Faculties.FirstOrDefault(f => f.FacultyId == keepId);
        if (again != null)
        {
            _selected = again;
            OnPropertyChanged(nameof(Selected));
            FillEditor(again);
        }
        else
        {
            StartNew();
        }
    }

    private void FillEditor(Faculty f)
    {
        _editingId = f.FacultyId;
        EditName = f.Name;
        EditPlanText = f.PlanCount.ToString();
        EditDescription = f.Description ?? string.Empty;
        FillSubjects(_links.Where(l => l.FacultyId == f.FacultyId).Select(l => l.SubjectId).ToHashSet());
        OnPropertyChanged(nameof(EditorTitle));
    }

    private void StartNew()
    {
        _editingId = 0;
        _selected = null;
        OnPropertyChanged(nameof(Selected));
        EditName = string.Empty;
        EditPlanText = string.Empty;
        EditDescription = string.Empty;
        FillSubjects(new HashSet<int>());
        OnPropertyChanged(nameof(EditorTitle));
    }

    private void FillSubjects(HashSet<int> checkedIds)
    {
        SubjectChecks.Clear();
        foreach (var s in _subjects)
            SubjectChecks.Add(new SubjectCheckItem(s.SubjectId, $"{s.Name} (мин. {s.MinScore})", checkedIds.Contains(s.SubjectId)));
    }

    private async Task SaveAsync()
    {
        var name = EditName.Trim();
        if (name.Length == 0)
        {
            Services.Dialogs.ShowError("Введите название факультета.");
            return;
        }
        if (!int.TryParse(EditPlanText.Trim(), out var plan) || plan <= 0 || plan > 10000)
        {
            Services.Dialogs.ShowError("План набора — целое положительное число.");
            return;
        }
        var subjectIds = SubjectChecks.Where(s => s.IsChecked).Select(s => s.SubjectId).ToList();
        if (subjectIds.Count == 0)
        {
            Services.Dialogs.ShowError("Отметьте хотя бы один предмет (вступительное испытание).");
            return;
        }

        var faculty = new Faculty
        {
            FacultyId = _editingId,
            Name = name,
            PlanCount = plan,
            Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim()
        };

        _editingId = await Services.Faculties.SaveAsync(faculty, subjectIds);
        await LoadAsync();
        Services.Dialogs.ShowInfo("Факультет сохранён.");
    }

    private async Task DeleteAsync()
    {
        if (Selected == null) return;
        if (!Services.Dialogs.Confirm($"Удалить факультет «{Selected.Name}»?"))
            return;

        await Services.Faculties.DeleteAsync(Selected.FacultyId);
        _editingId = 0;
        await LoadAsync();
    }
}
