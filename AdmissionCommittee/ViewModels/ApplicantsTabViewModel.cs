using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Список абитуриентов: поиск, исправление данных, удаление заявления.</summary>
public class ApplicantsTabViewModel : TabViewModelBase
{
    private List<Applicant> _all = new();
    private Applicant? _selected;
    private string _searchText = string.Empty;

    private string _editFullName = string.Empty;
    private DateTime? _editBirthDate;
    private string _editPhone = string.Empty;
    private string _editEmail = string.Empty;
    private string _editCertificateText = string.Empty;
    private Faculty? _editFaculty;

    public ApplicantsTabViewModel(AppServices services) : base(services, "Абитуриенты")
    {
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => Selected != null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected != null);
    }

    public ObservableCollection<Applicant> Applicants { get; } = new();
    public ObservableCollection<Faculty> Faculties { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public Applicant? Selected
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;

            EditFullName = value?.FullName ?? string.Empty;
            EditBirthDate = value?.BirthDate;
            EditPhone = value?.Phone ?? string.Empty;
            EditEmail = value?.Email ?? string.Empty;
            EditCertificateText = value?.CertificateAvg.ToString("0.00") ?? string.Empty;
            EditFaculty = value == null ? null : Faculties.FirstOrDefault(f => f.FacultyId == value.FacultyId);
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => Selected != null;

    public string EditFullName { get => _editFullName; set => SetProperty(ref _editFullName, value); }
    public DateTime? EditBirthDate { get => _editBirthDate; set => SetProperty(ref _editBirthDate, value); }
    public string EditPhone { get => _editPhone; set => SetProperty(ref _editPhone, value); }
    public string EditEmail { get => _editEmail; set => SetProperty(ref _editEmail, value); }
    public string EditCertificateText { get => _editCertificateText; set => SetProperty(ref _editCertificateText, value); }
    public Faculty? EditFaculty { get => _editFaculty; set => SetProperty(ref _editFaculty, value); }

    protected override async Task LoadAsync()
    {
        var keepId = Selected?.ApplicantId;

        var faculties = await Services.Faculties.GetAllAsync();
        Faculties.Clear();
        foreach (var f in faculties)
            Faculties.Add(f);

        _all = await Services.Applicants.GetAllAsync();
        ApplyFilter();
        Selected = Applicants.FirstOrDefault(a => a.ApplicantId == keepId);
    }

    private void ApplyFilter()
    {
        var text = SearchText.Trim();
        Applicants.Clear();
        foreach (var a in _all.Where(a => text.Length == 0
                                          || a.FullName.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                                          || a.Login.Contains(text, StringComparison.CurrentCultureIgnoreCase)))
            Applicants.Add(a);
    }

    private async Task SaveAsync()
    {
        if (Selected == null) return;

        var name = RegisterViewModel.NormalizeSpaces(EditFullName);
        if (name.Split(' ').Length < 2)
        {
            Services.Dialogs.ShowError("Укажите ФИО полностью.");
            return;
        }
        if (EditBirthDate == null)
        {
            Services.Dialogs.ShowError("Укажите дату рождения.");
            return;
        }
        if (EditFaculty == null)
        {
            Services.Dialogs.ShowError("Выберите факультет.");
            return;
        }
        if (!RegisterViewModel.TryParseCertificate(EditCertificateText, out var certificate))
        {
            Services.Dialogs.ShowError("Средний балл аттестата — число от 3,00 до 5,00.");
            return;
        }
        if (!string.IsNullOrWhiteSpace(EditEmail) && !Regex.IsMatch(EditEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            Services.Dialogs.ShowError("E-mail указан неверно.");
            return;
        }

        var updated = new Applicant
        {
            ApplicantId = Selected.ApplicantId,
            FullName = name,
            BirthDate = EditBirthDate.Value.Date,
            Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim(),
            Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim(),
            FacultyId = EditFaculty.FacultyId,
            CertificateAvg = certificate
        };

        await Services.Applicants.UpdateAsync(updated);
        await LoadAsync();
        Services.Dialogs.ShowInfo("Данные абитуриента сохранены.");
    }

    private async Task DeleteAsync()
    {
        if (Selected == null) return;
        if (!Services.Dialogs.Confirm($"Удалить заявление абитуриента «{Selected.FullName}» вместе с его баллами?\n" +
                                      "Учётная запись будет заблокирована."))
            return;

        await Services.Applicants.DeleteAsync(Selected);
        Selected = null;
        await LoadAsync();
    }
}
