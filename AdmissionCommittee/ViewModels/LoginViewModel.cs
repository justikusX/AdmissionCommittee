using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Экран входа: аутентификация по логину и хэшу пароля.</summary>
public class LoginViewModel : ViewModelBase
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockTime = TimeSpan.FromSeconds(30);

    private readonly AppServices _services;
    private readonly MainViewModel _main;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private int _failedAttempts;
    private DateTime _lockedUntil = DateTime.MinValue;

    public LoginViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        LoginCommand = new AsyncRelayCommand(LoginAsync,
            () => !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrEmpty(Password));
        GoToRegisterCommand = new RelayCommand(_main.ShowRegister);
    }

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand GoToRegisterCommand { get; }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        // Защита от подбора пароля: после 5 ошибок — пауза 30 секунд
        if (DateTime.Now < _lockedUntil)
        {
            var left = (int)Math.Ceiling((_lockedUntil - DateTime.Now).TotalSeconds);
            ErrorMessage = $"Слишком много неудачных попыток. Подождите {left} с.";
            return;
        }

        IsBusy = true;
        try
        {
            var user = await _services.Users.GetByLoginAsync(Login.Trim());
            var password = Password;
            bool ok = user != null && await Task.Run(() => PasswordHasher.Verify(password, user.PasswordHash));

            if (!ok || user == null)
            {
                _failedAttempts++;
                if (_failedAttempts >= MaxAttempts)
                {
                    _failedAttempts = 0;
                    _lockedUntil = DateTime.Now.Add(LockTime);
                }
                ErrorMessage = "Неверный логин или пароль.";
                Password = string.Empty;
                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage = "Учётная запись заблокирована. Обратитесь в приёмную комиссию.";
                return;
            }

            _failedAttempts = 0;
            _main.OnLoggedIn(user);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
