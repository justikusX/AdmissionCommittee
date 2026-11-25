using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdmissionCommittee.Infrastructure;

/// <summary>
/// Базовый класс для всех ViewModel: уведомляет представление об изменении свойств.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Записывает значение в поле и, если оно изменилось, уведомляет View.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private bool _isBusy;

    /// <summary>Идёт обращение к базе данных (можно показать индикатор).</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
}
