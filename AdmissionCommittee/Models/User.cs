namespace AdmissionCommittee.Models;

/// <summary>Названия ролей — как в таблице roles.</summary>
public static class RoleNames
{
    public const string Admin = "Администратор";
    public const string Applicant = "Абитуриент";
}

/// <summary>Учётная запись пользователя (таблица users).</summary>
public class User
{
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public bool IsAdmin => RoleName == RoleNames.Admin;
}
