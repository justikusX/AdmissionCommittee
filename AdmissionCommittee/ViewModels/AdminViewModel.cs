using System.Collections.ObjectModel;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Рабочее место администратора приёмной комиссии (набор вкладок).</summary>
public class AdminViewModel : ViewModelBase
{
    private TabViewModelBase? _selectedTab;

    public AdminViewModel(AppServices services)
    {
        Tabs = new ObservableCollection<TabViewModelBase>
        {
            new StatementTabViewModel(services),
            new RatingTabViewModel(services),
            new ApplicantsTabViewModel(services),
            new FacultiesTabViewModel(services),
            new SubjectsTabViewModel(services),
            new AuditTabViewModel(services)
        };
        SelectedTab = Tabs[0];
    }

    public ObservableCollection<TabViewModelBase> Tabs { get; }

    /// <summary>При переходе на вкладку её данные перечитываются из базы.</summary>
    public TabViewModelBase? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value) && value != null)
                value.RefreshCommand.Execute(null);
        }
    }
}
