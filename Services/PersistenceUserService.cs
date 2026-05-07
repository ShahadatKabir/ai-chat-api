using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class PersistenceUserService : IUserService
{
    private readonly IDataPersistenceService _persistence;
    private readonly ConcurrentDictionary<string, User> _users = new();
    private const string USERS_CACHE_KEY = "users";

    public PersistenceUserService(IDataPersistenceService persistence)
    {
        _persistence = persistence;
        _ = LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        var users = await _persistence.LoadAsync<List<User>>(USERS_CACHE_KEY);
        if (users != null)
        {
            foreach (var user in users)
            {
                _users.TryAdd(user.Id, user);
            }
        }
    }

    private async Task SaveUsersAsync()
    {
        await _persistence.SaveAsync(USERS_CACHE_KEY, _users.Values.ToList());
    }

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Username, email, and password are required.");
        }

        if (_users.Values.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) || 
                                    u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Username or email already exists.");
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = HashPassword(password)
        };

        _users.TryAdd(user.Id, user);
        await SaveUsersAsync();
        await _persistence.AppendAuditLogAsync("USER_REGISTER", user.Id, $"Username: {username}");

        return user;
    }

    public async Task<User> LoginAsync(string username, string password)
    {
        var user = _users.Values.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.IsActive);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            await _persistence.AppendAuditLogAsync("USER_LOGIN_FAILED", null, $"Username attempted: {username}");
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _persistence.SaveAsync(USERS_CACHE_KEY, _users.Values.ToList());
        await _persistence.AppendAuditLogAsync("USER_LOGIN", user.Id, $"Username: {username}");

        return user;
    }

    public async Task<User> GetUserByIdAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return user;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        if (_users.ContainsKey(user.Id))
        {
            _users[user.Id] = user;
            await SaveUsersAsync();
            await _persistence.AppendAuditLogAsync("USER_UPDATE", user.Id, "Profile updated");
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        if (_users.TryRemove(userId, out _))
        {
            await SaveUsersAsync();
            await _persistence.AppendAuditLogAsync("USER_DELETE", userId, "Account deleted");
            return true;
        }
        return false;
    }

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        return _users.Values.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}