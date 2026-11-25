using AdmissionCommittee.Data;
using AdmissionCommittee.Models;

namespace AdmissionCommittee.Services;

/// <summary>
/// Набор общих сервисов приложения. Создаётся один раз при запуске
/// и передаётся во ViewModel через конструктор.
/// </summary>
public class AppServices
{
    public AppServices(string connectionString, IDialogService dialogs)
    {
        Db = new Database(connectionString);
        Dialogs = dialogs;
        Users = new UserRepository(Db);
        Applicants = new ApplicantRepository(Db);
        Faculties = new FacultyRepository(Db);
        Subjects = new SubjectRepository(Db);
        Statement = new StatementRepository(Db);
        Rating = new RatingRepository(Db);
        Audit = new AuditRepository(Db);
    }

    public Database Db { get; }
    public IDialogService Dialogs { get; }

    public UserRepository Users { get; }
    public ApplicantRepository Applicants { get; }
    public FacultyRepository Faculties { get; }
    public SubjectRepository Subjects { get; }
    public StatementRepository Statement { get; }
    public RatingRepository Rating { get; }
    public AuditRepository Audit { get; }

    private User? _currentUser;

    /// <summary>Пользователь, вошедший в систему.</summary>
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            Db.CurrentLogin = value?.Login;
        }
    }
}
