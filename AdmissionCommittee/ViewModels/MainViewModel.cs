using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>
/// ViewModel главного окна. Отвечает за навигацию:
/// какой экран (ViewModel) сейчас показан в окне.
/// Нужное представление (View) WPF подбирает сам по DataTemplate из App.xaml.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private ViewModelBase? _currentView;

    public MainViewModel(AppServices services)
    {
        _services = services;
        LogoutCommand = new RelayCommand(Logout, () => IsLoggedIn);
        ShowLogin();
    }

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public bool IsLoggedIn => _services.CurrentUser != null;

    public string UserCaption => _services.CurrentUser is { } u ? $"{u.FullName} · {u.RoleName}" : string.Empty;

    public ICommand LogoutCommand { get; }

    public void ShowLogin() => CurrentView = new LoginViewModel(_services, this);

    public void ShowRegister()
    {
        var vm = new RegisterViewModel(_services, this);
        CurrentView = vm;
        vm.LoadCommand.Execute(null);
    }

    /// <summary>После входа показываем экран в зависимости от роли.</summary>
    public void OnLoggedIn(User user)
    {
        _services.CurrentUser = user;
        RefreshUserInfo();

        if (user.IsAdmin)
        {
            CurrentView = new AdminViewModel(_services);
        }
        else
        {
            var cabinet = new ApplicantCabinetViewModel(_services);
            CurrentView = cabinet;
            cabinet.LoadCommand.Execute(null);
        }
    }

    private void Logout()
    {
        _services.CurrentUser = null;
        RefreshUserInfo();
        ShowLogin();
    }

    private void RefreshUserInfo()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(UserCaption));
    }
}
