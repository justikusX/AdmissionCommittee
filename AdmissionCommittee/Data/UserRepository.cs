using AdmissionCommittee.Models;

namespace AdmissionCommittee.Data;

public class UserRepository
{
    private readonly Database _db;

    public UserRepository(Database db) => _db = db;

    public async Task<User?> GetByLoginAsync(string login)
    {
        const string sql = @"
            SELECT u.user_id, u.login, u.password_hash, u.full_name, u.is_active, r.role_name
            FROM users u
            JOIN roles r ON r.role_id = u.role_id
            WHERE u.login = @login";

        var users = await _db.QueryAsync(sql, r => new User
        {
            UserId = r.GetInt("user_id"),
            Login = r.GetStr("login"),
            PasswordHash = r.GetStr("password_hash"),
            FullName = r.GetStr("full_name"),
            IsActive = r.GetBool("is_active"),
            RoleName = r.GetStr("role_name")
        }, ("login", login));

        return users.FirstOrDefault();
    }

    public async Task<bool> LoginExistsAsync(string login)
    {
        var count = await _db.ScalarAsync("SELECT COUNT(*) FROM users WHERE login = @login", ("login", login));
        return Convert.ToInt64(count) > 0;
    }
}
