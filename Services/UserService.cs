using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public interface IUserService
{
    Task<User> RegisterAsync(string username, string email, string password);
    Task<User> LoginAsync(string username, string password);
    Task<User> GetUserByIdAsync(string userId);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId);
}

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Username, email, and password are required.");
        }

        if (_users.Values.Any(u => u.Username == username || u.Email == email))
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
        return user;
    }

    public async Task<User> LoginAsync(string username, string password)
    {
        var user = _users.Values.FirstOrDefault(u => u.Username == username && u.IsActive);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
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
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        return _users.TryRemove(userId, out _);
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