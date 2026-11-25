using System.Collections.ObjectModel;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>
/// Базовый класс вкладки администратора: заголовок, команда «Обновить»
/// и (при необходимости) фильтр по факультету.
/// </summary>
public abstract class TabViewModelBase : ViewModelBase
{
    private Faculty? _selectedFacultyFilter;
    private bool _suppressFilterReload;

    protected TabViewModelBase(AppServices services, string title)
    {
        Services = services;
        Title = title;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    protected AppServices Services { get; }

    public string Title { get; }

    public ICommand RefreshCommand { get; }

    protected abstract Task LoadAsync();

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------- Фильтр по факультету (используют «Ведомость» и «Рейтинг») ----------

    public ObservableCollection<Faculty> FacultyFilter { get; } = new();

    public Faculty? SelectedFacultyFilter
    {
        get => _selectedFacultyFilter;
        set
        {
            if (SetProperty(ref _selectedFacultyFilter, value) && !_suppressFilterReload && value != null)
                RefreshCommand.Execute(null);
        }
    }

    /// <summary>null — все факультеты.</summary>
    protected int? SelectedFacultyId =>
        SelectedFacultyFilter is { FacultyId: > 0 } f ? f.FacultyId : null;

    /// <summary>Перечитывает список факультетов для фильтра, сохраняя выбранный.</summary>
    protected async Task ReloadFacultyFilterAsync()
    {
        var keepId = SelectedFacultyFilter?.FacultyId ?? 0;
        var faculties = await Services.Faculties.GetAllAsync();

        _suppressFilterReload = true;
        try
        {
            FacultyFilter.Clear();
            FacultyFilter.Add(new Faculty { FacultyId = 0, Name = "Все факультеты" });
            foreach (var f in faculties)
                FacultyFilter.Add(f);

            SelectedFacultyFilter = FacultyFilter.FirstOrDefault(f => f.FacultyId == keepId) ?? FacultyFilter[0];
        }
        finally
        {
            _suppressFilterReload = false;
        }
    }
}
