using AdmissionCommittee.Infrastructure;

namespace AdmissionCommittee.ViewModels;

/// <summary>Предмет с галочкой — для выбора предметов факультета.</summary>
public class SubjectCheckItem : ViewModelBase
{
    private bool _isChecked;

    public SubjectCheckItem(int subjectId, string name, bool isChecked)
    {
        SubjectId = subjectId;
        Name = name;
        _isChecked = isChecked;
    }

    public int SubjectId { get; }
    public string Name { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
}
