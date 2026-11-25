using System.Collections.ObjectModel;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Журнал действий (последние 300 записей). Только просмотр.</summary>
public class AuditTabViewModel : TabViewModelBase
{
    public AuditTabViewModel(AppServices services) : base(services, "Журнал")
    {
    }

    public ObservableCollection<AuditEntry> Entries { get; } = new();

    protected override async Task LoadAsync()
    {
        var list = await Services.Audit.GetLastAsync(300);
        Entries.Clear();
        foreach (var e in list)
            Entries.Add(e);
    }
}
