using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Models;
using AdmissionCommittee.Services;

namespace AdmissionCommittee.ViewModels;

/// <summary>Регистрация абитуриента на один из факультетов.</summary>
public class RegisterViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly MainViewModel _main;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _passwordConfirm = string.Empty;
    private string _fullName = string.Empty;
    private DateTime? _birthDate = new DateTime(2008, 1, 1);
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _certificateText = string.Empty;
    private Faculty? _selectedFaculty;
    private string _errorMessage = string.Empty;

    public RegisterViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        BackCommand = new RelayCommand(_main.ShowLogin);
    }

    public ObservableCollection<Faculty> Faculties { get; } = new();

    public string Login { get => _login; set => SetProperty(ref _login, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string PasswordConfirm { get => _passwordConfirm; set => SetProperty(ref _passwordConfirm, value); }
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    public DateTime? BirthDate { get => _birthDate; set => SetProperty(ref _birthDate, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    /// <summary>Средний балл аттестата (вводится строкой, допускается и точка, и запятая).</summary>
    public string CertificateText { get => _certificateText; set => SetProperty(ref _certificateText, value); }

    public Faculty? SelectedFaculty
    {
        get => _selectedFaculty;
        set
        {
            if (SetProperty(ref _selectedFaculty, value))
                OnPropertyChanged(nameof(FacultyInfo));
        }
    }

    public string FacultyInfo => SelectedFaculty == null
        ? string.Empty
        : $"План набора: {SelectedFaculty.PlanCount} чел.\nВступительные испытания: {SelectedFaculty.SubjectsText}";

    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand LoadCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand BackCommand { get; }

    private async Task LoadAsync()
    {
        var list = await _services.Faculties.GetAllAsync();
        Faculties.Clear();
        foreach (var f in list)
            Faculties.Add(f);
    }

    private async Task RegisterAsync()
    {
        ErrorMessage = Validate(out decimal certificate) ?? string.Empty;
        if (ErrorMessage.Length > 0)
            return;

        IsBusy = true;
        try
        {
            var login = Login.Trim();
            if (await _services.Users.LoginExistsAsync(login))
            {
                ErrorMessage = "Такой логин уже занят.";
                return;
            }

            var password = Password;
            var hash = await Task.Run(() => PasswordHasher.Hash(password));

            var applicant = new Applicant
            {
                FullName = NormalizeSpaces(FullName),
                BirthDate = BirthDate!.Value.Date,
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                FacultyId = SelectedFaculty!.FacultyId,
                CertificateAvg = certificate
            };

            await _services.Applicants.RegisterAsync(login, hash, applicant);

            _services.Dialogs.ShowInfo("Регистрация прошла успешно!\nТеперь введите баллы по вступительным испытаниям.");

            var user = await _services.Users.GetByLoginAsync(login);
            if (user != null)
                _main.OnLoggedIn(user);
            else
                _main.ShowLogin();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Проверка ввода. Возвращает текст ошибки или null, если всё верно.</summary>
    private string? Validate(out decimal certificate)
    {
        certificate = 0;

        if (!Regex.IsMatch(Login.Trim(), @"^[A-Za-z0-9_.]{3,50}$"))
            return "Логин: 3–50 символов, латинские буквы, цифры, точка или подчёркивание.";

        if (Password.Length < 6 || !Password.Any(char.IsLetter) || !Password.Any(char.IsDigit))
            return "Пароль должен быть не короче 6 символов и содержать буквы и цифры.";

        if (Password != PasswordConfirm)
            return "Пароли не совпадают.";

        var name = NormalizeSpaces(FullName);
        if (name.Split(' ').Length < 2 || name.Length > 150)
            return "Укажите фамилию и имя (ФИО полностью).";

        if (BirthDate == null)
            return "Укажите дату рождения.";

        var age = DateTime.Today.Year - BirthDate.Value.Year;
        if (BirthDate.Value.Date > DateTime.Today.AddYears(-age)) age--;
        if (age < 14 || age > 70)
            return "Проверьте дату рождения (возраст абитуриента 14–70 лет).";

        if (!string.IsNullOrWhiteSpace(Phone) && !Regex.IsMatch(Phone.Trim(), @"^\+?[0-9\-\s\(\)]{6,20}$"))
            return "Телефон указан неверно.";

        if (!string.IsNullOrWhiteSpace(Email) && !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return "E-mail указан неверно.";

        if (SelectedFaculty == null)
            return "Выберите факультет.";

        if (!TryParseCertificate(CertificateText, out certificate))
            return "Средний балл аттестата — число от 3,00 до 5,00.";

        return null;
    }

    internal static bool TryParseCertificate(string text, out decimal value)
    {
        var normalized = (text ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               && value >= 3.00m && value <= 5.00m
               && decimal.Round(value, 2) == value;
    }

    internal static string NormalizeSpaces(string text)
        => Regex.Replace((text ?? string.Empty).Trim(), @"\s+", " ");
}
